// 与 vue-tsx-pro 文档站逐个部件对比计算后的样式：同一个示例（小节标题的 id 相同）里，按
// (data-scope, data-part, 第几个) 配对两边的元素，比较尺寸、字体、颜色、阴影、过渡，亮、暗主题各一遍。
// 不进 CI：要本地开着 vue-tsx-pro 文档站（pnpm dev:docs，5174）与本库演示项目（5300）。用法见 README.md。
import { mkdirSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    vue: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    'vue-origin': { type: 'string', default: 'http://localhost:5174' },
    'rune-origin': { type: 'string', default: 'http://localhost:5300' },
    page: { type: 'string' },
    mode: { type: 'string', default: 'static' },
    themes: { type: 'string', default: 'light,dark' },
    states: { type: 'boolean', default: false },
    width: { type: 'string', default: '1280' },
    tolerance: { type: 'string', default: '0.5' },
    shots: { type: 'boolean', default: false },
  },
});
const [slug, only] = positionals;
if (!slug) {
  console.error('用法：node tools/parity/parity.mjs <演示名> [示例 id,…] [--page=/components/<页面>] [--states] [--shots]');
  process.exit(1);
}
const vuePath = options.page ?? `/components/${slug}`;
const tolerance = Number(options.tolerance);
const outDir = resolve(repoRoot, 'artifacts/parity', slug);
mkdirSync(outDir, { recursive: true });

// playwright 用 vue-tsx-pro 浏览器测试装好的那份（本库的依赖规矩里没有 node 包）
const { chromium } = createRequire(join(options.vue, 'apps/host-demo/package.json'))('playwright');

/** 比较的属性：数值的按容差比，其余（颜色已换成 rgba）按字符串比 */
const NUMERIC = [
  'width', 'height',
  'padding-top', 'padding-right', 'padding-bottom', 'padding-left',
  'margin-top', 'margin-right', 'margin-bottom', 'margin-left',
  'border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width',
  'border-top-left-radius', 'border-top-right-radius', 'border-bottom-right-radius', 'border-bottom-left-radius',
  'font-size', 'font-weight', 'line-height', 'letter-spacing', 'row-gap', 'column-gap',
  'opacity', 'outline-width', 'outline-offset',
];
const TEXT = [
  'display', 'position', 'box-sizing', 'flex-direction', 'align-items', 'justify-content', 'flex-wrap',
  'font-family', 'font-style', 'text-align', 'text-transform', 'text-decoration-line', 'white-space',
  'color', 'background-color', 'background-image',
  'border-top-color', 'border-right-color', 'border-bottom-color', 'border-left-color',
  'border-top-style', 'outline-style', 'outline-color', 'box-shadow', 'cursor',
  'transition-property', 'transition-duration', 'transition-timing-function',
  'animation-name', 'animation-duration', 'fill', 'stroke', 'stroke-width',
];
/** 交互状态只看会变的属性 */
const STATE_PROPS = [
  'color', 'background-color', 'border-top-color', 'box-shadow', 'opacity',
  'outline-style', 'outline-color', 'outline-width', 'outline-offset', 'text-decoration-line',
];

/**
 * 在页面里执行：找到示例小节，列出效果区里带部件标记的元素（与它们里面的 svg），读计算后的样式。
 * 颜色统一画到画布上读回 rgba，两边写法不同（oklch、color-mix、十六进制）也能比较。
 */
