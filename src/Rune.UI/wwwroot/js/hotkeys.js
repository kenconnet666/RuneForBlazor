// 打开浮层的快捷键（data-rune-hotkey="mod+k"，同 vue-tsx-pro 的 useHotkeys）：Mod 在 macOS 上是 ⌘、其他系统是 Ctrl。
// 写在对话框（命令面板）上：按下时打开它（或关上已经打开的），输入框里照样生效。显示快捷键的 Kbd 写 data-rune-hotkey-label，
// macOS 上换成 ⌘ 的写法。

const isMac = /Mac|iPhone|iPad/.test(navigator.platform || navigator.userAgent);

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

/** 按下快捷键：返回要切换的浮层，没有时返回 null。 */
export function hotkeyTarget(event) {
  if (event.defaultPrevented || event.repeat) return null;
  for (const overlay of document.querySelectorAll('[data-rune-hotkey]')) {
    if (matches(event, overlay.getAttribute('data-rune-hotkey'))) {
      event.preventDefault();
      return overlay;
    }
  }
  return null;
}

/** 显示快捷键的文字：macOS 上 Ctrl 换成 ⌘（插入时调用）。 */
export function formatHotkeyLabel(label) {
  if (isMac && label.dataset.platformDone === undefined) {
    label.dataset.platformDone = '';
    label.textContent = label.textContent.replace(/Ctrl\s*/i, '⌘');
  }
}
