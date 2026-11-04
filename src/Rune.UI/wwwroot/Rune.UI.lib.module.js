// Rune.UI 的 JS 初始化器：Blazor 启动时按文件名约定（{程序集名}.lib.module.js）自动加载，静态 SSR、Server、WebAssembly 都会运行。
//
// 浮层尽量交给浏览器原生：popover 属性（顶层、点外面与 Esc 关闭、popovertarget 打开）、<dialog>（模态、焦点圈在里面、关闭后焦点还给触发元素）、
// @starting-style 进出场。组件不直接调用 JS，只在元素上写属性，这里用全局的事件委托补原生没有的几样：
//
//   data-rune-overlay        浮层（popover 元素或 dialog）：开关时发出冒泡的 rune-toggle（Blazor 里是 @onrunetoggle）
//   data-open="true|false"   受控的浮层：属性变了就打开或关闭（组件的 Open 参数）
//   data-rune-open="id"      点击时打开这个 id 的浮层（对话框的触发按钮；popover 用原生的 popovertarget）
//   data-rune-close          点击时关闭所在的浮层（对话框的取消按钮、关闭按钮）
//   data-rune-floating="bottom-start"
//                            锚定浮层（popover 元素）：打开时用 Floating UI 贴着触发元素（popovertarget 或 data-rune-tooltip 指向它的元素）
//                            定位并持续跟随，空间不够时翻转、贴着视口平移；data-offset 是距离，data-fit-height 时最大高度限制在视口剩下的空间里；
//                            最终方向写到 data-side / data-align；里面的 data-rune-arrow 是箭头。定位好之后才可见，菜单聚焦第一项，
//                            对话框类的浮层聚焦 autofocus 或第一个可聚焦元素
//   data-rune-light-dismiss  对话框：点遮罩关闭（原生的 closedby="any" 还没普及）
//   role="menu"              菜单：方向键、Home / End、类型搜索移动焦点；打开时聚焦第一项；选了菜单项就关闭
//   data-rune-list="space typeahead"
//                            列表的键盘宿主：role="listbox" 本身，或 aria-controls 指向列表的触发按钮、输入框。焦点留在宿主上，
//                            ↑↓、Home / End、Page Up / Down 移动高亮（宿主的 data-rune-active 与 aria-activedescendant、选项的 data-highlighted），
//                            Enter（space 时还有空格）点击高亮项（C# 处理选中），typeahead 时按字母跳到以它开头的项；
//                            列表自己获得焦点时高亮第一个已选项（没有就第一个可用项）；指针移过选项时高亮它。
//                            虚拟滚动的列表带 data-rune-options（全部选项的文字与不可用）与 data-rune-selected（第一个已选项的序号）
//   data-rune-tooltip="id"   提示的触发元素：指针停留 data-delay 毫秒后显示这个 id 的提示（刚关掉一个时直接显示），离开 100ms 后隐藏
//                            （可以移到提示上），键盘聚焦时立即显示；失焦、按下、Esc 隐藏；同一时间只显示一个
//   data-indeterminate       勾选框的半选：原生的 indeterminate 只能用 JS 设置，按这个属性同步
//   data-rune-focus="id"     点击时聚焦这个 id 的控件并滚到它（表单错误汇总里的链接）
//   data-rune-spin           数字输入框：↑↓ 加减步长、Page Up / Down 加减大步长、Home / End 到最小 / 最大，改完发 change（C# 再校正）
//   data-rune-ripple         按钮、菜单项：按下时从指针位置（键盘 Enter / 空格时从中心）扩散一圈波纹，松开后淡出
//   data-rune-inert          加载中、可聚焦的不可用按钮：拦下点击的默认动作（提交表单）
//   data-rune-copy="文字"     复制按钮：点击时（data-rune-copy-on-click）或 data-rune-copy-request 变了时把文字写入剪贴板，
//                            成功后按钮的 data-state 变成 copied（data-rune-copy-timeout 毫秒后复原）、播报 data-rune-copied-text，
//                            发出冒泡的 rune-copied（Blazor 里是 @onrunecopied）
//   data-rune-grid-span="n"  网格里跨 n 列的项：网格实际的列比 n 少时（按宽度自动换列的网格变窄）改成跨当前的列数
//   data-rune-control        输入框的外壳：点在空白处（内边距、图标、前后缀）时聚焦里面的输入元素；
//                            框里不在 Tab 顺序里的按钮（数字框的加减）按下时不抢走输入框的焦点
//   data-rune-repeat         按住时连续点击（数字框的加减）：停一会儿后每隔一小段时间再点一次，松开为止
//   data-rune-refocus="id"   点击后把焦点交给这个 id 的元素（清空按钮清空后回到输入框）
//   data-rune-reveal="id"    显示密码按钮：切换这个 id 的输入框的 type（password / text）与按钮的 aria-pressed
//   data-rune-count="id"     有字数计数的输入框：输入时更新这个 id 的计数（看得见的字数与超出上限的 data-over）
//   data-rune-autosize       多行文本框：输入时、值变了（属性值随之变化）时按 scrollHeight 设高度（上下限由 CSS 的 min/max-height 夹住）
//   data-rune-check-draw     复选框：用户点击勾上时把后面的对勾一笔画出（初次渲染、外部改值不画）
//   data-rune-draw="true"    图标的描线动画：插入时为 true、或由 false 变为 true 时一笔画出（减少动态效果时直接画好）
//   data-rune-dismiss        可移除元素（data-rune-dismissible，如标签）里的移除按钮：元素先离场（data-dismissing），
//                            结束后发出冒泡的 rune-dismiss（Blazor 里是 @onrunedismiss）；被移除时焦点交给同组的下一个
//                            （没有就上一个）可聚焦元素，没被移除就重新显示
//
// 模态对话框打开时锁定页面滚动。

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
  blazor.registerCustomEventType('runecopied', {
    browserEventName: 'rune-copied',
    createEventArgs: (event) => ({ success: event.detail.success }),
  });
  blazor.registerCustomEventType('runedismiss', {
    browserEventName: 'rune-dismiss',
    createEventArgs: () => ({}),
  });

  // 原生的 toggle（popover）与 close（dialog）不冒泡：在捕获阶段接住，转发成冒泡的 rune-toggle
  document.addEventListener('toggle', onPopoverToggle, true);
  document.addEventListener('close', (event) => {
    if (event.target instanceof HTMLDialogElement && event.target.matches('[data-rune-overlay]')) {
      updateScrollLock();
      announce(event.target, false);
    }
  }, true);

  // 捕获阶段：要赶在 Blazor 的链接拦截之前（它会把 #id 按 <base href> 解析成别的页面并跳过去）
  document.addEventListener('click', onFocusLink, true);
  // 加载中、可聚焦的不可用按钮：拦下点击的默认动作（提交表单、跟随链接）；使用方的点击处理组件已经去掉
  document.addEventListener('click', blockInert, true);
  document.addEventListener('click', onClick);
  document.addEventListener('keydown', onKeyDown);
  document.addEventListener('pointerdown', startRipple);
  document.addEventListener('pointerdown', focusOnShell);
  document.addEventListener('pointerdown', keepFocusOnListHost);
  document.addEventListener('pointerdown', (event) => hideTooltipOf(event.target));
  document.addEventListener('pointermove', highlightUnderPointer);
  document.addEventListener('change', (event) => {
    const target = event.target;
    if (target instanceof HTMLInputElement && target.checked && target.hasAttribute('data-rune-check-draw')) {
      const check = target.nextElementSibling;
      if (check instanceof SVGSVGElement) drawIcon(check);
    }
  });
  document.addEventListener('input', (event) => {
    const target = event.target;
    if (target instanceof HTMLTextAreaElement && target.hasAttribute('data-rune-autosize')) autosize(target);
    if (target instanceof Element && target.hasAttribute('data-rune-count')) updateCount(target);
  });
  document.addEventListener('focusin', onFocusIn);
  document.addEventListener('focusout', onFocusOut);
  document.addEventListener('pointerover', onPointerOver);
  document.addEventListener('pointerout', onPointerOut);

  // 组件改属性（data-open、data-indeterminate），这里同步原生状态；新插入的元素（第一次渲染就带着状态）一并处理
  new MutationObserver(onMutations).observe(document.documentElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeOldValue: true,
    attributeFilter: ['data-open', 'data-indeterminate', 'data-rune-draw', 'data-rune-copy-request', 'data-rune-autosize'],
  });
  syncWithin(document);
  // 装好了：端到端测试、对照工具等它出现再操作（脚本加载之前的点击没人处理）
  document.documentElement.setAttribute('data-rune-ready', '');
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

