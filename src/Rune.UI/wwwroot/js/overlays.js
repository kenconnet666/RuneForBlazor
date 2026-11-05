// 浮层的开关（popover、dialog）、对话框的关闭请求与锁定滚动、菜单的键盘、子菜单（悬停打开与安全三角）、右键菜单。

import { anchorFloating, releaseFloating } from './floating.js';
import { listPopoverClosed, listPopoverOpened, syncTriggerExpanded, typeahead } from './lists.js';
import { TABBABLE } from './shared.js';

// 受控浮层（data-open）报给 C# 但还没渲染回来的开关状态，按先后排队。渲染回来的 data-open 是这些旧状态的回响时不再照着开关：
// 否则在往返期间用户又切换过（打开后马上按 Esc）时，迟到的“打开”回响会把已经关掉的浮层重新打开，再报一次，最后停在打开
const pendingEchoes = new WeakMap();

// 用户关上浮层的原因（close 关闭按钮 / escape / outside 点遮罩）：关上时随 rune-toggle 报给 C#（onOpenChange 的原因）
const closeReasons = new WeakMap();

/** 记下接下来这次关闭的原因（原生 dialog 的 Esc 不经过 closeOrRequest，由 cancel 事件记下） */
export function noteCloseReason(overlay, reason) {
  closeReasons.set(overlay, reason);
}

