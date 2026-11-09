// 拖动（同 vue-tsx-pro 的 utils/dom/move.ts，参考 React Aria 的 useMove）：在 pointerdown 里调用 startMove，之后的移动、松开、取消都由它跟踪。
//   - setPointerCapture：指针移出元素、移出窗口也能继续拖
//   - pointercancel、失去捕获（系统手势、弹窗打断）按结束处理，不会卡在拖动状态
//   - 拖动期间禁止选中文字；只响应主按钮（鼠标左键、触摸、笔尖）
// 元素自己要设 touch-action（如 none），否则触摸拖动会被浏览器当成页面滚动。键盘替代由组件自己提供（WCAG 2.5.7）。
// Slider、Splitter、Tree 的拖动用它。

/**
 * 开始跟踪按下的这个指针。options：
 *   target   捕获指针、接收后续事件的元素（事件是委托在 document 上接住的，所以要显式给）
 *   onMove   ({ clientX, clientY, deltaX, deltaY }) => void
 *   onEnd    ({ ...最后的位置, cancelled }) => void
 *   threshold 移动超过这么多像素才捕获指针（按下的元素既能点也能拖时用，如 Tree 的行）：捕获后松开时的 click 落在 target 上，
 *            里面的按钮（展开箭头、勾选框）收不到；只是点一下的不捕获，click 照常落在点到的元素上。默认 0（按下就捕获）
 * 返回提前结束的函数。
 */
export function startMove(event, { target, onMove, onEnd, threshold = 0 }) {
  if (event.button !== 0) return () => {};
  const { pointerId, clientX: startX, clientY: startY } = event;
  let last = { clientX: startX, clientY: startY, deltaX: 0, deltaY: 0 };
  let finished = false;

  // 捕获失败（指针已经松开、元素不在文档里）时照常用元素上的事件，只是移出元素后收不到
  let captured = false;
  const capture = () => {
    captured = true;
    try {
      target.setPointerCapture(pointerId);
    } catch {}
  };
  if (threshold <= 0) capture();
  const root = document.documentElement;
  const userSelect = root.style.userSelect;
  root.style.userSelect = 'none';

  const move = (moved) => {
    if (moved.pointerId !== pointerId) return;
    last = { clientX: moved.clientX, clientY: moved.clientY, deltaX: moved.clientX - startX, deltaY: moved.clientY - startY };
    if (!captured && Math.hypot(last.deltaX, last.deltaY) >= threshold) capture();
    onMove(last);
  };
  const finish = (cancelled) => {
    if (finished) return;
    finished = true;
    target.removeEventListener('pointermove', move);
    target.removeEventListener('pointerup', up);
    target.removeEventListener('pointercancel', cancel);
    target.removeEventListener('lostpointercapture', cancel);
    root.style.userSelect = userSelect;
    if (target.hasPointerCapture?.(pointerId)) target.releasePointerCapture(pointerId);
    onEnd?.({ ...last, cancelled });
  };
  const up = (released) => released.pointerId === pointerId && finish(false);
  const cancel = (cancelled) => cancelled.pointerId === pointerId && finish(true);

  target.addEventListener('pointermove', move);
  target.addEventListener('pointerup', up);
  target.addEventListener('pointercancel', cancel);
  target.addEventListener('lostpointercapture', cancel);
  return () => finish(true);
}