function syncIndeterminate(input) {
  input.indeterminate = input.dataset.indeterminate === 'true';
}

/** 同步一个元素（含自身）里所有带状态属性的元素 */
function syncWithin(root) {
  const sync = (element) => {
    if (element.matches('[data-rune-overlay][data-open="true"]')) syncOpen(element);
    if (element.matches('input[data-indeterminate="true"]')) syncIndeterminate(element);
    if (element.matches('svg[data-rune-draw="true"]')) drawIcon(element);
    if (element.matches('[data-rune-grid-span]')) watchGrid(element.parentElement);
    if (element.matches('textarea[data-rune-autosize]')) autosize(element);
  };
  if (root instanceof Element) sync(root);
  root.querySelectorAll('[data-rune-overlay][data-open="true"], input[data-indeterminate="true"], svg[data-rune-draw="true"], [data-rune-grid-span], textarea[data-rune-autosize]').forEach(sync);
}

function onMutations(mutations) {
  for (const mutation of mutations) {
    if (mutation.type === 'attributes') {
      if (mutation.attributeName === 'data-indeterminate') syncIndeterminate(mutation.target);
      else if (mutation.attributeName === 'data-rune-autosize') autosize(mutation.target);
      else if (mutation.attributeName === 'data-rune-copy-request') {
        if (mutation.target.hasAttribute('data-rune-copy-request')) copyFrom(mutation.target);
      } else if (mutation.attributeName === 'data-rune-draw') {
        if (mutation.target.getAttribute('data-rune-draw') === 'true' && mutation.oldValue !== 'true') drawIcon(mutation.target);
      }
      else if (mutation.target.matches('[data-rune-overlay]')) syncOpen(mutation.target);
      continue;
    }
    for (const node of mutation.addedNodes) {
      if (node instanceof Element) syncWithin(node);
    }
    // 列表重新渲染（虚拟滚动换了一批选项、选项变了）：高亮放回高亮项上
    if (mutation.addedNodes.length > 0 && mutation.target instanceof Element) {
      const list = mutation.target.closest('[role="listbox"]');
      const host = list && hostOf(list);
      if (host?.hasAttribute('data-rune-active')) syncHighlight(host, list);
    }
  }
}