export function announce(overlay, open) {
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
// 由触发按钮上的 ↑ 打开的菜单：聚焦最后一项
const focusLast = new WeakSet();

/** 菜单的触发按钮上按 ↓ / ↑：打开菜单并聚焦第一项 / 最后一项（Enter、空格由原生的 popovertarget 打开） */
export function openMenuFromTrigger(event) {
  if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
  const id = event.target instanceof Element ? event.target.getAttribute('popovertarget') : null;
  const menu = id ? document.getElementById(id) : null;
  if (!menu?.matches('[role="menu"][data-rune-overlay]') || menu.matches(':popover-open')) return;
  event.preventDefault();
  if (event.key === 'ArrowUp') focusLast.add(menu);
  menu.showPopover({ source: event.target });
}

// 打开、定位好之后（之前不可见、聚焦不了）焦点移进去：菜单到第一项，对话框类的到 autofocus 或第一个可聚焦元素
function focusInto(overlay) {
  awaitingFocus.delete(overlay);
  // 指针停留打开的子菜单：焦点留在原处
  if (hoverOpened.delete(overlay)) return;
  if (!overlay.matches(':popover-open') || overlay.contains(document.activeElement)) return;
  const last = focusLast.has(overlay);
  focusLast.delete(overlay);
  const target = overlay.matches('[role="menu"]')
    ? menuItems(overlay).at(last ? -1 : 0)
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
  else {
    noteCloseReason(overlay, reason);
    close(overlay);
  }
}

export function requestClose(overlay, reason) {
  overlay.dispatchEvent(new CustomEvent('rune-close-request', { bubbles: true, detail: { reason } }));
}

/** 菜单自己的可用项：不含子菜单里的 */
function menuItems(menu) {
  return [...menu.querySelectorAll('[role^="menuitem"]:not([aria-disabled="true"])')]
    .filter((item) => item.parentElement?.closest('[role="menu"]') === menu);
}

/** 一串菜单最外层的那个（子菜单在父菜单的 DOM 里）：执行了一项、按 Tab 时关它，整串一起关 */
export function rootMenuOf(element) {
  const MENU = '[role="menu"][data-rune-overlay]';
  let menu = element.closest(MENU);
  for (let outer = menu?.parentElement?.closest(MENU); outer; outer = outer.parentElement?.closest(MENU)) menu = outer;
  return menu;
}

const SUBMENU_TRIGGER = '[role="menuitem"][aria-haspopup="menu"][popovertarget]';
/** 指针停在子菜单项上多久后打开（毫秒） */
const SUBMENU_OPEN_DELAY = 100;
/** 指针离开后，在安全三角里最多保持多久（毫秒） */
const SUBMENU_GRACE = 400;
// 指针停留打开的子菜单：不把焦点移进去
const hoverOpened = new WeakSet();
let subOpenTimer = 0;
let stopGrace = null;

const subMenuOf = (trigger) => document.getElementById(trigger.getAttribute('popovertarget'));
const isRtl = (element) => getComputedStyle(element).direction === 'rtl';

/** 子菜单项被点击（Enter、空格）：打开并聚焦第一项；已经开着（指针打开的）时直接把焦点移进去。原生的 popovertarget 会切换，拦下它 */
export function clickSubMenuTrigger(event, trigger) {
  event.preventDefault();
  openSubMenu(trigger);
}

function openSubMenu(trigger) {
  const sub = subMenuOf(trigger);
  if (!sub) return;
  if (sub.matches(':popover-open')) menuItems(sub)[0]?.focus();
  else sub.showPopover({ source: trigger });
}

/** 指针移到子菜单项上：停一会儿打开（不移焦点） */
export function hoverSubMenu(event) {
  if (event.pointerType === 'touch' || !(event.target instanceof Element)) return;
  const trigger = event.target.closest(SUBMENU_TRIGGER);
  if (!trigger || (event.relatedTarget instanceof Node && trigger.contains(event.relatedTarget))) return;
  stopGrace?.();
  clearTimeout(subOpenTimer);
  subOpenTimer = setTimeout(() => {
    const sub = subMenuOf(trigger);
    if (!trigger.isConnected || !sub || sub.matches(':popover-open')) return;
    hoverOpened.add(sub);
    sub.showPopover({ source: trigger });
  }, SUBMENU_OPEN_DELAY);
}

/** 指针离开子菜单项：朝子菜单移动（在离开点与子菜单近边组成的三角形里）时保持打开，否则关闭 */
export function leaveSubMenu(event) {
  if (!(event.target instanceof Element)) return;
  const trigger = event.target.closest(SUBMENU_TRIGGER);
  if (!trigger || (event.relatedTarget instanceof Node && trigger.contains(event.relatedTarget))) return;
  clearTimeout(subOpenTimer);
  const sub = subMenuOf(trigger);
  if (!sub?.matches(':popover-open')) return;
  stopGrace?.();
  const rect = sub.getBoundingClientRect();
  const edge = rect.left >= event.clientX ? rect.left : rect.right;
  const start = [event.clientX, event.clientY];
  const closeSub = () => {
    stopGrace?.();
    if (sub.matches(':popover-open')) sub.hidePopover();
  };
  const onMove = (move) => {
    if (move.target instanceof Node && (sub.contains(move.target) || trigger.contains(move.target))) {
      stopGrace?.();
      return;
    }
    if (!inTriangle([move.clientX, move.clientY], start, [edge, rect.top], [edge, rect.bottom])) closeSub();
  };
  const timer = setTimeout(closeSub, SUBMENU_GRACE);
  document.addEventListener('pointermove', onMove);
  stopGrace = () => {
    clearTimeout(timer);
    document.removeEventListener('pointermove', onMove);
    stopGrace = null;
  };
}

/** 点 p 是否在三角形 abc 里（三条边的同侧判断） */
function inTriangle(p, a, b, c) {
  const side = (from, to) => (p[0] - to[0]) * (from[1] - to[1]) - (from[0] - to[0]) * (p[1] - to[1]);
  const d1 = side(a, b);
  const d2 = side(b, c);
  const d3 = side(c, a);
  return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
}

// 右键菜单打开前焦点所在的元素：关闭后还给它
const contextOrigins = new WeakMap();

/** 右键（Shift+F10、菜单键）：在区域（data-rune-context-menu）里从指针位置弹出它的菜单 */
export function openContextMenu(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const region = target.closest('[data-rune-context-menu]');
  // 菜单渲染在区域里：在菜单上右键不再打开
  if (!region || region.hasAttribute('data-disabled') || target.closest('[role="menu"]')) return;
  const menu = region.querySelector(':scope > [role="menu"][data-rune-overlay]');
  if (!menu) return;
  event.preventDefault();
  if (!menu.matches(':popover-open')) contextOrigins.set(menu, document.activeElement instanceof HTMLElement ? document.activeElement : null);
  // 键盘打开时没有指针坐标：从获得焦点的元素左下角弹出
  let { clientX: x, clientY: y } = event;
  if (x === 0 && y === 0) {
    const rect = target.getBoundingClientRect();
    x = rect.left;
    y = rect.bottom;
  }
  menu.dataset.pointX = String(x);
  menu.dataset.pointY = String(y);
  // 已经开着时换到新的位置
  if (menu.matches(':popover-open')) anchorFloating(menu);
  else menu.showPopover();
}

function restoreContextFocus(overlay) {
  const origin = contextOrigins.get(overlay);
  if (!origin) return;
  contextOrigins.delete(overlay);
  const active = document.activeElement;
  if (!active || active === document.body || overlay.contains(active)) origin.focus();
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
      // 菜单里不用 Tab 切换：关掉整串菜单，焦点回到触发按钮（原生 popover 关闭时的行为）
      close(rootMenuOf(menu));
      return;
    case 'ArrowRight':
    case 'ArrowLeft': {
      // 往书写方向前进：打开子菜单；后退：关掉所在的子菜单，回到打开它的那一项
      const forward = (event.key === 'ArrowRight') !== isRtl(menu);
      const active = document.activeElement;
      if (forward && active?.matches(SUBMENU_TRIGGER) && items.includes(active)) {
        event.preventDefault();
        openSubMenu(active);
      } else if (!forward && menu.hasAttribute('data-rune-submenu')) {
        event.preventDefault();
        close(menu);
        document.querySelector(`[popovertarget="${CSS.escape(menu.id)}"]`)?.focus();
      }
      return;
    }
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
