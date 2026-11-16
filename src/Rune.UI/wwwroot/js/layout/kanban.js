// 看板（KanbanBoard 的 data-rune-kanban，同 vue-tsx-pro 的 useSortable 的分组部分）：卡片在列之间拖动，列内排序。
// 元素归 Blazor 管，JS 不挪卡片，只画拖动中的东西（属性、影子卡、插入线），放下时把结果交给 C#（rune-command）：
//   - 每列 data-rune-kanban-column="列"，卡片 data-rune-kanban-card="卡片"，把手 data-rune-kanban-handle；
//     卡片的 data-deny 是不能放进哪些列与原因（C# 按 CanMove 算好，拖动时不用往返）。
//   - 指针：在卡片上按下拖动超过 3px 开始（点在卡片里的链接、按钮上不算；触屏、触控笔只能从把手拖），原位的卡片变淡（data-dragging），
//     影子卡（卡片的副本，放在 data-rune-kanban-ghost 里）跟着指针；经过的列标出能不能放（data-drop），落点画一条插入线；
//     靠近看板、列的滚动边缘时自动滚动。Esc、指针取消时回到原处。
//   - 键盘：把手上空格 / Enter 拿起（data-lifted），← / → 换列（跟随 RTL），↑ / ↓、Home / End 调整落点，空格 / Enter 放下，Esc 取消；
//     焦点离开时：落点在自己的列里就地放下，在别的列里取消。
//   - 放下：别的列能放 → move（卡片、列、位置），不能放 → reject（卡片、列）；自己的列里换了位置且 data-reorderable → reorder（卡片、位置）。
//     拿起、放下时还有 grab、release（C# 据此给卡片内容 Dragging）。键盘放到别的列后，等那张卡片出现在新列里再把焦点放回它的把手。
//   - 读屏：每一步念一句（data-texts 是语言包的句子，{label}、{group}、{reason}、900001 换成实际的值）。

import { isRtl, sendCommand, speak } from '../core/shared.js';
import { startMove } from '../core/move.js';
import { edgeScroll, scrollParent } from '../table/sortable.js';

const COLUMN = '[data-rune-kanban-column]';
const CARD = '[data-rune-kanban-card]';
const HANDLE = '[data-rune-kanban-handle]';
const THRESHOLD = 3;

const columnKey = (column) => column.dataset.runeKanbanColumn;
const cardKey = (card) => card.dataset.runeKanbanCard;
const columnsOf = (root) => [...root.querySelectorAll(COLUMN)];
const cardsOf = (column) => [...column.querySelectorAll(CARD)];
const columnByKey = (root, key) => columnsOf(root).find((column) => columnKey(column) === key) ?? null;
const cardByKey = (root, key) => [...root.querySelectorAll(CARD)].find((card) => cardKey(card) === key) ?? null;
const listOf = (column) => column.querySelector('ul');
const labelOf = (element) => element.dataset.label ?? cardKey(element) ?? columnKey(element);

function reasonFor(card, column) {
  try {
    return JSON.parse(card.dataset.deny || '{}')[column] ?? null;
  } catch {
    return null;
  }
}

function say(root, name, values) {
  let text = JSON.parse(root.dataset.texts || '{}')[name];
  if (!text) return;
  for (const [key, value] of Object.entries(values)) text = text.replace(key, String(value));
  speak(text, true);
}

// 看板 → 拖动中的状态：{ key, card, own, from, group, index, pointer }
const states = new WeakMap();

export function watchKanban(root) {
  if (states.has(root) || root.hasAttribute('data-kanban-watched')) return;
  root.setAttribute('data-kanban-watched', '');
  root.addEventListener('pointerdown', (event) => grabWithPointer(root, event));
  root.addEventListener('keydown', (event) => onKeydown(root, event));
  root.addEventListener('focusout', (event) => onFocusOut(root, event));
}

