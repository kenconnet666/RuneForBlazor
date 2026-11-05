// AppShell 的全屏（data-rune-fullscreen="true|false" 写在外壳上）：状态在 C#，浏览器全屏只是顺带的。
// 变成 true 时请求浏览器全屏（点击后不久，仍在用户激活的时限内），变回 false 时退出；浏览器不让全屏（iframe 没有 allow="fullscreen"）时照样只隐藏顶栏和侧栏。
// 用户自己离开浏览器全屏（Esc、F11），或没进浏览器全屏时按 Esc：发出冒泡的 rune-command（fullscreen-exit），由 C# 改回状态。

/** 进了浏览器全屏的外壳：只有它离开浏览器全屏才算退出 */
let active = null;

export function syncFullscreen(shell) {
  if (shell.getAttribute('data-rune-fullscreen') === 'true') {
    if (active === shell || document.fullscreenElement) return;
    active = shell;
    const request = document.documentElement.requestFullscreen?.();
    if (!request) active = null;
    else request.catch(() => {
      if (active === shell) active = null;
    });
  } else if (active === shell) {
    active = null;
    if (document.fullscreenElement) document.exitFullscreen().catch(() => undefined);
  }
}

export function onFullscreenChange() {
  if (document.fullscreenElement || !active) return;
  const shell = active;
  active = null;
  if (shell.isConnected && shell.getAttribute('data-rune-fullscreen') === 'true') requestExit(shell);
}

// 没进浏览器全屏时由这里处理 Esc（进去了的话浏览器自己退出）；开着浮层时 Esc 先关浮层
export function exitFullscreenOnEscape(event) {
  if (event.key !== 'Escape' || event.defaultPrevented || document.fullscreenElement) return;
  const shell = document.querySelector('[data-rune-fullscreen="true"]');
  if (shell && !document.querySelector('dialog[open], :popover-open')) requestExit(shell);
}

// 外壳被移走（换了页）时还在浏览器全屏：退出
export function releaseFullscreen() {
  if (!active || active.isConnected) return;
  active = null;
  if (document.fullscreenElement) document.exitFullscreen().catch(() => undefined);
}

function requestExit(shell) {
  shell.dispatchEvent(new CustomEvent('rune-command', { bubbles: true, detail: { name: 'fullscreen-exit' } }));
}
