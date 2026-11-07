// 图表的绘图区共用的状态：每个绘图区一份（量尺寸、悬停、平移缩放），C# 写在 data-chart 上的悬停数据按原文缓存。

/** 绘图区 → 它的状态 */
export const states = new WeakMap();

/** data-chart 里的数据：C# 重画后会变，按原文缓存 */
export function dataOf(plot, state) {
  const raw = plot.dataset.chart ?? '';
  if (raw !== state.raw) {
    state.raw = raw;
    try {
      state.data = raw ? JSON.parse(raw) : null;
    } catch {
      state.data = null;
    }
  }
  return state.data;
}

/** 三位小数（同 C# 的 SvgFormat） */
export const round = (value) => Math.round(value * 1000) / 1000;