function onPopoverToggle(event) {
  const overlay = event.target;
  if (!(overlay instanceof HTMLElement)) return;
  const opened = event.newState === 'open';
  if (overlay.matches('[data-rune-floating]')) {
    if (!opened) releaseFloating(overlay);
    else if (overlay.matches('[data-rune-overlay]')) anchorFloating(overlay).then(() => focusInto(overlay));
    else anchorFloating(overlay);
  }
  if (overlay.matches('[popover][data-rune-overlay]')) announce(overlay, opened);
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

// 模态对话框打开时锁定页面滚动（关掉最后一个时恢复）；滚动条的位置留着，页面不会左右跳
function updateScrollLock() {
  const locked = document.querySelector('dialog[data-rune-overlay]:modal') !== null;
  document.documentElement.style.overflow = locked ? 'hidden' : '';
  document.documentElement.style.scrollbarGutter = locked ? 'stable' : '';
}

// ---- 点击 ----

function blockInert(event) {
  if (event.target instanceof Element && event.target.closest('[data-rune-inert]')) event.preventDefault();
}

function onFocusLink(event) {
  const link = event.target instanceof Element ? event.target.closest('[data-rune-focus]') : null;
  const control = link && document.getElementById(link.getAttribute('data-rune-focus'));
  if (!control) return;
  event.preventDefault();
  control.focus();
  control.scrollIntoView({ block: 'center', behavior: reducedMotion.matches ? 'auto' : 'smooth' });
}

function onClick(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;

  const refocus = target.closest('[data-rune-refocus]');
  if (refocus) document.getElementById(refocus.getAttribute('data-rune-refocus'))?.focus({ preventScroll: true });

  const reveal = target.closest('[data-rune-reveal]');
  if (reveal && !reveal.matches(':disabled')) {
    const input = document.getElementById(reveal.getAttribute('data-rune-reveal'));
    if (input instanceof HTMLInputElement) {
      const shown = input.type === 'password';
      input.type = shown ? 'text' : 'password';
      reveal.setAttribute('aria-pressed', String(shown));
    }
  }

  const copier = target.closest('[data-rune-copy-on-click]');
  if (copier && !copier.matches(':disabled, [aria-busy="true"]')) copyFrom(copier);

  const dismiss = target.closest('[data-rune-dismiss]');
  if (dismiss) {
    startDismiss(dismiss);
    return;
  }

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
  if (event.key === 'Escape' && shownTooltip) hideTooltip(shownTooltip);
  if (event.key === 'Enter' || event.key === ' ') startRipple(event);
  if (event.target instanceof HTMLInputElement && event.target.matches('[data-rune-spin]')) spin(event.target, event);
  const menu = event.target instanceof Element ? event.target.closest('[role="menu"][data-rune-overlay]') : null;
  if (menu) moveInMenu(menu, event);
  if (event.target instanceof Element && event.target.matches('[data-rune-list]')) moveInList(event.target, event);
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

// ---- 类型搜索 ----

// 每个菜单、列表各有一个输入缓冲（同 vue-tsx-pro 的 createTypeahead）
const typeaheadQueries = new WeakMap();

/**
 * 类型搜索（WAI-ARIA 的 type-ahead）：在菜单、列表里直接按字母，跳到以它开头的项。
 * 连续输入拼成一个词（“ex” 跳到 Export）；停顿 500 毫秒后重新开始；反复按同一个字母在以它开头的项之间轮换。
 * labels 是各项的文字，current 是当前项的序号（没有时 -1）；返回该落到的项的序号，不是可打印字符或没有匹配时 -1。
 */
function typeahead(owner, key, labels, current) {
  const state = typeaheadQueries.get(owner) ?? { query: '', timer: 0 };
  // 只认单个可打印字符；空格开头时不算（空格是激活键）
  if (!/^.$/su.test(key) || (key === ' ' && !state.query)) return -1;
  typeaheadQueries.set(owner, state);
  clearTimeout(state.timer);
  state.timer = setTimeout(() => (state.query = ''), 500);
  state.query += key.toLocaleLowerCase();

  const normalized = labels.map((label) => label.trim().toLocaleLowerCase());
  // 同一个字母反复按：从下一项开始找以它开头的，轮换
  const first = state.query.slice(0, key.length);
  const repeated = state.query === first.repeat(state.query.length / first.length);
  const search = repeated ? first : state.query;
  const start = repeated ? current + 1 : Math.max(current, 0);
  for (let offset = 0; offset < normalized.length; offset++) {
    const index = (start + offset) % normalized.length;
    if (normalized[index].startsWith(search)) return index;
  }
  return -1;
}

// ---- 列表 ----
//
// 高亮按序号记在宿主的 data-rune-active 上（选项的 id 是 `{列表 id}-{序号}`，aria-activedescendant 照此拼出），
// 渲染出来的那一项另写 data-highlighted 给样式用。虚拟滚动的列表里看不见的选项不在 DOM 里：C# 把各项的文字、
// 不可用写在列表的 data-rune-options 上，键盘照样能移到任何一项；目标还没渲染时先按比例估算着滚过去，
// 渲染出来后（DOM 变化时同步一次）再精确地滚进可见范围。

// 一次按键移动几项；first / last 到头尾
const listMoves = { ArrowDown: 1, ArrowUp: -1, PageDown: 10, PageUp: -10, Home: 'first', End: 'last' };

// data-rune-options 解析一次：值没变时直接用
const optionTables = new WeakMap();

// 高亮项还没渲染、等着滚进可见范围的宿主
const pendingScroll = new WeakSet();

/** 宿主控制的列表：宿主本身是 listbox，或经 aria-controls 指向它。 */
function listOf(host) {
  if (host.getAttribute('role') === 'listbox') return host;
  const id = host.getAttribute('aria-controls');
  return id ? document.getElementById(id) : null;
}

/** 列表的键盘宿主（焦点所在、写 aria-activedescendant 的元素）。 */
function hostOf(list) {
  if (list.hasAttribute('data-rune-list')) return list;
  return list.id ? document.querySelector(`[data-rune-list][aria-controls="${CSS.escape(list.id)}"]`) : null;
}

/** 各选项的文字与是否不可用，按序号排列：虚拟滚动的列表取 data-rune-options，否则从 DOM 读。 */
function optionTable(list) {
  const json = list.getAttribute('data-rune-options');
  if (!json) {
    return [...list.querySelectorAll('[role="option"]')].map((option) => ({
      label: option.dataset.label ?? option.textContent,
      disabled: option.getAttribute('aria-disabled') === 'true',
    }));
  }
  const cached = optionTables.get(list);
  if (cached?.json === json) return cached.table;
  const { labels, disabled } = JSON.parse(json);
  const off = new Set(disabled);
  const table = labels.map((label, index) => ({ label, disabled: off.has(index) }));
  optionTables.set(list, { json, table });
  return table;
}

function optionAt(list, index) {
  return index < 0 ? null : document.getElementById(`${list.id}-${index}`);
}

function indexOfOption(option) {
  return Number(option.getAttribute('aria-posinset')) - 1;
}

/** 高亮项的序号；没有、或选项变少后已经不在了时 -1。 */
function activeIndex(host, list) {
  const index = Number(host.getAttribute('data-rune-active') ?? -1);
  return index >= 0 && index < optionTable(list).length ? index : -1;
}

/** 高亮一项（-1 取消）；scroll 时滚进可见范围（键盘移动；指针移过的本来就看得见）。 */
function highlight(host, list, index, scroll) {
  if (index < 0) {
    host.removeAttribute('data-rune-active');
    host.removeAttribute('aria-activedescendant');
  } else {
    host.setAttribute('data-rune-active', String(index));
    host.setAttribute('aria-activedescendant', `${list.id}-${index}`);
  }
  if (scroll && index >= 0) pendingScroll.add(host);
  syncHighlight(host, list);
}

/** 把 data-highlighted 放到高亮项上（它刚渲染出来时也调用），等着滚动的话滚进可见范围。 */
function syncHighlight(host, list) {
  const index = activeIndex(host, list);
  const option = optionAt(list, index);
  for (const other of list.querySelectorAll('[role="option"][data-highlighted]')) {
    if (other !== option) other.removeAttribute('data-highlighted');
  }
  option?.setAttribute('data-highlighted', '');
  if (!pendingScroll.has(host)) return;
  if (index < 0) {
    pendingScroll.delete(host);
  } else if (option) {
    pendingScroll.delete(host);
    scrollOptionIntoView(list, option, index);
  } else {
    estimateScroll(list, index);
  }
}

function scrollOptionIntoView(list, option, index) {
  // 第一项上面是组名时连组名一起滚进来：组名不被挡住
  const label = index === 0 && option.previousElementSibling?.id?.startsWith(`${list.id}-group-`) ? option.previousElementSibling : null;
  (label ?? option).scrollIntoView({ block: 'nearest' });
  if (label) option.scrollIntoView({ block: 'nearest' });
}

// 高亮项还没渲染（虚拟滚动）：按它在全部选项里的位置估算着滚过去，让虚拟滚动把它附近的选项渲染出来
function estimateScroll(list, index) {
  const scroller = scrollParent(list);
  if (!scroller) return;
  const size = optionTable(list).length;
  scroller.scrollTop = (index / size) * scroller.scrollHeight - scroller.clientHeight / 2;
}

function scrollParent(element) {
  for (let node = element; node; node = node.parentElement) {
    const overflow = getComputedStyle(node).overflowY;
    if ((overflow === 'auto' || overflow === 'scroll') && node.scrollHeight > node.clientHeight) return node;
  }
  return null;
}

/** 列表获得焦点、下拉打开时：高亮第一个已选的可用项，没有就第一个可用项。 */
function highlightDefault(host, list) {
  const table = optionTable(list);
  // 虚拟滚动时已选项可能没渲染：C# 把它的序号写在 data-rune-selected 上
  const attribute = list.getAttribute('data-rune-selected');
  const selected = attribute !== null
    ? Number(attribute)
    : [...list.querySelectorAll('[role="option"][aria-selected="true"]')].map(indexOfOption).find((index) => !table[index]?.disabled) ?? -1;
  highlight(host, list, selected >= 0 ? selected : table.findIndex((option) => !option.disabled), true);
}

function moveInList(host, event) {
  if (event.altKey || event.ctrlKey || event.metaKey || event.isComposing) return;
  if (host.getAttribute('aria-disabled') === 'true') return;
  const list = listOf(host);
  if (!list) return;
  const flags = (host.getAttribute('data-rune-list') ?? '').split(' ');
  const table = optionTable(list);
  // 可用项的序号：方向键、类型搜索都跳过不可用的项
  const usable = table.flatMap((option, index) => (option.disabled ? [] : [index]));
  const current = usable.indexOf(activeIndex(host, list));
  const step = listMoves[event.key];
  if (step !== undefined) {
    event.preventDefault();
    if (usable.length === 0) return;
    let next;
    if (step === 'first') next = 0;
    else if (step === 'last') next = usable.length - 1;
    else if (current < 0) next = step > 0 ? 0 : usable.length - 1;
    else next = Math.min(Math.max(current + step, 0), usable.length - 1);
    highlight(host, list, usable[next], true);
    return;
  }
  if (event.key === 'Enter' || (event.key === ' ' && flags.includes('space'))) {
    const option = optionAt(list, activeIndex(host, list));
    // 没有高亮项时 Enter 照常（提交表单），空格不滚动页面
    if (option || event.key === ' ') event.preventDefault();
    if (option && option.getAttribute('aria-disabled') !== 'true') option.click();
    return;
  }
  if (!flags.includes('typeahead')) return;
  const match = typeahead(list, event.key, usable.map((index) => table[index].label), current);
  if (match < 0) return;
  event.preventDefault();
  highlight(host, list, usable[match], true);
}

// 焦点留在宿主上（列表、触发按钮、输入框）：按下选项不把焦点移走
function keepFocusOnListHost(event) {
  if (event.target instanceof Element && event.target.closest('[role="listbox"] [role="option"]')) event.preventDefault();
}

function highlightUnderPointer(event) {
  const option = event.target instanceof Element ? event.target.closest('[role="option"]') : null;
  if (!option || option.hasAttribute('data-highlighted') || option.getAttribute('aria-disabled') === 'true') return;
  const list = option.closest('[role="listbox"]');
  const host = list && hostOf(list);
  if (host) highlight(host, list, indexOfOption(option), false);
}

// ---- 提示 ----
//
// 同 vue-tsx-pro 的 hover-intent：指针停留 delay 后显示，离开 100ms 后隐藏（可以移到提示上，WCAG 1.4.13）；
// 键盘聚焦（:focus-visible）立即显示，按下触发元素、失焦、Esc 时隐藏；触屏不显示。所有提示一组：同一时间只显示一个，
// 刚关掉一个（300ms 内）时下一个直接显示，在一排按钮上移动不用每次等。

const TOOLTIP_CLOSE_DELAY = 100;
const TOOLTIP_SKIP_DELAY = 300;
const tooltipTimers = new WeakMap();
let shownTooltip = null;
let tooltipClosedAt = 0;

function tooltipOf(trigger) {
  const id = trigger.getAttribute('data-rune-tooltip');
  return id ? document.getElementById(id) : null;
}

function scheduleTooltip(tooltip, show, wait) {
  clearTimeout(tooltipTimers.get(tooltip));
  const run = () => (show ? showTooltip(tooltip) : hideTooltip(tooltip));
  if (wait <= 0) run();
  else tooltipTimers.set(tooltip, setTimeout(run, wait));
}

function showTooltip(tooltip) {
  if (!tooltip.isConnected || tooltip.matches(':popover-open')) return;
  if (shownTooltip && shownTooltip !== tooltip) hideTooltip(shownTooltip);
  shownTooltip = tooltip;
  tooltip.showPopover();
}

function hideTooltip(tooltip) {
  clearTimeout(tooltipTimers.get(tooltip));
  if (tooltip.matches(':popover-open')) {
    tooltip.hidePopover();
    tooltipClosedAt = Date.now();
  }
  if (shownTooltip === tooltip) shownTooltip = null;
}

// 指针进出：pointerover / pointerout 冒泡，按 relatedTarget 判断是不是真的进出了触发元素（或提示）
function onPointerOver(event) {
  if (event.pointerType === 'touch' || !(event.target instanceof Element)) return;
  const trigger = event.target.closest('[data-rune-tooltip]');
  if (trigger && !trigger.contains(event.relatedTarget)) {
    const tooltip = tooltipOf(trigger);
    if (!tooltip) return;
    const warm = shownTooltip !== null || Date.now() - tooltipClosedAt < TOOLTIP_SKIP_DELAY;
    scheduleTooltip(tooltip, true, warm ? 0 : Number(tooltip.dataset.delay ?? 500));
    return;
  }
  // 移到提示上：取消正要做的隐藏
  const tooltip = event.target.closest('[role="tooltip"][popover]');
  if (tooltip) clearTimeout(tooltipTimers.get(tooltip));
}

function onPointerOut(event) {
  if (!(event.target instanceof Element)) return;
  const owner = event.target.closest('[data-rune-tooltip], [role="tooltip"][popover]');
  if (!owner || owner.contains(event.relatedTarget)) return;
  const tooltip = owner.matches('[data-rune-tooltip]') ? tooltipOf(owner) : owner;
  if (tooltip) scheduleTooltip(tooltip, false, TOOLTIP_CLOSE_DELAY);
}

function hideTooltipOf(element) {
  const trigger = element instanceof Element ? element.closest('[data-rune-tooltip]') : null;
  const tooltip = trigger && tooltipOf(trigger);
  if (tooltip) hideTooltip(tooltip);
}

function onFocusIn(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  // 只在键盘聚焦时显示；点击聚焦（按钮被按下）时不显示
  if (target.matches('[data-rune-tooltip]:focus-visible')) {
    const tooltip = tooltipOf(target);
    if (tooltip) scheduleTooltip(tooltip, true, 0);
  }
  // 列表自己获得焦点：还没有高亮项时高亮默认项（触发按钮、输入框做宿主时由打开下拉的一方决定）
  if (target.matches('[role="listbox"][data-rune-list]') && activeIndex(target, target) < 0) highlightDefault(target, target);
}

function onFocusOut(event) {
  if (event.target instanceof Element && event.target.matches('[data-rune-tooltip]')) hideTooltipOf(event.target);
}

// ---- 数字输入框 ----

const spinKeys = { ArrowUp: 1, ArrowDown: -1, PageUp: 10, PageDown: -10 };

/**
 * 按键加减：从输入框里现在的文字算（还没提交的输入也算），写回后发 change 交给 C#（它再解析、夹到范围、对齐步长）。
 * 文字按 data-decimal 的小数点解析，忽略千分位与单位；只读、不可用时不处理。
 */
function spin(input, event) {
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

// ---- 波纹 ----

const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');

/**
 * 在带 data-rune-ripple 的元素上开始一个波纹（同 vue-tsx-pro 的 ripple）：半透明的当前文字颜色，
 * 实底按钮上是浅色、描边按钮上是深色；松开、移出、取消或失去焦点时淡出并移除。
 * 宿主要有 position: relative 与 overflow: hidden（SharedStyles.RippleHost）。减少动效时不扩散，只是按下时变色。
 */
/** 复制按钮：写入剪贴板（点击当下同步开始写，Safari 才认），成功后显示对勾、播报，过一会儿复原 */
function copyFrom(button) {
  const text = button.getAttribute('data-rune-copy') ?? '';
  const finish = (success) => {
    if (success) {
      button.setAttribute('data-state', 'copied');
      speak(button.getAttribute('data-rune-copied-text') || '');
      clearTimeout(copyTimers.get(button));
      const timeout = Number(button.getAttribute('data-rune-copy-timeout')) || 2000;
      copyTimers.set(button, setTimeout(() => button.removeAttribute('data-state'), timeout));
    }
    button.dispatchEvent(new CustomEvent('rune-copied', { bubbles: true, detail: { success } }));
  };
  if (!navigator.clipboard?.writeText) {
    finish(false);
    return;
  }
  navigator.clipboard.writeText(text).then(() => finish(true), () => finish(false));
}

const copyTimers = new WeakMap();

/** 让读屏念出一句话：共用一个礼貌的播报区；先清空、稍后再写入，同一句话连说两次也会念 */
function speak(message) {
  if (!message) return;
  let region = document.querySelector('[data-rune-announcer]');
  if (!region) {
    region = document.createElement('div');
    region.setAttribute('data-rune-announcer', '');
    region.setAttribute('aria-live', 'polite');
    region.setAttribute('aria-atomic', 'true');
    Object.assign(region.style, {
      position: 'absolute', width: '1px', height: '1px', margin: '-1px', padding: '0',
      overflow: 'hidden', clipPath: 'inset(50%)', whiteSpace: 'nowrap', border: '0',
    });
  }
  // 模态对话框打开时放进对话框里：对话框外的内容读屏读不到
  const host = document.querySelector('dialog[open]:modal') ?? document.body;
  if (region.parentElement !== host) host.append(region);
  region.textContent = '';
  clearTimeout(speakTimer);
  speakTimer = setTimeout(() => (region.textContent = message), 100);
}

let speakTimer;

/** 量网格实际有几列（计算后的 grid-template-columns 有几段），跨列多于它的项改成跨当前的列数 */
const gridObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target } of entries) clampGridSpans(target);
});
const watchedGrids = new WeakSet();

