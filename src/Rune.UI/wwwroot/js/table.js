// 数据表格（table[data-rune-table]，DataTable）：
// - 点一行交给 C#（rune-command row-click，值是行的 key），点在行里的控件上不算。
// - 单元格导航（data-cell-navigation，同 vue-tsx-pro 的 gridFocus）：表体的数据格（td[data-column]）之间用方向键走，
//   Home / End 到行首行尾（Ctrl 时到表头表尾），Page Up / Down 跳 10 行；只有一格能 Tab 进入（tabindex 由这里写）。
//   虚拟滚动下聚焦的格滚进视口，后面的行随之渲染出来。
// - 编辑：可编辑的格（data-editable）上按 Enter / F2、双击（data-edit-trigger="click" 时单击）或直接打字开始编辑（rune-command edit：行␟列[␟打的字]）；
//   编辑中（td[data-editing]）Enter 提交、Tab / Shift + Tab 提交并到下一个可编辑的格、Esc 放弃，焦点离开这一格时提交。
//   编辑器出现时焦点移进去；C# 写 data-focus-cell（序列号:行␟列）时焦点回到那一格。
import { sendCommand } from './shared.js';

// 格里的控件：点在上面交给控件自己
const CELL_CONTROLS = 'a, button, input, select, textarea, label, [role="button"], [contenteditable="true"]';
const FOCUSABLE = 'input:not([type="hidden"]):not(:disabled), textarea:not(:disabled), select:not(:disabled), button:not(:disabled), [tabindex]:not([tabindex="-1"])';
const PAGE_ROWS = 10;
const SEPARATOR = '\u001f';

const watching = new WeakSet();
// 每张表上次聚焦的格（行 key + 列 key）：重新渲染后 Tab 回来仍落在它上面
const current = new WeakMap();

const rowsOf = (table) => [...table.querySelectorAll(':scope > tbody > tr[data-key]')].map((row) => [...row.querySelectorAll(':scope > td[data-column]')]).filter((cells) => cells.length > 0);
const cellKey = (cell) => `${cell.parentElement.getAttribute('data-key')}${SEPARATOR}${cell.getAttribute('data-column')}`;
const isRtl = (element) => getComputedStyle(element).direction === 'rtl';

/** 只有一格能 Tab 进入：上次聚焦的格，没有时是第一格 */
function syncTabStop(table) {
  if (!table.hasAttribute('data-cell-navigation')) return;
  const cells = rowsOf(table).flat();
  const remembered = current.get(table);
  const stop = cells.find((cell) => cellKey(cell) === remembered) ?? cells[0];
  for (const cell of cells) {
    const value = cell === stop ? '0' : '-1';
    if (cell.getAttribute('tabindex') !== value) cell.setAttribute('tabindex', value);
  }
}

function moveFocus(table, event) {
  const rows = rowsOf(table);
  const row = rows.findIndex((cells) => cells.includes(event.target));
  if (row < 0) return false;
  const column = rows[row].indexOf(event.target);
  const rtl = isRtl(table);
  const last = rows.length - 1;
  const at = (r, c) => {
    const cells = rows[Math.min(Math.max(r, 0), last)];
    return cells[Math.min(Math.max(c, 0), cells.length - 1)];
  };
  const ctrl = event.ctrlKey;
  const targets = {
    ArrowRight: ctrl ? undefined : () => at(row, column + (rtl ? -1 : 1)),
    ArrowLeft: ctrl ? undefined : () => at(row, column + (rtl ? 1 : -1)),
    ArrowDown: ctrl ? undefined : () => at(row + 1, column),
    ArrowUp: ctrl ? undefined : () => at(row - 1, column),
    Home: () => (ctrl ? rows[0][0] : rows[row][0]),
    End: () => (ctrl ? rows[last].at(-1) : rows[row].at(-1)),
    PageDown: ctrl ? undefined : () => at(row + PAGE_ROWS, column),
    PageUp: ctrl ? undefined : () => at(row - PAGE_ROWS, column),
  };
  const next = targets[event.key]?.();
  if (!next) return false;
  event.preventDefault();
  next.focus();
  return true;
}

