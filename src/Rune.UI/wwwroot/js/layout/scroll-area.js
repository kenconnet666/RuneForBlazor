// 滚动区域（data-rune-scroll-area，同 vue-tsx-pro 的 ScrollArea / OverlayScrollbar）：原生滚动，浮层滚动条。
// 这里管滚动条的一切：按视口的尺寸与滚动位置写滑块的大小与位置（行内样式，不经过渲染），滚动、悬停时显示（轨道的 data-state），
// 停下后淡出；拖动滑块、点轨道的空白处翻一页、滚轮落在滚动条上时转交给视口；内容超出时视口进 Tab 顺序（data-focusable）。
// 组件只渲染结构：视口是 [data-rune-scroll-viewport]，轨道是 [data-rune-scrollbar]（data-axis 为 x / y），滑块是轨道的第一个子元素。
// 整页的浮层滚动条（data-rune-page-scrollbar，PageScrollbar）是同一套：视口是 document.scrollingElement，滚动事件在 window 上，
// 轨道本身就是指针移入时显示的范围（窗口边缘）。

import { startMove } from '../core/move.js';

/** 滑块的最小长度（px）：内容再长也能抓得住 */
const MIN_THUMB = 20;

const watched = new WeakSet();

const isRtl = (element) => getComputedStyle(element).direction === 'rtl';

/** 开始管理一个滚动区域（插入时调用；重复调用无副作用） */
export function watchScrollArea(root) {
  if (watched.has(root)) return;
  const viewport = root.querySelector(':scope > [data-rune-scroll-viewport]');
  const content = viewport?.firstElementChild;
  if (!viewport || !content) return;
  watched.add(root);
  attach(root, viewport, content, viewport, [...root.querySelectorAll(':scope > [data-rune-scrollbar]')]);
}

let page = null;

/** 开始管理整页的浮层滚动条（插入时调用）：同一时间只有一条，换了元素（增强导航）时先拆掉旧的 */
export function watchPageScrollbar(track) {
  if (page?.track === track) return;
  page?.detach();
  page = { track, detach: attach(track, document.scrollingElement, document.body, window, [track]) };
}

/**
 * 管理一组滚动条。root：指针移入时显示的范围；viewport：滚动的元素；content：内容（尺寸变化时重新量）；
 * scroller：发出 scroll 事件的对象（整页时是 window）；tracks：轨道。返回拆除函数（摘掉挂在 window 上的监听）。
 */
