// 一行标签按宽度收起（data-rune-overflow，同 vue-tsx-pro 的 useOverflowItems）。

// 同 vue-tsx-pro 的 useOverflowItems（参考 rc-overflow）：所有标签都渲染，放不下的移出排版、藏起来但仍可测量；
// 宽度变了、标签加减了就重新量。量不到布局（宽度为 0）时全部显示。至少显示一个。

const overflowObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const entry of entries) scheduleOverflow(entry.target);
});

const watchedOverflow = new WeakSet();

const pendingOverflow = new Set();

export function watchOverflow(row) {
  if (!watchedOverflow.has(row)) {
    watchedOverflow.add(row);
    overflowObserver?.observe(row);
  }
  scheduleOverflow(row);
}

// 同一帧里的多次请求合成一次测量
export function scheduleOverflow(row) {
  if (pendingOverflow.size === 0) {
    requestAnimationFrame(() => {
      const rows = [...pendingOverflow];
      pendingOverflow.clear();
      rows.forEach(measureOverflow);
    });
  }
  pendingOverflow.add(row);
}

function measureOverflow(row) {
  if (!row.isConnected) return;
  const items = [...row.querySelectorAll(':scope > [data-rune-overflow-item]')];
  const rest = row.querySelector(':scope > [data-rune-overflow-rest]');
  const style = getComputedStyle(row);
  const available = row.clientWidth - (parseFloat(style.paddingInlineStart) || 0) - (parseFloat(style.paddingInlineEnd) || 0);
  const gap = parseFloat(style.columnGap) || 0;
  const widths = items.map((item) => item.getBoundingClientRect().width);
  const restWidth = rest?.getBoundingClientRect().width ?? 0;
  const visible = available <= 0 && widths.every((width) => width === 0)
    ? items.length
    : Math.max(fitCount(widths, restWidth, gap, available), Math.min(1, items.length));
  items.forEach((item, index) => item.toggleAttribute('data-overflow-hidden', index >= visible));
  if (!rest) return;
  const hidden = items.length - visible;
  rest.toggleAttribute('data-overflow-hidden', hidden === 0);
  const label = hidden > 0 ? JSON.parse(row.getAttribute('data-rune-more') ?? '[]')[hidden - 1] : null;
  // 文字没变时不写：写了会触发 DOM 变化，又排一次测量
  if (label && rest.textContent !== label) rest.textContent = label;
}

/** 都放得下时全部显示；否则留出“+N”的宽度后尽量多放。 */
function fitCount(widths, restWidth, gap, available) {
  const total = widths.reduce((sum, width, index) => sum + width + (index ? gap : 0), 0);
  if (total <= available) return widths.length;
  let used = restWidth;
  let count = 0;
  for (const width of widths) {
    const next = used + gap + width;
    if (next > available) break;
    used = next;
    count++;
  }
  return count;
}
