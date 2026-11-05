// 通知区域（data-rune-toaster，同 vue-tsx-pro 的 Toaster）：F8 跳进区域；焦点在一条通知里时 Esc 关闭它；
// 关闭后焦点交给相邻的通知，没有时回到进入区域之前的位置；横向滑动超过宽度的 35% 就关闭；焦点进出区域时告诉 C#（暂停计时）。
// 关闭由 C# 做：这里发出冒泡的 rune-command（dismiss；区域上的 focus-enter / focus-leave）。

import { startMove } from './move.js';
import { reducedMotion, sendCommand, TABBABLE } from './shared.js';

/** 滑动超过宽度的这个比例就关闭 */
const SWIPE_RATIO = 0.35;

// 每个区域：焦点进来之前在哪里（最后一条被关闭后回到那里）
const outsideFocus = new WeakMap();

/** F8（同 Radix Toast）：有通知时焦点跳到通知区域；输入框里也能用 */
export function focusToaster(event) {
  if (event.key !== 'F8' || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
  // 页面上可能有几个区域（示例各自一个）：进有通知的那个
  const region = [...document.querySelectorAll('[data-rune-toaster]')].find((element) => element.querySelector('[data-rune-toast]'));
  if (!region) return;
  event.preventDefault();
  region.focus();
}

/** 焦点从外面进入区域：记下来处，告诉 C# 暂停计时 */
export function onToasterFocusIn(event) {
  const region = event.target instanceof Element ? event.target.closest('[data-rune-toaster]') : null;
  if (!region) return;
  const from = event.relatedTarget;
  if (from instanceof Node && region.contains(from)) return;
  if (from instanceof HTMLElement) outsideFocus.set(region, from);
  sendCommand(region, 'focus-enter');
}

/** 焦点离开区域（不是在区域里移动）：告诉 C# 继续计时 */
export function onToasterFocusOut(event) {
  const region = event.target instanceof Element ? event.target.closest('[data-rune-toaster]') : null;
  if (!region) return;
  const to = event.relatedTarget;
  if (to instanceof Node && region.contains(to)) return;
  sendCommand(region, 'focus-leave');
}

/** 焦点在一条通知里时按 Esc：关闭它（不能关闭的不处理） */
export function keyToast(event) {
  if (event.key !== 'Escape' || !(event.target instanceof Element)) return;
  const item = event.target.closest('[data-rune-toast][data-closable]');
  if (!item) return;
  event.preventDefault();
  event.stopPropagation();
  dismiss(item);
}

/** 点了通知的关闭按钮（C# 关闭）：焦点在它上面时先交出去 */
export function clickToastClose(target) {
  const item = target.closest('[data-rune-toast-close]')?.closest('[data-rune-toast]');
  if (item) moveFocusAway(item);
}

/** 横向拖动通知：跟手移动、渐隐；松开时过了阈值就滑出并关闭，不够就弹回 */
export function grabToast(event) {
  if (!(event.target instanceof Element) || event.target.closest('button, a, input, textarea, select')) return;
  const item = event.target.closest('[data-rune-toast][data-closable]');
  if (!item) return;
  const width = item.getBoundingClientRect().width;
  let dragging = false;
  startMove(event, {
    target: item,
    onMove: ({ deltaX }) => {
      // 动过 4px 才算拖：只是点一下（选文字）不跟手
      if (!dragging && Math.abs(deltaX) < 4) return;
      dragging = true;
      // 拖动中去掉进出场的 CSS 过渡，不然每一帧都在追赶
      item.style.transition = 'none';
      item.style.translate = `${deltaX}px 0`;
      item.style.opacity = String(Math.max(0, 1 - Math.abs(deltaX) / width));
    },
    onEnd: ({ deltaX, cancelled }) => {
      if (!dragging) return;
      const settled = () => {
        item.style.removeProperty('transition');
        item.style.removeProperty('translate');
        item.style.removeProperty('opacity');
      };
      const duration = reducedMotion.matches ? 0 : 180;
      if (!cancelled && Math.abs(deltaX) > width * SWIPE_RATIO) {
        const out = item.animate([{ translate: `${Math.sign(deltaX) * width}px 0`, opacity: 0 }], { duration, easing: 'ease-in', fill: 'forwards' });
        out.finished.then(() => {
          dismiss(item);
          out.cancel();
          item.style.opacity = '0';
        });
        return;
      }
      item.animate([{ translate: '0 0', opacity: 1 }], { duration, easing: 'ease-out' }).finished.then(settled);
      settled();
    },
  });
}

function dismiss(item) {
  moveFocusAway(item);
  sendCommand(item, 'dismiss');
}

// 一条通知要关闭：焦点在它里面时交给相邻的通知（后面的优先），没有时回到进入区域之前的位置
function moveFocusAway(item) {
  if (!item.contains(document.activeElement)) return;
  const region = item.closest('[data-rune-toaster]');
  const items = [...region.querySelectorAll('[data-rune-toast]:not([data-state="closing"])')];
  const index = items.indexOf(item);
  const neighbor = items[index + 1] ?? items[index - 1];
  const target = (neighbor && neighbor !== item ? neighbor.querySelector(TABBABLE) : null) ?? outsideFocus.get(region);
  if (target?.isConnected) target.focus();
  else region.focus();
}