function watchGrid(grid) {
  if (!grid || !gridObserver) return;
  if (!watchedGrids.has(grid)) {
    watchedGrids.add(grid);
    gridObserver.observe(grid);
  }
  clampGridSpans(grid);
}

function clampGridSpans(grid) {
  const tracks = getComputedStyle(grid).gridTemplateColumns.split(' ').filter(Boolean).length;
  for (const item of grid.children) {
    const span = Number(item.getAttribute('data-rune-grid-span'));
    if (!span) continue;
    item.style.gridColumn = tracks > 0 && tracks < span ? `span ${tracks} / span ${tracks}` : '';
  }
}

/** 点在输入框外壳的空白处（不是输入元素本身，也不是框里的按钮、链接）：焦点交给里面的输入元素，不让外壳把焦点抢走 */
function focusOnShell(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const shell = target.closest('[data-rune-control]');
  if (!shell) return;
  const button = target.closest('button');
  if (button && shell.contains(button) && button.getAttribute('tabindex') === '-1' && !button.disabled) {
    // 焦点留在输入框里（读屏、键盘接着用）；按住的按钮连续点击
    event.preventDefault();
    if (button.hasAttribute('data-rune-repeat')) repeatWhileHeld(button);
    return;
  }
  if (target.closest('input, textarea, select, button, a')) return;
  const control = shell.querySelector(':scope > :is(input, textarea, select)');
  if (!control || control.disabled) return;
  event.preventDefault();
  control.focus();
}

