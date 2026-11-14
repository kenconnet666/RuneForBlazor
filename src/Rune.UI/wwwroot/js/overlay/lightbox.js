// 图片预览（Lightbox 的 data-rune-lightbox-viewport，同 vue-tsx-pro 的 Lightbox 与 usePanZoom）：平移、缩放、旋转都在这里，
// 当场写在内容（data-rune-lightbox-content）与图片（data-rune-lightbox-image）的行内样式上，不经过 C#。状态只有平移 x、y、缩放 scale 与旋转：
//   - 图片加载好、打开、视口大小变了（还没被缩放、拖动过时）、旋转、还原之后，整张放进视口并居中（不超过原大，四周留 24px）；
//     换一张图（C# 按地址重建图片元素）时方向回正，关上后下次打开重新放进视口
//   - 拖动平移（内容比视口大时边缘不进到视口里，比视口小时居中）；滚轮、触控板以指针为中心缩放，两指捏合缩放；双击放大两档，已放大时放回视口
//   - 工具栏的按钮（data-rune-lightbox-action：zoom-in / zoom-out / rotate-left / rotate-right / reset），缩放比例写进 data-rune-lightbox-zoom
//   - 键盘：+ / − 缩放，0 还原，R 向右旋转，↑↓ 平移；← / →：图片比视口宽时平移，否则点上一张、下一张的按钮（data-rune-lightbox-step，
//     C# 换图；从右到左排版时对调）
// 拖动、捏合期间视口上写 data-interacting（内容不要过渡）。

import { startMove } from '../core/move.js';

const MIN = 0.05;
const MAX = 8;
/** 每一档缩放的倍数（按钮、键盘；双击是两档） */
const STEP = 1.25;
const PADDING = 24;
/** 键盘方向键每次平移多少 px */
const KEY_PAN = 40;

const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

// 视口 → { x, y, scale, rotation, fitted（还没被用户缩放、拖动过）, image（已经排好的那张图） }
const states = new WeakMap();
const watchedViewports = new WeakSet();

const resizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target } of entries) {
    if (!target.isConnected) {
      resizeObserver.unobserve(target);
      continue;
    }
    const state = stateOf(target);
    if (state.fitted) fit(target, { instant: true });
    else apply(target, state);
  }
});

function stateOf(viewport) {
  let state = states.get(viewport);
  if (!state) {
    state = { x: 0, y: 0, scale: 1, rotation: 0, fitted: true, image: null };
    states.set(viewport, state);
  }
  return state;
}

const imageOf = (viewport) => viewport.querySelector('img[data-rune-lightbox-image]');
const contentOf = (viewport) => viewport.querySelector('[data-rune-lightbox-content]');
const sizeOf = (viewport) => ({ width: viewport.clientWidth, height: viewport.clientHeight });

/** 旋转之后图片占的框；图片还没加载好时为 null */
function boxOf(viewport) {
  const image = imageOf(viewport);
  if (!image || !image.naturalWidth) return null;
  const sideways = stateOf(viewport).rotation % 180 !== 0;
  return sideways
    ? { width: image.naturalHeight, height: image.naturalWidth }
    : { width: image.naturalWidth, height: image.naturalHeight };
}

/** 平移的限制：内容比视口大时边缘不进到视口里，比视口小时居中 */
function clampPan(state, viewport, box) {
  const axis = (offset, view, size) => (size <= view ? (view - size) / 2 : clamp(offset, view - size, 0));
  return { x: axis(state.x, viewport.width, box.width * state.scale), y: axis(state.y, viewport.height, box.height * state.scale), scale: state.scale };
}

/** 整张放进视口并居中的缩放：放得下的最大值，不超过原大 */
function fitScale(viewport, box) {
  const { width, height } = sizeOf(viewport);
  const fits = (room, size) => (size > 0 && room > 0 ? room / size : 1);
  return clamp(Math.min(fits(width - PADDING * 2, box.width), fits(height - PADDING * 2, box.height), 1), MIN, MAX);
}

