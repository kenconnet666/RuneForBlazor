// 菜单（role="menu" 的 popover）：打开时聚焦哪一项、方向键与类型搜索、子菜单（悬停打开与安全三角、← / →）、右键菜单。
// 开关本身走原生 popover，开关事件与打开后的聚焦在 overlays.js，这里只回答“聚焦哪一项”。

import { anchorFloating } from './floating.js';
import { typeahead } from '../input/lists.js';
import { isRtl } from '../core/shared.js';

const MENU = '[role="menu"][data-rune-overlay]';
const SUBMENU_TRIGGER = '[role="menuitem"][aria-haspopup="menu"][popovertarget]';
/** 指针停在子菜单项上多久后打开（毫秒） */
const SUBMENU_OPEN_DELAY = 100;
/** 指针离开后，在安全三角里最多保持多久（毫秒） */
const SUBMENU_GRACE = 400;

// 由触发按钮上的 ↑ 打开的菜单：聚焦最后一项
const focusLast = new WeakSet();
// 指针停留打开的子菜单：不把焦点移进去
const hoverOpened = new WeakSet();
// 右键菜单打开前焦点所在的元素：关闭后还给它
const contextOrigins = new WeakMap();
let subOpenTimer = 0;
let stopGrace = null;

const subMenuOf = (trigger) => document.getElementById(trigger.getAttribute('popovertarget'));
const hide = (menu) => {
  if (menu?.matches(':popover-open')) menu.hidePopover();
};

/** 菜单自己的可用项：不含子菜单里的 */
function menuItems(menu) {
  return [...menu.querySelectorAll('[role^="menuitem"]:not([aria-disabled="true"])')]
    .filter((item) => item.parentElement?.closest('[role="menu"]') === menu);
}

/** 一串菜单最外层的那个（子菜单在父菜单的 DOM 里）：执行了一项、按 Tab 时关它，整串一起关 */
export function rootMenuOf(element) {
  let menu = element.closest(MENU);
  for (let outer = menu?.parentElement?.closest(MENU); outer; outer = outer.parentElement?.closest(MENU)) menu = outer;
  return menu;
}

/** 这个菜单是不是指针停留打开的子菜单（问一次就清掉标记）：是的话焦点留在原处 */
export function openedByHover(menu) {
  return hoverOpened.delete(menu);
}

/** 菜单打开、定位好之后该聚焦的项：第一项，触发按钮上按 ↑ 打开的是最后一项；还没有项时 undefined */
export function menuFocusTarget(menu) {
  const last = focusLast.delete(menu);
  return menuItems(menu).at(last ? -1 : 0);
}

/** 菜单的触发按钮上按 ↓ / ↑：打开菜单并聚焦第一项 / 最后一项（Enter、空格由原生的 popovertarget 打开） */
export function openMenuFromTrigger(event) {
  if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
  const id = event.target instanceof Element ? event.target.getAttribute('popovertarget') : null;
  const menu = id ? document.getElementById(id) : null;
  if (!menu?.matches(MENU) || menu.matches(':popover-open')) return;
  event.preventDefault();
  if (event.key === 'ArrowUp') focusLast.add(menu);
  menu.showPopover({ source: event.target });
}

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
    hide(sub);
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

/** 右键（Shift+F10、菜单键）：在区域（data-rune-context-menu）里从指针位置弹出它的菜单 */
export function openContextMenu(event) {
  // 浏览器自己发了 contextmenu（桌面的右键、Android 的长按）：不用等长按的计时
  cancelContextPress();
  const target = event.target;
  // 里面的元素自己处理了右键（画布、地图、第三方控件调了 preventDefault）：不抢
  if (event.defaultPrevented || !(target instanceof Element)) return;
  const menu = contextMenuOf(target);
  if (!menu) return;
  event.preventDefault();
  // 键盘打开时没有指针坐标：从获得焦点的元素左下角弹出
  let { clientX: x, clientY: y } = event;
  if (x === 0 && y === 0) {
    const rect = target.getBoundingClientRect();
    x = rect.left;
    y = rect.bottom;
  }
  showContextMenu(menu, x, y);
}

/** 这个元素所在的右键区域的菜单；区域不可用、元素在菜单里（菜单渲染在区域里，在菜单上右键不再打开）时没有 */
function contextMenuOf(target) {
  const region = target.closest('[data-rune-context-menu]');
  if (!region || region.hasAttribute('data-disabled') || target.closest('[role="menu"]')) return null;
  return region.querySelector(`:scope > ${MENU}`);
}

function showContextMenu(menu, x, y) {
  if (!menu.matches(':popover-open')) contextOrigins.set(menu, document.activeElement instanceof HTMLElement ? document.activeElement : null);
  menu.dataset.pointX = String(x);
  menu.dataset.pointY = String(y);
  // 已经开着时换到新的位置
  if (menu.matches(':popover-open')) anchorFloating(menu);
  else menu.showPopover();
}

// 长按：iOS 的 Safari 长按不发 contextmenu，触屏、触控笔按住不动一会儿当作右键（同 Radix 的 ContextMenu）
const LONG_PRESS = 700;
// 长按期间手指挪动超过多少（px）就不算
const PRESS_SLOP = 10;
let press = null;

export function onContextPressStart(event) {
  cancelContextPress();
  if (event.pointerType === 'mouse' || !(event.target instanceof Element)) return;
  const menu = contextMenuOf(event.target);
  if (!menu) return;
  const { clientX: x, clientY: y } = event;
  press = {
    x,
    y,
    timer: setTimeout(() => {
      press = null;
      if (menu.isConnected) showContextMenu(menu, x, y);
    }, LONG_PRESS),
  };
}

/** 手指挪开了（在滚动、拖动）：不算长按 */
export function onContextPressMove(event) {
  if (press && Math.hypot(event.clientX - press.x, event.clientY - press.y) > PRESS_SLOP) cancelContextPress();
}

/** 手指抬起来了、被系统打断了 */
export function cancelContextPress() {
  if (!press) return;
  clearTimeout(press.timer);
  press = null;
}

/** 右键菜单关上了：焦点还没被移到别处时还给打开前的元素 */
export function restoreContextFocus(overlay) {
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
      hide(rootMenuOf(menu));
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
        hide(menu);
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