/** 输入时更新字数计数里看得见的字数（读屏的完整句子在值提交后由组件更新） */
function updateCount(input) {
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
function autosize(textarea) {
  textarea.style.height = 'auto';
  textarea.style.height = `${textarea.scrollHeight}px`;
}

/** 图标描线：每一笔的长度统一成 1，虚线从 0 画到 1（时长、缓动取主题；减少动态效果时直接画好） */
function drawIcon(svg) {
  if (reducedMotion.matches) return;
  const style = getComputedStyle(svg);
  const duration = parseFloat(style.getPropertyValue('--rune-durations-xl')) || 500;
  const easing = style.getPropertyValue('--rune-easings-enter').trim() || 'ease-out';
  for (const stroke of svg.children) {
    stroke.setAttribute('pathLength', '1');
    // 圆头的线长度为 0 时还会画出一个点：开头用平头
    stroke.animate(
      [{ strokeDasharray: '0 1', strokeLinecap: 'butt' }, { strokeDasharray: '1 1', strokeLinecap: 'round' }],
      { duration, easing },
    );
  }
}

/** 可移除元素的离场：先播放离场过渡，结束后交给 Blazor 移除；等一会儿还在（使用方没移除）就重新显示 */
function startDismiss(button) {
  const target = button.closest('[data-rune-dismissible]');
  if (!target || target.hasAttribute('data-dismissing')) return;
  const candidates = focusCandidatesAfterRemoval(target);
  target.setAttribute('data-dismissing', '');
  afterTransition(target, () => {
    target.dispatchEvent(new CustomEvent('rune-dismiss', { bubbles: true }));
    // Server 模式要等一次往返：每 50ms 看一次，最多 2 秒
    let checks = 0;
    const timer = setInterval(() => {
      if (!target.isConnected) {
        clearInterval(timer);
        const active = document.activeElement;
        if (!active || active === document.body) candidates.find((item) => item.isConnected)?.focus();
      } else if (++checks >= 40) {
        clearInterval(timer);
        target.removeAttribute('data-dismissing');
      }
    }, 50);
  });
}

/** 元素被移除后接手焦点的候选：同组（父元素）里在它后面的可聚焦元素，再是前面的；焦点不在它里面时不接管 */
function focusCandidatesAfterRemoval(element) {
  const group = element.parentElement;
  if (!group || !element.contains(document.activeElement)) return [];
  const others = [...group.querySelectorAll(TABBABLE)].filter((item) => !element.contains(item));
  const following = others.filter((item) => element.compareDocumentPosition(item) & Node.DOCUMENT_POSITION_FOLLOWING);
  const preceding = others.filter((item) => !following.includes(item)).reverse();
  return [...following, ...preceding];
}

const TABBABLE = 'a[href], button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"])';

/** 过渡结束后执行：没有过渡（减少动态效果时时长为 0）立即执行；transitionend 没来（页面在后台）时按时长兜底 */
function afterTransition(element, done) {
  const seconds = (value) => Math.max(0, ...value.split(',').map((part) => parseFloat(part) * (part.trim().endsWith('ms') ? 0.001 : 1)));
  const style = getComputedStyle(element);
  const total = seconds(style.transitionDuration) + seconds(style.transitionDelay);
  if (total === 0) {
    requestAnimationFrame(done);
    return;
  }
  let finished = false;
  const finish = () => {
    if (finished) return;
    finished = true;
    element.removeEventListener('transitionend', onEnd);
    done();
  };
  const onEnd = (event) => event.target === element && finish();
  element.addEventListener('transitionend', onEnd);
  setTimeout(finish, total * 1000 + 50);
}

function startRipple(event) {
  const keyboard = event instanceof KeyboardEvent;
  const host = event.target instanceof Element ? event.target.closest('[data-rune-ripple]') : null;
  if (!host || host.matches(':disabled, [aria-disabled="true"], [aria-busy="true"]')) return;
  if (keyboard ? event.repeat || event.target !== host : event.button !== 0) return;

  const rect = host.getBoundingClientRect();
  const x = keyboard ? rect.width / 2 : event.clientX - rect.left;
  const y = keyboard ? rect.height / 2 : event.clientY - rect.top;
  // 圆心到最远的角的距离：扩散完正好盖满整个元素
  const radius = Math.hypot(Math.max(x, rect.width - x), Math.max(y, rect.height - y));
  const ripple = document.createElement('span');
  ripple.setAttribute('aria-hidden', 'true');
  Object.assign(ripple.style, {
    position: 'absolute',
    left: `${x - radius}px`,
    top: `${y - radius}px`,
    width: `${radius * 2}px`,
    height: `${radius * 2}px`,
    borderRadius: '50%',
    pointerEvents: 'none',
    backgroundColor: 'color-mix(in srgb, currentColor 22%, transparent)',
  });
  host.append(ripple);
  const motion = !reducedMotion.matches;
  const easing = getComputedStyle(host).getPropertyValue('--rune-easings-standard').trim() || 'ease-out';
  ripple.animate([{ transform: 'scale(0)' }, { transform: 'scale(1)' }], { duration: motion ? 450 : 0, easing, fill: 'forwards' });

  const releaseEvents = keyboard
    ? [['keyup', host], ['blur', host]]
    : [['pointerup', window], ['pointercancel', window], ['pointerleave', host]];
  const release = () => {
    for (const [type, target] of releaseEvents) target.removeEventListener(type, release);
    const fade = motion ? 250 : 0;
    ripple.animate([{ opacity: 1 }, { opacity: 0 }], { duration: fade, easing: 'ease-in', fill: 'forwards' }).finished.then(() => ripple.remove());
    // 页面不渲染时（后台标签页）动画不推进、finished 不会完成：到时间兜底移除
    setTimeout(() => ripple.remove(), fade + 100);
  };
  for (const [type, target] of releaseEvents) target.addEventListener(type, release);
}

// ---- 锚定浮层的定位 ----
//
// 同 vue-tsx-pro 的 utils/dom/floating：Floating UI 计算位置（offset、flip、shift，可选 arrow 与 size），autoUpdate 在滚动、缩放、
// 锚点变化时重新定位。库第一次打开浮层时才加载（wwwroot/vendor，随 Rune.UI 分发）。第一次算好位置之前浮层不可见，不会在默认位置闪一下。

let floatingUi = null;
const loadFloatingUi = () => (floatingUi ??= import('./vendor/floating-ui.dom.mjs'));

// 正在跟随触发元素的浮层 → 停止跟随
const floatingCleanups = new WeakMap();

// 与视口边缘至少留出的距离（px）
const VIEWPORT_PADDING = 8;
// 缩放原点朝向锚点：弹出在下方时从上边缘展开
const TRANSFORM_ORIGIN = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };
// 箭头贴着浮层的哪条边（与弹出方向相反），转多少度才朝向锚点（画了右、下两条边的方块）
const ARROW_EDGE = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };
const ARROW_ROTATION = { top: 45, right: 135, bottom: 225, left: -45 };

