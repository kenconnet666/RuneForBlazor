// 数据表格（table[data-rune-table]，DataTable）：点一行交给 C#（rune-command row-click，值是行的 key），点在行里的控件上不算。
import { sendCommand } from './shared.js';

// 格里的控件：点在上面交给控件自己
const CELL_CONTROLS = 'a, button, input, select, textarea, label, [role="button"], [contenteditable="true"]';

const watching = new WeakSet();

/** 表格出现时接管点行 */
export function watchTable(table) {
  if (watching.has(table)) return;
  watching.add(table);
  table.addEventListener('click', (event) => {
    if (!table.hasAttribute('data-row-click') || event.target.closest(CELL_CONTROLS)) return;
    const row = event.target.closest('tbody > tr[data-key]');
    if (row && table.contains(row)) sendCommand(table, 'row-click', row.getAttribute('data-key'));
  });
}
