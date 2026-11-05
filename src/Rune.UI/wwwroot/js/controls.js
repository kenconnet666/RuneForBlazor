// 输入框一类：数字框的按键加减、点外壳聚焦、字数计数、按住连续点击、多行自动增高、半选、键盘命令、跳到字段的链接。

import { pressedWhileOpen } from './lists.js';
import { reducedMotion } from './shared.js';

export function syncIndeterminate(input) {
  input.indeterminate = input.dataset.indeterminate === 'true';
}

export function onFocusLink(event) {
  const link = event.target instanceof Element ? event.target.closest('[data-rune-focus]') : null;
  const control = link && document.getElementById(link.getAttribute('data-rune-focus'));
  if (!control) return;
  event.preventDefault();
  control.focus();
  control.scrollIntoView({ block: 'center', behavior: reducedMotion.matches ? 'auto' : 'smooth' });
}

/** Backspace（输入框里是空的时候）：把 data-rune-backspace 的命令交给 C#（多选下拉移除最后一项）。 */
export function sendBackspaceCommand(event) {
  const target = event.target;
  if (!(target instanceof Element) || event.altKey || event.ctrlKey || event.metaKey) return;
  const name = target.getAttribute('data-rune-backspace');
  if (!name || (target instanceof HTMLInputElement && target.value !== '')) return;
  event.preventDefault();
  target.dispatchEvent(new CustomEvent('rune-command', { bubbles: true, detail: { name } }));
}


const spinKeys = { ArrowUp: 1, ArrowDown: -1, PageUp: 10, PageDown: -10 };

/**
 * 按键加减：从输入框里现在的文字算（还没提交的输入也算），写回后发 change 交给 C#（它再解析、夹到范围、对齐步长）。
 * 文字按 data-decimal 的小数点解析，忽略千分位与单位；只读、不可用时不处理。
 */
export function spin(input, event) {
  if (input.readOnly || input.disabled) return;
  const min = parseNumber(input.getAttribute('aria-valuemin'));
  const max = parseNumber(input.getAttribute('aria-valuemax'));
  let next;
  if (event.key === 'Home' && min !== null) next = min;
  else if (event.key === 'End' && max !== null) next = max;
  else if (event.key in spinKeys) {
    const step = parseNumber(input.dataset.step) ?? 1;
    const big = parseNumber(input.dataset.bigStep);
    const delta = Math.abs(spinKeys[event.key]) === 10 ? (big ?? step * 10) * Math.sign(spinKeys[event.key]) : step * spinKeys[event.key];
    const current = parseLocal(input.value, input.dataset.decimal ?? '.') ?? min ?? 0;
    next = current + delta;
    if (min !== null) next = Math.max(min, next);
    if (max !== null) next = Math.min(max, next);
    // 浮点误差：按步长的小数位数取整（0.1 + 0.2 → 0.3）
    const digits = Math.max(decimals(step), decimals(current));
    next = Number(next.toFixed(Math.min(digits, 20)));
  } else return;
  event.preventDefault();
  input.value = String(next).replace('.', input.dataset.decimal ?? '.');
  input.dispatchEvent(new Event('change', { bubbles: true }));
}

function parseNumber(text) {
  if (text === null || text === undefined || text === '') return null;
  const value = Number(text);
  return Number.isFinite(value) ? value : null;
}

// 按文化的小数点解析显示的文字：全角转半角，去掉千分位、货币符号、单位
function parseLocal(text, decimalSeparator) {
  const halfWidth = text.replace(/[０-９]/g, (c) => String.fromCharCode(c.charCodeAt(0) - 0xfee0)).replace(/[．。]/g, '.').replace(/[－−]/g, '-');
  const kept = [...halfWidth].filter((c) => /[0-9-]/.test(c) || c === decimalSeparator).join('');
  return parseNumber(decimalSeparator === '.' ? kept : kept.replace(decimalSeparator, '.'));
}

function decimals(value) {
  const text = String(value);
  const dot = text.indexOf('.');
  return dot < 0 ? 0 : text.length - dot - 1;
}

/** 点在输入框外壳的空白处（不是输入元素本身，也不是框里的按钮、链接）：焦点交给里面的输入元素，不让外壳把焦点抢走 */
export function focusOnShell(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const shell = target.closest('[data-rune-control]');
  if (!shell) return;
  // 下拉：记下按下时弹层开没开（见 toggleFromShell）
  const panel = shell.querySelector(':scope > button[role="combobox"]')?.popoverTargetElement;
  if (panel?.matches(':popover-open')) pressedWhileOpen.add(shell);
  else pressedWhileOpen.delete(shell);
  const button = target.closest('button');
  if (button && shell.contains(button) && button.getAttribute('tabindex') === '-1' && !button.disabled) {
    // 焦点留在输入框里（读屏、键盘接着用）；按住的按钮连续点击
    event.preventDefault();
    if (button.hasAttribute('data-rune-repeat')) repeatWhileHeld(button);
    return;
  }
  if (target.closest('input, textarea, select, button, a')) return;
  const control = shell.querySelector(':scope > :is(input, textarea, select, button[role="combobox"])');
  if (!control || control.disabled) return;
  event.preventDefault();
  control.focus();
}

/** 输入时更新字数计数里看得见的字数（读屏的完整句子在值提交后由组件更新） */
export function updateCount(input) {
  const counter = document.getElementById(input.getAttribute('data-rune-count'));
  if (!counter) return;
  const count = input.value.length;
  const max = Number(counter.getAttribute('data-max')) || null;
  const value = counter.querySelector('[aria-hidden="true"]');
  if (value) value.textContent = max ? `${count} / ${max}` : String(count);
  counter.toggleAttribute('data-over', max != null && count > max);
}

/** 按住时连续点击：400ms 后每 80ms 点一次（第一下由松开时的 click 完成），松开、移开或按钮变成不可用时停 */
function repeatWhileHeld(button) {
  let timer = setTimeout(function tick() {
    if (button.disabled || !button.isConnected) return stop();
    button.click();
    timer = setTimeout(tick, 80);
  }, 400);
  const stop = () => {
    clearTimeout(timer);
    for (const type of ['pointerup', 'pointercancel', 'pointerleave']) button.removeEventListener(type, stop);
  };
  for (const type of ['pointerup', 'pointercancel', 'pointerleave']) button.addEventListener(type, stop);
}

/** 多行文本框随内容增高：先让高度回到 auto 才量得出变矮后的 scrollHeight；上下限由 CSS 夹住 */
export function autosize(textarea) {
  textarea.style.height = 'auto';
  textarea.style.height = `${textarea.scrollHeight}px`;
}
