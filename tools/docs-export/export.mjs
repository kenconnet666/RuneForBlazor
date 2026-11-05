// 把文档站导出成静态文件（部署到 Cloudflare Pages 这类静态托管）：
//
//   node tools/docs-export/export.mjs                  发布 apps/Rune.Docs，预渲染全部页面，写到 artifacts/docs-site
//   node tools/docs-export/export.mjs --skip-publish   用已有的发布结果（artifacts/docs-publish）
//   node tools/docs-export/export.mjs --preview        导出后按 Cloudflare Pages 的规则在 http://localhost:5320 预览
//   node tools/docs-export/export.mjs --serve          不导出，只预览已有的导出结果
//
// 做法：
// 1. dotnet publish（Release）。整站是 WebAssembly 的交互根，服务端只做预渲染。
// 2. 静态资源照发布结果里的端点清单（Rune.Docs.staticwebassets.endpoints.json）复制：MapStaticAssets 用带指纹的地址
//    （app.abc123.css、import map 里的 JS），磁盘上只有原名，清单写着每个地址对应哪个文件。压缩版本（.br、.gz）不要：Cloudflare 自己压缩。
// 3. 启动发布好的服务端，从首页开始按站内链接与 iframe 的地址爬完全部页面，每页存成 路径.html（Cloudflare Pages 对 /components/button
//    直接取 components/button.html，不跳转），首页是 index.html，找不到页面存成 404.html。
import { spawn, spawnSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { createServer as createNetServer } from 'node:net';
import { dirname, extname, join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options } = parseArgs({
  options: {
    out: { type: 'string', default: join(repoRoot, 'artifacts/docs-site') },
    publish: { type: 'string', default: join(repoRoot, 'artifacts/docs-publish') },
    'skip-publish': { type: 'boolean', default: false },
    preview: { type: 'boolean', default: false },
    serve: { type: 'boolean', default: false },
  },
});
const out = resolve(options.out);
const publishDir = resolve(options.publish);

if (options.serve) preview(out);
else await exportSite();

async function exportSite() {
  if (!options['skip-publish']) {
    console.log('发布 apps/Rune.Docs（Release）…');
    const result = spawnSync('dotnet', ['publish', join(repoRoot, 'apps/Rune.Docs/Rune.Docs.csproj'), '-c', 'Release', '-o', publishDir], { stdio: 'inherit' });
    if (result.status !== 0) throw new Error('dotnet publish 失败');
  }

  rmSync(out, { recursive: true, force: true });
  mkdirSync(out, { recursive: true });

  // ───────── 静态资源 ─────────
  const manifest = JSON.parse(readFileSync(join(publishDir, 'Rune.Docs.staticwebassets.endpoints.json'), 'utf8'));
  let assets = 0;
  for (const endpoint of manifest.Endpoints) {
    if (endpoint.Selectors?.length) continue;
    if (/\.(br|gz)$/.test(endpoint.AssetFile)) continue;
    const target = join(out, endpoint.Route);
    if (existsSync(target)) continue;
    mkdirSync(dirname(target), { recursive: true });
    cpSync(join(publishDir, 'wwwroot', endpoint.AssetFile), target);
    assets++;
  }
  console.log(`复制了 ${assets} 个静态资源`);

  // ───────── 预渲染页面 ─────────
  const port = await freePort();
  const origin = `http://127.0.0.1:${port}`;
  const server = spawn('dotnet', [join(publishDir, 'Rune.Docs.dll'), '--urls', origin], {
    cwd: publishDir,
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Production' },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let serverLog = '';
  server.stdout.on('data', (chunk) => (serverLog += chunk));
  server.stderr.on('data', (chunk) => (serverLog += chunk));

  try {
    await waitUntilServing(origin);
    const pages = await crawl(origin);
    for (const [path, html] of pages) {
      const file = path === '/' ? 'index.html' : `${path.slice(1)}.html`;
      mkdirSync(dirname(join(out, file)), { recursive: true });
      writeFileSync(join(out, file), html);
    }
    const notFound = await fetch(`${origin}/not-found`);
    writeFileSync(join(out, '404.html'), await notFound.text());
    // import map 里有服务端运行时生成的模块（_framework/resource-collection.*.js，WebAssembly 启动时要它），不在静态资源里：从服务端取下来
    let generated = 0;
    for (const target of importMapTargets(pages.values())) {
      const file = join(out, target);
      if (existsSync(file) || /\.(br|gz)$/.test(target)) continue;
      const response = await fetch(`${origin}/${target}`);
      if (!response.ok) throw new Error(`import map 里的 ${target} 取不到：${response.status}`);
      mkdirSync(dirname(file), { recursive: true });
      writeFileSync(file, Buffer.from(await response.arrayBuffer()));
      generated++;
    }
    if (generated) console.log(`取下了 ${generated} 个运行时生成的模块`);
    console.log(`导出了 ${pages.size} 个页面与 404.html → ${out}`);
  } catch (error) {
    console.error(serverLog);
    throw error;
  } finally {
    server.kill();
  }

  if (options.preview) preview(out);
}

/** 从首页开始，按站内链接（href）与 iframe（src）爬完全部页面：地址 → HTML */
async function crawl(origin) {
  const pages = new Map();
  const queue = ['/'];
  const failures = [];
  while (queue.length) {
    const path = queue.shift();
    if (pages.has(path)) continue;
    const response = await fetch(origin + path, { redirect: 'manual' });
    if (response.status !== 200 || !response.headers.get('content-type')?.includes('text/html')) {
      failures.push(`${path}：${response.status}`);
      pages.set(path, null);
      continue;
    }
    const html = await response.text();
    pages.set(path, html);
    for (const link of linksIn(html)) if (!pages.has(link) && !queue.includes(link)) queue.push(link);
  }
  if (failures.length) throw new Error(`这些页面取不到：\n${failures.join('\n')}`);
  return new Map([...pages].filter(([, html]) => html !== null));
}

/** 页面 import map（script type="importmap"）里的目标地址（去掉开头的 ./ 与 /） */
function importMapTargets(htmls) {
  const targets = new Set();
  for (const html of htmls) {
    for (const match of html.matchAll(/<script type="importmap"[^>]*>([\s\S]*?)<\/script>/g)) {
      const map = JSON.parse(match[1]);
      const values = [...Object.values(map.imports ?? {}), ...Object.values(map.scopes ?? {}).flatMap((scope) => Object.values(scope))];
      for (const value of values) if (!/^[a-z]+:/i.test(value)) targets.add(value.replace(/^\.?\//, ''));
    }
  }
  return targets;
}

/** 页面里的站内地址：相对 <base href="/"> 或以 / 开头，没有扩展名（不是静态资源），去掉查询与锚点 */
function linksIn(html) {
  const links = new Set();
  for (const match of html.matchAll(/\s(?:href|src)="([^"]*)"/g)) {
    let value = match[1].replace(/&amp;/g, '&').split(/[?#]/)[0];
    if (/^[a-z]+:|^\/\/|^_/i.test(value) || value.startsWith('/_')) continue;
    if (extname(value)) continue;
    value = '/' + value.replace(/^\/+/, '').replace(/\/+$/, '');
    links.add(value);
  }
  return links;
}

async function waitUntilServing(origin) {
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    try {
      if ((await fetch(origin)).ok) return;
    } catch {
      // 还没起来
    }
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('文档站 60 秒内没有起来');
}

function freePort() {
  return new Promise((resolvePort) => {
    const probe = createNetServer().listen(0, '127.0.0.1', () => {
      const { port } = probe.address();
      probe.close(() => resolvePort(port));
    });
  });
}

/** 按 Cloudflare Pages 的规则预览：/a/b 取 a/b.html，目录取 index.html，找不到时 404.html */
function preview(root) {
  const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.wasm': 'application/wasm', '.svg': 'image/svg+xml', '.dat': 'application/octet-stream', '.txt': 'text/plain' };
  const candidates = (path) => [join(root, path), join(root, `${path}.html`), join(root, path, 'index.html')];
  createServer((request, response) => {
    const path = decodeURIComponent(new URL(request.url, 'http://x').pathname);
    const file = candidates(path).find((f) => existsSync(f) && statSync(f).isFile());
    const status = file ? 200 : 404;
    const served = file ?? join(root, '404.html');
    response.writeHead(status, { 'content-type': types[extname(served)] ?? 'application/octet-stream' });
    response.end(readFileSync(served));
  }).listen(5320, () => console.log('预览：http://localhost:5320（Ctrl+C 结束）'));
}
