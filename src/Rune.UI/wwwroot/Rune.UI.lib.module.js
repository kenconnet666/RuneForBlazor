// Rune.UI 的 JS 初始化器：Blazor 启动时按文件名约定（{程序集名}.lib.module.js）自动加载，静态 SSR、Server、WebAssembly 都会运行。
//
// 浮层尽量交给浏览器原生：popover 属性（顶层、点外面与 Esc 关闭、popovertarget 打开）、<dialog>（模态、焦点圈在里面、关闭后焦点还给触发元素）、
// CSS 锚点定位与 @starting-style 进出场。组件不直接调用 JS，只在元素上写属性，这里用全局的事件委托补原生没有的几样：
//
//   data-rune-overlay        浮层（popover 元素或 dialog）：开关时发出冒泡的 rune-toggle（Blazor 里是 @onrunetoggle）
//   data-open="true|false"   受控的浮层：属性变了就打开或关闭（组件的 Open 参数）
//   data-rune-open="id"      点击时打开这个 id 的浮层（对话框的触发按钮；popover 用原生的 popovertarget）
//   data-rune-close          点击时关闭所在的浮层（对话框的取消按钮、关闭按钮）
//   data-rune-light-dismiss  对话框：点遮罩关闭（原生的 closedby="any" 还没普及）
//   role="menu"              菜单：方向键、Home / End、首字母移动焦点；打开时聚焦第一项；选了菜单项就关闭
//   data-rune-tooltip-root   提示：Esc 隐藏；触发元素获得焦点时 aria-describedby 指向提示
//
// 模态对话框打开时锁定页面滚动；不支持锚点定位的浏览器在浮层打开时按触发元素摆放一次。

let installed = false;

/** Blazor Web App 的初始化入口。 */
export function afterWebStarted(blazor) {
  install(blazor);
}

/** 独立的 Blazor Server / WebAssembly 应用的初始化入口。 */
export function afterStarted(blazor) {
  install(blazor);
}

function install(blazor) {
  if (installed) return;
  installed = true;

  blazor.registerCustomEventType('runetoggle', {
    browserEventName: 'rune-toggle',
    createEventArgs: (event) => ({ open: event.detail.open }),
  });

  // 原生的 toggle（popover）与 close（dialog）不冒泡：在捕获阶段接住，转发成冒泡的 rune-toggle
  document.addEventListener('toggle', onPopoverToggle, true);
  document.addEventListener('close', (event) => {
    if (event.target instanceof HTMLDialogElement && event.target.matches('[data-rune-overlay]')) {
      updateScrollLock();
      announce(event.target, false);
    }
  }, true);

  document.addEventListener('click', onClick);
  document.addEventListener('keydown', onKeyDown);
  document.addEventListener('focusin', onFocusIn);
  document.addEventListener('focusout', clearTooltipDismissal);
  document.addEventListener('pointerout', clearTooltipDismissal);

  // 受控的浮层：组件改 data-open，这里同步原生状态；新插入的浮层（第一次渲染就是打开的）一并处理
  new MutationObserver(onMutations).observe(document.documentElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeFilter: ['data-open'],
  });
  document.querySelectorAll('[data-rune-overlay][data-open]').forEach(syncOpen);
}

function announce(overlay, open) {
  overlay.dispatchEvent(new CustomEvent('rune-toggle', { bubbles: true, detail: { open } }));
}

// ---- 打开与关闭 ----

function isOpen(overlay) {
  return overlay instanceof HTMLDialogElement ? overlay.open : overlay.matches(':popover-open');
}

