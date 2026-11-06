// 布局：网格里跨列的项按实际列数收窄；尺寸观察；占满到窗口底边的区域量出自己的顶边。

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
