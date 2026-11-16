// 拼图滑块验证（SlideCaptcha 的 data-rune-slide-captcha，同 vue-tsx-pro 的 SlideCaptcha）：拖动、键盘微调与轨迹都在这里，
// 松手时把 { x, track, duration } 交给 C#（rune-command 的 release）。位置只画在页面上（滑块、滑过的一段、拼图的 left），不往返。
//   - data-status（idle / verifying / success / error）与 data-challenge（这一张的标识）由 C# 写：只有 idle 且有图时能拖；
//     换了图、或从别的状态回到 idle 时滑块回到起点；变成 error 时滑块晃一下（减少动态效果时不晃）。
//   - data-travel 是拼图能走的像素（背景宽减拼图宽），data-width 是背景宽：位置换算成像素直接就是答案。
//   - 键盘：←/→ 挪 2px（从右到左排版时反过来）、Home/End、Enter/空格松手。

import { isRtl, sendCommand } from '../core/shared.js';

/** 滑块的宽度，也是滑块条的高度（同样式里的 HandleSize） */
const HANDLE = '2.75rem';
/** 键盘每次移动的距离（px） */
const KEY_STEP = 2;
/** 轨迹最多记多少个点，太长的拖动只留一部分 */
const MAX_POINTS = 240;

const states = new WeakMap();

const part = (root, name) => root.querySelector(`[data-rune-slide-${name}]`);
const clamp01 = (value) => Math.min(1, Math.max(0, value));
const travelOf = (root) => Number(root.dataset.travel) || 0;
const interactive = (root) => !!root.dataset.challenge && root.dataset.status === 'idle';

export function watchSlideCaptcha(root) {
  if (states.has(root)) return;
  const state = { position: 0, track: [], startedAt: 0, challenge: root.dataset.challenge, status: root.dataset.status };
  states.set(root, state);
  root.addEventListener('pointerdown', (event) => grab(root, state, event));
  root.addEventListener('keydown', (event) => onKeydown(root, state, event));
  new MutationObserver(() => sync(root, state)).observe(root, { attributes: true, attributeFilter: ['data-status', 'data-challenge'] });
  paint(root, state);
}

/** C# 换了图或状态：回到起点、没对准时晃一下 */
function sync(root, state) {
  const { challenge, status } = root.dataset;
  if (challenge !== state.challenge || (status === 'idle' && state.status !== 'idle')) {
    state.position = 0;
    state.track = [];
  }
  if (status === 'error' && state.status !== 'error' && !matchMedia('(prefers-reduced-motion: reduce)').matches) {
    part(root, 'handle')?.animate(
      [{ translate: '0' }, { translate: '-6px' }, { translate: '6px' }, { translate: '-4px' }, { translate: '4px' }, { translate: '0' }],
      { duration: 360 },
    );
  }
  state.challenge = challenge;
  state.status = status;
  paint(root, state);
}

const pixels = (root, state) => Math.round(state.position * travelOf(root));

/** 把位置画上去：滑块、滑过的一段、拼图，以及滑块的读屏值 */
function paint(root, state) {
  const position = state.position;
  const handle = part(root, 'handle');
  const fill = part(root, 'fill');
  const piece = part(root, 'piece');
  const hint = part(root, 'hint');
  const x = pixels(root, state);
  if (handle) {
    handle.style.left = `calc(${position} * (100% - ${HANDLE}))`;
    handle.setAttribute('aria-valuenow', String(x));
    const text = handle.dataset.valuetext;
    if (text) handle.setAttribute('aria-valuetext', text.replace('900001', String(x)));
  }
  if (fill) fill.style.width = `calc(${position} * (100% - ${HANDLE}) + ${HANDLE})`;
  const width = Number(root.dataset.width) || 0;
  if (piece && width) piece.style.left = `${(position * travelOf(root)) / width * 100}%`;
  // 拖开以后提示让位；判定中、有结果时提示是状态文字，一直显示
  hint?.toggleAttribute('data-hidden', position > 0 && root.dataset.status === 'idle');
}

function record(root, state) {
  if (!state.track.length) state.startedAt = performance.now();
  if (state.track.length < MAX_POINTS) state.track.push({ t: Math.round(performance.now() - state.startedAt), x: pixels(root, state) });
}

function release(root, state) {
  if (!state.track.length) return;
  const result = { x: pixels(root, state), track: state.track, duration: Math.round(performance.now() - state.startedAt) };
  state.track = [];
  sendCommand(root, 'release', JSON.stringify(result));
}

/** 拖动中跟手、不加过渡：滑块、滑过的一段、拼图都标上 data-dragging */
function setDragging(root, on) {
  for (const name of ['handle', 'fill', 'piece']) part(root, name)?.toggleAttribute('data-dragging', on);
}

function grab(root, state, event) {
  const handle = part(root, 'handle');
  const track = part(root, 'track');
  if (event.button !== 0 || !handle || !track || !handle.contains(event.target) || !interactive(root)) return;
  event.preventDefault();
  handle.focus();
  handle.setPointerCapture(event.pointerId);
  const room = Math.max(track.clientWidth - handle.offsetWidth, 1);
  const startX = event.clientX;
  const startPosition = state.position;
  state.track = [];
  record(root, state);
  setDragging(root, true);
  const move = (next) => {
    state.position = clamp01(startPosition + (next.clientX - startX) / room);
    record(root, state);
    paint(root, state);
  };
  const end = (next) => {
    handle.removeEventListener('pointermove', move);
    handle.removeEventListener('pointerup', end);
    handle.removeEventListener('pointercancel', end);
    if (handle.hasPointerCapture(next.pointerId)) handle.releasePointerCapture(next.pointerId);
    setDragging(root, false);
    if (next.type === 'pointercancel') {
      state.position = 0;
      state.track = [];
      paint(root, state);
    } else release(root, state);
  };
  handle.addEventListener('pointermove', move);
  handle.addEventListener('pointerup', end);
  handle.addEventListener('pointercancel', end);
}

function onKeydown(root, state, event) {
  const handle = part(root, 'handle');
  if (!handle || event.target !== handle || !interactive(root)) return;
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    release(root, state);
    return;
  }
  const step = KEY_STEP / Math.max(travelOf(root), 1);
  const rtl = isRtl(root);
  const forward = rtl ? 'ArrowLeft' : 'ArrowRight';
  const backward = rtl ? 'ArrowRight' : 'ArrowLeft';
  const next = event.key === forward ? clamp01(state.position + step)
    : event.key === backward ? clamp01(state.position - step)
    : event.key === 'Home' ? 0
    : event.key === 'End' ? 1
    : null;
  if (next === null) return;
  event.preventDefault();
  state.position = next;
  record(root, state);
  paint(root, state);
}
