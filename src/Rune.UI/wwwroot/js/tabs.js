// 标签页的横排标签栏（data-rune-tabs，同 vue-tsx-pro 的 TabList）：
// - 列表放不下时在栏上写 data-overflowing、去掉两边滚动按钮的 hidden，能往前、往后滚时滚动按钮才可用；点按钮滚大半屏。
// - 切到一个滚在外面的标签（方向键、程序改值）：把它滚进来。
// - 可关闭的标签（data-closable）上按 Delete、鼠标中键点：转成点它的关闭按钮（[data-rune-tab-close]）；
//   关掉的标签有焦点时，等 C# 选好旁边的标签，再把焦点交给它（元素一移除，焦点就丢到 body 上了）。

import { speak } from './shared.js';

const watched = new WeakSet();
// 正在关闭的标签（有焦点时）：标签栏 → 那个标签
const closing = new WeakMap();

const listOf = (bar) => bar.querySelector(':scope > [role="tablist"]');

/** 开始管理一个标签栏（插入时调用；重复调用无副作用）。 */
export function watchTabs(bar) {
  if (watched.has(bar)) return;
  const list = listOf(bar);
  if (!list) return;
  watched.add(bar);
  const measure = () => measureOverflow(bar, list);
  new ResizeObserver(measure).observe(list);
  list.addEventListener('scroll', measure, { passive: true });
  new MutationObserver(() => {
    measure();
    revealSelected(list);
    refocusSelected(bar, list);
  }).observe(list, { childList: true, subtree: true, attributes: true, attributeFilter: ['aria-selected'] });
  measure();
  revealSelected(list);
}

function measureOverflow(bar, list) {
  // 从右到左排版时 scrollLeft 是负的：取绝对值就是离起始端多远
  const scrolled = Math.abs(list.scrollLeft);
  const max = list.scrollWidth - list.clientWidth;
  bar.toggleAttribute('data-overflowing', max > 1);
  for (const button of bar.querySelectorAll(':scope > [data-rune-tab-scroll]')) {
    const canScroll = button.getAttribute('data-rune-tab-scroll') === '1' ? scrolled < max - 1 : scrolled > 1;
    button.toggleAttribute('hidden', max <= 1);
    button.toggleAttribute('disabled', !canScroll);
  }
}

function revealSelected(list) {
  const tab = list.querySelector('[role="tab"][aria-selected="true"]');
  if (!tab || list.scrollWidth <= list.clientWidth) return;
  const outer = list.getBoundingClientRect();
  const inner = tab.getBoundingClientRect();
  if (inner.left < outer.left) list.scrollBy({ left: inner.left - outer.left - 16 });
  else if (inner.right > outer.right) list.scrollBy({ left: inner.right - outer.right + 16 });
}

function refocusSelected(bar, list) {
  const closed = closing.get(bar);
  if (!closed) return;
  // 关掉的标签还在页面上：C# 还没处理完
  if (closed.isConnected) return;
  closing.delete(bar);
  const tab = list.querySelector('[role="tab"][aria-selected="true"]');
  if (tab && !list.contains(document.activeElement)) tab.focus();
}

/** 点滚动按钮：往前或往后滚大半屏（按书写方向）。 */
export function scrollTabs(event) {
  const button = event.target instanceof Element ? event.target.closest('[data-rune-tab-scroll]') : null;
  const bar = button?.closest('[data-rune-tabs]');
  const list = bar && listOf(bar);
  if (!list) return;
  const direction = Number(button.getAttribute('data-rune-tab-scroll'));
  const rtl = getComputedStyle(list).direction === 'rtl';
  list.scrollBy({ left: (rtl ? -direction : direction) * list.clientWidth * 0.8, behavior: 'smooth' });
}

function closeTab(tab) {
  const button = tab.parentElement?.querySelector(':scope > [data-rune-tab-close]');
  if (!button || tab.disabled) return;
  const bar = tab.closest('[data-rune-tabs]');
  if (bar && tab === document.activeElement) closing.set(bar, tab);
  button.click();
  // WebAssembly 下 C# 在点击时同步就处理完了：先查一次
  if (bar) refocusSelected(bar, listOf(bar));
}

/** 标签上的按键：Alt + ←→ 挪动（可排序时），Delete 关闭（可关闭时）。 */
export function keyTabs(event) {
  if (keyReorder(event)) return;
  if (event.key !== 'Delete' || !(event.target instanceof Element) || !event.target.matches('[role="tab"][data-closable]')) return;
  event.preventDefault();
  closeTab(event.target);
}

/** 可关闭的标签上点鼠标中键（同浏览器的标签页）。 */
export function auxClickTabs(event) {
  if (event.button !== 1 || !(event.target instanceof Element)) return;
  const tab = event.target.closest('[role="tab"][data-closable]');
  if (tab) closeTab(tab);
}

// ─────────────── 拖动排序 ───────────────
// 列表写 data-reorderable 时：拖动标签越过别的标签的中线就换位（同 Sortable），在标签上按 Alt + ←→ 往前、往后挪一位（从右到左排版时反过来）。
// 只发出 rune-reorder（新的顺序），由 C# 让使用方重排、重新渲染：JS 不挪 Blazor 管的元素。等页面上的顺序跟上之前不再发第二次。
// 键盘挪动后念出新位置（data-moved-template 是语言包的句子，{label}、900001、900002 换成标签文字、位置与总数），焦点留在挪动的标签上。

