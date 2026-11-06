// 可拖动排序的列表（Sortable 的 data-rune-sortable="vertical|horizontal"，同 vue-tsx-pro 的 useSortable 的单列部分）：
// 每项（data-rune-sortable-item="key"）有一个把手（data-rune-sortable-handle）。元素归 Blazor 管，JS 不挪它们：
//   - 指针：拖过 3px 开始，被拖的项跟着指针（transform），越过别的项的中线时其余项用 transform 让位（CSS 过渡）；
//     靠近滚动容器边缘时自动滚动。松开时被拖的项滑到落点，发出 rune-reorder（新的顺序）交给 C#；页面上的顺序跟上后去掉所有位移
//     （位置不变，没有跳动）。C# 没采纳（受控、拒绝）时滑回原处。Esc、指针取消时回到原处，不发事件。
//   - 键盘：空格 / Enter 拿起、放下，方向键（竖排 ↑↓，横排 ←→，跟随 RTL）移一位、Home / End 到两端，每一步发 rune-reorder；
//     Esc 回到拿起时的位置；焦点离开时就地放下。元素被 Blazor 移动后焦点放回把手上。
//   - 读屏：每一步念一句（data-texts 是语言包的句子，{label} 与 900001、900002 换成名称、位置、总数）。

import { speak } from './shared.js';
import { startMove } from './move.js';

const ITEM = '[data-rune-sortable-item]';
const THRESHOLD = 3;
/** 指针离滚动容器边缘多近（px）时开始自动滚动，每帧最多滚多少 px */
const EDGE = 40;
const MAX_SCROLL_STEP = 16;

const itemsOf = (list) => [...list.querySelectorAll(`:scope > ${ITEM}`)];
const keyOf = (item) => item.dataset.runeSortableItem;
const orderOf = (list) => itemsOf(list).map(keyOf);
const itemByKey = (list, key) => itemsOf(list).find((item) => keyOf(item) === key) ?? null;
const handleOf = (item) => item?.querySelector('[data-rune-sortable-handle]') ?? null;
const labelOf = (item) => item.dataset.label ?? keyOf(item);
const horizontalOf = (list) => list.dataset.runeSortable === 'horizontal';

function say(list, name, item, index, total) {
  const template = JSON.parse(list.dataset.texts || '{}')[name];
  if (!template) return;
  speak(template.replace('{label}', labelOf(item)).replace('900001', String(index + 1)).replace('900002', String(total)));
}

/** 把 key 移到 to 处的新顺序 */
function moved(order, key, to) {
  const next = order.filter((other) => other !== key);
  next.splice(to, 0, key);
  return next;
}

/** 请 C# 按新顺序重排；页面上的顺序跟上（或超时没跟上）后调用 done(adopted) */
function requestOrder(list, order, done) {
  const wanted = order.join('\n');
  if (orderOf(list).join('\n') === wanted) {
    done?.(true);
    return;
  }
  list.dispatchEvent(new CustomEvent('rune-reorder', { bubbles: true, detail: { values: order } }));
  if (!done) return;
  // WebAssembly 下 C# 在发事件时同步就重排完了：先查一次再等
  if (orderOf(list).join('\n') === wanted) {
    done(true);
    return;
  }
  let finished = false;
  const finish = (adopted) => {
    if (finished) return;
    finished = true;
    observer.disconnect();
    clearTimeout(timer);
    done(adopted);
  };
  const observer = new MutationObserver(() => orderOf(list).join('\n') === wanted && finish(true));
  observer.observe(list, { childList: true });
  const timer = setTimeout(() => finish(false), 1500);
}

/** 去掉各项的位移：instant 时不要过渡（顺序已经跟上，位置不变） */
function clearShifts(list, instant) {
  const items = itemsOf(list);
  if (instant) list.setAttribute('data-settling', '');
  for (const item of items) {
    item.style.removeProperty('transform');
    item.removeAttribute('data-dragging');
  }
  if (instant) {
    // 读一次布局，让“无过渡”生效后再恢复
    void list.offsetHeight;
    list.removeAttribute('data-settling');
  }
}

// ───────── 键盘 ─────────

// 键盘拿起的项：list → { key, from }
const grabbed = new WeakMap();
// 键盘挪动期间 Blazor 会移动元素、让把手失焦：这期间的失焦不算放下
const reordering = new WeakSet();

function refocus(list, key) {
  const handle = handleOf(itemByKey(list, key));
  if (handle && document.activeElement !== handle) handle.focus({ preventScroll: true });
}

function setGrabbed(list, key, on) {
  const item = itemByKey(list, key);
  item?.toggleAttribute('data-grabbed', on);
  handleOf(item)?.setAttribute('aria-pressed', on ? 'true' : 'false');
}