function attach(root, viewport, content, scroller, tracks) {
  const visibility = root.dataset.visibility ?? 'hover';
  const hideDelay = Number(root.dataset.hideDelay ?? 600);
  const bars = tracks.map((track) => ({
    track,
    thumb: track.firstElementChild,
    axis: track.dataset.axis,
    overflowing: false,
  }));
  // 整页：模态框打开锁住滚动时（overflow: hidden）不显示
  const locked = () =>
    scroller === window && [document.documentElement, document.body].some((element) => getComputedStyle(element).overflowY === 'hidden');
  let active = false;
  let hovering = false;
  let dragging = false;
  let hideTimer;

  /** 视口在这个方向的尺寸与滚动位置；横向时滚动位置取绝对值（从右到左排版时 scrollLeft 为负） */
  const measure = (axis) =>
    axis === 'y'
      ? { view: viewport.clientHeight, size: viewport.scrollHeight, position: viewport.scrollTop }
      : { view: viewport.clientWidth, size: viewport.scrollWidth, position: Math.abs(viewport.scrollLeft) };
  const trackLength = (bar) => (bar.axis === 'y' ? bar.track.clientHeight : bar.track.clientWidth);
  const thumbLength = (view, size, track) => Math.max(MIN_THUMB, (view / size) * track);
  /** 从右到左排版时横向的方向反过来 */
  const sign = (axis) => (axis === 'x' && isRtl(viewport) ? -1 : 1);

  function paint() {
    // 看不见的（收起的折叠面板里）不量：在这种子树里读尺寸，浏览器每次都要为它单独排一遍版；显示出来时 ResizeObserver 会再通知
    if (viewport.checkVisibility?.() === false) return;
    const scrollLocked = locked();
    for (const bar of bars) {
      const { view, size, position } = measure(bar.axis);
      const track = trackLength(bar);
      const length = thumbLength(view, size, track);
      const offset = size > view ? (position / (size - view)) * (track - length) : 0;
      bar.overflowing = !scrollLocked && size > view + 1;
      if (bar.axis === 'y') {
        bar.thumb.style.height = `${length}px`;
        bar.thumb.style.transform = `translateY(${offset}px)`;
      } else {
        bar.thumb.style.width = `${length}px`;
        bar.thumb.style.transform = `translateX(${sign('x') * offset}px)`;
      }
    }
    // 能滚动时视口才进 Tab 顺序：没超出的侧栏、代码块不多一个空的 Tab 位
    if (root.hasAttribute('data-focusable')) {
      const overflowing = bars.some((bar) => bar.overflowing);
      if (overflowing !== viewport.hasAttribute('tabindex')) {
        if (overflowing) viewport.setAttribute('tabindex', '0');
        else viewport.removeAttribute('tabindex');
      }
    }
    showState();
  }

  function showState() {
    for (const bar of bars) {
      const state = bar.overflowing && (visibility === 'always' || active) ? 'visible' : 'hidden';
      if (bar.track.dataset.state !== state) bar.track.dataset.state = state;
    }
  }

  function show() {
    active = true;
    showState();
    scheduleHide();
  }

  function scheduleHide() {
    clearTimeout(hideTimer);
    hideTimer = setTimeout(() => {
      if (dragging || (visibility === 'hover' && hovering)) return;
      active = false;
      showState();
    }, hideDelay);
  }

  let frame = 0;
  const paintLater = () => {
    if (frame) return;
    frame = requestAnimationFrame(() => {
      frame = 0;
      paint();
    });
  };

  const onScroll = () => {
    paint();
    show();
  };
  scroller.addEventListener('scroll', onScroll, { passive: true });
  // 整页：视口就是窗口，窗口变了大小要重新量（html 元素的尺寸跟着内容走，ResizeObserver 看不到）
  if (scroller === window) window.addEventListener('resize', paintLater);
  root.addEventListener('pointerenter', () => {
    hovering = true;
    if (visibility !== 'hover') return;
    // 绝对定位的后代撑出、收回溢出时内容尺寸不变，ResizeObserver 察觉不到：显示前重新量
    paint();
    show();
  });
  root.addEventListener('pointerleave', () => {
    hovering = false;
    scheduleHide();
  });
  const observer = new ResizeObserver(paintLater);
  observer.observe(viewport);
  observer.observe(content);

  for (const bar of bars) {
    // 点在轨道的空白处：朝那一侧翻一页
    bar.track.addEventListener('pointerdown', (event) => {
      if (event.button !== 0 || event.target !== bar.track) return;
      const rect = bar.thumb.getBoundingClientRect();
      const { view } = measure(bar.axis);
      const rtl = sign(bar.axis) === -1;
      const before = bar.axis === 'y' ? event.clientY < rect.top : rtl ? event.clientX > rect.right : event.clientX < rect.left;
      const pageSize = (before ? -1 : 1) * view * 0.9;
      viewport.scrollBy(bar.axis === 'y' ? { top: pageSize } : { left: sign(bar.axis) * pageSize });
    });
    // 拖动滑块：位移按“可滚动距离 / 滑块可移动距离”换算成滚动距离
    bar.thumb.addEventListener('pointerdown', (event) => {
      if (event.button !== 0) return;
      event.preventDefault();
      const { view, size, position } = measure(bar.axis);
      const track = trackLength(bar);
      const ratio = (size - view) / Math.max(1, track - thumbLength(view, size, track));
      const direction = sign(bar.axis);
      dragging = true;
      startMove(event, {
        target: bar.thumb,
        onMove: ({ deltaX, deltaY }) => {
          const target = position + (bar.axis === 'y' ? deltaY : direction * deltaX) * ratio;
          if (bar.axis === 'y') viewport.scrollTop = target;
          else viewport.scrollLeft = direction * target;
        },
        onEnd: () => {
          dragging = false;
          scheduleHide();
        },
      });
    });
    // 滚动条盖在视口上面：滚轮落在它上面时转交给视口
    bar.track.addEventListener('wheel', (event) => {
      event.preventDefault();
      viewport.scrollBy({ left: event.deltaX, top: event.deltaY });
    }, { passive: false });
  }

  paintLater();

  return () => {
    scroller.removeEventListener('scroll', onScroll);
    if (scroller === window) window.removeEventListener('resize', paintLater);
    observer.disconnect();
  };
}
