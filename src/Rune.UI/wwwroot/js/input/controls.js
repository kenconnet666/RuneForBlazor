// 输入框一类：数字框的按键加减、点外壳聚焦、字数计数、按住连续点击、多行自动增高、半选、键盘命令、跳到字段的链接、清空后回到输入框、显示密码。

import { popoverOfHost, pressedWhileOpen } from './lists.js';
import { reducedMotion, sendCommand, TABBABLE } from '../core/shared.js';

/** 点了 data-rune-refocus 的按钮（清空）：焦点交给它指向的输入框 */
export function refocusFrom(target) {
  const button = target.closest('[data-rune-refocus]');
  if (button) document.getElementById(button.getAttribute('data-rune-refocus'))?.focus({ preventScroll: true });
}

/** 点了显示密码按钮（data-rune-reveal）：切换输入框的 type 与按钮的 aria-pressed */
export function toggleReveal(target) {
  const button = target.closest('[data-rune-reveal]');
  if (!button || button.matches(':disabled')) return;
  const input = document.getElementById(button.getAttribute('data-rune-reveal'));
  if (!(input instanceof HTMLInputElement)) return;
  const shown = input.type === 'password';
  input.type = shown ? 'text' : 'password';
  button.setAttribute('aria-pressed', String(shown));
}

export function syncIndeterminate(input) {
  input.indeterminate = input.dataset.indeterminate === 'true';
}

export function onFocusLink(event) {
  const link = event.target instanceof Element ? event.target.closest('[data-rune-focus]') : null;
  const control = link && document.getElementById(link.getAttribute('data-rune-focus'));
  if (!control) return;
  event.preventDefault();
  // 控件是一组（DateField 的分段、单选组）、本身不能聚焦时：聚焦里面第一个能聚焦的
  (control.matches(TABBABLE) ? control : control.querySelector(TABBABLE) ?? control).focus();
  // 跳到正文（SkipLink）滚到起始处；表单错误汇总里的链接把控件放在中间
  control.scrollIntoView({ block: link.getAttribute('data-focus-block') ?? 'center', behavior: reducedMotion.matches ? 'auto' : 'smooth' });
}

/**
 * 表单提交没通过（data-rune-invalid-submit 变了）：焦点到错误汇总（data-rune-error-summary），没有汇总时到第一个出错的控件。
 * 等一帧再找：控件的 aria-invalid 可能在随后的渲染里才写上。
 */
export function focusInvalid(form) {
  requestAnimationFrame(() => {
    const target = form.querySelector('[data-rune-error-summary]') ?? firstInvalidControl(form);
    if (!target) return;
    target.focus({ preventScroll: true });
    target.scrollIntoView({ block: 'center', behavior: reducedMotion.matches ? 'auto' : 'smooth' });
  });
}

// 单选组、勾选组的 aria-invalid 写在组上（div）：聚焦组里选中的那项，没有就第一项
function firstInvalidControl(form) {
  const invalid = form.querySelector('[aria-invalid="true"]');
  if (!invalid || invalid.matches('input, select, textarea, button, [tabindex]')) return invalid;
  return invalid.querySelector('input:checked, input:not(:disabled), button:not(:disabled), [tabindex]');
}

/**
 * 要 C# 处理的按键转成命令（不用每次按键都往返一次）；在列表的键盘处理之后调用，按键已经被用掉（defaultPrevented）时不发：
 * Backspace 时取 data-rune-backspace（输入框里是空的时候，多选移除最后一项）；
 * Esc 时取 data-rune-escape（弹层关着、输入框里有字的时候，Combobox 清空；弹层开着时 Esc 先关弹层）；
 * Enter 时取 data-rune-enter（输入框里有字、列表没有选中高亮项的时候，TagsInput 把文字加成标签）。
 */
export function sendKeyCommand(event) {
  const target = event.target;
  if (!(target instanceof Element) || event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey) return;
  const typed = target instanceof HTMLInputElement && target.value.trim() !== '';
  let name = null;
  if (event.key === 'Backspace' && !(target instanceof HTMLInputElement && target.value !== '')) name = target.getAttribute('data-rune-backspace');
  else if (event.key === 'Escape' && typed && !popoverOfHost(target)?.matches(':popover-open')) name = target.getAttribute('data-rune-escape');
  else if (event.key === 'Enter' && typed) name = target.getAttribute('data-rune-enter');
  if (!name) return;
  event.preventDefault();
  sendCommand(target, name);
}

