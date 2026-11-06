// 浮层的开关（popover、dialog）、打开后的焦点、对话框的关闭请求与锁定滚动。菜单的键盘、子菜单、右键菜单在 menus.js。

import { anchorFloating, releaseFloating } from './floating.js';
import { listPopoverClosed, listPopoverOpened, syncTriggerExpanded } from './lists.js';
import { menuFocusTarget, openedByHover, restoreContextFocus } from './menus.js';
import { TABBABLE } from './shared.js';

// 受控浮层（data-open）报给 C# 但还没渲染回来的开关状态，按先后排队。渲染回来的 data-open 是这些旧状态的回响时不再照着开关：
// 否则在往返期间用户又切换过（打开后马上按 Esc）时，迟到的“打开”回响会把已经关掉的浮层重新打开，再报一次，最后停在打开
const pendingEchoes = new WeakMap();

// 用户关上浮层的原因（close 关闭按钮 / escape / outside 点遮罩）：关上时随 rune-toggle 报给 C#（onOpenChange 的原因）
const closeReasons = new WeakMap();

/** 记下接下来这次关闭的原因（原生 dialog 的 Esc 不经过 closeOrRequest，由 cancel 事件记下） */
function noteCloseReason(overlay, reason) {
  closeReasons.set(overlay, reason);
}

function announce(overlay, open) {
  if (overlay.hasAttribute('data-open')) {
    if (!pendingEchoes.has(overlay)) pendingEchoes.set(overlay, []);
    pendingEchoes.get(overlay).push(open);
  }
  // 打开都算 trigger（C# 自己打开的会因状态相同而忽略）；关上时没记下原因的（C# 改 Open 引起的）不带原因
  const reason = open ? 'trigger' : closeReasons.get(overlay) ?? null;
  closeReasons.delete(overlay);
  overlay.dispatchEvent(new CustomEvent('rune-toggle', { bubbles: true, detail: { open, reason } }));
}


function isOpen(overlay) {
  return overlay instanceof HTMLDialogElement ? overlay.open : overlay.matches(':popover-open');
}

export function open(overlay) {
  if (!overlay.isConnected || isOpen(overlay)) return;
  if (overlay instanceof HTMLDialogElement) {
    overlay.showModal();
    updateScrollLock();
    // 对话框打开不发 toggle（较新的浏览器才有），这里补一个
    announce(overlay, true);
  } else {
    overlay.showPopover();
  }
}

function close(overlay) {
  if (!isOpen(overlay)) return;
  if (overlay instanceof HTMLDialogElement) overlay.close();
  else overlay.hidePopover();
}

export function syncOpen(overlay) {
  const state = overlay.dataset.open;
  if (state !== 'true' && state !== 'false') return;
  const echoes = pendingEchoes.get(overlay);
  if (echoes?.length) {
    // 是排在最前的那次回响：后面还有没回来的（C# 还没追上用户）就先不动；不是回响（C# 自己改的）就清空队列、照办
    if (echoes.shift() === (state === 'true')) {
      if (echoes.length > 0) return;
    } else {
      echoes.length = 0;
    }
  }
  if (state === 'true') open(overlay);
  else close(overlay);
}

export function onPopoverToggle(event) {
  const overlay = event.target;
  if (!(overlay instanceof HTMLElement)) return;
  const opened = event.newState === 'open';
  if (overlay.matches('[data-rune-floating]')) {
    if (!opened) releaseFloating(overlay);
    else if (overlay.matches('[data-rune-overlay]')) anchorFloating(overlay).then(() => {
      focusInto(overlay);
      listPopoverOpened(overlay);
    });
    else anchorFloating(overlay);
  }
  if (!opened) {
    listPopoverClosed(overlay);
    restoreContextFocus(overlay);
  }
  syncTriggerExpanded(overlay, opened);
  // 悬停卡片也要让 C# 知道开关（第一次打开才渲染内容）；它不是 data-rune-overlay：打开时焦点不移进去
  if (overlay.matches('[popover][data-rune-overlay], [popover][data-rune-hover-panel="card"]')) announce(overlay, opened);
}

// 打开时还没有可聚焦内容的浮层（Server 模式下第一次打开时内容由 C# 随后渲染）：内容插进来时再聚焦一次
const awaitingFocus = new WeakSet();
// 打开、定位好之后（之前不可见、聚焦不了）焦点移进去：菜单到第一项，对话框类的到 autofocus 或第一个可聚焦元素
function focusInto(overlay) {
  awaitingFocus.delete(overlay);
  // 指针停留打开的子菜单：焦点留在原处
  if (openedByHover(overlay)) return;
  if (!overlay.matches(':popover-open') || overlay.contains(document.activeElement)) return;
  const target = overlay.matches('[role="menu"]')
    ? menuFocusTarget(overlay)
    : overlay.querySelector('[autofocus]') ?? overlay.querySelector(TABBABLE);
  if (target) target.focus();
  else awaitingFocus.add(overlay);
}

