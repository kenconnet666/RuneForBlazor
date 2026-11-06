// 跟着滚动位置变的浮动：固钉（Affix）贴住时写 data-stuck，回到顶部（BackTop）滚过阈值才出现、点了滚回顶部。
// 都在浏览器里处理，滚动时没有往返。

import { reducedMotion, sendCommand } from './shared.js';

// 固钉（data-rune-affix="top|bottom"）：sticky 那层前面（贴底时是后面）有一个零高度的哨兵（data-rune-affix-sentinel），
// 滚过它就是贴住了。贴住的距离读 sticky 那层计算后的 top / bottom；data-target 是 sticky 相对的滚动容器的 id，不写时是视口。
// data-rune-affix-report 时贴住、离开发出 rune-command（stick / unstick）给 C#
const affixObservers = new WeakMap();

export function watchAffix(element) {
  if (affixObservers.has(element) || typeof IntersectionObserver === 'undefined') return;
  const bottom = element.dataset.runeAffix === 'bottom';
  const sentinel = bottom ? element.nextElementSibling : element.previousElementSibling;
  if (!sentinel?.hasAttribute('data-rune-affix-sentinel')) return;
  const style = getComputedStyle(element);
  const offset = parseFloat(bottom ? style.bottom : style.top) || 0;
  const root = element.dataset.target ? document.getElementById(element.dataset.target) : null;
  const observer = new IntersectionObserver(([entry]) => {
    if (!element.isConnected) {
      observer.disconnect();
      return;
    }
    const bounds = entry.rootBounds;
    const stuck = !entry.isIntersecting && (bottom
      ? entry.boundingClientRect.top > (bounds?.bottom ?? innerHeight)
      : entry.boundingClientRect.bottom < (bounds?.top ?? 0));
    if (stuck === element.hasAttribute('data-stuck')) return;
    element.toggleAttribute('data-stuck', stuck);
    if (element.hasAttribute('data-rune-affix-report')) sendCommand(element, stuck ? 'stick' : 'unstick');
  }, { root, rootMargin: bottom ? `0px 0px ${-offset}px 0px` : `${-offset}px 0px 0px 0px`, threshold: [0, 1] });
  observer.observe(sentinel);
  affixObservers.set(element, observer);
}

// 回到顶部（data-rune-back-top="阈值 px"）：滚过阈值写 data-shown（CSS 淡入）、去掉 aria-hidden；data-target 是滚动容器的 id，不写时是整页
const pageBackTops = new Set();
const watchedScrollers = new WeakSet();

const scrollerOf = (button) => (button.dataset.target ? document.getElementById(button.dataset.target) : document.scrollingElement);

function syncBackTop(button) {
  const shown = (scrollerOf(button)?.scrollTop ?? 0) > Number(button.dataset.runeBackTop || 400);
  if (shown === button.hasAttribute('data-shown')) return;
  button.toggleAttribute('data-shown', shown);
  if (shown) button.removeAttribute('aria-hidden');
  else button.setAttribute('aria-hidden', 'true');
}

function syncPageBackTops() {
  for (const button of pageBackTops) {
    if (button.isConnected) syncBackTop(button);
    else pageBackTops.delete(button);
  }
}

export function watchBackTop(button) {
  if (!button.dataset.target) {
    if (pageBackTops.size === 0) window.addEventListener('scroll', syncPageBackTops, { passive: true });
    pageBackTops.add(button);
  } else {
    const scroller = scrollerOf(button);
    if (scroller && !watchedScrollers.has(scroller)) {
      watchedScrollers.add(scroller);
      scroller.addEventListener('scroll', () => {
        for (const other of document.querySelectorAll(`[data-rune-back-top][data-target="${CSS.escape(scroller.id)}"]`)) syncBackTop(other);
      }, { passive: true });
    }
  }
  syncBackTop(button);
}

/** 点了回到顶部：平滑滚回（减少动态效果时直接跳回），焦点回到开头（整页是 body，容器是容器本身），键盘用户不用再往回 Tab */
export function clickBackTop(target) {
  const button = target.closest('[data-rune-back-top]');
  if (!button) return;
  const scroller = scrollerOf(button);
  if (!scroller) return;
  scroller.scrollTo({ top: 0, behavior: reducedMotion.matches ? 'instant' : 'smooth' });
  const focusTarget = button.dataset.target ? scroller : document.body;
  if (!focusTarget.hasAttribute('tabindex')) focusTarget.setAttribute('tabindex', '-1');
  focusTarget.focus({ preventScroll: true });
}
