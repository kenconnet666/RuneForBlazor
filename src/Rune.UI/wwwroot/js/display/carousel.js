// 轮播（data-rune-carousel，同 vue-tsx-pro 的 Carousel）：滚动区域是原生横向滚动加 scroll-snap，这里管
//   - 上一张 / 下一张（data-carousel-step="-1|1"）、页点（data-carousel-dot="i"）：滚到那一张；loop 时绕回另一头
//   - 滚动停下（最后一次 scroll 之后 100 ms）时按位置算出最前面那张：页点、箭头的状态当场改，发 rune-command carousel-index 交给 C#
//   - C# 换了序号（data-index）：滚过去
//   - 自动播放（data-autoplay 毫秒）：指针停在上面、焦点进到里面、点了暂停（data-paused）时停；偏好减少动态效果时一开始就是暂停的
// 静态 SSR 也一样能用（只是序号不回到 C#）。
import { sendCommand } from '../core/shared.js';

const carousels = new WeakMap();
const reducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

/** 开始管理一个轮播（插入时调用；重复调用无副作用） */
export function watchCarousel(root) {
  if (carousels.has(root)) return;
  const viewport = root.querySelector('[data-carousel-viewport]');
  if (!viewport) return;
  const state = { root, viewport, index: Number(root.getAttribute('data-index')) || 0, hovering: false, focused: false, timer: 0, scrollTimer: 0 };
  carousels.set(root, state);
  if (reducedMotion()) root.toggleAttribute('data-paused', true);

  root.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target.closest('[data-carousel-step], [data-carousel-dot], [data-carousel-pause]') : null;
    if (!target || !root.contains(target)) return;
    if (target.hasAttribute('data-carousel-pause')) {
      root.toggleAttribute('data-paused');
      // 点了“播放”一定恢复：焦点还在这个按钮上，不能因为“焦点在里面”又停着
      if (!root.hasAttribute('data-paused')) state.focused = false;
      sync(state);
    } else if (target.hasAttribute('data-carousel-dot')) go(state, Number(target.getAttribute('data-carousel-dot')));
    else go(state, state.index + Number(target.getAttribute('data-carousel-step')));
  });
  viewport.addEventListener('scroll', () => {
    clearTimeout(state.scrollTimer);
    state.scrollTimer = setTimeout(() => settle(state, nearest(state)), 100);
  }, { passive: true });
  root.addEventListener('pointerenter', (event) => {
    if (event.pointerType !== 'mouse') return;
    state.hovering = true;
    sync(state);
  });
  root.addEventListener('pointerleave', () => {
    state.hovering = false;
    sync(state);
  });
  root.addEventListener('focusin', (event) => {
    // 只有键盘聚焦才停：用指针点翻页按钮得到的焦点不算，不然点一下自动播放就一直停着
    if (!(event.target instanceof Element) || !event.target.matches(':focus-visible')) return;
    state.focused = true;
    sync(state);
  });
  root.addEventListener('focusout', (event) => {
    if (root.contains(event.relatedTarget)) return;
    state.focused = false;
    sync(state);
  });
  new MutationObserver(() => {
    const next = Number(root.getAttribute('data-index')) || 0;
    if (next !== state.index) {
      state.index = next;
      scrollTo(state, next);
    }
    sync(state);
  }).observe(root, { attributes: true, attributeFilter: ['data-index', 'data-last', 'data-autoplay'] });

  if (state.index > 0) scrollTo(state, state.index, true);
  sync(state);
}

const last = (state) => Number(state.root.getAttribute('data-last')) || 0;
const loops = (state) => state.root.hasAttribute('data-loop');
const interval = (state) => Number(state.root.getAttribute('data-autoplay')) || 0;
const playing = (state) => interval(state) > 0 && last(state) > 0 && !state.root.hasAttribute('data-paused') && !state.hovering && !state.focused;

/** 第 i 张对齐到起始边时的 scrollLeft（从右到左排版时是负数） */
function offsetOf(viewport, i) {
  const slide = viewport.children[i];
  if (!slide) return 0;
  return getComputedStyle(viewport).direction === 'rtl' ? slide.offsetLeft + slide.offsetWidth - viewport.clientWidth : slide.offsetLeft;
}

/** 滚动停在哪一张：离哪张对齐的位置最近；滚到底就是最后一张 */
function nearest(state) {
  const { viewport } = state;
  if (Math.abs(viewport.scrollLeft) >= viewport.scrollWidth - viewport.clientWidth - 1) return last(state);
  let best = 0;
  for (let i = 1; i <= last(state); i++) {
    if (Math.abs(viewport.scrollLeft - offsetOf(viewport, i)) < Math.abs(viewport.scrollLeft - offsetOf(viewport, best))) best = i;
  }
  return best;
}

function scrollTo(state, i, instant = false) {
  state.viewport.scrollTo({ left: offsetOf(state.viewport, i), behavior: instant || reducedMotion() ? 'instant' : 'smooth' });
}

/** 换到第 target 张：loop 时绕回另一头 */
function go(state, target) {
  const count = last(state) + 1;
  const next = loops(state) ? (target + count) % count : Math.min(Math.max(target, 0), last(state));
  settle(state, next);
  scrollTo(state, next);
}

/** 记下最前面那张：页点、箭头当场改，交给 C# */
function settle(state, index) {
  const changed = index !== state.index;
  state.index = index;
  if (changed) sendCommand(state.root, 'carousel-index', String(index));
  sync(state);
}

/** 页点、箭头、暂停按钮、播报与自动播放的计时，按当前的状态 */
function sync(state) {
  const { root, viewport, index } = state;
  for (const dot of root.querySelectorAll('[data-carousel-dot]')) {
    const current = Number(dot.getAttribute('data-carousel-dot')) === index;
    dot.toggleAttribute('data-active', current);
    if (current) dot.setAttribute('aria-current', 'true');
    else dot.removeAttribute('aria-current');
  }
  if (!loops(state)) {
    for (const arrow of root.querySelectorAll('[data-carousel-step]')) {
      const step = Number(arrow.getAttribute('data-carousel-step'));
      arrow.toggleAttribute('disabled', step < 0 ? index <= 0 : index >= last(state));
    }
  }
  const pause = root.querySelector('[data-carousel-pause]');
  if (pause) pause.setAttribute('aria-label', pause.getAttribute(root.hasAttribute('data-paused') ? 'data-label-play' : 'data-label-pause'));
  const play = playing(state);
  // 自动播放时关掉播报，不然每换一张都念
  viewport.setAttribute('aria-live', play ? 'off' : 'polite');
  clearTimeout(state.timer);
  if (play) state.timer = setTimeout(() => go(state, state.index + 1), interval(state));
}
