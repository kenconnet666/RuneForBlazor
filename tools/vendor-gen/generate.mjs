// 把浏览器端用到的第三方脚本放进 Rune.UI 的 wwwroot/vendor（JS 初始化器按需 import，随 Rune.UI 包分发，不依赖 CDN）。
//
// 用法：node tools/vendor-gen/generate.mjs [--check]
//   --check   只比较不写入；任一生成文件与现有内容不一致（或多出、缺少文件）时以非零状态退出（CI 用）
//
// 生成的内容：
//   wwwroot/vendor/floating-ui.core.mjs   @floating-ui/core 的浏览器构建（已内联 @floating-ui/utils，没有 import）
//   wwwroot/vendor/floating-ui.dom.mjs    @floating-ui/dom 的浏览器构建，import 改指同目录的 core
//   wwwroot/vendor/LICENSE-floating-ui.txt
//   wwwroot/vendor/shiki/                 Shiki（代码高亮）：shiki.mjs 是核心与 JavaScript 正则引擎，langs/ 下每种语言一个模块，
//                                         共用的语法在 chunks/ 里；用 tools/vendor-gen/shiki 的锁文件安装、esbuild 打包
//
// 依赖：node 内置模块；Floating UI 用 npm pack 与 tar 下载，Shiki 用 npm ci 装到临时目录。来源与许可见 NOTICE.md。

import { execFileSync, execSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { dirname, join, relative, resolve } from 'node:path';
import { parseArgs } from 'node:util';

/** Floating UI 的版本：与 vue-tsx-pro 的依赖一致；升级时改这里并重新生成 */
const FLOATING_UI_VERSION = '1.8.0';

/** Shiki 打包的语言（@shikijs/langs 里的名字）；别名在 js/highlight.js 里 */
const SHIKI_LANGUAGES = ['razor', 'csharp', 'html', 'css', 'javascript', 'typescript', 'json', 'xml', 'shellscript', 'powershell', 'diff', 'sql'];

const { values } = parseArgs({ options: { check: { type: 'boolean', default: false } } });
const vendorDir = resolve(import.meta.dirname, '../../src/Rune.UI/wwwroot/vendor');
const shikiDir = join(vendorDir, 'shiki');

const temp = mkdtempSync(join(tmpdir(), 'rune-vendor-'));

/** 下载并解压一个 npm 包，返回包目录 */
function unpack(name, version) {
  // Windows 上 npm 是 npm.cmd，要经 shell 执行；命令里只有固定的包名与版本号
  const file = execSync(`npm pack ${name}@${version} --silent`, { cwd: temp, encoding: 'utf8' }).trim().split('\n').pop();
  // 解压到相对目录：Windows 的 tar 把绝对路径里的 C: 当成远程主机
  const dir = name.split('/').pop();
  mkdirSync(join(temp, dir));
  execFileSync('tar', ['-xzf', file, '-C', dir], { cwd: temp, stdio: 'inherit' });
  return join(temp, dir, 'package');
}

function floatingUi() {
  const core = unpack('@floating-ui/core', FLOATING_UI_VERSION);
  const dom = unpack('@floating-ui/dom', FLOATING_UI_VERSION);
  const coreSource = readFileSync(join(core, 'dist/floating-ui.core.browser.min.mjs'), 'utf8');
  if (/\bfrom\s*"/.test(coreSource)) throw new Error('@floating-ui/core 的浏览器构建出现了 import，要改成打包');
  const domSource = readFileSync(join(dom, 'dist/floating-ui.dom.browser.min.mjs'), 'utf8');
  if (!domSource.includes('from"@floating-ui/core"')) throw new Error('@floating-ui/dom 的浏览器构建不再 import "@floating-ui/core"，检查生成方式');
  const banner = (name) => `// ${name}@${FLOATING_UI_VERSION}（MIT，见 LICENSE-floating-ui.txt）；由 tools/vendor-gen 生成，不要手改\n`;
  return [
    [join(vendorDir, 'floating-ui.core.mjs'), banner('@floating-ui/core') + coreSource.trimEnd() + '\n'],
    [join(vendorDir, 'floating-ui.dom.mjs'), banner('@floating-ui/dom') + domSource.replace('from"@floating-ui/core"', 'from"./floating-ui.core.mjs"').trimEnd() + '\n'],
    [join(vendorDir, 'LICENSE-floating-ui.txt'), readFileSync(join(dom, 'LICENSE'), 'utf8')],
  ];
}

async function shiki() {
  const work = join(temp, 'shiki');
  mkdirSync(work);
  for (const file of ['package.json', 'package-lock.json']) copyFileSync(join(import.meta.dirname, 'shiki', file), join(work, file));
  execSync('npm ci --no-audit --no-fund --loglevel=error', { cwd: work, stdio: 'inherit' });

  // 入口：核心只导出用到的三个函数；每种语言一个入口，esbuild 把共用的语法（html、css、javascript 被 razor 引用）拆进 chunks
  writeFileSync(join(work, 'shiki.js'),
    "export { createHighlighterCore, createCssVariablesTheme } from 'shiki/core';\n" +
    "export { createJavaScriptRegexEngine } from 'shiki/engine/javascript';\n");
  const entryPoints = [{ in: join(work, 'shiki.js'), out: 'shiki' }];
  for (const lang of SHIKI_LANGUAGES) {
    writeFileSync(join(work, `lang-${lang}.js`), `export { default } from '@shikijs/langs/${lang}';\n`);
    entryPoints.push({ in: join(work, `lang-${lang}.js`), out: `langs/${lang}` });
  }
  const esbuild = createRequire(join(work, 'package.json'))('esbuild');
  // esbuild 的服务进程用着临时目录里的可执行文件：打包完停掉，临时目录才删得掉
  const result = await esbuild.build({
    entryPoints,
    // 工作目录设成临时目录：块名的哈希按相对路径算，临时目录名每次不同，否则每次生成的文件名都变
    absWorkingDir: work,
    bundle: true,
    splitting: true,
    format: 'esm',
    minify: true,
    legalComments: 'none',
    outdir: join(work, 'out'),
    outExtension: { '.js': '.mjs' },
    chunkNames: 'chunks/[name]-[hash]',
    metafile: true,
    write: true,
    logLevel: 'warning',
  }).finally(() => esbuild.stop());

  // 打包进去的包：各自的许可全文汇总成一个文件
  const packages = new Set();
  for (const input of Object.keys(result.metafile.inputs)) {
    const match = /node_modules[\\/]((?:@[^\\/]+[\\/])?[^\\/]+)/.exec(input);
    if (match) packages.add(match[1].replace('\\', '/'));
  }
  const licenses = [...packages].sort().map((name) => {
    const dir = join(work, 'node_modules', name);
    const version = JSON.parse(readFileSync(join(dir, 'package.json'), 'utf8')).version;
    const file = readdirSync(dir).find((f) => /^licen[cs]e/i.test(f));
    if (!file) throw new Error(`${name} 没有许可文件`);
    return `==== ${name}@${version} ====\n\n${readFileSync(join(dir, file), 'utf8').trim()}\n`;
  });

  const banner = '// Shiki 4.5.0 打包（MIT，见 ../LICENSE-shiki.txt）；由 tools/vendor-gen 生成，不要手改\n';
  const outputs = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const path = join(dir, entry.name);
      if (entry.isDirectory()) walk(path);
      else outputs.push([join(shikiDir, relative(join(work, 'out'), path)), banner + readFileSync(path, 'utf8').trimEnd() + '\n']);
    }
  };
  walk(join(work, 'out'));
  outputs.push([join(vendorDir, 'LICENSE-shiki.txt'), licenses.join('\n')]);
  return outputs;
}

