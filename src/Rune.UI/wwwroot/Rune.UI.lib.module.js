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
//   data-rune-guard-close    对话框：要关闭时（Esc、点遮罩、data-rune-close）不直接关，发出冒泡的 rune-close-request
//                            （Blazor 里是 @onrunecloserequest，带原因 escape / outside / close），由 C# 决定关不关（确认中、关闭前确认）
//   role="menu"              菜单：方向键、Home / End、类型搜索移动焦点；打开时聚焦第一项；选了菜单项就关闭
//   data-rune-list="space typeahead"
//                            列表的键盘宿主：role="listbox" 本身，或 aria-controls 指向列表的触发按钮、输入框。焦点留在宿主上，
//                            ↑↓、Home / End、Page Up / Down 移动高亮（宿主的 data-rune-active 与 aria-activedescendant、选项的 data-highlighted），
//                            Enter（space 时还有空格）点击高亮项（C# 处理选中），typeahead 时按字母跳到以它开头的项；
//                            列表自己获得焦点时高亮第一个已选项（没有就第一个可用项）；指针移过选项时高亮它。
//                            虚拟滚动的列表带 data-rune-options（全部选项的文字与不可用）与 data-rune-selected（第一个已选项的序号）。
//                            text 时宿主是输入框（搜索框）：Home / End、空格、字母留给输入
//   选项弹出层（Select）     触发按钮是带 popovertarget 的列表宿主：关闭时 ↓ / Enter / 空格打开（↑ 打开并高亮最后一项），单选时按字母直接选中；
//                            打开后高亮默认项，Tab 关闭；单选列表选中一项后关闭；关闭时焦点在弹层里就还给触发按钮。
//                            触发按钮的 aria-expanded 与外壳（data-rune-control）的 data-state 在开关时同步；点外壳的空白处与点触发按钮一样
//   data-anchor="id"         锚定浮层贴着这个 id 的元素定位（Select 的弹层贴着输入框外壳），而不是触发元素
//   data-rune-query="词"      弹层里的列表：搜索词变了时高亮第一个匹配项，停顿后播报 data-rune-results（“12 个选项”）
//   data-rune-backspace="命令" 按 Backspace（输入框里是空的时候）发出冒泡的 rune-command（Blazor 里是 @onrunecommand，带命令名）：
//                            多选下拉移除最后一个已选项
//   data-rune-escape="命令"   弹层关着时在有字的输入框里按 Esc：发出 rune-command（Combobox 清空）
//   data-rune-popover="id"   输入框做列表宿主时它的弹层（输入框不能做 popovertarget）：输入时打开，↑↓ 打开并发 show-all 命令
//   data-rune-hide-empty     这样的弹层只在输入过、且有建议（或正在加载，aria-busy）时显示（Autocomplete）
//   输入法组合期间           列表宿主输入框（data-rune-list 含 text）的 input 事件拦下，组合结束补发一次：不拿拼音字母去过滤
//   data-rune-overflow       一行标签（Select 的 MaxTagCount 按宽度）：量出放得下几个 data-rune-overflow-item，其余写 data-overflow-hidden
//                            （CSS 藏起来但仍可测量），data-rune-overflow-rest（“+N”）的文字取自 data-rune-more 里的第 N 项
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
//
// 这个文件只负责安装：注册自定义事件、挂全局的事件委托与 DOM 变化监听，再分给 js/ 下各模块：
// overlays（浮层开关、对话框、菜单）、floating（定位）、lists（列表与选项弹出层）、tooltips（提示）、overflow（标签收起）、
// controls（输入框一类）、effects（复制、波纹、描线、离场）、layout（网格）、shared（共用的小工具）。