/** 编辑中的按键：把输入框的值先交出去（Tab 不会失焦，回车不一定触发 change），再提交或放弃 */
function keyInEditor(table, event, cell) {
  if (event.defaultPrevented) return;
  if (event.key === 'Escape') {
    event.preventDefault();
    event.stopPropagation();
    sendCommand(table, 'cancel', cellKey(cell));
    return;
  }
  if (event.key !== 'Enter' && event.key !== 'Tab') return;
  event.preventDefault();
  flushInput(event.target);
  const name = event.key === 'Enter' ? 'commit' : event.shiftKey ? 'commit-prev' : 'commit-next';
  sendCommand(table, name, cellKey(cell));
}

function flushInput(target) {
  if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement) target.dispatchEvent(new Event('change', { bubbles: true }));
}

function startEdit(table, cell, typed) {
  sendCommand(table, 'edit', typed === undefined ? cellKey(cell) : `${cellKey(cell)}${SEPARATOR}${typed}`);
}

function onKeydown(table, event) {
  if (!table.hasAttribute('data-cell-navigation') || event.altKey || event.metaKey) return;
  const cell = event.target.closest?.('td[data-column]');
  if (!cell || !table.contains(cell)) return;
  if (cell.hasAttribute('data-editing')) return keyInEditor(table, event, cell);
  if (event.target !== cell) return;
  if (moveFocus(table, event)) return;
  if (!cell.hasAttribute('data-editable')) return;
  if (event.key === 'Enter' || event.key === 'F2') {
    event.preventDefault();
    startEdit(table, cell);
  } else if (event.key.length === 1 && !event.ctrlKey) {
    // 直接打字：用打的字替换原值
    event.preventDefault();
    startEdit(table, cell, event.key);
  }
}

/** 焦点离开编辑中的格（点了别处）：提交 */
function onFocusout(table, event) {
  const cell = event.target.closest?.('td[data-editing]');
  if (!cell || cell.contains(event.relatedTarget)) return;
  flushInput(event.target);
  sendCommand(table, 'commit', cellKey(cell));
}

function onFocusin(table, event) {
  const cell = event.target;
  if (!(cell instanceof HTMLTableCellElement) || !cell.hasAttribute('data-column') || !table.contains(cell)) return;
  current.set(table, cellKey(cell));
  syncTabStop(table);
}

function pointerEdit(table, event, trigger) {
  if (table.getAttribute('data-edit-trigger') !== trigger) return;
  const cell = event.target.closest?.('td[data-editable]:not([data-editing])');
  if (!cell || !table.contains(cell) || event.target.closest(CELL_CONTROLS)) return;
  startEdit(table, cell);
}

/** C# 写的 data-focus-cell：编辑完、放弃后焦点回到格上；新出现的编辑器：焦点移进去 */
function applyFocus(table) {
  const request = table.getAttribute('data-focus-cell');
  if (request && request !== table.__runeFocusRequest) {
    table.__runeFocusRequest = request;
    const key = request.slice(request.indexOf(':') + 1);
    const cell = rowsOf(table).flat().find((item) => cellKey(item) === key);
    if (cell) {
      current.set(table, key);
      syncTabStop(table);
      cell.focus();
    }
  }
  const editor = table.querySelector('td[data-editing] [data-rune-cell-editor]');
  if (editor && editor !== table.__runeEditor) {
    table.__runeEditor = editor;
    if (!editor.contains(document.activeElement)) editor.querySelector('[autofocus]')?.focus() ?? editor.querySelector(FOCUSABLE)?.focus();
  }
  if (!editor) table.__runeEditor = null;
}

/** 表格出现时接管点行、网格导航与编辑的按键 */
export function watchTable(table) {
  if (watching.has(table)) return;
  watching.add(table);
  table.addEventListener('click', (event) => {
    pointerEdit(table, event, 'click');
    if (!table.hasAttribute('data-row-click') || event.target.closest(CELL_CONTROLS)) return;
    const row = event.target.closest('tbody > tr[data-key]');
    if (row && table.contains(row)) sendCommand(table, 'row-click', row.getAttribute('data-key'));
  });
  table.addEventListener('dblclick', (event) => pointerEdit(table, event, 'dblclick'));
  table.addEventListener('keydown', (event) => onKeydown(table, event));
  table.addEventListener('focusout', (event) => onFocusout(table, event));
  table.addEventListener('focusin', (event) => onFocusin(table, event));
  new MutationObserver(() => {
    if (!table.isConnected) return;
    syncTabStop(table);
    applyFocus(table);
  }).observe(table, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-focus-cell', 'data-editing', 'data-cell-navigation'] });
  syncTabStop(table);
  applyFocus(table);
}