function collect({ side, ids, props, canvasWidth }) {
  const probe = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
  const colorPattern = /(?:rgba?|hsla?|oklch|oklab|lab|lch|color)\([^()]*\)|#[0-9a-f]{3,8}\b|\b(?:transparent|currentcolor|white|black)\b/gi;
  const rgba = (value) => {
    probe.clearRect(0, 0, 1, 1);
    probe.fillStyle = '#000';
    probe.fillStyle = value;
    probe.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = probe.getImageData(0, 0, 1, 1).data;
    return a === 0 ? 'transparent' : `rgba(${r},${g},${b},${Math.round((a / 255) * 100) / 100})`;
  };
  const normalize = (value) =>
    value.replace(colorPattern, (match) => (/currentcolor/i.test(match) ? match : rgba(match)));
  // 交互状态读样式时也用它
  window.parityNormalize = normalize;

  /** 效果区：vue 是示例卡片里的排布网格，本库是 DemoSection 的 data-demo-canvas */
  const canvasOf = (heading) => {
    const section = heading.parentElement;
    if (side === 'rune') return section.querySelector('[data-demo-canvas]');
    let element = section.querySelector('[role=group]');
    while (element && !(getComputedStyle(element).display === 'grid' && element.firstElementChild)) {
      element = element.firstElementChild;
    }
    return element;
  };

  const headings = [...document.querySelectorAll('main section > h2[id]')].filter(
    (heading) => !ids || ids.includes(heading.id),
  );
  const result = {};
  for (const heading of headings) {
    const canvas = canvasOf(heading);
    if (!canvas) continue;
    if (side === 'rune' && canvasWidth?.[heading.id]) {
      // 效果区与文档站同宽，占满宽度的元素才可比
      canvas.style.boxSizing = 'border-box';
      canvas.style.width = `${canvasWidth[heading.id]}px`;
    }
    const counts = new Map();
    const elements = [];
    const add = (element, key) => {
      const index = counts.get(key) ?? 0;
      counts.set(key, index + 1);
      element.dataset.parityKey = `${heading.id}|${key}#${index}`;
      elements.push({ key: `${key}#${index}`, element });
    };
    for (const element of canvas.querySelectorAll('[data-scope], svg')) {
      if (element.dataset.scope && element.tagName !== "svg") {
        add(element, `${element.dataset.scope}/${element.dataset.part ?? '?'}`);
      } else {
        // 图标按所在组件的根部件配对：vue 的图标常常不是单独的部件，本库的在 icon 部件里
        const owner = element.parentElement.closest('[data-part=root]');
        const ownerKey = owner && canvas.contains(owner) ? owner.dataset.parityKey.split('|')[1] : 'canvas';
        add(element, `${ownerKey}>svg`);
      }
    }
    const read = (element) => {
      const style = getComputedStyle(element);
      const values = {};
      for (const prop of props) values[prop] = normalize(style.getPropertyValue(prop));
      const rect = element.getBoundingClientRect();
      values.width = `${rect.width}px`;
      values.height = `${rect.height}px`;
      return values;
    };
    result[heading.id] = {
      canvasWidth: canvas.getBoundingClientRect().width,
      elements: Object.fromEntries(
        elements.map(({ key, element }) => [
          key,
          { tag: element.tagName.toLowerCase(), text: element.textContent.trim().slice(0, 16), values: read(element) },
        ]),
      ),
    };
  }
  return result;
}

/** 两个值是否一致：数值按容差，其余按字符串 */
function same(prop, a, b) {
  if (NUMERIC.includes(prop)) {
    const [x, y] = [parseFloat(a), parseFloat(b)];
    if (!Number.isNaN(x) && !Number.isNaN(y)) return Math.abs(x - y) <= tolerance;
  }
  return a === b;
}

/**
 * 比较配对好的元素（[键, vue 的值, rune 的值]），同一部件的相同差异合并成一条（“button/root ×4”）。
 * state 是交互状态（hover、focus、active），写在部件后面
 */