/** 拿起：记下原来的列与位置 */
function lift(root, card, pointer) {
  const own = columnKey(card.closest(COLUMN));
  const from = cardsOf(card.closest(COLUMN)).indexOf(card);
  const state = { key: cardKey(card), card, own, from, group: own, index: from, pointer };
  states.set(root, state);
  card.toggleAttribute(pointer ? 'data-dragging' : 'data-lifted', true);
  card.querySelector(HANDLE)?.setAttribute('aria-pressed', 'true');
  sendCommand(root, 'grab', state.key);
  say(root, 'grabbed', { '{label}': labelOf(card), '{group}': labelOf(card.closest(COLUMN)), 900001: from + 1 });
  return state;
}

/** 清掉拖动中画的东西 */
function clear(root, state) {
  states.delete(root);
  const card = cardByKey(root, state.key) ?? state.card;
  card.removeAttribute('data-dragging');
  card.removeAttribute('data-lifted');
  card.querySelector(HANDLE)?.setAttribute('aria-pressed', 'false');
  for (const column of columnsOf(root)) {
    column.removeAttribute('data-drop');
    const reason = column.querySelector('[data-rune-kanban-reason]');
    if (reason) {
      reason.textContent = '';
      reason.removeAttribute('data-shown');
    }
  }
  root.querySelector('[data-rune-kanban-indicator]')?.removeAttribute('data-shown');
  const ghost = root.querySelector('[data-rune-kanban-ghost]');
  if (ghost) {
    ghost.replaceChildren();
    ghost.removeAttribute('data-shown');
  }
  sendCommand(root, 'release');
}

/** 落点换到 group 的第 index 位：标出那一列能不能放、画插入线 */
function retarget(root, state, group, index) {
  const column = columnByKey(root, group);
  if (!column) return;
  const own = group === state.own;
  const siblings = cardsOf(column).filter((card) => cardKey(card) !== state.key);
  state.group = group;
  state.index = Math.max(0, Math.min(index, siblings.length));
  const reason = own ? null : reasonFor(state.card, group);
  for (const other of columnsOf(root)) {
    const over = other === column && !own;
    if (over) other.setAttribute('data-drop', reason ? 'denied' : 'allowed');
    else other.removeAttribute('data-drop');
    const text = other.querySelector('[data-rune-kanban-reason]');
    if (!text) continue;
    text.textContent = over && reason ? reason : '';
    text.toggleAttribute('data-shown', over && !!reason);
  }
  // 自己的列里没换位置时不画线
  const indicator = root.querySelector('[data-rune-kanban-indicator]');
  if (!indicator) return;
  if (own && (state.index === state.from || !root.hasAttribute('data-reorderable'))) {
    indicator.removeAttribute('data-shown');
    return;
  }
  placeIndicator(indicator, column, siblings, state.index, !!reason);
}

/** 插入线：落点前一张与后一张卡片的中间（视口坐标） */
function placeIndicator(indicator, column, siblings, index, denied) {
  const list = listOf(column);
  if (!list) return;
  const box = list.getBoundingClientRect();
  const style = getComputedStyle(list);
  const left = box.left + parseFloat(style.paddingLeft);
  const width = list.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
  const gap = parseFloat(style.rowGap) || 0;
  const before = siblings[index - 1]?.getBoundingClientRect();
  const after = siblings[index]?.getBoundingClientRect();
  const top = after ? after.top - gap / 2 : before ? before.bottom + gap / 2 : box.top + parseFloat(style.paddingTop);
  indicator.style.left = `${left}px`;
  indicator.style.width = `${width}px`;
  indicator.style.top = `${top}px`;
  indicator.toggleAttribute('data-denied', denied);
  indicator.setAttribute('data-shown', '');
}

