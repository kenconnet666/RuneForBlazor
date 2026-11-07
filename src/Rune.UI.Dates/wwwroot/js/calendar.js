// 日历（[data-rune-calendar]，同 vue-tsx-pro 的 handleCalendarKeydown 与 CalendarView）：日期网格的键盘与区间预览，状态在 C#。
// - 方向键、Page Up / Down、Home / End 在这里算出要去的日期：在这一页里就当场把焦点移过去，再交给 C#（rune-command focus），
//   翻页、夹到可选范围由 C# 定；渲染后焦点跟到 C# 认定的那一格（[data-focused]）。
// - 点一格交 activate（选中，或在月份、年份视图里进到下一级），Esc 在区间选到一半时交 cancel（没有时不拦，交给弹层关闭）。
// - 区间选到一半（data-anchor）：从开始到悬停（没有时是焦点）那一格画预览（data-preview*，C# 不写这几个属性）。
import { isRtl, sendCommand } from '../../Rune.UI/js/core/shared.js';

const states = new WeakMap();

/** 开始管理一个日历（插入时调用；重复调用无副作用） */
export function watchCalendar(root) {
  if (states.has(root)) return;
  const state = { focused: root.dataset.focusedDate, seen: root.dataset.focusedDate, sent: [], hovered: null, wantFocus: false };
  states.set(root, state);
  root.addEventListener('keydown', (event) => onKeydown(root, state, event));
  root.addEventListener('click', (event) => {
    const button = cellOf(root, event.target);
    if (button && button.getAttribute('aria-disabled') !== 'true') sendCommand(root, 'activate', button.dataset.date);
  });
  // 点击、程序聚焦到别的格子时，键盘从这一格接着走
  root.addEventListener('focusin', (event) => {
    const button = cellOf(root, event.target);
    if (button && !button.hasAttribute('data-focused') && !button.hasAttribute('data-outside')) moveTo(root, state, dayIn(root, button.dataset.date, state.focused), false);
  });
  root.addEventListener('pointerover', (event) => {
    const button = cellOf(root, event.target);
    if (!button || button.getAttribute('aria-disabled') === 'true') return;
    state.hovered = button.dataset.date;
    preview(root, state);
  });
  root.addEventListener('pointerleave', () => {
    state.hovered = null;
    preview(root, state);
  });
  new MutationObserver(() => afterRender(root, state)).observe(root, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-focused-date', 'data-anchor', 'data-view'] });
}

const cellOf = (root, target) => {
  const button = target instanceof Element ? target.closest('button[data-date]') : null;
  return button && root.contains(button) ? button : null;
};

/** C# 渲染了：焦点日期不是自己刚交出去的就以它为准；要跟焦点时移到 [data-focused] 那一格；重画预览 */
function afterRender(root, state) {
  const focused = root.dataset.focusedDate;
  if (focused !== state.seen) {
    state.seen = focused;
    const mine = state.sent.indexOf(focused);
    if (mine >= 0) state.sent.splice(0, mine + 1);
    else {
      state.sent = [];
      state.focused = focused;
    }
  }
  if (state.wantFocus) {
    const target = root.querySelector('button[data-focused]');
    if (target && target.dataset.date === cellKey(root, state.focused)) {
      state.wantFocus = false;
      if (document.activeElement !== target) target.focus();
    }
  }
  preview(root, state);
}

// ───────── 日期运算（ISO 文字，按 UTC 算，不受时区影响） ─────────

const toDate = (iso) => new Date(`${iso}T00:00:00Z`);
const toIso = (date) => date.toISOString().slice(0, 10);

function addDays(iso, days) {
  const date = toDate(iso);
  date.setUTCDate(date.getUTCDate() + days);
  return toIso(date);
}

/** 加几个月：那个月没有这一天时取最后一天（1 月 31 日加一个月是 2 月 28 日） */
function addMonths(iso, months) {
  const date = toDate(iso);
  const day = date.getUTCDate();
  date.setUTCDate(1);
  date.setUTCMonth(date.getUTCMonth() + months);
  const last = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + 1, 0)).getUTCDate();
  date.setUTCDate(Math.min(day, last));
  return toIso(date);
}

/** 这一格的 data-date：日期格是那一天，月份格是那月 1 日，年份格是那年 1 月 1 日 */
function cellKey(root, iso) {
  const view = root.dataset.view;
  if (view === 'month') return `${iso.slice(0, 7)}-01`;
  if (view === 'quarter') return `${iso.slice(0, 4)}-${String(Math.floor((Number(iso.slice(5, 7)) - 1) / 3) * 3 + 1).padStart(2, '0')}-01`;
  if (view === 'year') return `${iso.slice(0, 4)}-01-01`;
  return iso;
}

/** 月份、年份视图里聚焦到某格：保留原来的日（与月），回到日期视图时还是那一天（同方向键） */
function dayIn(root, cell, focused) {
  const view = root.dataset.view;
  if (view === 'day' || !focused) return cell;
  const months = (Number(cell.slice(0, 4)) - Number(focused.slice(0, 4))) * 12 + (view === 'year' ? 0 : Number(cell.slice(5, 7)) - Number(focused.slice(5, 7)));
  return addMonths(focused, months);
}