const DRAG_THRESHOLD = 4;
const pendingOrders = new WeakMap();

const orderOf = (list) => [...list.querySelectorAll('[role="tab"]')].map((tab) => tab.dataset.value);

function requestOrder(list, order) {
  const key = order.join('\n');
  if (pendingOrders.get(list) === key || key === orderOf(list).join('\n')) return;
  pendingOrders.set(list, key);
  list.dispatchEvent(new CustomEvent('rune-reorder', { bubbles: true, detail: { values: order } }));
}

function moved(order, value, to) {
  const next = order.filter((item) => item !== value);
  next.splice(to, 0, value);
  return next;
}

/** 在可排序的标签上按下：越过阈值后开始拖动。 */
export function grabTab(event) {
  if (event.button !== 0 || event.pointerType === 'touch' || !(event.target instanceof Element)) return;
  const tab = event.target.closest('[role="tab"]');
  const list = tab?.closest('[role="tablist"][data-reorderable]');
  if (!list || tab.disabled) return;
  const value = tab.dataset.value;
  const { clientX: startX, pointerId } = event;
  let dragging = false;
  const move = (moveEvent) => {
    if (moveEvent.pointerId !== pointerId) return;
    if (!dragging) {
      if (Math.abs(moveEvent.clientX - startX) < DRAG_THRESHOLD) return;
      dragging = true;
      // 标签的元素会被重排：捕获在不动的列表上
      try { list.setPointerCapture(pointerId); } catch {}
    }
    const current = list.querySelector(`[role="tab"][data-value="${CSS.escape(value)}"]`);
    current?.setAttribute('data-dragging', '');
    // 页面上的顺序还没跟上上一次的请求：先不算
    const pending = pendingOrders.get(list);
    const order = orderOf(list);
    if (pending && pending !== order.join('\n')) return;
    pendingOrders.delete(list);
    const tabs = [...list.querySelectorAll('[role="tab"]')];
    const over = tabs.find((item) => {
      const rect = item.getBoundingClientRect();
      return moveEvent.clientX >= rect.left && moveEvent.clientX <= rect.right;
    });
    const from = order.indexOf(value);
    const to = over ? order.indexOf(over.dataset.value) : -1;
    if (!over || to < 0 || to === from) return;
    const rect = over.getBoundingClientRect();
    const center = rect.left + rect.width / 2;
    // 按书写方向“越过中线”：往后挪时要过了它的中线，往前挪时要回到中线之前
    const rtl = getComputedStyle(list).direction === 'rtl';
    const past = rtl ? moveEvent.clientX < center : moveEvent.clientX > center;
    if (to > from === past) requestOrder(list, moved(order, value, to));
  };
  const end = (endEvent) => {
    if (endEvent.pointerId !== pointerId) return;
    document.removeEventListener('pointermove', move);
    document.removeEventListener('pointerup', end);
    document.removeEventListener('pointercancel', end);
    list.querySelector('[role="tab"][data-dragging]')?.removeAttribute('data-dragging');
    // 拖过了就不算点击（不切换标签）
    if (dragging) list.addEventListener('click', (click) => click.stopPropagation(), { capture: true, once: true });
  };
  document.addEventListener('pointermove', move);
  document.addEventListener('pointerup', end);
  document.addEventListener('pointercancel', end);
}

/** 可排序的标签上按 Alt + ←→：挪一位，念出新位置。 */
function keyReorder(event) {
  if (!event.altKey || event.ctrlKey || event.metaKey || !(event.target instanceof Element)) return false;
  const tab = event.target.closest('[role="tab"]');
  const list = tab?.closest('[role="tablist"][data-reorderable]');
  if (!list || (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight')) return false;
  event.preventDefault();
  const rtl = getComputedStyle(list).direction === 'rtl';
  const forward = rtl ? 'ArrowLeft' : 'ArrowRight';
  const order = orderOf(list);
  const value = tab.dataset.value;
  const to = order.indexOf(value) + (event.key === forward ? 1 : -1);
  if (to < 0 || to >= order.length) return true;
  requestOrder(list, moved(order, value, to));
  const template = list.getAttribute('data-moved-template');
  if (template) speak(template.replace('{label}', tab.textContent.trim()).replace('900001', String(to + 1)).replace('900002', String(order.length)));
  // 标签的元素被移动过：顺序跟上之后焦点放回它上面。WebAssembly 下 C# 在发事件时同步就重排完了，先查一次再等
  const settled = () => {
    const again = list.querySelector(`[role="tab"][data-value="${CSS.escape(value)}"]`);
    if (orderOf(list).indexOf(value) !== to || !again) return false;
    if (document.activeElement !== again) again.focus();
    return true;
  };
  if (settled()) return true;
  const refocus = new MutationObserver(() => settled() && refocus.disconnect());
  refocus.observe(list, { childList: true, subtree: true });
  setTimeout(() => refocus.disconnect(), 2000);
  return true;
}
