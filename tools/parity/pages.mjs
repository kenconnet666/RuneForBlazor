// 整页对照：同一地址、同一视口、同一主题下打开 vue-tsx-pro 文档站与本库文档站，比较站点外壳（顶栏、侧栏、正文、本页目录、标题的位置与大小）
// 和第一屏的画面。示例画布里的细节由 parity.mjs 比；这里看的是页面骨架。
// 页面取两边侧栏里都有的地址（再加首页），也可以用 --pages 指定。
// 用法：node tools/parity/pages.mjs [--pages=/,/components/button] [--widths=1440,390] [--themes=light,dark] [--vue-url=…] [--rune-url=…]
// 报告写到 artifacts/parity/_pages/report.md，截图在同一目录；有差异时退出码为 1。
// Git Bash 里 --pages 的 "/" 会被改写成 Windows 路径，前面加 MSYS_NO_PATHCONV=1。
import { mkdirSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';
import { fixClock } from './clock.mjs';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options } = parseArgs({
  options: {
    vue: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    'vue-url': { type: 'string', default: 'http://localhost:5174' },
    'rune-url': { type: 'string', default: 'http://localhost:5310' },
    pages: { type: 'string' },
    widths: { type: 'string', default: '1440,390' },
    themes: { type: 'string', default: 'light' },
    // 外壳盒子允许的偏差（像素）、画面允许不同的像素比例
    tolerance: { type: 'string', default: '0.5' },
    threshold: { type: 'string', default: '0.01' },
  },
});
const { chromium } = createRequire(join(options.vue, 'apps/host-demo/package.json'))('playwright');
const out = join(repoRoot, 'artifacts/parity/_pages');
mkdirSync(out, { recursive: true });

const sites = { vue: options['vue-url'], rune: options['rune-url'] };
const tolerance = Number(options.tolerance);
const threshold = Number(options.threshold);

// 外壳的盒子：AppShell 的几块与正文的第一个标题
const SHELL = { header: 'header', nav: 'nav', main: 'main', aside: 'aside', h1: 'main h1' };

const browser = await chromium.launch();

async function sidebarPaths(base) {
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(base + '/', { waitUntil: 'networkidle' });
  const paths = await page.$$eval('nav a[href]', (links) => links.map((a) => new URL(a.href).pathname.replace(/\/$/, '') || '/'));
  await page.close();
  return paths;
}

const pages = options.pages
  ? options.pages.split(',')
  : await (async () => {
      const [vue, rune] = await Promise.all([sidebarPaths(sites.vue), sidebarPaths(sites.rune)]);
      return ['/', ...vue.filter((path) => path !== '/' && rune.includes(path))];
    })();

async function capture(base, path, width, theme) {
  const context = await browser.newContext({ viewport: { width, height: 900 }, colorScheme: theme, reducedMotion: 'reduce' });
  await fixClock(context);
  const page = await context.newPage();
  await page.goto(base + path, { waitUntil: 'networkidle' });
  await page.evaluate(() => document.fonts.ready);
  // 排队渲染的示例、懒画的图表都要时间
  await page.waitForTimeout(1500);
  const boxes = await page.evaluate((shell) => Object.fromEntries(Object.entries(shell).map(([name, selector]) => {
    const element = document.querySelector(selector);
    const rect = element?.getBoundingClientRect();
    return [name, rect && rect.width > 0 ? [rect.x, rect.y, rect.width, rect.height].map((n) => Math.round(n * 10) / 10) : null];
  })), SHELL);
  const png = await page.screenshot();
  await context.close();
  return { boxes, png };
}

// 两张截图里明显不同（任一通道差 > 40）的像素比例：在浏览器里用 canvas 算，不用另装图像库
const diffPage = await browser.newPage();
async function pixelDiff(a, b) {
  return diffPage.evaluate(async ([a, b]) => {
    const load = async (base64) => createImageBitmap(await (await fetch(`data:image/png;base64,${base64}`)).blob());
    const [x, y] = await Promise.all([load(a), load(b)]);
    const width = Math.min(x.width, y.width);
    const height = Math.min(x.height, y.height);
    const pixels = (image) => {
      const canvas = new OffscreenCanvas(width, height);
      const context = canvas.getContext('2d');
      context.drawImage(image, 0, 0);
      return context.getImageData(0, 0, width, height).data;
    };
    const [p, q] = [pixels(x), pixels(y)];
    let different = 0;
    for (let i = 0; i < p.length; i += 4)
      if (Math.abs(p[i] - q[i]) > 40 || Math.abs(p[i + 1] - q[i + 1]) > 40 || Math.abs(p[i + 2] - q[i + 2]) > 40) different++;
    return different / (width * height);
  }, [a.toString('base64'), b.toString('base64')]);
}

const rows = [];
let failures = 0;
for (const theme of options.themes.split(','))
  for (const width of options.widths.split(',').map(Number))
    for (const path of pages) {
      const [vue, rune] = await Promise.all([capture(sites.vue, path, width, theme), capture(sites.rune, path, width, theme)]);
      const name = `${width}-${theme}${path === '/' ? '-home' : path.replaceAll('/', '-')}`;
      writeFileSync(join(out, `${name}-vue.png`), vue.png);
      writeFileSync(join(out, `${name}-rune.png`), rune.png);
      const shell = Object.keys(SHELL).flatMap((part) => {
        const [a, b] = [vue.boxes[part], rune.boxes[part]];
        // 正文、侧栏的高度随内容（页数）变，不算外壳的差异
        const compared = (box) => box && (part === 'main' || part === 'nav' ? box.slice(0, 3) : box);
        const same = JSON.stringify(a) === JSON.stringify(b)
          || (a && b && compared(a).every((n, i) => Math.abs(n - compared(b)[i]) <= tolerance));
        return same ? [] : [`${part}: vue ${a?.join(',') ?? '无'} · rune ${b?.join(',') ?? '无'}`];
      });
      const pixels = await pixelDiff(vue.png, rune.png);
      const bad = shell.length > 0 || pixels > threshold;
      if (bad) failures++;
      rows.push({ path, width, theme, shell, pixels, bad });
      console.log(`${bad ? '✗' : '✓'} ${width} ${theme} ${path}  画面 ${(pixels * 100).toFixed(1)}%${shell.length ? '  ' + shell.join('；') : ''}`);
    }
await browser.close();

const report = [
  '# 整页对照：vue-tsx-pro 文档站 ↔ rune 文档站',
  '',
  `外壳盒子允许偏差 ${tolerance}px，第一屏画面允许 ${threshold * 100}% 的像素不同。截图：\`<宽>-<主题>-<地址>-{vue,rune}.png\`。`,
  '',
  '| 地址 | 宽 | 主题 | 第一屏不同的像素 | 外壳的差异 |',
  '| --- | --- | --- | --- | --- |',
  ...rows.sort((a, b) => b.pixels - a.pixels)
    .map((row) => `| ${row.path} | ${row.width} | ${row.theme} | ${(row.pixels * 100).toFixed(1)}% | ${row.shell.join('<br>')} |`),
  '',
].join('\n');
writeFileSync(join(out, 'report.md'), report);
console.log(`\n${failures} / ${rows.length} 页有差异，报告在 ${join(out, 'report.md')}`);
process.exitCode = failures > 0 ? 1 : 0;
