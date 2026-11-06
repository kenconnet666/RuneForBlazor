// 提及（data-rune-mentions 的 textarea，同 vue-tsx-pro 的 Mentions）：输入 @ 弹出人员列表，选中后写成“@名字 ”。
// 判断光标前是不是正在写的提及、弹出与关闭、列表的键盘、写入选中的名字都在这里；@ 后面的文字发给 C#（rune-command mention-query）过滤。
// - 弹出：@ 在开头或前面不是字母数字（邮箱里的 @ 不算），@ 与光标之间没有空白；列表贴在 @ 的位置（data-caret-anchor，见 floating.js）。
// - 键盘：↑↓ 移动、Enter 或 Tab 选中、Esc 关闭（这个 @ 不再弹出，直到重新输入）；焦点一直在文本框里。中文输入法组合期间不弹出、不过滤。

import { moveInList } from './lists.js';
import { sendCommand } from './shared.js';

const WORD_BEFORE = /[A-Za-z0-9_.]/;
// 文本框 → { at: 正在提及的 @ 的位置, query, dismissed: 按 Esc 关掉的那个 @ }
const states = new WeakMap();

const isMentions = (target) => target instanceof HTMLTextAreaElement && target.hasAttribute('data-rune-mentions');
const panelOf = (textarea) => document.getElementById(textarea.getAttribute('data-rune-popover'));
const stateOf = (textarea) => {
  let state = states.get(textarea);
  if (!state) states.set(textarea, (state = { at: null, query: null, dismissed: null }));
  return state;
};

/** 看光标前面是不是正在写的提及：是就把 @ 后面的文字发给 C# 并打开，不是就关闭 */
function detect(textarea) {
  if (textarea.readOnly || textarea.disabled) return;
  const prefix = textarea.getAttribute('data-rune-mentions') || '@';
  const state = stateOf(textarea);
  const panel = panelOf(textarea);
  const caret = textarea.selectionStart;
  const before = textarea.value.slice(0, caret);
  const at = before.lastIndexOf(prefix);
  const typed = at < 0 ? '' : before.slice(at + prefix.length);
  const mentioning = at >= 0 && textarea.selectionEnd === caret && !WORD_BEFORE.test(before[at - 1] ?? '')
    && !/\s/.test(typed) && typed.length <= 30;
  if (!mentioning || at === state.dismissed) {
    if (!mentioning) state.dismissed = null;
    state.at = null;
    state.query = null;
    if (panel?.matches(':popover-open')) panel.hidePopover();
    return;
  }
  if (state.at !== at || state.query !== typed) sendCommand(textarea, 'mention-query', typed);
  state.at = at;
  state.query = typed;
  // 定位按这个 @ 的位置（floating.js 读 data-mention-at）
  textarea.dataset.mentionAt = String(at);
  if (panel && !panel.matches(':popover-open')) panel.showPopover();
}

export function onMentionsInput(event) {
  if (isMentions(event.target) && !event.isComposing) detect(event.target);
}

export function onMentionsCompositionEnd(event) {
  if (isMentions(event.target)) detect(event.target);
}

export function onMentionsClick(target) {
  if (isMentions(target)) detect(target);
}

/** 列表打开时的按键：处理了返回 true */
export function keyMentions(event) {
  const textarea = event.target;
  if (!isMentions(textarea) || event.isComposing) return false;
  const panel = panelOf(textarea);
  if (!panel?.matches(':popover-open')) return false;
  if (event.key === 'Escape') {
    event.preventDefault();
    const state = stateOf(textarea);
    state.dismissed = state.at;
    panel.hidePopover();
    return true;
  }
  if (event.key === 'Tab') {
    const active = document.getElementById(textarea.getAttribute('aria-activedescendant') ?? '');
    if (!active) return false;
    event.preventDefault();
    active.click();
    return true;
  }
  // ↑↓ 移动高亮、Enter 选中（点高亮项）；Home / End 留给文本框
  if (event.key === 'ArrowUp' || event.key === 'ArrowDown' || event.key === 'Enter') {
    moveInList(textarea, event);
    return true;
  }
  return false;
}

/** 移动光标（方向键、Home / End）后重新判断；↑↓ 在列表打开时是移动高亮，不算 */
export function onMentionsKeyUp(event) {
  const textarea = event.target;
  if (!isMentions(textarea) || event.isComposing) return;
  const open = panelOf(textarea)?.matches(':popover-open');
  if (open && (event.key === 'ArrowUp' || event.key === 'ArrowDown')) return;
  if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key) || !open) detect(textarea);
}

/** 文本框失焦：关上列表 */
export function onMentionsFocusOut(event) {
  if (!isMentions(event.target)) return;
  const panel = panelOf(event.target);
  if (panel?.matches(':popover-open')) panel.hidePopover();
}

/** 选中了列表里的一项（点击，或 Enter / Tab 转成的点击）：把“@过滤词”换成“@名字 ”，光标放在后面，通知 C# 文字变了 */
export function pickMention(target) {
  const option = target.closest('[data-rune-mentions-panel] [role="option"]:not([aria-disabled="true"])');
  if (!option) return;
  const panel = option.closest('[data-rune-mentions-panel]');
  const textarea = document.getElementById(panel.getAttribute('data-rune-mentions-panel'));
  const state = textarea && states.get(textarea);
  if (!state || state.at === null) return;
  const prefix = textarea.getAttribute('data-rune-mentions') || '@';
  const inserted = `${prefix}${option.getAttribute('data-label')} `;
  const start = state.at;
  const caret = textarea.selectionStart;
  textarea.value = textarea.value.slice(0, start) + inserted + textarea.value.slice(caret);
  state.at = null;
  state.query = null;
  textarea.focus();
  textarea.setSelectionRange(start + inserted.length, start + inserted.length);
  textarea.dispatchEvent(new Event('input', { bubbles: true }));
  textarea.dispatchEvent(new Event('change', { bubbles: true }));
  if (panel.matches(':popover-open')) panel.hidePopover();
}