/** 按键 → 要去的日期；不处理的键返回 null */
function targetOf(root, iso, event) {
  const forward = isRtl(root) ? -1 : 1;
  const view = root.dataset.view;
  if (view === 'day') {
    const first = Number(root.dataset.firstDay ?? 0);
    const offset = (toDate(iso).getUTCDay() - first + 7) % 7;
    switch (event.key) {
      case 'ArrowRight': return addDays(iso, forward);
      case 'ArrowLeft': return addDays(iso, -forward);
      case 'ArrowDown': return addDays(iso, 7);
      case 'ArrowUp': return addDays(iso, -7);
      case 'PageDown': return addMonths(iso, event.shiftKey ? 12 : 1);
      case 'PageUp': return addMonths(iso, event.shiftKey ? -12 : -1);
      case 'Home': return addDays(iso, -offset);
      case 'End': return addDays(iso, 6 - offset);
      default: return null;
    }
  }
  // 季度面板（QuarterCalendar）：一行四格，←→ 一个季度（到头时换年），Home / End 到第一、第四季度，Page Up / Down 换年
  if (view === 'quarter') {
    const month = Number(iso.slice(5, 7));
    switch (event.key) {
      case 'ArrowRight': return addMonths(iso, 3 * forward);
      case 'ArrowLeft': return addMonths(iso, -3 * forward);
      case 'PageDown': return addMonths(iso, 12);
      case 'PageUp': return addMonths(iso, -12);
      case 'Home': return addMonths(iso, 1 - month);
      case 'End': return addMonths(iso, 10 - month);
      default: return null;
    }
  }
  // 月份、年份视图：3 列的网格，一格是一个月或一年
  const unit = view === 'month' ? 1 : 12;
  const year = Number(iso.slice(0, 4));
  switch (event.key) {
    case 'ArrowRight': return addMonths(iso, forward * unit);
    case 'ArrowLeft': return addMonths(iso, -forward * unit);
    case 'ArrowDown': return addMonths(iso, 3 * unit);
    case 'ArrowUp': return addMonths(iso, -3 * unit);
    case 'PageDown': return addMonths(iso, view === 'month' ? 12 : 120);
    case 'PageUp': return addMonths(iso, view === 'month' ? -12 : -120);
    case 'Home': return view === 'month' ? addMonths(iso, 1 - Number(iso.slice(5, 7))) : addMonths(iso, -12 * (year % 10));
    case 'End': return view === 'month' ? addMonths(iso, 12 - Number(iso.slice(5, 7))) : addMonths(iso, 12 * (9 - (year % 10)));
    default: return null;
  }
}

/** 移动焦点日期：在这一页里就当场把焦点移过去，再交给 C# */
function moveTo(root, state, iso, focus = true) {
  state.focused = iso;
  state.sent.push(iso);
  if (state.sent.length > 20) state.sent.shift();
  const button = root.querySelector(`button[data-date="${cellKey(root, iso)}"]:not([data-outside])`);
  if (focus) {
    state.wantFocus = true;
    if (button) {
      root.querySelectorAll('button[data-date][tabindex="0"]').forEach((other) => other !== button && other.setAttribute('tabindex', '-1'));
      button.setAttribute('tabindex', '0');
      button.focus();
    }
  }
  sendCommand(root, 'focus', iso);
  preview(root, state);
}

function onKeydown(root, state, event) {
  if (event.altKey || event.ctrlKey || event.metaKey || !cellOf(root, event.target) || root.hasAttribute('data-disabled')) return;
  if (event.key === 'Escape') {
    if (!root.dataset.anchor) return;
    event.preventDefault();
    event.stopPropagation();
    sendCommand(root, 'cancel');
    return;
  }
  const target = targetOf(root, state.focused ?? root.dataset.focusedDate, event);
  if (!target) return;
  event.preventDefault();
  moveTo(root, state, target);
}

/** 区间选到一半：从开始到悬停（没有时是焦点）那一格画预览 */
function preview(root, state) {
  const anchor = root.dataset.anchor;
  const end = anchor ? cellKey(root, state.hovered ?? state.focused ?? anchor) : null;
  const [from, to] = anchor ? [anchor, end].sort() : [];
  for (const button of root.querySelectorAll('button[data-date]')) {
    const date = button.dataset.date;
    const inside = anchor != null && !button.hasAttribute('data-outside') && date >= from && date <= to;
    for (const element of [button, button.parentElement]) {
      toggle(element, 'data-preview', inside);
      toggle(element, 'data-preview-start', inside && date === from);
      toggle(element, 'data-preview-end', inside && date === to);
    }
  }
}

function toggle(element, name, on) {
  if (element.hasAttribute(name) !== on) element.toggleAttribute(name, on);
}
