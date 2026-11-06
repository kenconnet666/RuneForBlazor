#!/usr/bin/env node
// 计算样式快照：测试台（Rune.Demo，默认 5300）每个文档页里每个元素约 60 个外观属性的计算值，亮、暗主题各一遍；
// 可交互的元素再强制 :hover、:focus-visible、:active 各记一次会变的那些属性。改样式引擎、整理样式时前后各拍一份再比，
// 真实的差异应当为 0（同 vue-tsx-pro 样式改造的验收）。不进 CI，本地跑。
//
//   node tools/style-snapshot/snapshot.mjs capture before.json [页面,…]   拍快照（不写页面时取首页列出的全部文档页）
//   node tools/style-snapshot/snapshot.mjs compare before.json after.json  比对，列出不同的项；有差异时退出码为 1
//
// 元素按文档顺序编号（改样式不改 DOM 结构）；减少动态效果下拍，过渡瞬间完成。Playwright 用 vue-tsx-pro 的 apps/host-demo 装好的那份（同 tools/parity）。
import { readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    vue: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    origin: { type: 'string', default: 'http://localhost:5300' },
    mode: { type: 'string', default: 'static' },
    themes: { type: 'string', default: 'light,dark' },
  },
});
const [command, ...args] = positionals;

const PROPS = [
  'display', 'position', 'top', 'right', 'bottom', 'left', 'z-index', 'box-sizing', 'width', 'height',
  'min-width', 'min-height', 'max-width', 'max-height', 'margin-top', 'margin-right', 'margin-bottom', 'margin-left',
  'padding-top', 'padding-right', 'padding-bottom', 'padding-left',
  'border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width', 'border-top-style', 'border-top-color',
  'border-right-color', 'border-bottom-color', 'border-left-color', 'border-top-left-radius', 'border-top-right-radius',
  'border-bottom-left-radius', 'border-bottom-right-radius', 'background-color', 'background-image', 'color',
  'font-family', 'font-size', 'font-weight', 'font-style', 'line-height', 'letter-spacing', 'text-align', 'text-decoration-line',
  'text-transform', 'white-space', 'opacity', 'visibility', 'box-shadow', 'outline-style', 'outline-width', 'outline-color',
  'outline-offset', 'transform', 'cursor', 'overflow-x', 'overflow-y', 'flex-direction', 'flex-wrap', 'flex-grow', 'flex-shrink',
  'align-items', 'justify-content', 'row-gap', 'column-gap', 'grid-template-columns', 'fill', 'stroke', 'stroke-width',
  'object-fit', 'pointer-events', 'user-select', 'vertical-align', 'transition-property', 'animation-name', 'filter',
];
const STATE_PROPS = ['color', 'background-color', 'border-top-color', 'box-shadow', 'outline-style', 'outline-color', 'outline-width',
  'opacity', 'transform', 'text-decoration-line', 'cursor'];
const INTERACTIVE = 'a[href], button, input, select, textarea, summary, label, [tabindex], [role=button], [role=option], [role=tab], [role=menuitem], [role=switch], [role=checkbox], [role=radio]';
const STATES = ['hover', 'focus-visible', 'active'];

if (command === 'capture') await capture(args[0], args[1]?.split(','));
else if (command === 'compare') compare(args[0], args[1]);
else {
  console.error('用法：node tools/style-snapshot/snapshot.mjs capture <输出.json> [页面,…] | compare <前.json> <后.json>');
  process.exit(1);
}

async function capture(output, pages) {
  const { chromium } = createRequire(join(options.vue, 'apps/host-demo/package.json'))('playwright');
  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 }, reducedMotion: 'reduce', locale: 'en-US' });
  const page = await context.newPage();
  if (!pages) {
    await page.goto(options.origin);
    pages = await page.$$eval(`a[href^="${options.mode}/"]`, (links) => links.map((link) => link.getAttribute('href').split('/')[1]));
  }
  const result = {};
  for (const slug of pages) {
    for (const theme of options.themes.split(',')) {
      await page.emulateMedia({ colorScheme: theme, reducedMotion: 'reduce' });
      await page.goto(`${options.origin}/${options.mode}/${slug}`, { waitUntil: 'networkidle' });
      await page.waitForSelector('html[data-rune-ready]', { state: 'attached' });
      const elements = await page.evaluate(({ PROPS }) => {
        const all = [...document.body.querySelectorAll('*')].filter((element) => !['STYLE', 'SCRIPT', 'TEMPLATE'].includes(element.tagName));
        all.forEach((element, index) => element.setAttribute('data-snapshot-index', String(index)));
        return all.map((element) => {
          const style = getComputedStyle(element);
          return PROPS.map((prop) => style.getPropertyValue(prop)).join('\u0001');
        });
      }, { PROPS });
      const states = await captureStates(page);
      result[`${slug}|${theme}`] = { elements, states };
      process.stdout.write(`${slug} ${theme}: ${elements.length} 个元素、${Object.keys(states).length} 个状态\n`);
    }
  }
  await browser.close();
  writeFileSync(output, JSON.stringify({ props: PROPS, stateProps: STATE_PROPS, pages: result }));
}

/** 可交互的元素各强制一种状态（CDP 的 forcePseudoState），记下会变的属性 */
async function captureStates(page) {
  const session = await page.context().newCDPSession(page);
  await session.send('DOM.enable');
  await session.send('CSS.enable');
  const { root } = await session.send('DOM.getDocument', { depth: 0 });
  const { nodeIds } = await session.send('DOM.querySelectorAll', { nodeId: root.nodeId, selector: INTERACTIVE });
  const states = {};
  for (const nodeId of nodeIds) {
    const { attributes } = await session.send('DOM.getAttributes', { nodeId });
    const index = attributes[attributes.indexOf('data-snapshot-index') + 1];
    if (index === undefined) continue;
    for (const state of STATES) {
      await session.send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: [state] });
      const values = await page.evaluate(({ index, STATE_PROPS }) => {
        const style = getComputedStyle(document.querySelector(`[data-snapshot-index="${index}"]`));
        return STATE_PROPS.map((prop) => style.getPropertyValue(prop)).join('\u0001');
      }, { index, STATE_PROPS });
      states[`${index}:${state}`] = values;
    }
    await session.send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: [] });
  }
  await session.detach();
  return states;
}

function compare(beforePath, afterPath) {
  const before = JSON.parse(readFileSync(beforePath, 'utf8'));
  const after = JSON.parse(readFileSync(afterPath, 'utf8'));
  const lines = [];
  for (const [key, page] of Object.entries(before.pages)) {
    const other = after.pages[key];
    if (!other) {
      lines.push(`${key}：后一份没有这一页`);
      continue;
    }
    if (page.elements.length !== other.elements.length) lines.push(`${key}：元素个数不同 ${page.elements.length} → ${other.elements.length}`);
    page.elements.forEach((values, index) => diff(`${key} #${index}`, before.props, values, other.elements[index]));
    for (const [state, values] of Object.entries(page.states)) diff(`${key} #${state}`, before.stateProps, values, other.states[state]);
  }
  console.log(lines.length ? lines.join('\n') : `没有差异（${Object.keys(before.pages).length} 页·主题）`);
  process.exit(lines.length ? 1 : 0);

  function diff(label, props, a, b) {
    if (b === undefined) {
      lines.push(`${label}：后一份没有`);
      return;
    }
    if (a === b) return;
    const left = a.split('\u0001');
    const right = b.split('\u0001');
    props.forEach((prop, i) => {
      if (left[i] !== right[i]) lines.push(`${label} ${prop}: ${left[i]} → ${right[i]}`);
    });
  }
}