/** 有元素插进了打开着的浮层：它还在等可聚焦的内容、焦点也没被用户移到别处时，把焦点移进去 */
export function focusArrivedContent(node) {
  const overlay = node.closest?.('[data-rune-overlay]');
  if (!overlay || !awaitingFocus.has(overlay)) return;
  const active = document.activeElement;
  if (active && active !== document.body && !overlay.contains(active) && active.getAttribute('popovertarget') !== overlay.id) {
    awaitingFocus.delete(overlay);
    return;
  }
  focusInto(overlay);
}

// 锁定期间恢复原样的函数；没锁时为 null
let restoreScroll = null;

// 模态对话框打开时锁定页面滚动（关掉最后一个时恢复，同 vue-tsx-pro 的 lockScroll）：去掉滚动条后在结束边补上等宽的内边距，
// 页面内容不左右跳。不用 scrollbar-gutter: stable：它留出的槽在视口里，贴着结束边的抽屉会和视口边缘隔着一条
function updateScrollLock() {
  const locked = document.querySelector('dialog[data-rune-overlay]:modal') !== null;
  if (locked === (restoreScroll !== null)) return;
  const root = document.documentElement;
  if (!locked) {
    restoreScroll();
    restoreScroll = null;
    return;
  }
  const scrollbar = innerWidth - root.clientWidth;
  const previous = { overflow: root.style.overflow, paddingInlineEnd: root.style.paddingInlineEnd };
  if (scrollbar > 0) root.style.paddingInlineEnd = `calc(${getComputedStyle(root).paddingInlineEnd || '0px'} + ${scrollbar}px)`;
  root.style.overflow = 'hidden';
  restoreScroll = () => Object.assign(root.style, previous);
}

// 要先问 C# 的对话框（data-rune-guard-close）发出关闭请求，其余直接关
export function closeOrRequest(overlay, reason) {
  if (overlay.matches('[data-rune-guard-close]')) requestClose(overlay, reason);
  else {
    noteCloseReason(overlay, reason);
    close(overlay);
  }
}

function requestClose(overlay, reason) {
  overlay.dispatchEvent(new CustomEvent('rune-close-request', { bubbles: true, detail: { reason } }));
}

/** 对话框上按了 Esc（原生先发不冒泡的 cancel，在捕获阶段接住）：要先问 C# 的拦下来改发关闭请求，其余记下原因照常关 */
export function onDialogCancel(event) {
  const dialog = event.target;
  if (!(dialog instanceof HTMLDialogElement)) return;
  if (dialog.matches('[data-rune-guard-close]')) {
    event.preventDefault();
    requestClose(dialog, 'escape');
  } else {
    noteCloseReason(dialog, 'escape');
  }
}

/** 对话框关上了（原生 close 不冒泡，在捕获阶段接住）：解除滚动锁定，报给 C# */
export function onDialogClose(event) {
  const dialog = event.target;
  if (!(dialog instanceof HTMLDialogElement) || !dialog.matches('[data-rune-overlay]')) return;
  updateScrollLock();
  announce(dialog, false);
}

// 点了里面的链接（通常是换页）就关上的对话框（data-rune-close-on-link：AppShell 窄屏时的导航抽屉）
export function closeOnLink(target) {
  const overlay = target.closest('a[href]')?.closest('[data-rune-close-on-link]');
  if (overlay) closeOrRequest(overlay, 'close');
}

const closeWhenWatched = new WeakSet();

// 媒体查询成立时关上的对话框（data-rune-close-when="(min-width: 64rem)"：窗口变宽、侧栏常驻后导航抽屉没必要再开着）
export function watchCloseWhen(overlay) {
  if (closeWhenWatched.has(overlay)) return;
  closeWhenWatched.add(overlay);
  const query = matchMedia(overlay.getAttribute('data-rune-close-when'));
  const onChange = () => {
    if (!overlay.isConnected) {
      query.removeEventListener('change', onChange);
      closeWhenWatched.delete(overlay);
    } else if (query.matches) {
      closeOrRequest(overlay, 'close');
    }
  };
  query.addEventListener('change', onChange);
}