/** 目录下现有的全部文件 */
function filesUnder(dir) {
  if (!existsSync(dir)) return [];
  return readdirSync(dir, { withFileTypes: true, recursive: true })
    .filter((entry) => entry.isFile())
    .map((entry) => join(entry.parentPath ?? entry.path, entry.name));
}

let outputs;
try {
  outputs = [...floatingUi(), ...(await shiki())];
} finally {
  rmSync(temp, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
}

const normalize = (text) => text.replaceAll('\r\n', '\n');
const generated = new Set(outputs.map(([path]) => resolve(path)));
// Shiki 的块名带内容哈希：版本变了旧文件要删掉，--check 时多出的文件也算过期
const leftovers = filesUnder(shikiDir).map((path) => resolve(path)).filter((path) => !generated.has(path));

let stale = false;
for (const [path, content] of outputs) {
  if (values.check) {
    const current = existsSync(path) ? normalize(readFileSync(path, 'utf8')) : null;
    if (current !== normalize(content)) {
      console.error(`已过期：${path}`);
      stale = true;
    }
  } else {
    mkdirSync(dirname(path), { recursive: true });
    writeFileSync(path, content);
  }
}
for (const path of leftovers) {
  if (values.check) {
    console.error(`多余的文件：${path}`);
    stale = true;
  } else {
    rmSync(path);
  }
}
if (stale) process.exit(1);
console.log(values.check
  ? '生成文件均为最新'
  : `已写入 ${outputs.length} 个文件（Floating UI ${FLOATING_UI_VERSION}、Shiki，${SHIKI_LANGUAGES.length} 种语言）${leftovers.length ? `，删除 ${leftovers.length} 个旧文件` : ''}`);