/**
 * 粘贴到带 data-rune-paste="命令" 的输入框：内容里有分隔符（data-rune-delimiters 里的字符）或换行、制表符时拦下，
 * 把整段文字随命令交给 C# 拆开（TagsInput 一次加几个标签）；只有一段时照常粘贴。
 */
export function sendPasteCommand(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const name = target.getAttribute('data-rune-paste');
  if (!name) return;
  const text = event.clipboardData?.getData('text') ?? '';
  const separators = [...(target.getAttribute('data-rune-delimiters') ?? ''), '\n', '\r', '\t'];
  if (![...text].some((char) => separators.includes(char))) return;
  event.preventDefault();
  sendCommand(target, name, text);
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
  // 点在框里能聚焦的元素上（输入元素、按钮、链接、日期的段、带 tabindex 的取色块）：照常由浏览器聚焦它；只认框里的，外面的滚动区域也带 tabindex
  const focusable = target.closest('input, textarea, select, button, a[href], [tabindex], [contenteditable]');
  if (focusable && focusable !== shell && shell.contains(focusable)) return;
  // 分段输入（DateField、TimeField、DateRangePicker）：焦点进第一个没填的段（都填了时第一段）
  const segments = shell.querySelector(':scope > [data-rune-date-segments]');
  if (segments) {
    if (segments.getAttribute('aria-disabled') === 'true') return;
    event.preventDefault();
    // 有几组时（DateRangePicker）在各组里找
    const groups = ':scope > [data-rune-date-segments] > [role="spinbutton"]';
    (shell.querySelector(`${groups}[data-placeholder]`) ?? shell.querySelector(groups))?.focus();
    return;
  }
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

/**
 * 分组显示的输入框（data-rune-format="3,4,4"，TextField 的 Format）：输入时取出原文（只留接受的字符、截到最长）、按组重排，
 * 光标放回刚才那个字符的后面；赶在 Blazor 读值之前（捕获阶段），C# 拿到的是分好组的文字，再去掉分隔写回原文。
 */
export function formatInput(event) {
  const input = event.target;
  if (!(input instanceof HTMLInputElement) || !input.hasAttribute('data-rune-format') || event.isComposing) return;
  const format = textFormatOf(input);
  const caret = input.selectionStart ?? input.value.length;
  const before = format.parse(input.value.slice(0, caret)).length;
  const raw = format.parse(input.value);
  const shown = format.format(raw);
  if (shown === input.value) return;
  input.value = shown;
  const position = displayPosition(format, raw, before);
  input.setSelectionRange(position, position);
}

const textFormats = new WeakMap();

function textFormatOf(input) {
  const cached = textFormats.get(input);
  const key = ['format', 'format-accept', 'format-max', 'format-separator', 'format-upper'].map((name) => input.getAttribute(`data-rune-${name}`)).join('|');
  if (cached?.key === key) return cached;
  const groups = input.getAttribute('data-rune-format').split(',').map(Number);
  const accept = new RegExp(`^${input.getAttribute('data-rune-format-accept') || '[0-9]'}$`);
  const max = Number(input.getAttribute('data-rune-format-max')) || groups.reduce((sum, size) => sum + size, 0);
  const separator = input.getAttribute('data-rune-format-separator') ?? ' ';
  const upper = input.hasAttribute('data-rune-format-upper');
  const format = {
    key,
    parse: (text) => Array.from(upper ? text.toUpperCase() : text).filter((char) => accept.test(char)).slice(0, max).join(''),
    format: (raw) => {
      const parts = [];
      for (let index = 0, rest = raw; rest; index++) {
        const size = groups[Math.min(index, groups.length - 1)];
        parts.push(rest.slice(0, size));
        rest = rest.slice(size);
      }
      return parts.join(separator);
    },
  };
  textFormats.set(input, format);
  return format;
}

/** 原文里第 count 个字符之后，对应显示文字里的位置（同 vue-tsx-pro 的 displayPosition） */
function displayPosition(format, raw, count) {
  if (count <= 0) return 0;
  const shown = format.format(raw);
  let seen = 0;
  for (let index = 0; index < shown.length; index++) {
    if (format.parse(shown[index]) !== '') seen++;
    if (seen === count) return index + 1;
  }
  return shown.length;
}
