// 宽度调整手柄（data-rune-resize，同 vue-tsx-pro 的 ResizeHandle）：拖动、← / →（横向手柄是 ↑ / ↓）、双击回到 data-default，
// 都在这里当场改父元素的尺寸（行内的 width / height），夹在 aria-valuemin、aria-valuemax 之间；一次操作结束（松开、按键、双击）
// 才发出冒泡的 rune-resize（size）交给 C#。从右到左排版下横向方向自动镜像；data-edge="start" 时往起始方向拖是变宽。

import { startMove } from './move.js';

const watched = new WeakSet();

const horizontalOf = (handle) => handle.getAttribute('aria-orientation') === 'horizontal';

const sizeOf = (handle) => {
  const rect = handle.parentElement.getBoundingClientRect();
  return horizontalOf(handle) ? rect.height : rect.width;
};

function fit(handle, size) {
  const min = Number(handle.getAttribute('aria-valuemin') ?? 0);
  const maxText = handle.getAttribute('aria-valuemax');
  // 没给上限时最多到视口减去 48px：拖得再宽也留一条能点到外面（同 vue-tsx-pro 的抽屉）
  const viewport = horizontalOf(handle) ? window.innerHeight : window.innerWidth;
  const max = maxText === null ? viewport - 48 : Number(maxText);
  return Math.round(Math.min(Math.max(size, min), max));
}

/** 改父元素的尺寸并更新当前值；按档位给的最大宽度（抽屉的 30rem）这时不再适用，上限由手柄自己夹 */
function apply(handle, size) {
  const style = handle.parentElement.style;
  if (horizontalOf(handle)) {
    style.height = `${size}px`;
    style.maxHeight = 'none';
  } else {
    style.width = `${size}px`;
    style.maxWidth = 'none';
  }
  handle.setAttribute('aria-valuenow', String(size));
}

function commit(handle, size) {
  handle.dispatchEvent(new CustomEvent('rune-resize', { bubbles: true, detail: { size } }));
}

/** 往变宽方向是 +1：起始边的手柄方向相反；横线不受从右到左排版的影响 */
function signOf(handle) {
  const rtl = !horizontalOf(handle) && getComputedStyle(handle.parentElement).direction === 'rtl';
  return (rtl ? -1 : 1) * (handle.dataset.edge === 'start' ? -1 : 1);
}

/** 开始管理一个手柄（插入时调用）：没给宽度时按父元素量出当前值（可聚焦的分隔条必须有 aria-valuenow） */
export function watchResizeHandle(handle) {
  if (watched.has(handle) || !handle.parentElement) return;
  watched.add(handle);
  new ResizeObserver(() => {
    const size = String(Math.round(sizeOf(handle)));
    if (handle.getAttribute('aria-valuenow') !== size) handle.setAttribute('aria-valuenow', size);
  }).observe(handle.parentElement);
}

export function grabResizeHandle(event) {
  if (event.button !== 0 || !(event.target instanceof Element) || !event.target.matches('[data-rune-resize]')) return;
  const handle = event.target;
  const from = sizeOf(handle);
  const sign = signOf(handle);
  const horizontal = horizontalOf(handle);
  let size = Math.round(from);
  event.preventDefault();
  startMove(event, {
    target: handle,
    onMove: ({ deltaX, deltaY }) => {
      size = fit(handle, from + sign * (horizontal ? deltaY : deltaX));
      apply(handle, size);
    },
    onEnd: () => commit(handle, size),
  });
}

export function keyResizeHandle(event) {
  if (!(event.target instanceof Element) || !event.target.matches('[data-rune-resize]')) return;
  const handle = event.target;
  const horizontal = horizontalOf(handle);
  const rtl = !horizontal && getComputedStyle(handle.parentElement).direction === 'rtl';
  const forward = horizontal ? 'ArrowDown' : rtl ? 'ArrowLeft' : 'ArrowRight';
  const backward = horizontal ? 'ArrowUp' : rtl ? 'ArrowRight' : 'ArrowLeft';
  const step = Number(handle.dataset.step ?? 16);
  const delta = (event.key === forward ? step : event.key === backward ? -step : 0) * (handle.dataset.edge === 'start' ? -1 : 1);
  if (!delta) return;
  event.preventDefault();
  const size = fit(handle, sizeOf(handle) + delta);
  apply(handle, size);
  commit(handle, size);
}

export function resetResizeHandle(event) {
  if (!(event.target instanceof Element) || !event.target.matches('[data-rune-resize][data-default]')) return;
  const handle = event.target;
  const size = fit(handle, Number(handle.dataset.default));
  apply(handle, size);
  commit(handle, size);
}
