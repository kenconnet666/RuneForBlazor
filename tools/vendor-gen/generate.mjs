#!/usr/bin/env node
// 把浏览器端用到的第三方脚本放进 Rune.UI 的 wwwroot/vendor（JS 初始化器按需 import，随 Rune.UI 包分发，不依赖 CDN）。
//
// 用法：node tools/vendor-gen/generate.mjs [--check]
//   --check   只比较不写入；任一生成文件与现有内容不一致时以非零状态退出（CI 用）
//
// 现在只有 Floating UI（浮层定位，与 vue-tsx-pro 用同一个库、同一个版本，位置算法一致）：
//   wwwroot/vendor/floating-ui.core.mjs   @floating-ui/core 的浏览器构建（已内联 @floating-ui/utils，没有 import）
//   wwwroot/vendor/floating-ui.dom.mjs    @floating-ui/dom 的浏览器构建，import 改指同目录的 core
//   wwwroot/vendor/LICENSE-floating-ui.txt
//
// 依赖：node 内置模块；下载用 npm pack 与 tar。来源与许可见 NOTICE.md。

import { execFileSync, execSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

/** Floating UI 的版本：与 vue-tsx-pro 的依赖一致；升级时改这里并重新生成 */
const FLOATING_UI_VERSION = '1.8.0';

const { values } = parseArgs({ options: { check: { type: 'boolean', default: false } } });
const vendorDir = resolve(import.meta.dirname, '../../src/Rune.UI/wwwroot/vendor');

const temp = mkdtempSync(join(tmpdir(), 'rune-vendor-'));
/** 下载并解压一个 npm 包，返回包目录 */
function unpack(name) {
  // Windows 上 npm 是 npm.cmd，要经 shell 执行；命令里只有固定的包名与版本号
  const file = execSync(`npm pack ${name}@${FLOATING_UI_VERSION} --silent`, { cwd: temp, encoding: 'utf8' }).trim().split('\n').pop();
  // 解压到相对目录：Windows 的 tar 把绝对路径里的 C: 当成远程主机
  const dir = name.split('/').pop();
  mkdirSync(join(temp, dir));
  execFileSync('tar', ['-xzf', file, '-C', dir], { cwd: temp, stdio: 'inherit' });
  return join(temp, dir, 'package');
}

let outputs;
try {
  const core = unpack('@floating-ui/core');
  const dom = unpack('@floating-ui/dom');
  const coreSource = readFileSync(join(core, 'dist/floating-ui.core.browser.min.mjs'), 'utf8');
  if (/\bfrom\s*"/.test(coreSource)) throw new Error('@floating-ui/core 的浏览器构建出现了 import，要改成打包');
  const domSource = readFileSync(join(dom, 'dist/floating-ui.dom.browser.min.mjs'), 'utf8');
  if (!domSource.includes('from"@floating-ui/core"')) throw new Error('@floating-ui/dom 的浏览器构建不再 import "@floating-ui/core"，检查生成方式');
  const banner = (name) => `// ${name}@${FLOATING_UI_VERSION}（MIT，见 LICENSE-floating-ui.txt）；由 tools/vendor-gen 生成，不要手改\n`;
  outputs = [
    [join(vendorDir, 'floating-ui.core.mjs'), banner('@floating-ui/core') + coreSource.trimEnd() + '\n'],
    [join(vendorDir, 'floating-ui.dom.mjs'), banner('@floating-ui/dom') + domSource.replace('from"@floating-ui/core"', 'from"./floating-ui.core.mjs"').trimEnd() + '\n'],
    [join(vendorDir, 'LICENSE-floating-ui.txt'), readFileSync(join(dom, 'LICENSE'), 'utf8')],
  ];
} finally {
  rmSync(temp, { recursive: true, force: true });
}

let stale = false;
for (const [path, content] of outputs) {
  if (values.check) {
    const current = existsSync(path) ? readFileSync(path, 'utf8').replaceAll('\r\n', '\n') : null;
    if (current !== content.replaceAll('\r\n', '\n')) {
      console.error(`已过期：${path}`);
      stale = true;
    }
  } else {
    writeFileSync(path, content);
    console.log(`已写入 ${path}`);
  }
}
if (stale) process.exit(1);
console.log(values.check ? '生成文件均为最新' : `Floating UI ${FLOATING_UI_VERSION}`);
