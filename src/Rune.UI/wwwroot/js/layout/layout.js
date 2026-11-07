// 布局：网格里跨列的项按实际列数收窄；尺寸观察；占满到窗口底边的区域量出自己的顶边；截断的文字量出是否真的截断了；跑马灯按一份内容的长度算一圈的时长。

import { reducedMotion } from '../core/shared.js';

/** 量网格实际有几列（计算后的 grid-template-columns 有几段），跨列多于它的项改成跨当前的列数 */
const gridObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target } of entries) clampGridSpans(target);
});

const watchedGrids = new WeakSet();

export function watchGrid(grid) {
  if (!grid || !gridObserver) return;
  if (!watchedGrids.has(grid)) {
    watchedGrids.add(grid);
    gridObserver.observe(grid);
  }
  clampGridSpans(grid);
}

function clampGridSpans(grid) {
  const tracks = getComputedStyle(grid).gridTemplateColumns.split(' ').filter(Boolean).length;
  for (const item of grid.children) {
    const span = Number(item.getAttribute('data-rune-grid-span'));
    if (!span) continue;
    item.style.gridColumn = tracks > 0 && tracks < span ? `span ${tracks} / span ${tracks}` : '';
  }
}

// 尺寸观察（data-rune-observe-size）：内容宽度变了（取整后）就发出冒泡的 rune-resize（size 是宽度，px），
// 交给 C# 按宽度排版（每行放几个）。只在变了时发，避免小数抖动反复往返。
const reportedWidths = new WeakMap();
const sizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target, contentRect } of entries) {
    const width = Math.round(contentRect.width);
    if (width === 0 || reportedWidths.get(target) === width) continue;
    reportedWidths.set(target, width);
    target.dispatchEvent(new CustomEvent('rune-resize', { bubbles: true, detail: { size: width } }));
  }
});

/** 开始观察一个元素的宽度（插入时调用；重复调用无副作用） */
export function watchSize(element) {
  sizeObserver?.observe(element);
}

// 占满到窗口底边（ViewportFill 的 data-rune-viewport-fill）：量出自己的顶边在文档里的位置（不随滚动变），写成 --rune-fill-top，
// 高度由 CSS 按它算。窗口变化、上面的内容变高（查询栏换行、提示出现，文档的尺寸跟着变）时重新量
const fills = new Set();
let fillListening = false;

function measureFills() {
  for (const element of fills) {
    if (!element.isConnected) {
      fills.delete(element);
      continue;
    }
    const top = `${Math.round(element.getBoundingClientRect().top + window.scrollY)}px`;
    if (element.style.getPropertyValue('--rune-fill-top') !== top) element.style.setProperty('--rune-fill-top', top);
  }
}

/** 开始量一个占满到窗口底边的区域（插入时调用；重复调用无副作用） */
export function watchViewportFill(element) {
  fills.add(element);
  if (!fillListening) {
    fillListening = true;
    window.addEventListener('resize', measureFills);
    if (typeof ResizeObserver !== 'undefined') new ResizeObserver(measureFills).observe(document.body);
  }
  measureFills();
}

// 截断的文字（Ellipsis）：data-rune-ellipsis-text 的内容超出了（单行比宽、多行比高，差 1px 以内是小数取整）就在外层写 data-truncated。
// 截断时展开按钮由 CSS 显示；外层是 data-rune-ellipsis="tooltip" 时文字可聚焦（键盘用户也能看提示里的全文），提示只在截断时弹出（hover-layers）。
// 展开着（data-expanded）时不量：保持截断的标记，“收起”一直在
const ellipsisObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target } of entries) measureEllipsis(target);
});

/** 开始量一段截断的文字（插入时调用；重新渲染出新的文字元素时再调用） */
export function watchEllipsis(text) {
  ellipsisObserver?.observe(text);
  measureEllipsis(text);
}

function measureEllipsis(text) {
  const root = text.closest('[data-rune-ellipsis]');
  if (!root || !text.isConnected || root.hasAttribute('data-expanded')) return;
  const over = text.scrollWidth > text.clientWidth + 1 || text.scrollHeight > text.clientHeight + 1;
  root.toggleAttribute('data-truncated', over);
  if (over && root.dataset.runeEllipsis === 'tooltip') text.setAttribute('tabindex', '0');
  else text.removeAttribute('tabindex');
}

// 跑马灯（Marquee 的 data-rune-marquee="速度 px/s"）：一份内容（data-rune-marquee-group，含它后面的间距）的长度除以速度是一圈的时长，
// 写成 --rune-marquee-duration；内容、宽度变了重新量。减少动态效果时不滚动，容器可以聚焦（键盘也能手动滚动）
const marqueeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target } of entries) measureMarquee(target.closest('[data-rune-marquee]'));
});

export function watchMarquee(root) {
  const group = root.querySelector('[data-rune-marquee-group]');
  if (group) marqueeObserver?.observe(group);
  measureMarquee(root);
}

function measureMarquee(root) {
  const group = root?.querySelector('[data-rune-marquee-group]');
  if (!group) return;
  const vertical = root.dataset.direction === 'up' || root.dataset.direction === 'down';
  const length = Math.max(vertical ? group.offsetHeight : group.offsetWidth, 1);
  const seconds = `${(length / Math.max(Number(root.dataset.runeMarquee) || 40, 1)).toFixed(2)}s`;
  const track = group.parentElement;
  if (track.style.getPropertyValue('--rune-marquee-duration') !== seconds) track.style.setProperty('--rune-marquee-duration', seconds);
  if (reducedMotion.matches) root.setAttribute('tabindex', '0');
  else root.removeAttribute('tabindex');
}

reducedMotion.addEventListener('change', () => document.querySelectorAll('[data-rune-marquee]').forEach(measureMarquee));
