// 平移缩放（网络图、地图，同 vue-tsx-pro 的 usePanZoom）：拖动平移、按住 Ctrl 滚轮缩放、双击放大一档或回到全图，地图右上角的按钮。
// 变换写在 g[data-pan-zoom] 的 transform 上（C# 渲染的初值不变，重画时不会被拉回）；只按滚轮时浮出提示，页面照常滚动。
import { dataOf, round, states } from './state.js';

// 缩放的范围与每一档的倍数：悬停数据的 panZoom（网络图 0.3–4、1.25 倍，地图 1–12、1.5 倍）
const limitsOf = (plot, state) => dataOf(plot, state)?.panZoom ?? { min: 0.3, max: 4, step: 1.25 };

function viewOf(state) {
  return (state.view ??= { x: 0, y: 0, scale: 1 });
}

/**
 * 应用平移缩放：g[data-pan-zoom] 整个缩放（区域、连线；连线用 non-scaling-stroke 不变粗）；
 * 带 data-follow-x、data-follow-y 的元素（地点、名字）只跟着移动、不变大
 */
function applyView(plot, state, next) {
  const view = viewOf(state);
  Object.assign(view, next);
  const transform = `translate(${round(view.x)} ${round(view.y)}) scale(${round(view.scale)})`;
  plot.querySelectorAll('[data-pan-zoom]').forEach((element) => element.setAttribute('transform', transform));
  for (const element of plot.querySelectorAll('[data-follow-x]')) {
    const x = Number(element.dataset.followX);
    const y = Number(element.dataset.followY);
    element.setAttribute('transform', `translate(${round(x * (view.scale - 1) + view.x)} ${round(y * (view.scale - 1) + view.y)})`);
  }
}

function zoomAround(plot, state, scale, origin) {
  const view = viewOf(state);
  const { min, max } = limitsOf(plot, state);
  const next = Math.min(max, Math.max(min, scale));
  const ratio = next / view.scale;
  applyView(plot, state, { x: origin.x - (origin.x - view.x) * ratio, y: origin.y - (origin.y - view.y) * ratio, scale: next });
}

export function watchPanZoom(plot, state) {
  const local = (event) => {
    const box = plot.querySelector('[data-chart-viewport]')?.getBoundingClientRect() ?? plot.getBoundingClientRect();
    return { x: event.clientX - box.left, y: event.clientY - box.top };
  };
  const enabled = (event) => dataOf(plot, state)?.panZoom && event.target instanceof Element && event.target.closest('[data-chart-viewport]');
  plot.addEventListener('wheel', (event) => {
    if (!enabled(event)) return;
    if (!event.ctrlKey && !event.metaKey) {
      const hint = plot.closest('[role="figure"]')?.querySelector('[data-part="wheelHint"]');
      hint?.setAttribute('data-open', '');
      clearTimeout(state.hintTimer);
      state.hintTimer = setTimeout(() => hint?.removeAttribute('data-open'), 1500);
      return;
    }
    event.preventDefault();
    // 触控板的捏合每次 delta 很小、鼠标滚轮一格约 100：按 delta 连续缩放
    zoomAround(plot, state, viewOf(state).scale * Math.exp(-event.deltaY * (event.deltaMode === 1 ? 0.05 : 0.002)), local(event));
  }, { passive: false });
  plot.addEventListener('pointerdown', (event) => {
    // 拖过之后不一定有 click（松开在别的元素上）：每次按下重新算
    state.dragged = false;
    if (event.button !== 0 || !enabled(event)) return;
    const start = { ...viewOf(state) };
    const origin = { x: event.clientX, y: event.clientY };
    let moving = false;
    const move = (next) => {
      const dx = next.clientX - origin.x;
      const dy = next.clientY - origin.y;
      if (!moving && Math.hypot(dx, dy) < 3) return;
      moving = true;
      applyView(plot, state, { x: start.x + dx, y: start.y + dy });
    };
    const end = () => {
      removeEventListener('pointermove', move);
      removeEventListener('pointerup', end);
      removeEventListener('pointercancel', end);
      state.dragged = moving;
    };
    addEventListener('pointermove', move);
    addEventListener('pointerup', end);
    addEventListener('pointercancel', end);
  });
  plot.addEventListener('dblclick', (event) => {
    if (!enabled(event) || event.target.closest('[data-part="node"]')) return;
    const view = viewOf(state);
    const { step } = limitsOf(plot, state);
    if (view.scale > 1) applyView(plot, state, { x: 0, y: 0, scale: 1 });
    else zoomAround(plot, state, view.scale * step * step, local(event));
  });
}

// 缩放按钮（地图右上角）：data-chart-zoom 是 in、out、fit，作用在同一个 figure 的绘图区上
document.addEventListener('click', (event) => {
  const button = event.target instanceof Element ? event.target.closest('[data-chart-zoom]') : null;
  const plot = button?.closest('[role="figure"]')?.querySelector('[data-rune-chart]');
  const state = plot && states.get(plot);
  if (!state) return;
  const { step } = limitsOf(plot, state);
  const center = { x: plot.clientWidth / 2, y: plot.clientHeight / 2 };
  const action = button.dataset.chartZoom;
  if (action === 'fit') applyView(plot, state, { x: 0, y: 0, scale: 1 });
  else zoomAround(plot, state, viewOf(state).scale * (action === 'in' ? step : 1 / step), center);
});