function drop(list) {
  const state = grabbed.get(list);
  if (!state) return;
  grabbed.delete(list);
  setGrabbed(list, state.key, false);
  const order = orderOf(list);
  say(list, 'dropped', itemByKey(list, state.key), order.indexOf(state.key), order.length);
}

function stepTo(list, key, to, announce) {
  const order = orderOf(list);
  const next = moved(order, key, to);
  reordering.add(list);
  requestOrder(list, next, () => {
    reordering.delete(list);
    refocus(list, key);
    setGrabbed(list, key, grabbed.has(list));
    announce(next);
  });
}

export function keySortable(event) {
  if (!(event.target instanceof Element) || event.altKey || event.ctrlKey || event.metaKey) return false;
  const handle = event.target.closest('[data-rune-sortable-handle]');
  const item = handle?.closest(ITEM);
  const list = item?.parentElement;
  if (!list?.matches('[data-rune-sortable]') || handle.matches(':disabled') || list.hasAttribute('data-disabled')) return false;
  const key = keyOf(item);
  const state = grabbed.get(list);
  if (event.key === ' ' || event.key === 'Enter') {
    event.preventDefault();
    if (state?.key === key) {
      drop(list);
      return true;
    }
    if (state) drop(list);
    const order = orderOf(list);
    grabbed.set(list, { key, from: order.indexOf(key) });
    setGrabbed(list, key, true);
    say(list, 'grabbed', item, order.indexOf(key), order.length);
    return true;
  }
  if (state?.key !== key) return false;
  const order = orderOf(list);
  const from = order.indexOf(key);
  if (event.key === 'Escape') {
    event.preventDefault();
    grabbed.delete(list);
    stepTo(list, key, state.from, () => say(list, 'cancelled', itemByKey(list, key), state.from, order.length));
    return true;
  }
  const rtl = getComputedStyle(list).direction === 'rtl';
  const forward = horizontalOf(list) ? (rtl ? 'ArrowLeft' : 'ArrowRight') : 'ArrowDown';
  const backward = horizontalOf(list) ? (rtl ? 'ArrowRight' : 'ArrowLeft') : 'ArrowUp';
  const to = event.key === forward ? from + 1
    : event.key === backward ? from - 1
    : event.key === 'Home' ? 0
    : event.key === 'End' ? order.length - 1
    : null;
  if (to === null) return false;
  event.preventDefault();
  if (to < 0 || to >= order.length || to === from) return true;
  stepTo(list, key, to, (next) => say(list, 'moved', itemByKey(list, key), next.indexOf(key), next.length));
  return true;
}

/** 焦点离开拿起的把手：就地放下（挪动中 Blazor 移动元素引起的失焦不算） */
export function blurSortable(event) {
  if (!(event.target instanceof Element) || !event.target.matches('[data-rune-sortable-handle][aria-pressed="true"]')) return;
  const list = event.target.closest('[data-rune-sortable]');
  if (!list || reordering.has(list) || dragging.has(list)) return;
  drop(list);
}

// ───────── 指针 ─────────

const dragging = new WeakSet();

