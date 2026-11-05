// 浮层的开关（popover、dialog）、对话框的关闭请求与锁定滚动、菜单的键盘。

import { anchorFloating, releaseFloating } from './floating.js';
import { listPopoverClosed, listPopoverOpened, syncTriggerExpanded, typeahead } from './lists.js';
import { TABBABLE } from './shared.js';

export function announce(overlay, open) {
  overlay.dispatchEvent(new CustomEvent('rune-toggle', { bubbles: true, detail: { open } }));
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
  if (overlay.dataset.open === 'true') open(overlay);
  else if (overlay.dataset.open === 'false') close(overlay);
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
  if (!opened) listPopoverClosed(overlay);
  syncTriggerExpanded(overlay, opened);
  // 悬停卡片也要让 C# 知道开关（第一次打开才渲染内容）；它不是 data-rune-overlay：打开时焦点不移进去
  if (overlay.matches('[popover][data-rune-overlay], [popover][data-rune-hover-panel="card"]')) announce(overlay, opened);
}

// 打开、定位好之后（之前不可见、聚焦不了）焦点移进去：菜单到第一项，对话框类的到 autofocus 或第一个可聚焦元素
function focusInto(overlay) {
  if (!overlay.matches(':popover-open') || overlay.contains(document.activeElement)) return;
  if (overlay.matches('[role="menu"]')) {
    menuItems(overlay)[0]?.focus();
    return;
  }
  (overlay.querySelector('[autofocus]') ?? overlay.querySelector(TABBABLE))?.focus();
}

// 锁定期间恢复原样的函数；没锁时为 null
let restoreScroll = null;

// 模态对话框打开时锁定页面滚动（关掉最后一个时恢复，同 vue-tsx-pro 的 lockScroll）：去掉滚动条后在结束边补上等宽的内边距，
// 页面内容不左右跳。不用 scrollbar-gutter: stable：它留出的槽在视口里，贴着结束边的抽屉会和视口边缘隔着一条
export function updateScrollLock() {
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
  else close(overlay);
}

export function requestClose(overlay, reason) {
  overlay.dispatchEvent(new CustomEvent('rune-close-request', { bubbles: true, detail: { reason } }));
}

function menuItems(menu) {
  return [...menu.querySelectorAll('[role^="menuitem"]:not([aria-disabled="true"])')];
}

export function moveInMenu(menu, event) {
  const items = menuItems(menu);
  if (items.length === 0) return;
  const index = items.indexOf(document.activeElement);
  let next;
  switch (event.key) {
    case 'ArrowDown':
      next = items[(index + 1) % items.length];
      break;
    case 'ArrowUp':
      next = items[(index - 1 + items.length) % items.length];
      break;
    case 'Home':
      next = items[0];
      break;
    case 'End':
      next = items[items.length - 1];
      break;
    case 'Tab':
      // 菜单里不用 Tab 切换：关掉菜单，焦点回到触发按钮（原生 popover 关闭时的行为）
      close(menu);
      return;
    default:
      if (!event.ctrlKey && !event.metaKey && !event.altKey) {
        const match = typeahead(menu, event.key, items.map((item) => item.textContent), index);
        if (match >= 0) next = items[match];
      }
  }
  if (next) {
    event.preventDefault();
    next.focus();
  }
}