function open(overlay) {
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

function syncOpen(overlay) {
  if (overlay.dataset.open === 'true') open(overlay);
  else if (overlay.dataset.open === 'false') close(overlay);
}

function onMutations(mutations) {
  for (const mutation of mutations) {
    if (mutation.type === 'attributes') {
      if (mutation.target.matches('[data-rune-overlay]')) syncOpen(mutation.target);
      continue;
    }
    for (const node of mutation.addedNodes) {
      if (!(node instanceof Element)) continue;
      if (node.matches('[data-rune-overlay][data-open="true"]')) syncOpen(node);
      node.querySelectorAll('[data-rune-overlay][data-open="true"]').forEach(syncOpen);
    }
  }
}

function onPopoverToggle(event) {
  const overlay = event.target;
  if (!(overlay instanceof HTMLElement) || !overlay.matches('[popover][data-rune-overlay]')) return;
  const opened = event.newState === 'open';
  if (opened) {
    placeWithoutAnchorSupport(overlay);
    if (overlay.matches('[role="menu"]')) menuItems(overlay)[0]?.focus();
  }
  announce(overlay, opened);
}

// 模态对话框打开时锁定页面滚动（关掉最后一个时恢复）；滚动条的位置留着，页面不会左右跳
function updateScrollLock() {
  const locked = document.querySelector('dialog[data-rune-overlay]:modal') !== null;
  document.documentElement.style.overflow = locked ? 'hidden' : '';
  document.documentElement.style.scrollbarGutter = locked ? 'stable' : '';
}

// ---- 点击 ----

function onClick(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;

  const opener = target.closest('[data-rune-open]');
  if (opener) {
    const overlay = document.getElementById(opener.getAttribute('data-rune-open'));
    if (overlay) open(overlay);
    return;
  }

  // 选了菜单项（不可用的除外）或点了关闭按钮：关闭所在的浮层
  const item = target.closest('[role^="menuitem"]:not([aria-disabled="true"]), [data-rune-close]');
  if (item) {
    const overlay = item.closest('[data-rune-overlay]');
    if (overlay) close(overlay);
    return;
  }

  // 点在对话框的遮罩上：事件目标是 dialog 本身，位置在它的边框之外
  if (target instanceof HTMLDialogElement && target.matches('[data-rune-light-dismiss][open]')) {
    const rect = target.getBoundingClientRect();
    const inside = event.clientX >= rect.left && event.clientX <= rect.right && event.clientY >= rect.top && event.clientY <= rect.bottom;
    if (!inside && event.detail > 0) close(target);
  }
}

// ---- 键盘 ----

function onKeyDown(event) {
  if (event.key === 'Escape') dismissTooltips();
  const menu = event.target instanceof Element ? event.target.closest('[role="menu"][data-rune-overlay]') : null;
  if (menu) moveInMenu(menu, event);
}

function menuItems(menu) {
  return [...menu.querySelectorAll('[role^="menuitem"]:not([aria-disabled="true"])')];
}

function moveInMenu(menu, event) {
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
      // 首字母：从当前项之后找第一个以它开头的菜单项
      if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
        const key = event.key.toLocaleLowerCase();
        const ordered = [...items.slice(index + 1), ...items.slice(0, index + 1)];
        next = ordered.find((item) => item.textContent.trim().toLocaleLowerCase().startsWith(key));
      }
  }
  if (next) {
    event.preventDefault();
    next.focus();
  }
}

// ---- 提示 ----

// WCAG 1.4.13：不移开鼠标、焦点也能把提示关掉
function dismissTooltips() {
  document.querySelectorAll('[data-rune-tooltip-root]:is(:hover, :focus-within)').forEach((root) => root.setAttribute('data-dismissed', ''));
}

// 鼠标与焦点都离开后恢复，下次悬停照常显示
function clearTooltipDismissal(event) {
  const root = event.target instanceof Element ? event.target.closest('[data-rune-tooltip-root][data-dismissed]') : null;
  if (!root) return;
  requestAnimationFrame(() => {
    if (!root.matches(':hover, :focus-within')) root.removeAttribute('data-dismissed');
  });
}

// 触发元素是使用方写的（按钮、链接），组件没法给它加属性：获得焦点时补上 aria-describedby，读屏读完名称接着读提示
function onFocusIn(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const root = target.closest('[data-rune-tooltip-root]');
  const tooltipId = root?.getAttribute('data-rune-tooltip-root');
  if (tooltipId && !target.hasAttribute('aria-describedby')) target.setAttribute('aria-describedby', tooltipId);
}

// ---- 不支持锚点定位时的兜底 ----

const anchorSupported = typeof CSS !== 'undefined' && CSS.supports('anchor-name: --rune');

function placeWithoutAnchorSupport(overlay) {
  if (anchorSupported) return;
  const anchor = document.querySelector(`[popovertarget="${CSS.escape(overlay.id)}"]`);
  if (!anchor) return;
  const gap = 4;
  const a = anchor.getBoundingClientRect();
  const o = overlay.getBoundingClientRect();
  const rtl = getComputedStyle(anchor).direction === 'rtl';
  let top;
  let left;
  switch (overlay.dataset.placement) {
    case 'top':
    case 'bottom': {
      const below = a.bottom + gap;
      const above = a.top - gap - o.height;
      const preferBelow = overlay.dataset.placement === 'bottom';
      const fitsBelow = below + o.height <= innerHeight;
      const fitsAbove = above >= 0;
      top = (preferBelow ? fitsBelow || !fitsAbove : !fitsAbove && fitsBelow) ? below : above;
      left = rtl ? a.right - o.width : a.left;
      break;
    }
    default: {
      const after = rtl ? a.left - gap - o.width : a.right + gap;
      const before = rtl ? a.right + gap : a.left - gap - o.width;
      const preferAfter = overlay.dataset.placement === 'end';
      const fits = (x) => x >= 0 && x + o.width <= innerWidth;
      left = preferAfter ? (fits(after) ? after : before) : (fits(before) ? before : after);
      top = a.top;
    }
  }
  overlay.style.position = 'fixed';
  overlay.style.inset = 'auto';
  overlay.style.margin = '0';
  overlay.style.top = `${Math.max(0, Math.min(top, innerHeight - o.height))}px`;
  overlay.style.left = `${Math.max(0, Math.min(left, innerWidth - o.width))}px`;
}
