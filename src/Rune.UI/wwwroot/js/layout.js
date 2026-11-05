// 布局：网格里跨列的项按实际列数收窄。

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