function compareValues(pairs, state) {
  const grouped = new Map();
  for (const [key, a, b] of pairs) {
    // 没有轮廓时轮廓的宽度、颜色、偏移看不见
    const outlined = a['outline-style'] !== 'none' || b['outline-style'] !== 'none';
    for (const prop of Object.keys(a)) {
      if (same(prop, a[prop], b[prop])) continue;
      if (!outlined && prop.startsWith('outline-')) continue;
      const part = key.replace(/#\d+/g, '');
      const id = `${part}|${prop}|${a[prop]}|${b[prop]}`;
      const entry = grouped.get(id) ?? { part, prop, vue: a[prop], rune: b[prop], keys: [] };
      entry.keys.push(key);
      grouped.set(id, entry);
    }
  }
  return [...grouped.values()].map((entry) => {
    const where = entry.keys.length > 1 ? `${entry.part} ×${entry.keys.length}` : entry.keys[0];
    return `${where}${state ? ` :${state}` : ''}  ${entry.prop}: vue ${entry.vue} · rune ${entry.rune}`;
  });
}

/** 对比一个示例：按键配对，比较计算后的样式，列出只有一边有的部件 */
function diffSection(vue, rune) {
  const vueKeys = Object.keys(vue.elements);
  const runeKeys = Object.keys(rune.elements);
  const missing = vueKeys.filter((key) => !(key in rune.elements));
  const extra = runeKeys.filter((key) => !(key in vue.elements));
  const lines = compareValues(
    vueKeys.filter((key) => key in rune.elements).map((key) => [key, vue.elements[key].values, rune.elements[key].values]),
  );
  // 只有一边有的部件按部件名计数（结构不同的部件，比如本库给 Flex、文字也加了部件标记）
  const summary = (keys) => {
    const counts = new Map();
    for (const key of keys) {
      const part = key.replace(/#\d+/g, '');
      counts.set(part, (counts.get(part) ?? 0) + 1);
    }
    return [...counts].map(([part, count]) => (count > 1 ? `${part} ×${count}` : part)).join('，');
  };
  if (missing.length) lines.push(`只有 vue：${summary(missing)}`);
  if (extra.length) lines.push(`只有 rune：${summary(extra)}`);
  return { paired: vueKeys.length - missing.length, lines };
}

/** 打开一边的页面并等示例渲染完 */
async function open(context, side, theme) {
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', (error) => errors.push(error.message));
  if (side === 'vue') {
    await page.addInitScript((value) => localStorage.setItem('vue-tsx-pro-theme', value), theme);
    await page.goto(`${options['vue-origin']}${vuePath}#parity`);
    // 冷启动的开发服务器第一次编译要一会儿；带锚点打开时示例全部立即渲染
    await page.waitForFunction(() => document.querySelector('main h1'), null, { timeout: 120_000 });
  } else {
    await page.goto(`${options['rune-origin']}/${options.mode}/${slug}`);
    await page.waitForSelector('[data-demo-canvas]', { timeout: 60_000 });
  }
  await page.waitForTimeout(800);
  return { page, errors };
}

/** 交互状态：逐个元素悬停、键盘聚焦、按下，读会变的属性 */
async function readStates(page, keys) {
  const states = {};
  for (const key of keys) {
    const locator = page.locator(`[data-parity-key="${key}"]`);
    const interactive = await locator.evaluate((element) =>
      element.matches('button, a[href], input, select, textarea, [tabindex], [role=button], [role=menuitem]'),
    );
    if (!interactive || !(await locator.isVisible())) continue;
    const read = () =>
      locator.evaluate((element, props) => {
        const style = getComputedStyle(element);
        return Object.fromEntries(props.map((prop) => [prop, window.parityNormalize(style.getPropertyValue(prop))]));
      }, STATE_PROPS);
    await locator.hover();
    const hover = await read();
    await page.keyboard.press('Shift');
    await locator.focus();
    const focus = await read();
    await locator.blur();
    const box = await locator.boundingBox();
    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await page.mouse.down();
    const active = await read();
    await page.mouse.up();
    await page.mouse.move(0, 0);
    states[key] = { hover, focus, active };
  }
  return states;
}

const browser = await chromium.launch();
const ids = only?.split(',');
const report = [`# ${slug}：vue-tsx-pro ${vuePath} ↔ rune /${options.mode}/${slug}`, ''];
let total = 0;
for (const theme of options.themes.split(',')) {
  const contextOptions = {
    viewport: { width: Number(options.width), height: 900 },
    deviceScaleFactor: 1,
    colorScheme: theme === 'dark' ? 'dark' : 'light',
    reducedMotion: options.states ? 'reduce' : 'no-preference',
  };
  const vueContext = await browser.newContext(contextOptions);
  const runeContext = await browser.newContext(contextOptions);
  const [vue, rune] = await Promise.all([open(vueContext, 'vue', theme), open(runeContext, 'rune', theme)]);
  const props = [...new Set([...NUMERIC, ...TEXT])].filter((prop) => prop !== 'width' && prop !== 'height');
  const vueData = await vue.page.evaluate(collect, { side: 'vue', ids, props });
  const canvasWidth = Object.fromEntries(Object.entries(vueData).map(([id, section]) => [id, section.canvasWidth]));
  const runeData = await rune.page.evaluate(collect, { side: 'rune', ids, props, canvasWidth });

  report.push(`## ${theme}`, '');
  for (const id of Object.keys(vueData)) {
    if (!runeData[id]) {
      report.push(`### ${id}`, '', '本库演示里没有这个示例', '');
      console.log(`[${theme}] ${id}：本库演示里没有这个示例`);
      continue;
    }
    const { paired, lines } = diffSection(vueData[id], runeData[id]);
    if (options.states) {
      const keys = Object.keys(vueData[id].elements)
        .filter((key) => key in runeData[id].elements)
        .map((key) => `${id}|${key}`);
      const [vueStates, runeStates] = await Promise.all([readStates(vue.page, keys), readStates(rune.page, keys)]);
      const both = Object.keys(vueStates).filter((key) => key in runeStates);
      for (const state of ['hover', 'focus', 'active']) {
        const pairs = both.map((key) => [key.split('|')[1], vueStates[key][state], runeStates[key][state]]);
        lines.push(...compareValues(pairs, state));
      }
    }
    total += lines.length;
    console.log(`[${theme}] ${id}：配对 ${paired} 个元素，${lines.length} 处差异`);
    for (const line of lines.slice(0, 40)) console.log(`    ${line}`);
    if (lines.length > 40) console.log(`    ……另有 ${lines.length - 40} 处，见报告`);
    report.push(`### ${id}`, '', `配对 ${paired} 个元素，${lines.length} 处差异`, '', ...lines.map((line) => `- ${line}`), '');
    if (options.shots) {
      for (const [side, { page }] of [['vue', vue], ['rune', rune]]) {
        const heading = page.locator(`[id="${id}"]`).locator('xpath=..');
        await heading.screenshot({ path: join(outDir, `${id}-${theme}-${side}.png`) });
      }
    }
  }
  const missingInVue = Object.keys(runeData).filter((id) => !vueData[id]);
  if (missingInVue.length) console.log(`[${theme}] 文档站没有的示例：${missingInVue.join('、')}`);
  for (const [side, { errors }] of [['vue', vue], ['rune', rune]]) {
    if (errors.length) console.log(`[${theme}] ${side} 页面错误：${errors.slice(0, 3).join(' | ')}`);
  }
  await vueContext.close();
  await runeContext.close();
}
await browser.close();
writeFileSync(join(outDir, 'report.md'), report.join('\n'));
console.log(`\n共 ${total} 处差异，报告在 ${join(outDir, 'report.md')}`);
process.exitCode = total ? 1 : 0;