/** 把状态写到元素上；instant 时不要过渡（新的一张、刚打开） */
function render(viewport, { instant = false } = {}) {
  const state = stateOf(viewport);
  const content = contentOf(viewport);
  const image = imageOf(viewport);
  const box = boxOf(viewport);
  if (!content || !image || !box) return;
  if (instant) content.style.transition = 'none';
  content.style.transform = `translate(${state.x}px, ${state.y}px) scale(${state.scale})`;
  content.style.width = `${box.width}px`;
  content.style.height = `${box.height}px`;
  image.style.width = `${image.naturalWidth}px`;
  image.style.height = `${image.naturalHeight}px`;
  image.style.transform = `translate(-50%, -50%) rotate(${state.rotation}deg)`;
  if (instant) {
    // 读一次布局，让“无过渡”生效后再恢复
    void content.offsetWidth;
    content.style.removeProperty('transition');
  }
  // 文字节点归 Blazor 管：只改它的值，不换节点
  const zoom = viewport.closest('dialog')?.querySelector('[data-rune-lightbox-zoom]')?.firstChild;
  if (zoom) zoom.nodeValue = `${Math.round(state.scale * 100)}%`;
}

/** 平移、缩放到 next（限制平移）；用户的操作，之后视口变了大小也不再重新放进视口 */
function apply(viewport, next) {
  const box = boxOf(viewport);
  if (!box) return;
  const state = stateOf(viewport);
  Object.assign(state, clampPan(next, sizeOf(viewport), box));
  render(viewport);
}

function fit(viewport, options) {
  const box = boxOf(viewport);
  if (!box) return;
  const state = stateOf(viewport);
  const { width, height } = sizeOf(viewport);
  if (width === 0 || height === 0) return;
  const scale = fitScale(viewport, box);
  Object.assign(state, { x: (width - box.width * scale) / 2, y: (height - box.height * scale) / 2, scale, fitted: true });
  render(viewport, options);
}

/** 以 origin（视口内坐标）为中心缩放到 scale：origin 下的内容点缩放前后不动 */
function zoomTo(viewport, scale, origin = { x: viewport.clientWidth / 2, y: viewport.clientHeight / 2 }) {
  const state = stateOf(viewport);
  const next = clamp(scale, MIN, MAX);
  if (next === state.scale) return;
  const ratio = next / state.scale;
  state.fitted = false;
  apply(viewport, { x: origin.x - (origin.x - state.x) * ratio, y: origin.y - (origin.y - state.y) * ratio, scale: next });
}

function panBy(viewport, dx, dy) {
  const state = stateOf(viewport);
  state.fitted = false;
  apply(viewport, { x: state.x + dx, y: state.y + dy, scale: state.scale });
}

function rotate(viewport, step) {
  const state = stateOf(viewport);
  state.rotation = (state.rotation + step * 90 + 360) % 360;
  fit(viewport);
}

function reset(viewport) {
  stateOf(viewport).rotation = 0;
  fit(viewport);
}

/** 事件的位置换成视口内坐标 */
function local(viewport, event) {
  const rect = viewport.getBoundingClientRect();
  return { x: event.clientX - rect.left, y: event.clientY - rect.top };
}

/** 图片加载好了（或插入时已经在缓存里）：新的一张方向回正，整张放进视口 */
export function showLightboxImage(image) {
  if (!(image instanceof HTMLImageElement) || !image.hasAttribute('data-rune-lightbox-image') || !image.naturalWidth) return;
  const viewport = image.closest('[data-rune-lightbox-viewport]');
  if (!viewport) return;
  const state = stateOf(viewport);
  if (state.image !== image) {
    state.image = image;
    state.rotation = 0;
  }
  fit(viewport, { instant: true });
}

