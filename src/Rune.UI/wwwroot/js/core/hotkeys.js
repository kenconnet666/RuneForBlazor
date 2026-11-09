// 快捷键（data-rune-hotkey="mod+k"，同 vue-tsx-pro 的 useHotkeys）：Mod 在 macOS 上是 ⌘、其他系统是 Ctrl。
// 写在对话框（命令面板）上：按下时打开它（或关上已经打开的），输入框里照样生效。
// 写在菜单项上：菜单挂载期间（不必打开）按下时点击它；焦点在输入框里时不触发，有模态对话框时只认对话框里的。
// 显示快捷键的元素写 data-rune-hotkey-label（值是原写法时按它重写），macOS 上换成 ⌘⇧ 这样的符号。

const isMac = /Mac|iPhone|iPad/.test(navigator.platform || navigator.userAgent);

const SYMBOLS = { ctrl: '⌃', alt: '⌥', shift: '⇧', meta: '⌘' };
const ARROWS = { arrowup: '↑', arrowdown: '↓', arrowleft: '←', arrowright: '→' };
/** 输入法组合中的按键（同 vue-tsx-pro 的 isComposingKey）：方向键、Enter、退格是给输入法的（选候选、上屏、删组合里的字）；Safari 组合开始那一下 isComposing 还是 false，只有 keyCode 229 */
export function isComposingKey(event) {
  return event.isComposing || event.keyCode === 229;
}

export const TYPING = 'input, textarea, select, [contenteditable]:not([contenteditable="false"])';

function matches(event, hotkey) {
  const parts = hotkey.toLowerCase().split('+');
  const key = parts.pop();
  const mod = parts.includes('mod');
  const wantCtrl = parts.includes('ctrl') || (mod && !isMac);
  const wantMeta = parts.includes('meta') || (mod && isMac);
  return event.key.toLowerCase() === key
    && event.ctrlKey === wantCtrl && event.metaKey === wantMeta
    && event.altKey === parts.includes('alt') && event.shiftKey === parts.includes('shift');
}

/** 菜单项的快捷键这时能不能触发：不在输入框里打字，模态对话框打开时只认对话框里的 */
function itemHotkeyAllowed(item, event) {
  if (event.target instanceof Element && event.target.closest(TYPING)) return false;
  const modal = document.querySelector('dialog[open]:modal');
  return !modal || modal.contains(item);
}

/** 按下快捷键：返回要切换的浮层或要点击的菜单项（菜单项上带 role），没有时返回 null。 */
export function hotkeyTarget(event) {
  if (event.defaultPrevented || event.repeat) return null;
  for (const element of document.querySelectorAll('[data-rune-hotkey]')) {
    if (!matches(event, element.getAttribute('data-rune-hotkey'))) continue;
    if (element.matches('[role^="menuitem"]') && !itemHotkeyAllowed(element, event)) continue;
    event.preventDefault();
    return element;
  }
  return null;
}

/** macOS 上快捷键的显示文字：修饰键用符号（⌘⇧K），方向键是箭头；其他系统的写法服务器已经写好（Hotkeys.Format） */
function formatMacHotkey(combo) {
  return combo.trim().split(/\s+/).map((step) => {
    const parts = step.toLowerCase().split('+');
    const raw = step.split('+').pop();
    const key = ARROWS[raw.toLowerCase()] ?? (raw.length === 1 ? raw.toUpperCase() : raw.charAt(0).toUpperCase() + raw.slice(1));
    const has = (name) => parts.includes(name) || (name === 'meta' && parts.includes('mod'));
    return ['ctrl', 'alt', 'shift', 'meta'].filter(has).map((name) => SYMBOLS[name]).join('') + key;
  }).join(' ');
}

/** 显示快捷键的文字按当前系统重写（插入时调用）：属性值是原写法时按它格式化，没写值的（命令面板）把 Ctrl 换成 ⌘。 */
export function formatHotkeyLabel(label) {
  if (!isMac || label.dataset.platformDone !== undefined) return;
  label.dataset.platformDone = '';
  const combo = label.getAttribute('data-rune-hotkey-label');
  label.textContent = combo ? formatMacHotkey(combo) :label.textContent.replace(/Ctrl\s*/i, '⌘');
}