/** 浮层的触发元素：popovertarget 指向它（Popover、Menu），或 data-rune-tooltip 指向它（Tooltip）。 */
function floatingAnchorOf(overlay) {
  const id = CSS.escape(overlay.id);
  return document.querySelector(`[popovertarget="${id}"], [data-rune-tooltip="${id}"]`);
}

/** 贴着触发元素定位并持续跟随；第一次定位完成（可见）时完成。 */
async function anchorFloating(overlay) {
  const anchor = floatingAnchorOf(overlay);
  if (!anchor) return;
  overlay.style.visibility = 'hidden';
  const [{ computePosition, autoUpdate, offset, flip, shift, arrow: arrowMiddleware, size }] = await Promise.all([loadFloatingUi(), contentRendered(overlay)]);
  // 加载期间又关上了
  if (!overlay.matches(':popover-open')) {
    overlay.style.visibility = '';
    return;
  }
  releaseFloating(overlay);
  const arrow = overlay.querySelector(':scope > [data-rune-arrow]');
  // 箭头伸出浮层一半（旋转后对角线的一半约等于边长的 0.7）：距离加上这一段，尖角才不压在锚点上
  const arrowSize = arrow?.offsetWidth ?? 0;
  const middleware = [
    offset(Number(overlay.dataset.offset ?? 6) + arrowSize * 0.7),
    flip({ padding: VIEWPORT_PADDING }),
    shift({ padding: VIEWPORT_PADDING }),
  ];
  // 箭头离浮层的圆角留出距离，不会画到圆角外面
  if (arrow) middleware.push(arrowMiddleware({ element: arrow, padding: arrowSize }));
  const matchWidth = overlay.hasAttribute('data-match-width');
  const fitHeight = overlay.hasAttribute('data-fit-height');
  if (matchWidth || fitHeight) {
    middleware.push(size({
      padding: VIEWPORT_PADDING,
      apply({ rects, availableHeight, elements }) {
        if (matchWidth) elements.floating.style.minWidth = `${rects.reference.width}px`;
        if (fitHeight) elements.floating.style.maxHeight = `${Math.max(0, availableHeight)}px`;
      },
    }));
  }
  const placement = overlay.dataset.runeFloating;
  let stopped = false;
  return new Promise((resolve) => {
    const update = () => computePosition(anchor, overlay, { placement, strategy: 'fixed', middleware }).then(({ x, y, placement: final, middlewareData }) => {
      if (stopped) return;
      const [side, align = 'center'] = final.split('-');
      if (arrow && middlewareData.arrow) {
        const { x: arrowX, y: arrowY } = middlewareData.arrow;
        Object.assign(arrow.style, {
          left: arrowX != null ? `${arrowX}px` : '',
          top: arrowY != null ? `${arrowY}px` : '',
          right: '',
          bottom: '',
          [ARROW_EDGE[side]]: `${-arrowSize / 2}px`,
          transform: `rotate(${ARROW_ROTATION[side]}deg)`,
        });
      }
      Object.assign(overlay.style, { left: `${x}px`, top: `${y}px`, visibility: '', transformOrigin: TRANSFORM_ORIGIN[side] });
      overlay.dataset.side = side;
      overlay.dataset.align = align;
      resolve();
    });
    const cleanup = autoUpdate(anchor, overlay, update);
    floatingCleanups.set(overlay, () => {
      stopped = true;
      cleanup();
      resolve();
    });
  });
}

// 第一次打开才渲染内容的浮层（Popover 的 Lazy）：打开时还是空的，C# 收到 rune-toggle 才渲染。
// 等内容到了再定位、再显示，不先按空盒子定位闪一下（最多等 1 秒，静态渲染、内容本来就空时不卡住）
function contentRendered(overlay) {
  const hasContent = () => [...overlay.childNodes].some((node) =>
    node instanceof Element ? !node.hasAttribute('data-rune-arrow') : node.textContent.trim() !== '');
  if (hasContent()) return Promise.resolve();
  return new Promise((resolve) => {
    const done = () => {
      observer.disconnect();
      clearTimeout(timer);
      resolve();
    };
    const observer = new MutationObserver(() => hasContent() && done());
    const timer = setTimeout(done, 1000);
    observer.observe(overlay, { childList: true });
  });
}

/** 停止跟随（关闭时）；重复调用无副作用。 */
function releaseFloating(overlay) {
  floatingCleanups.get(overlay)?.();
  floatingCleanups.delete(overlay);
}