/** 放下：别的列能放的报告 move，不能放的报告 reject；自己的列里换了位置的报告 reorder */
function drop(root, state) {
  const { key, own, from, group, index } = state;
  const label = labelOf(state.card);
  clear(root, state);
  if (group === own) {
    const reorder = index !== from && root.hasAttribute('data-reorderable');
    if (reorder) sendCommand(root, 'reorder', `${key}\n${index}`);
    say(root, 'dropped', { '{label}': label, '{group}': labelOf(columnByKey(root, own)), 900001: (reorder ? index : from) + 1 });
    return;
  }
  const reason = reasonFor(state.card, group);
  if (reason) {
    say(root, 'rejected', { '{label}': label, '{reason}': reason });
    sendCommand(root, 'reject', `${key}\n${group}`);
    return;
  }
  say(root, 'dropped', { '{label}': label, '{group}': labelOf(columnByKey(root, group)), 900001: index + 1 });
  sendCommand(root, 'move', `${key}\n${group}\n${index}`);
  if (!state.pointer) refocusWhenMoved(root, key, group);
}

function cancel(root, state) {
  clear(root, state);
  say(root, 'cancelled', { '{label}': labelOf(state.card), '{group}': labelOf(columnByKey(root, state.own)), 900001: state.from + 1 });
}

/** 键盘放到别的列：等那张卡片出现在新列里（使用方改了数据），把焦点放回它的把手；焦点已经去了别处（对话框）时不抢 */
function refocusWhenMoved(root, key, group) {
  const settle = () => {
    const card = cardByKey(root, key);
    if (!card || columnKey(card.closest(COLUMN)) !== group) return false;
    const focused = document.activeElement;
    if (!focused || focused === document.body || root.contains(focused)) card.querySelector(HANDLE)?.focus({ preventScroll: false });
    return true;
  };
  // WebAssembly 下 C# 在发命令时同步就重新渲染完了：先查一次再等
  if (settle()) return;
  const observer = new MutationObserver(() => settle() && stop());
  const timer = setTimeout(() => stop(), 3000);
  const stop = () => {
    observer.disconnect();
    clearTimeout(timer);
  };
  observer.observe(root, { childList: true, subtree: true });
}

// ───────── 键盘 ─────────

function onKeydown(root, event) {
  if (!(event.target instanceof Element) || event.altKey || event.ctrlKey || event.metaKey) return;
  const handle = event.target.closest(HANDLE);
  const card = handle?.closest(CARD);
  if (!card || !root.contains(card)) return;
  const state = states.get(root);
  if (event.key === ' ' || event.key === 'Enter') {
    event.preventDefault();
    if (state?.key === cardKey(card)) drop(root, state);
    else {
      if (state) cancel(root, state);
      lift(root, card, false);
    }
    return;
  }
  if (!state || state.key !== cardKey(card) || state.pointer) return;
  if (event.key === 'Escape') {
    event.preventDefault();
    cancel(root, state);
    return;
  }
  const order = columnsOf(root).map(columnKey);
  const rtl = isRtl(root);
  const across = { [rtl ? 'ArrowLeft' : 'ArrowRight']: 1, [rtl ? 'ArrowRight' : 'ArrowLeft']: -1 }[event.key];
  if (across !== undefined) {
    event.preventDefault();
    const next = order[Math.max(0, Math.min(order.indexOf(state.group) + across, order.length - 1))];
    if (next === state.group) return;
    retarget(root, state, next, next === state.own ? state.from : state.index);
    announceOver(root, state);
    return;
  }
  const column = columnByKey(root, state.group);
  const count = cardsOf(column).filter((other) => cardKey(other) !== state.key).length;
  const step = { ArrowDown: 1, ArrowUp: -1 }[event.key];
  const index = event.key === 'Home' ? 0 : event.key === 'End' ? count : step === undefined ? null : state.index + step;
  if (index === null) return;
  event.preventDefault();
  // 自己的列里：能排序时才移动
  if (state.group === state.own && !root.hasAttribute('data-reorderable')) return;
  retarget(root, state, state.group, index);
  announceOver(root, state);
}

function announceOver(root, state) {
  const reason = state.group === state.own ? null : reasonFor(state.card, state.group);
  say(root, reason ? 'denied' : 'over', {
    '{label}': labelOf(state.card),
    '{group}': labelOf(columnByKey(root, state.group)),
    '{reason}': reason ?? '',
    900001: state.index + 1,
  });
}

