// 图表的绘图区（[data-rune-chart]，同 vue-tsx-pro 的 usePlotArea）：图形由 C# 画，这里量尺寸，悬停、点击、平移缩放、导出见 chart/ 下各模块。
// 量尺寸：ResizeObserver 量出绘图区的内容尺寸，进入过视口附近（IntersectionObserver，提前 200px；打印前全部算看见）之后
// 以 rune-command size（"宽,高"，取整，变了才发）交给 C#，C# 按尺寸重排。一页很多图表时先画看得见的。
import { sendCommand } from '../../Rune.UI/js/core/shared.js';
import './chart/export.js';
import { watchPanZoom } from './chart/pan-zoom.js';
import { hover, onClick, onPointerMove } from './chart/pointer.js';
import { states } from './chart/state.js';

// 绘图区离视口多近就算看见了（px）：滚到之前先画好，进来时已经是画好的
const SEEN_MARGIN = 200;

const sizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target, contentRect } of entries) {
    const state = states.get(target);
    if (!state) continue;
    state.size = [Math.round(contentRect.width), Math.round(contentRect.height)];
    report(target, state);
  }
});

const seenObserver = typeof IntersectionObserver === 'undefined' ? null : new IntersectionObserver((entries) => {
  for (const entry of entries) {
    if (!entry.isIntersecting) continue;
    seenObserver.unobserve(entry.target);
    const state = states.get(entry.target);
    if (!state) continue;
    state.seen = true;
    report(entry.target, state);
  }
}, { rootMargin: `${SEEN_MARGIN}px` });

addEventListener('beforeprint', () => {
  for (const plot of document.querySelectorAll('[data-rune-chart]')) {
    const state = states.get(plot);
    if (state && !state.seen) {
      state.seen = true;
      report(plot, state);
    }
  }
});

/** 看见了、量到了大小、和上次交出去的不同：交给 C# */
function report(plot, state) {
  if (!state.seen || !state.size) return;
  const [width, height] = state.size;
  if (width === 0 || height === 0) return;
  const text = `${width},${height}`;
  if (text === state.reported) return;
  state.reported = text;
  sendCommand(plot, 'size', text);
}

const near = (element) => {
  const box = element.getBoundingClientRect();
  return box.top < innerHeight + SEEN_MARGIN && box.bottom > -SEEN_MARGIN && box.left < innerWidth + SEEN_MARGIN && box.right > -SEEN_MARGIN;
};

/** 开始管理一个绘图区（插入时调用；重复调用无副作用） */
export function watchChart(plot) {
  if (states.has(plot)) return;
  // 没有 IntersectionObserver 的环境、挂上时就在视口附近的：立即算看见（不等观察器的第一次回调）
  const state = { seen: !seenObserver || near(plot), size: null, reported: null, data: null, raw: null, hovered: null };
  states.set(plot, state);
  if (!state.seen) seenObserver.observe(plot);
  if (sizeObserver) sizeObserver.observe(plot);
  else {
    state.size = [Math.round(plot.clientWidth), Math.round(plot.clientHeight)];
    report(plot, state);
  }
  plot.addEventListener('pointermove', (event) => onPointerMove(plot, state, event));
  plot.addEventListener('pointerleave', () => hover(plot, state, null));
  plot.addEventListener('click', (event) => onClick(plot, state, event));
  watchPanZoom(plot, state);
}