/** 视口插入时装上指针、滚轮、键盘与工具栏的处理（重复调用无副作用） */
export function watchLightbox(viewport) {
  if (watchedViewports.has(viewport)) return;
  watchedViewports.add(viewport);
  resizeObserver?.observe(viewport);
  const dialog = viewport.closest('dialog');

  // 按着的触点：两个时捏合
  const pointers = new Map();
  let pinch = null;

  viewport.addEventListener('pointerdown', (event) => {
    if (event.button !== 0) return;
    pointers.set(event.pointerId, local(viewport, event));
    if (pointers.size === 2) {
      const [a, b] = pointers.values();
      pinch = { distance: Math.hypot(a.x - b.x, a.y - b.y), state: { ...stateOf(viewport) } };
      return;
    }
    if (pointers.size > 2) return;
    const start = { ...stateOf(viewport) };
    startMove(event, {
      target: viewport,
      onMove: ({ deltaX, deltaY }) => {
        viewport.setAttribute('data-interacting', '');
        if (pinch) return;
        stateOf(viewport).fitted = false;
        apply(viewport, { x: start.x + deltaX, y: start.y + deltaY, scale: start.scale });
      },
      onEnd: () => {
        pointers.clear();
        pinch = null;
        viewport.removeAttribute('data-interacting');
      },
    });
  });
  // 两个触点的移动：捏合（中心是两指的中点）
  viewport.addEventListener('pointermove', (event) => {
    if (!pointers.has(event.pointerId)) return;
    pointers.set(event.pointerId, local(viewport, event));
    if (!pinch) return;
    const [a, b] = pointers.values();
    const scale = clamp((pinch.state.scale * Math.hypot(a.x - b.x, a.y - b.y)) / pinch.distance, MIN, MAX);
    const ratio = scale / pinch.state.scale;
    const origin = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
    stateOf(viewport).fitted = false;
    apply(viewport, { x: origin.x - (origin.x - pinch.state.x) * ratio, y: origin.y - (origin.y - pinch.state.y) * ratio, scale });
  });
  const lift = (event) => {
    pointers.delete(event.pointerId);
    if (pointers.size < 2) pinch = null;
  };
  viewport.addEventListener('pointerup', lift);
  viewport.addEventListener('pointercancel', lift);

  // 触控板的捏合每次 delta 很小、鼠标滚轮一格约 100：按 delta 连续缩放，两种都顺手。
  // 要能 preventDefault（不让页面跟着滚、不让浏览器整页缩放）：不能是 passive
  viewport.addEventListener('wheel', (event) => {
    event.preventDefault();
    const factor = Math.exp(-event.deltaY * (event.deltaMode === 1 ? 0.05 : 0.002));
    zoomTo(viewport, stateOf(viewport).scale * factor, local(viewport, event));
  }, { passive: false });

  viewport.addEventListener('dblclick', (event) => {
    const box = boxOf(viewport);
    if (!box) return;
    if (stateOf(viewport).scale > fitScale(viewport, box) + 0.001) fit(viewport);
    else zoomTo(viewport, stateOf(viewport).scale * STEP * STEP, local(viewport, event));
  });

  viewport.addEventListener('keydown', (event) => {
    if (event.altKey || event.ctrlKey || event.metaKey) return;
    const state = stateOf(viewport);
    const actions = {
      '+': () => zoomTo(viewport, state.scale * STEP),
      '=': () => zoomTo(viewport, state.scale * STEP),
      '-': () => zoomTo(viewport, state.scale / STEP),
      0: () => reset(viewport),
      ArrowUp: () => panBy(viewport, 0, KEY_PAN),
      ArrowDown: () => panBy(viewport, 0, -KEY_PAN),
    };
    const action = actions[event.key];
    if (!action) return;
    event.preventDefault();
    action();
  });

  if (!dialog) return;
  dialog.addEventListener('keydown', (event) => {
    if (event.altKey || event.ctrlKey || event.metaKey || event.defaultPrevented) return;
    if (event.key === 'r' || event.key === 'R') {
      event.preventDefault();
      rotate(viewport, 1);
      return;
    }
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    event.preventDefault();
    const left = event.key === 'ArrowLeft';
    // 放大到比视口宽：在图片区里是平移
    const box = boxOf(viewport);
    if (viewport.contains(event.target) && box && box.width * stateOf(viewport).scale > viewport.clientWidth) {
      panBy(viewport, left ? KEY_PAN : -KEY_PAN, 0);
      return;
    }
    const rtl = getComputedStyle(dialog).direction === 'rtl';
    const step = left !== rtl ? 'previous' : 'next';
    dialog.querySelector(`[data-rune-lightbox-step="${step}"]`)?.click();
  });
  dialog.addEventListener('click', (event) => {
    const button = event.target instanceof Element ? event.target.closest('[data-rune-lightbox-action]') : null;
    if (!button || !dialog.contains(button)) return;
    const action = button.getAttribute('data-rune-lightbox-action');
    if (action === 'zoom-in') zoomTo(viewport, stateOf(viewport).scale * STEP);
    else if (action === 'zoom-out') zoomTo(viewport, stateOf(viewport).scale / STEP);
    else if (action === 'rotate-left') rotate(viewport, -1);
    else if (action === 'rotate-right') rotate(viewport, 1);
    else if (action === 'reset') reset(viewport);
  });
  // 关上：下次打开时重新放进视口、方向回正
  dialog.addEventListener('close', () => {
    // 很快关了再开时迟到的 close：已经又开着了，不动
    if (dialog.open) return;
    const state = stateOf(viewport);
    state.rotation = 0;
    state.fitted = true;
  });
}