function onFocusOut(root, event) {
  const state = states.get(root);
  if (!state || state.pointer || !(event.target instanceof Element) || !event.target.matches(HANDLE)) return;
  if (event.target.closest(CARD) !== state.card) return;
  if (state.group === state.own) drop(root, state);
  else cancel(root, state);
}

// ───────── 指针 ─────────

function grabWithPointer(root, event) {
  if (event.button !== 0 || !(event.target instanceof Element) || states.has(root)) return;
  const target = event.target;
  const card = target.closest(CARD);
  if (!card || !root.contains(card)) return;
  const onHandle = !!target.closest(HANDLE);
  // 触屏、触控笔只能从把手拖：卡片要留给手指滚动；卡片里的链接、按钮、输入框照常点
  if (/touch|pen/.test(event.pointerType) && !onHandle) return;
  if (!onHandle && target.closest('a, input, select, textarea, button, [contenteditable]')) return;
  const origin = card.getBoundingClientRect();
  const ghost = root.querySelector('[data-rune-kanban-ghost]');
  const board = root.querySelector('[data-rune-kanban-board]') ?? root;
  let state = null;
  let pointer = { x: event.clientX, y: event.clientY };
  let frame = 0;

  const columnAt = () => columnsOf(root).find((column) => {
    const rect = column.getBoundingClientRect();
    return pointer.x >= rect.left && pointer.x <= rect.right && pointer.y >= rect.top && pointer.y <= rect.bottom;
  });

  // 落点：指针在哪一列、那一列里排在指针前面的卡片有几张（不算被拖的这张）
  const track = () => {
    const column = columnAt();
    if (!column) {
      retarget(root, state, state.own, state.from);
      return;
    }
    const siblings = cardsOf(column).filter((other) => other !== card);
    const index = siblings.filter((other) => {
      const rect = other.getBoundingClientRect();
      return rect.top + rect.height / 2 < pointer.y;
    }).length;
    retarget(root, state, columnKey(column), index);
  };

  const autoScroll = () => {
    cancelAnimationFrame(frame);
    const column = columnAt();
    const scrollers = new Set([scrollParent(board), column ? scrollParent(listOf(column) ?? column) : null]);
    let scrolled = false;
    for (const scroller of scrollers) if (scroller && edgeScroll(scroller, pointer)) scrolled = true;
    if (!scrolled) return;
    track();
    frame = requestAnimationFrame(autoScroll);
  };

  const stop = startMove(event, {
    // 卡片会被重新渲染：捕获放在不动的看板上
    target: root,
    threshold: THRESHOLD,
    onMove: ({ deltaX, deltaY, clientX, clientY }) => {
      pointer = { x: clientX, y: clientY };
      if (!state) {
        if (Math.hypot(deltaX, deltaY) < THRESHOLD) return;
        state = lift(root, card, true);
        if (ghost) {
          const copy = card.cloneNode(true);
          copy.removeAttribute('data-rune-kanban-card');
          copy.removeAttribute('data-dragging');
          copy.querySelector(HANDLE)?.setAttribute('tabindex', '-1');
          ghost.replaceChildren(copy);
          ghost.style.width = `${origin.width}px`;
          ghost.setAttribute('data-shown', '');
        }
      }
      if (ghost) {
        ghost.style.left = `${origin.left + deltaX}px`;
        ghost.style.top = `${origin.top + deltaY}px`;
      }
      track();
      autoScroll();
    },
    onEnd: ({ cancelled }) => {
      cancelAnimationFrame(frame);
      document.removeEventListener('keydown', onEscape, true);
      if (!state) return;
      // 拖过了就不算点击
      root.addEventListener('click', (click) => click.stopPropagation(), { capture: true, once: true });
      // 松开在看板外面：不算数，回到原处
      if (cancelled || !columnAt()) cancel(root, state);
      else drop(root, state);
    },
  });

  // 拖动中按 Esc：取消，回到原处
  const onEscape = (keyEvent) => {
    if (keyEvent.key !== 'Escape' || !state) return;
    keyEvent.preventDefault();
    keyEvent.stopPropagation();
    stop();
  };
  document.addEventListener('keydown', onEscape, true);
}