export function grabSortable(event) {
  if (event.button !== 0 || !(event.target instanceof Element)) return;
  const handle = event.target.closest('[data-rune-sortable-handle]');
  const item = handle?.closest(ITEM);
  const list = item?.parentElement;
  if (!list?.matches('[data-rune-sortable]') || handle.matches(':disabled') || list.hasAttribute('data-disabled') || dragging.has(list)) return;
  const horizontal = horizontalOf(list);
  const rtl = horizontal && getComputedStyle(list).direction === 'rtl';
  const items = itemsOf(list);
  const keys = items.map(keyOf);
  const key = keyOf(item);
  const from = keys.indexOf(key);
  const scroller = scrollParent(list);
  const scrollOf = () => (horizontal ? scroller.scrollLeft : scroller.scrollTop);
  const startScroll = scrollOf();
  // 按下时各项在轴上的起止（内容坐标）：判断越过了谁的中线、让位多少
  const spans = items.map((element) => {
    const rect = element.getBoundingClientRect();
    return horizontal ? { start: rect.left + startScroll, end: rect.right + startScroll } : { start: rect.top + startScroll, end: rect.bottom + startScroll };
  });
  const gap = spans.length > 1 ? Math.abs(spans[1].start - spans[0].end) : 0;
  const size = spans[from].end - spans[from].start + gap;
  const center = (span) => (span.start + span.end) / 2;
  let started = false;
  let delta = 0;
  let to = from;
  let pointer = { x: event.clientX, y: event.clientY };
  let frame = 0;
  event.preventDefault();

  // 落点：沿书写方向排在被拖的项前面的项有几个（RTL 横排时“前面”在右边）
  const targetIndex = () => {
    const moving = center(spans[from]) + delta + (scrollOf() - startScroll);
    return keys.filter((other, index) => index !== from && (rtl ? center(spans[index]) > moving : center(spans[index]) < moving)).length;
  };

  // 其余项让位：落点与原位之间的项朝原位那边挪一个被拖项的长度
  const shift = () => {
    items.forEach((element, index) => {
      if (index === from) return;
      const offset = from < to && index > from && index <= to ? -size : to < from && index >= to && index < from ? size : 0;
      const along = rtl ? -offset : offset;
      element.style.transform = along ? (horizontal ? `translateX(${along}px)` : `translateY(${along}px)`) : '';
    });
  };

  const follow = () => {
    const along = delta + (scrollOf() - startScroll);
    item.style.transform = horizontal ? `translateX(${along}px)` : `translateY(${along}px)`;
  };

  const autoScroll = () => {
    cancelAnimationFrame(frame);
    if (!edgeScroll(scroller, pointer)) return;
    follow();
    to = targetIndex();
    shift();
    frame = requestAnimationFrame(autoScroll);
  };

  const stop = startMove(event, {
    // 项会因为位移移动：捕获放在不动的列表上
    target: list,
    onMove: ({ deltaX, deltaY, clientX, clientY }) => {
      const along = horizontal ? deltaX : deltaY;
      if (!started) {
        if (Math.abs(along) < THRESHOLD) return;
        started = true;
        dragging.add(list);
        item.setAttribute('data-dragging', '');
        say(list, 'grabbed', item, from, keys.length);
      }
      delta = along;
      pointer = { x: clientX, y: clientY };
      follow();
      to = targetIndex();
      shift();
      autoScroll();
    },
    onEnd: ({ cancelled }) => {
      cancelAnimationFrame(frame);
      document.removeEventListener('keydown', onEscape, true);
      if (!started) return;
      // 拖过了就不算点击把手
      list.addEventListener('click', (click) => click.stopPropagation(), { capture: true, once: true });
      if (cancelled || to === from) {
        item.removeAttribute('data-dragging');
        clearShifts(list, false);
        dragging.delete(list);
        say(list, 'cancelled', item, from, keys.length);
        return;
      }
      // 滑到落点：往后挪时尾部对齐落点那一项的尾部，往前挪时头部对齐它的头部
      const landing = to > from ? spans[to].end - spans[from].end : spans[to].start - spans[from].start;
      item.removeAttribute('data-dragging');
      item.style.transform = horizontal ? `translateX(${landing}px)` : `translateY(${landing}px)`;
      const next = moved(keys, key, to);
      requestOrder(list, next, (adopted) => {
        // 顺序跟上了：元素已经在新位置，去掉位移时不要过渡；没采纳：滑回原处
        clearShifts(list, adopted);
        dragging.delete(list);
        if (adopted) say(list, 'dropped', itemByKey(list, key) ?? item, to, keys.length);
        else say(list, 'cancelled', item, from, keys.length);
      });
    },
  });

  // 拖动中按 Esc：取消，回到原处
  const onEscape = (keyEvent) => {
    if (keyEvent.key !== 'Escape') return;
    keyEvent.preventDefault();
    keyEvent.stopPropagation();
    stop();
  };
  document.addEventListener('keydown', onEscape, true);
}

/** 指针靠近 scroller 的边缘时朝那边滚一步；滚了返回 true */
function edgeScroll(scroller, pointer) {
  const rect = scroller === document.scrollingElement ? new DOMRect(0, 0, innerWidth, innerHeight) : scroller.getBoundingClientRect();
  const speed = (along, low, high) => along < low + EDGE ? -MAX_SCROLL_STEP * (1 - (along - low) / EDGE)
    : along > high - EDGE ? MAX_SCROLL_STEP * (1 - (high - along) / EDGE)
    : 0;
  const left = scroller.scrollWidth > scroller.clientWidth ? speed(pointer.x, rect.left, rect.right) : 0;
  const top = scroller.scrollHeight > scroller.clientHeight ? speed(pointer.y, rect.top, rect.bottom) : 0;
  if (!left && !top) return false;
  const before = [scroller.scrollLeft, scroller.scrollTop];
  scroller.scrollBy({ left, top });
  return scroller.scrollLeft !== before[0] || scroller.scrollTop !== before[1];
}

/** 最近的能滚动的祖先（含自己）；都不能滚时是页面 */
function scrollParent(element) {
  for (let current = element; current; current = current.parentElement) {
    const { overflowX, overflowY } = getComputedStyle(current);
    const scrollable = /auto|scroll|overlay/;
    if ((scrollable.test(overflowY) && current.scrollHeight > current.clientHeight)
      || (scrollable.test(overflowX) && current.scrollWidth > current.clientWidth)) return current;
  }
  return document.scrollingElement ?? document.documentElement;
}
