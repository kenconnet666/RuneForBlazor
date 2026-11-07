// 级联选择（data-rune-cascader 的弹层，同 vue-tsx-pro 的 Cascader）：每一列是一个列表（data-rune-list），
// 列里的 ↑↓、Home / End、类型搜索、Enter 由 lists.js 处理；这里管列之间：→ 进入下一级（下一列渲染出来后把焦点移过去），
// ← 回到上一级，触发按钮上 ↓ 打开，搜索框空着时 ↓ 进入各列；高亮变了发 rune-command（highlight），C# 据此显示下一列。

import { isRtl, sendCommand } from '../core/shared.js';

const COLUMN = '[data-rune-cascader-column]';
const levelOf = (column) => Number(column.getAttribute('data-rune-cascader-column'));
const columnAt = (panel, level) => panel.querySelector(`${COLUMN}[data-rune-cascader-column="${level}"]`);
const activeOf = (column) => Number(column.getAttribute('data-rune-active') ?? -1);
const optionAt = (column, index) => (index >= 0 ? document.getElementById(`${column.id}-${index}`) : null);

/** 按键：处理了（不必再交给列表）时返回 true */
export function keyCascader(event) {
  const target = event.target;
  if (!(target instanceof Element) || event.altKey || event.ctrlKey || event.metaKey) return false;

  // 触发按钮上 ↓ 打开（Enter、空格由原生的 popovertarget 打开）
  if (target.matches('[data-rune-cascader-trigger]') && event.key === 'ArrowDown') {
    const panel = target.popoverTargetElement;
    if (panel && !panel.matches(':popover-open')) {
      event.preventDefault();
      panel.showPopover({ source: target });
    }
    return true;
  }

  const panel = target.closest('[data-rune-cascader]');
  if (!panel) return false;

  // 搜索框空着：↓ 进入各列（已选路径的最后一列）
  if (target.matches('input[data-rune-cascader-search]') && !target.value.trim() && event.key === 'ArrowDown') {
    event.preventDefault();
    // 打开后各列由 C# 渲染（Server 模式要等一次往返）：还没有时等它们出来
    whenRendered(panel, () => {
      const columns = [...panel.querySelectorAll(COLUMN)];
      return columns.findLast((column) => column.hasAttribute('data-rune-active')) ?? columns[0];
    });
    return true;
  }

  if (!target.matches(COLUMN)) return false;
  const column = target;
  const level = levelOf(column);
  const forward = isRtl(panel) ? 'ArrowLeft' : 'ArrowRight';
  const backward = isRtl(panel) ? 'ArrowRight' : 'ArrowLeft';
  const option = optionAt(column, activeOf(column));
  const branch = option?.matches('[aria-haspopup]:not([aria-disabled="true"])');

  if (event.key === forward) {
    event.preventDefault();
    if (!branch) return true;
    // 下一列已经是这一项的（高亮时 C# 已经显示出来）：直接进去，不等往返
    const next = columnAt(panel, level + 1);
    if (next?.querySelector('[role="option"]') && next.getAttribute('aria-label') === option.textContent.trim()) {
      next.focus();
      return true;
    }
    sendCommand(column, 'enter', String(activeOf(column)));
    focusWhenRendered(panel, level + 1, option.textContent.trim());
    return true;
  }
  if (event.key === backward) {
    event.preventDefault();
    if (level > 0) columnAt(panel, level - 1)?.focus();
    return true;
  }
  // 有下一级的项上按 Enter：点它（ChangeOnSelect 时选中），再进入下一级
  if (event.key === 'Enter' && branch) {
    event.preventDefault();
    option.click();
    focusWhenRendered(panel, level + 1, option.textContent.trim());
    return true;
  }
  // 其余按键交给列表；移动了高亮再告诉 C#（列表处理完之后）
  const before = activeOf(column);
  queueMicrotask(() => {
    if (activeOf(column) !== before) reportHighlight(column);
  });
  return false;
}

/** 高亮换了：告诉 C#，下一列跟着换 */
function reportHighlight(column) {
  const index = activeOf(column);
  if (index >= 0) sendCommand(column, 'highlight', String(index));
}

/** 这一项的下一列（名称是它的文字）渲染出来（懒加载要等取回来）后把焦点移过去 */
function focusWhenRendered(panel, level, label) {
  whenRendered(panel, () => {
    const column = columnAt(panel, level);
    return column?.getAttribute('aria-label') === label && column.querySelector('[role="option"]') ? column : null;
  });
}

/** find 找到要聚焦的列（C# 渲染出来）后聚焦它；弹层关了就不等了，最多等 5 秒 */
function whenRendered(panel, find) {
  const started = performance.now();
  const tick = () => {
    if (!panel.matches(':popover-open')) return;
    const column = find();
    if (column) {
      column.focus();
      return;
    }
    if (performance.now() - started < 5000) requestAnimationFrame(tick);
  };
  tick();
}

/** 焦点进到一列：还没有高亮项时高亮第一个能选的，并告诉 C# */
export function onCascaderFocusIn(event) {
  const column = event.target;
  if (!(column instanceof Element) || !column.matches(COLUMN) || column.hasAttribute('data-rune-active')) return;
  const options = [...column.querySelectorAll('[role="option"]')];
  const first = options.findIndex((option) => option.getAttribute('aria-disabled') !== 'true');
  if (first < 0) return;
  column.setAttribute('data-rune-active', String(first));
  column.setAttribute('aria-activedescendant', `${column.id}-${first}`);
  options[first].setAttribute('data-highlighted', '');
  reportHighlight(column);
}

/** 点了一列里的选项：焦点到这一列（按下时焦点没有移动，见 keepFocusOnListHost） */
export function clickCascader(target) {
  const column = target.closest(`${COLUMN}`);
  if (column && target.closest('[role="option"]') && document.activeElement !== column) column.focus();
}