import { autosize, focusOnShell, onFocusLink, sendKeyCommand, spin, syncIndeterminate, updateCount } from './js/controls.js';
import { blockInert, copyFrom, drawIcon, startDismiss, startRipple } from './js/effects.js';
import { watchGrid } from './js/layout.js';
import { activeIndex, closeAfterPick, highlightDefault, highlightUnderPointer, hostOf, keepFocusOnListHost, moveInList, onHostInput, onOptionsChanged, onQueryChanged, syncHideEmpty, syncHighlight, toggleFromShell } from './js/lists.js';
import { scheduleOverflow, watchOverflow } from './js/overflow.js';
import { announce, closeOrRequest, moveInMenu, onPopoverToggle, open, requestClose, syncOpen, updateScrollLock } from './js/overlays.js';
import { hideTooltip, hideTooltipOf, onPointerOut, onPointerOver, scheduleTooltip, shownTooltip, tooltipOf } from './js/tooltips.js';

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
  blazor.registerCustomEventType('runecloserequest', {
    browserEventName: 'rune-close-request',
    createEventArgs: (event) => ({ reason: event.detail.reason }),
  });
  blazor.registerCustomEventType('runecommand', {
    browserEventName: 'rune-command',
    createEventArgs: (event) => ({ name: event.detail.name }),
  });

  // 原生的 toggle（popover）与 close（dialog）不冒泡：在捕获阶段接住，转发成冒泡的 rune-toggle
  document.addEventListener('toggle', onPopoverToggle, true);
  // Esc：原生 dialog 先发 cancel（不冒泡，在捕获阶段接住）；要先问 C# 的对话框拦下来，改发关闭请求
  document.addEventListener('cancel', (event) => {
    if (event.target instanceof HTMLDialogElement && event.target.matches('[data-rune-guard-close]')) {
      event.preventDefault();
      requestClose(event.target, 'escape');
    }
  }, true);
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
  // 输入法组合期间的 input 不交给 C#（不拿拼音字母去过滤），组合结束补发一次
  document.addEventListener('input', (event) => {
    if (event.isComposing && event.target instanceof Element && event.target.matches('input[data-rune-list~="text"]')) event.stopImmediatePropagation();
  }, true);
  document.addEventListener('compositionend', (event) => {
    if (event.target instanceof Element && event.target.matches('input[data-rune-list~="text"]')) event.target.dispatchEvent(new Event('input', { bubbles: true }));
  });
  document.addEventListener('input', (event) => {
    onHostInput(event);
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
    attributeFilter: ['data-open', 'data-indeterminate', 'data-rune-draw', 'data-rune-copy-request', 'data-rune-autosize', 'data-rune-query', 'aria-busy'],
  });
  syncWithin(document);
  // 装好了：端到端测试、对照工具等它出现再操作（脚本加载之前的点击没人处理）
  document.documentElement.setAttribute('data-rune-ready', '');
}

/** 同步一个元素（含自身）里所有带状态属性的元素 */
function syncWithin(root) {
  const sync = (element) => {
    if (element.matches('[data-rune-overlay][data-open="true"]')) syncOpen(element);
    if (element.matches('input[data-indeterminate="true"]')) syncIndeterminate(element);
    if (element.matches('svg[data-rune-draw="true"]')) drawIcon(element);
    if (element.matches('[data-rune-grid-span]')) watchGrid(element.parentElement);
    if (element.matches('textarea[data-rune-autosize]')) autosize(element);
    if (element.matches('[data-rune-overflow]')) watchOverflow(element);
  };
  if (root instanceof Element) sync(root);
  root.querySelectorAll('[data-rune-overlay][data-open="true"], input[data-indeterminate="true"], svg[data-rune-draw="true"], [data-rune-grid-span], textarea[data-rune-autosize], [data-rune-overflow]').forEach(sync);
}

function onMutations(mutations) {
  for (const mutation of mutations) {
    if (mutation.type === 'attributes') {
      if (mutation.attributeName === 'data-indeterminate') syncIndeterminate(mutation.target);
      else if (mutation.attributeName === 'data-rune-autosize') autosize(mutation.target);
      else if (mutation.attributeName === 'data-rune-query') onQueryChanged(mutation.target);
      else if (mutation.attributeName === 'aria-busy') syncHideEmpty(mutation.target.closest('[role="listbox"]'));
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
    if (!(mutation.target instanceof Element)) continue;
    // 列表重新渲染（虚拟滚动换了一批选项、选项变了）：高亮放回高亮项上
    if (mutation.addedNodes.length > 0) {
      const list = mutation.target.closest('[role="listbox"]');
      const host = list && hostOf(list);
      if (host?.hasAttribute('data-rune-active')) syncHighlight(host, list);
      if (list) onOptionsChanged(list);
    }
    // 一行标签里加减了标签（或“+N”的文字变了）：重新量
    const row = mutation.target.closest('[data-rune-overflow]');
    if (row) scheduleOverflow(row);
  }
}

function onClick(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;

  closeAfterPick(target);
  toggleFromShell(target);

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
    if (overlay) closeOrRequest(overlay, 'close');
    return;
  }

  // 点在对话框的遮罩上：事件目标是 dialog 本身，位置在它的边框之外
  if (target instanceof HTMLDialogElement && target.matches('[data-rune-light-dismiss][open]')) {
    const rect = target.getBoundingClientRect();
    const inside = event.clientX >= rect.left && event.clientX <= rect.right && event.clientY >= rect.top && event.clientY <= rect.bottom;
    if (!inside && event.detail > 0) closeOrRequest(target, 'outside');
  }
}


function onKeyDown(event) {
  if (event.key === 'Escape' && shownTooltip) hideTooltip(shownTooltip);
  if (event.key === 'Backspace' || event.key === 'Escape') sendKeyCommand(event);
  if (event.key === 'Enter' || event.key === ' ') startRipple(event);
  if (event.target instanceof HTMLInputElement && event.target.matches('[data-rune-spin]')) spin(event.target, event);
  const menu = event.target instanceof Element ? event.target.closest('[role="menu"][data-rune-overlay]') : null;
  if (menu) moveInMenu(menu, event);
  if (event.target instanceof Element && event.target.matches('[data-rune-list]')) moveInList(event.target, event);
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
