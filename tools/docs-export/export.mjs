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
// 4. 合并、挪走了的旧页面地址（DocRedirects）写成 _redirects，由 Cloudflare Pages 永久跳转到新页的小节；
//    静态资源的原名也在里面，临时跳到带哈希的地址（只导出带哈希的文件，见下面的 hashed）。
// 5. 文档数据（示例的源码、API 表、图标库）：爬页面时用到过的地址取 /_docs/files，逐个写出（浏览器站内换页时下载）。
// 6. 运行时生成的模块与样式表（主题变量）从服务端取下来；_headers 给带哈希的文件长期缓存、让 Cloudflare 压缩 ICU 数据。
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
  // 每个文件有两个地址：原名（app.css）与带内容哈希的（app.abc123.css，清单里它的 label 是原名）。只导出带哈希的那个，
  // 原名写成 302 跳过去（下面的 _redirects）：这样目录里只有内容变了名字就变的文件，可以整个目录长期缓存；
  // 页面都按带哈希的地址引用，按原名取的（示例里的模型文件）多一次跳转，照样取得到
  const hashed = new Map();
  for (const endpoint of manifest.Endpoints) {
    if (/\.(br|gz)$/.test(endpoint.Route)) continue;
    const label = endpoint.EndpointProperties?.find((property) => property.Name === 'label')?.Value;
    if (label && label !== endpoint.Route) hashed.set(label, endpoint.Route);
  }
  let assets = 0;
  for (const endpoint of manifest.Endpoints) {
    if (endpoint.Selectors?.length) continue;
    if (/\.(br|gz)$/.test(endpoint.AssetFile)) continue;
    if (hashed.has(endpoint.Route)) continue;
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
    const redirects = await fetch(`${origin}/_redirects`);
    if (!redirects.ok) throw new Error(`/_redirects 取不到：${redirects.status}`);
    // 文档页的跳转（永久），加上静态资源的原名 → 带哈希的地址（临时：下次部署哈希会变）
    const aliases = [...hashed].map(([label, route]) => `/${label} /${route} 302\n`).join('');
    // 页面里写了 <link rel="icon">，仍有浏览器与爬虫直接要 /favicon.ico：给它标志，不回三百多 KB 的 404 页
    const logo = hashed.get('_content/Rune.Docs.Content/logo.svg');
    writeFileSync(join(out, '_redirects'), (await redirects.text()) + aliases + (logo ? `/favicon.ico /${logo} 302\n` : ''));
    // sitemap.xml：站点地址取自 wrangler.jsonc 的自定义域名；整页示例（frame/…，嵌在别的页面里的）不列
    const domain = /"pattern":\s*"([^"]+)"/.exec(readFileSync(join(repoRoot, 'apps/Rune.Docs/wrangler.jsonc'), 'utf8'))?.[1];
    if (domain) {
      const urls = [...pages.keys()].filter((path) => !path.startsWith('/frame/')).sort().map((path) => `  <url><loc>https://${domain}${path === '/' ? '/' : path}</loc></url>\n`);
      writeFileSync(join(out, 'sitemap.xml'), `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls.join('')}</urlset>\n`);
    }
    // 文档数据（示例的源码、API 表、图标库，Rune.Docs.Server）：爬页面时用到过的都在 /_docs/files 里，浏览器站内换页时下载它们
    const docsFiles = (await (await fetch(`${origin}/_docs/files`)).text()).split('\n').filter(Boolean);
    for (const path of docsFiles) {
      const response = await fetch(`${origin}/${path}`);
      if (!response.ok) throw new Error(`文档数据 ${path} 取不到：${response.status}`);
      mkdirSync(dirname(join(out, path)), { recursive: true });
      writeFileSync(join(out, path), Buffer.from(await response.arrayBuffer()));
    }
    console.log(`写出了 ${docsFiles.length} 个文档数据文件`);
    // 服务端运行时生成、不在静态资源里的文件：import map 里的模块（_framework/resource-collection.*.js，WebAssembly 启动时要它）
    // 与页面引用的样式表（主题变量的 rune-theme.*.css）：从服务端取下来
    let generated = 0;
    for (const target of new Set([...importMapTargets(pages.values()), ...stylesheetTargets(pages.values())])) {
      const file = join(out, target);
      if (existsSync(file) || /\.(br|gz)$/.test(target)) continue;
      const response = await fetch(`${origin}/${target}`);
      if (!response.ok) throw new Error(`${target} 取不到：${response.status}`);
      mkdirSync(dirname(file), { recursive: true });
      writeFileSync(file, Buffer.from(await response.arrayBuffer()));
      generated++;
    }
    if (generated) console.log(`取下了 ${generated} 个运行时生成的文件`);
    // 响应头（Cloudflare 的 _headers）：
    // - _framework、_content 下只有带内容哈希的文件（原名的见上面的 hashed），主题变量的样式表也按内容命名：
    //   内容变了名字就变，可以长期缓存、不用每次回源确认（默认 max-age=0，再次打开时一百多个文件逐个确认）
    // - 文档数据（_docs，示例的源码与 API 表）不带哈希、只在部署时变：缓存十分钟，站内来回换页不重复取
    // - Cloudflare 不压缩 application/octet-stream：ICU 数据（_framework/icudt*.dat，1.5 MB，压缩后约 0.45 MB）标成 application/wasm 让它压缩
    //   （运行时按字节读取，不看类型）
    const immutable = '  Cache-Control: public, max-age=31536000, immutable';
    writeFileSync(join(out, '_headers'), [
      '/_framework/*', immutable,
      '/_content/*', immutable,
      '/rune-theme.*', immutable,
      '/_docs/*', '  Cache-Control: public, max-age=600',
      '/_framework/*.dat', '  Content-Type: application/wasm',
      '',
    ].join('\n'));
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

/** 页面引用的站内样式表（link rel="stylesheet" 的 href，去掉开头的 ./ 与 /） */
function stylesheetTargets(htmls) {
  const targets = new Set();
  for (const html of htmls)
    for (const match of html.matchAll(/<link rel="stylesheet" href="([^"]+)"/g))
      if (!/^[a-z]+:/i.test(match[1])) targets.add(match[1].replace(/^\.?\//, '').split('?')[0]);
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

/** 按 Cloudflare Pages 的规则预览：先按 _redirects 跳转，/a/b 取 a/b.html，目录取 index.html，找不到时 404.html */
function preview(root) {
  const redirectsFile = join(root, '_redirects');
  const redirects = new Map(
    existsSync(redirectsFile)
      ? readFileSync(redirectsFile, 'utf8').split(/\r?\n/).filter(Boolean).map((line) => line.split(' ')).map(([from, to, status]) => [from, { to, status: Number(status ?? 301) }])
      : [],
  );
  const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.wasm': 'application/wasm', '.svg': 'image/svg+xml', '.dat': 'application/octet-stream', '.txt': 'text/plain' };
  const candidates = (path) => [join(root, path), join(root, `${path}.html`), join(root, path, 'index.html')];
  createServer((request, response) => {
    const path = decodeURIComponent(new URL(request.url, 'http://x').pathname);
    if (redirects.has(path)) {
      response.writeHead(redirects.get(path).status, { location: redirects.get(path).to });
      response.end();
      return;
    }
    const file = candidates(path).find((f) => existsSync(f) && statSync(f).isFile());
    const status = file ? 200 : 404;
    const served = file ?? join(root, '404.html');
    response.writeHead(status, { 'content-type': types[extname(served)] ?? 'application/octet-stream' });
    response.end(readFileSync(served));
  }).listen(5320, () => console.log('预览：http://localhost:5320（Ctrl+C 结束）'));
}
