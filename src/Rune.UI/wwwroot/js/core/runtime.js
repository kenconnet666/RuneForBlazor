// JS 初始化器的主体（入口 Rune.UI.lib.module.js 在后台加载它，各个 data-rune-* 属性的约定写在入口的开头）：
// 挂全局的事件委托与 DOM 变化监听，再分给同目录下的各模块。

import { autosize, focusInvalid, focusOnShell, formatInput, onFocusLink, refocusFrom, sendKeyCommand, sendPasteCommand, spin, syncIndeterminate, toggleReveal, updateCount } from '../input/controls.js';
import { blockInert, copyFrom, drawCheckOnChange, drawIcon, startDismiss, startRipple } from './effects.js';
import { createFilePreviews, onFileDragEnter, onFileDragLeave, onFileDrop, pasteFiles, releaseDetachedPreviews, releaseFilePreviews, sendRelativePaths } from '../input/files.js';
import { guardLeave } from './leave.js';
import { clickCascader, keyCascader, onCascaderFocusIn } from '../input/cascader.js';
import { keyMentions, onMentionsClick, onMentionsCompositionEnd, onMentionsFocusOut, onMentionsInput, onMentionsKeyUp, pickMention } from '../input/mentions.js';
import { releasePointer, trackPointer, watchPointer } from './pointer.js';
import { clickToastClose, focusToaster, grabToast, keyToast, onToasterFocusIn, onToasterFocusOut } from '../overlay/toast.js';
import { watchEllipsis, watchGrid, watchMarquee, watchSize, watchViewportFill } from '../layout/layout.js';
import { onTweenValueChanged, watchTween } from './tween.js';
import { clickBackTop, watchAffix, watchBackTop } from '../layout/affix.js';
import { closeSpeedDialsOutside, keySpeedDial } from '../layout/float-button.js';
import { drawWatermark } from '../layout/watermark.js';
import { blurSortable, grabSortable, keySortable } from '../table/sortable.js';
import { showLightboxImage, watchLightbox } from '../overlay/lightbox.js';
import { closeTreeAfterPick, keyTree, pressTreeItem } from '../display/tree.js';
import { watchVirtual } from '../table/virtual.js';
import { keyTour, watchTour } from '../overlay/tour.js';
import { watchTable } from '../table/table.js';
import { watchLocalSetting } from './storage.js';
import { activeIndex, closeAfterPick, doubleClickOption, highlightFirstOnFocus, highlightDefault, highlightUnderPointer, holdComposingInput, hostOf, keepFocusOnListHost, moveInList, onHostInput, onOptionsChanged, onQueryChanged, releaseComposedInput, syncHideEmpty, syncHighlight, toggleFromShell, watchListEnd } from '../input/lists.js';
import { watchIndicator } from '../layout/indicator.js';
import { scheduleOverflow, watchOverflow } from '../layout/overflow.js';
import { moveRoving, noteRovingFocus, watchRoving } from './roving.js';
import { onOtpFocus, onOtpInput, watchOtp } from '../input/otp.js';
import { endRatingPreview, pickRating, previewRating } from '../input/rating.js';
import { watchPageScrollbar, watchScrollArea } from '../layout/scroll-area.js';
import { highlightCode } from '../display/highlight.js';
import { watchToc } from '../navigation/toc.js';
import { navigationEnded, navigationStarted, watchLoadingBar } from '../navigation/loading-bar.js';
import { formatHotkeyLabel, hotkeyTarget, isComposingKey } from './hotkeys.js';
import { auxClickTabs, grabTab, keyTabs, scrollTabs, watchTabs } from '../navigation/tabs.js';
import { watchCollapse } from '../layout/collapse.js';
import { grabResizeHandle, grabSplitter, keyResizeHandle, keySplitter, resetResizeHandle, resetSplitter, watchResizeHandle } from '../layout/resize.js';
import { grabSlider, keySlider, watchSlider } from '../input/slider.js';
import { chooseTheme, fillTokenValue, installTheme, reapplyTheme, watchThemeSwitcher } from './theme.js';
import { exitFullscreenOnEscape, onFullscreenChange, releaseFullscreen, syncFullscreen } from './fullscreen.js';
import { closeOnLink, closeOrRequest, focusArrivedContent, grabDialog, onDialogCancel, onDialogClose, onPopoverToggle, open, syncOpen, watchCloseWhen } from '../overlay/overlays.js';
import { clickSubMenuTrigger, hoverSubMenu, leaveSubMenu, moveInMenu, openContextMenu, openMenuFromTrigger, rootMenuOf } from '../overlay/menus.js';
import { onHoverEscape, onHoverFocusIn, onHoverFocusOut, onHoverPointerDown, onHoverPointerOut, onHoverPointerOver } from '../overlay/hover-layers.js';

/**
 * 装上事件委托与 DOM 变化监听（入口在 Blazor 启动后调用一次）。document 上的事件经入口的 on(事件, 处理函数, 是否捕获) 挂到
 * 入口启动时就占好的位置上：处理顺序仍排在 Blazor 自己的事件委托之前。
 */
export function install(blazor, on) {
  // 静态 SSR 页面之间的增强导航：带 data-navigation 的加载条跟着走（独立的 Server / WebAssembly 应用没有这两个事件）
  blazor.addEventListener?.('enhancednavigationstart', navigationStarted);
  blazor.addEventListener?.('enhancednavigationend', navigationEnded);
  // 增强导航合并新页面时根元素的属性（data-theme、底色）可能被换掉：按记住的选择再写一次
  blazor.addEventListener?.('enhancednavigationend', reapplyTheme);
  installTheme();

  // 原生的 toggle（popover）与 close（dialog）不冒泡：在捕获阶段接住，转发成冒泡的 rune-toggle
  on('toggle', onPopoverToggle, true);
  // 对话框的 cancel（Esc）与 close 也不冒泡
  on('cancel', onDialogCancel, true);
  on('close', onDialogClose, true);

  // 捕获阶段：要赶在 Blazor 的链接拦截之前（它会把 #id 按 <base href> 解析成别的页面并跳过去）
  on('click', onFocusLink, true);
  // 离开确认：同样要赶在 Blazor 的链接拦截之前
  on('click', guardLeave, true);
  // 加载中、可聚焦的不可用按钮：拦下点击的默认动作（提交表单、跟随链接）；使用方的点击处理组件已经去掉
  on('click', blockInert, true);
  on('click', onClick);
  on('keydown', onKeyDown);
  on('pointerdown', startRipple);
  on('pointerdown', focusOnShell);
  on('pointerdown', keepFocusOnListHost);
  on('pointerdown', pressTreeItem);
  on('pointerdown', grabSlider);
  on('pointerdown', grabResizeHandle);
  on('pointerdown', grabSplitter);
  on('pointerdown', grabSortable);
  on('pointerdown', grabTab);
  on('pointerdown', grabToast);
  on('pointerdown', grabDialog);
  on('pointerdown', closeSpeedDialsOutside);
  on('dragenter', onFileDragEnter);
  on('dragleave', onFileDragLeave);
  on('drop', onFileDrop);
  on('dblclick', resetResizeHandle);
  on('dblclick', resetSplitter);
  on('dblclick', doubleClickOption);
  on('auxclick', auxClickTabs);
  on('pointerdown', onHoverPointerDown);
  on('pointermove', highlightUnderPointer);
  on('pointermove', trackPointer);
  document.documentElement.addEventListener('pointerleave', releasePointer);
  on('change', drawCheckOnChange);
  // 图片（头像、Image、预览的大图）：load / error 不冒泡，在捕获阶段接住
  on('load', (event) => {
    markImage(event.target);
    showLightboxImage(event.target);
  }, true);
  on('error', (event) => markImage(event.target), true);
  // 输入法组合期间的 input 不交给 C#（不拿拼音字母去过滤），组合结束补发一次
  on('input', holdComposingInput, true);
  // 验证码：赶在 Blazor 读值之前过滤
  on('input', onOtpInput, true);
  // 分组显示的输入框（手机号、身份证号）：同样赶在 Blazor 读值之前重排
  on('input', formatInput, true);
  on('compositionend', (event) => {
    releaseComposedInput(event);
    onMentionsCompositionEnd(event);
  });
  on('keyup', onMentionsKeyUp);
  on('input', (event) => {
    onHostInput(event);
    onMentionsInput(event);
    const target = event.target;
    if (target instanceof HTMLTextAreaElement && target.hasAttribute('data-rune-autosize')) autosize(target);
    if (target instanceof Element && target.hasAttribute('data-rune-count')) updateCount(target);
  });
  on('paste', sendPasteCommand);
  on('paste', pasteFiles);
  // 图片墙选到的图片：赶在 InputFile 之前建本地地址
  on('change', createFilePreviews, true);
  on('change', sendRelativePaths, true);
  on('fullscreenchange', onFullscreenChange);
  on('focusin', onFocusIn);
  on('focusout', onFocusOut);
  on('pointerover', onHoverPointerOver);
  on('pointerover', hoverSubMenu);
  on('pointerout', leaveSubMenu);
  on('contextmenu', openContextMenu);
  on('pointerover', previewRating);
  on('pointerout', onHoverPointerOut);
  on('pointerout', endRatingPreview);

  // 组件改属性（data-open、data-indeterminate），这里同步原生状态；新插入的元素（第一次渲染就带着状态）一并处理
  new MutationObserver(onMutations).observe(document.documentElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeOldValue: true,
    attributeFilter: ['data-open', 'data-indeterminate', 'data-rune-draw', 'data-rune-copy-request', 'data-rune-autosize', 'data-rune-query', 'aria-busy', 'data-rune-fullscreen', 'data-rune-invalid-submit', 'data-value', 'data-rune-watermark', 'data-rune-upload-previews'],
  });
  syncWithin(document);
  // 装好了：端到端测试、对照工具等它出现再操作（脚本加载之前的点击没人处理）。页面上已有的元素要的按需模块也要装好：
  // 不然在日期输入框里打字时它的模块可能还没加载完，按键没人处理
  Promise.allSettled([...loading]).then(() => document.documentElement.setAttribute('data-rune-ready', ''));
}

// 别的包（Rune.UI.Dates、Rune.Charts）的模块：第一次遇到它的元素时才加载，只用表格、表单的页面不下载。加载中的记在 loading 里（见 install）
const loading = new Set();
const lazy = (load, name) => (element) => {
  const pending = load().then((module) => module[name](element));
  loading.add(pending);
  pending.finally(() => loading.delete(pending));
};

// 插入时（第一次渲染就带着状态）要处理的元素：选择器与处理函数，一处登记
const watched = [
  ['[data-rune-overlay][data-open="true"]', syncOpen],
  ['input[data-indeterminate="true"]', syncIndeterminate],
  ['svg[data-rune-draw="true"]', drawIcon],
  ['[data-rune-grid-span]', (element) => watchGrid(element.parentElement)],
  ['[data-rune-balanced]', watchGrid],
  ['[data-rune-list-end]', watchListEnd],
  ['[data-rune-observe-size]', watchSize],
  ['textarea[data-rune-autosize]', autosize],
  ['[data-rune-overflow]', watchOverflow],
  ['[data-rune-roving]', watchRoving],
  ['[data-rune-indicator]', watchIndicator],
  ['[data-rune-slider]', watchSlider],
  ['[data-rune-otp]', watchOtp],
  ['[data-rune-scroll-area]', watchScrollArea],
  ['[data-rune-page-scrollbar]', watchPageScrollbar],
  ['[data-rune-collapse]', watchCollapse],
  ['[data-rune-resize]', watchResizeHandle],
  ['img[data-rune-image]', (image) => image.complete && markImage(image)],
  ['[data-rune-lightbox-viewport]', watchLightbox],
  ['img[data-rune-lightbox-image]', (image) => image.complete && showLightboxImage(image)],
  ['code[data-rune-highlight]', highlightCode],
  ['[data-rune-tabs]', watchTabs],
  ['[data-rune-toc]', watchToc],
  ['[data-rune-loading-bar]', watchLoadingBar],
  ['[data-rune-hotkey-label]', formatHotkeyLabel],
  ['dialog[data-rune-close-when]', watchCloseWhen],
  ['[data-rune-fullscreen="true"]', syncFullscreen],
  ['[data-rune-theme-switcher]', watchThemeSwitcher],
  ['[data-rune-token-value]', fillTokenValue],
  ['[data-rune-tween]', watchTween],
  ['[data-rune-viewport-fill]', watchViewportFill],
  ['[data-rune-affix]', watchAffix],
  ['[data-rune-back-top]', watchBackTop],
  ['[data-rune-ellipsis-text]', watchEllipsis],
  ['[data-rune-marquee]', watchMarquee],
  ['[data-rune-watermark]', drawWatermark],
  ['[data-rune-pointer]', watchPointer],
  ['[data-rune-virtual]', watchVirtual],
  ['dialog[data-rune-tour]', watchTour],
  ['table[data-rune-table]', watchTable],
  ['template[data-rune-local-setting]', watchLocalSetting],
  ['[data-rune-carousel]', lazy(() => import('../display/carousel.js'), 'watchCarousel')],
  ['[data-rune-color-picker]', lazy(() => import('../input/color-picker.js'), 'watchColorPicker')],
  ['[data-rune-cropper]', lazy(() => import('../input/cropper.js'), 'watchCropper')],
  ['[data-rune-signature]', lazy(() => import('../input/signature.js'), 'watchSignature')],
  ['[data-rune-date-segments]', lazy(() => import('../../../Rune.UI.Dates/js/date-segments.js'), 'watchDateSegments')],
  ['[data-rune-calendar]', lazy(() => import('../../../Rune.UI.Dates/js/calendar.js'), 'watchCalendar')],
  ['[data-rune-chart]', lazy(() => import('../../../Rune.Charts/js/charts.js'), 'watchChart')],
];
const anyWatched = watched.map(([selector]) => selector).join(', ');

/** 同步一个元素（含自身）里所有带状态属性的元素 */
function syncWithin(root) {
  const sync = (element) => {
    for (const [selector, handle] of watched) if (element.matches(selector)) handle(element);
  };
  // 根节点多半不是要处理的元素：先用合起来的选择器问一次，不逐条比
  if (root instanceof Element && root.matches(anyWatched)) sync(root);
  root.querySelectorAll(anyWatched).forEach(sync);
}

/** 祖先里有没有也是这一批新插入的（它的 querySelectorAll 已经覆盖了这个节点） */
function hasAddedAncestor(node, added) {
  for (let parent = node.parentElement; parent; parent = parent.parentElement) if (added.has(parent)) return true;
  return false;
}

/** 图片加载完或失败了：写在它自己的 data-state 上（换图时图片按地址重建，状态跟着重置） */
function markImage(image) {
  if (!(image instanceof HTMLImageElement) || !image.hasAttribute('data-rune-image')) return;
  image.dataset.state = image.complete && image.naturalWidth > 0 ? 'loaded' : 'error';
}

function onMutations(mutations) {
  // 这一批插入的元素、子节点变了的父元素（其中有插入的）、有没有删除：先收集，后面每样只处理一次
  const added = [];
  const changed = new Set();
  const grown = new Set();
  let removed = false;
  for (const mutation of mutations) {
    if (mutation.type === 'attributes') {
      if (mutation.attributeName === 'data-indeterminate') syncIndeterminate(mutation.target);
      else if (mutation.attributeName === 'data-rune-autosize') autosize(mutation.target);
      else if (mutation.attributeName === 'data-rune-query') onQueryChanged(mutation.target);
      else if (mutation.attributeName === 'aria-busy') syncHideEmpty(mutation.target.closest('[role="listbox"]'));
      else if (mutation.attributeName === 'data-rune-fullscreen') syncFullscreen(mutation.target);
      else if (mutation.attributeName === 'data-rune-watermark') drawWatermark(mutation.target);
      else if (mutation.attributeName === 'data-rune-upload-previews') releaseFilePreviews(mutation.target);
      else if (mutation.attributeName === 'data-value') { if (mutation.target.hasAttribute('data-rune-tween')) onTweenValueChanged(mutation.target); }
      else if (mutation.attributeName === 'data-rune-invalid-submit') { if (mutation.target.hasAttribute('data-rune-invalid-submit')) focusInvalid(mutation.target); }
      else if (mutation.attributeName === 'data-rune-copy-request') {
        if (mutation.target.hasAttribute('data-rune-copy-request')) copyFrom(mutation.target);
      } else if (mutation.attributeName === 'data-rune-draw') {
        if (mutation.target.getAttribute('data-rune-draw') === 'true' && mutation.oldValue !== 'true') drawIcon(mutation.target);
      }
      else if (mutation.target.matches('[data-rune-overlay]')) syncOpen(mutation.target);
      continue;
    }
    for (const node of mutation.addedNodes) if (node instanceof Element) added.push(node);
    if (mutation.removedNodes.length > 0) removed = true;
    if (!(mutation.target instanceof Element)) continue;
    changed.add(mutation.target);
    if (mutation.addedNodes.length > 0) grown.add(mutation.target);
  }

  // Blazor 先插父元素、再逐个插子元素：一棵新子树的每个节点各有一条记录。只从最外层的处理，里面的由它的 querySelectorAll 覆盖
  const addedSet = new Set(added);
  for (const node of added) {
    if (!node.isConnected || hasAddedAncestor(node, addedSet)) continue;
    syncWithin(node);
    focusArrivedContent(node);
  }
  if (removed) {
    releaseFullscreen();
    releaseDetachedPreviews();
  }
  // 列表重新渲染（虚拟滚动换了一批选项、选项变了）：高亮放回高亮项上。一个列表一批只处理一次
  const lists = new Set();
  for (const target of grown) {
    const list = target.closest('[role="listbox"], [role="tree"]');
    if (list) lists.add(list);
  }
  for (const list of lists) {
    const host = hostOf(list);
    if (host?.hasAttribute('data-rune-active')) syncHighlight(host, list);
    onOptionsChanged(list);
  }
  // 一行标签里加减了标签（或“+N”的文字变了）：重新量
  const rows = new Set();
  for (const target of changed) {
    const row = target.closest('[data-rune-overflow]');
    if (row) rows.add(row);
  }
  rows.forEach(scheduleOverflow);
  // 均衡换列的网格加减了项：列数跟着项数变
  for (const target of changed) if (target.hasAttribute('data-rune-balanced')) watchGrid(target);
}

function onClick(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;

  pickRating(event);
  scrollTabs(event);
  closeAfterPick(target);
  closeTreeAfterPick(target);
  clickCascader(target);
  pickMention(target);
  onMentionsClick(target);
  toggleFromShell(target);
  clickToastClose(target);

  refocusFrom(target);
  toggleReveal(target);
  clickBackTop(target);

  const copier = target.closest('[data-rune-copy-on-click]');
  if (copier && !copier.matches(':disabled, [aria-busy="true"]')) copyFrom(copier);

  closeOnLink(target);
  chooseTheme(target);

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

  // 子菜单项：打开子菜单；选了菜单项（不可用、不关菜单的除外）：关闭整串菜单；点了关闭按钮：关闭所在的浮层
  const item = target.closest('[role^="menuitem"]:not([aria-disabled="true"]), [data-rune-close]');
  if (item) {
    if (item.matches('[aria-haspopup="menu"][popovertarget]')) clickSubMenuTrigger(event, item);
    if (item.matches('[aria-haspopup="menu"], [data-rune-keep-open]')) return;
    const overlay = item.matches('[role^="menuitem"]') ? rootMenuOf(item) : item.closest('[data-rune-overlay]');
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
  // 输入法组合中的按键全归输入法：不加减数字、不移动高亮、不删已选项、不触发快捷键
  if (isComposingKey(event)) return;
  // 打开浮层的快捷键（命令面板的 Mod+K）：已经开着时关上
  const hotkeyed = hotkeyTarget(event);
  if (hotkeyed?.matches('[role^="menuitem"]')) {
    hotkeyed.click();
    return;
  }
  if (hotkeyed) {
    if (hotkeyed.open) closeOrRequest(hotkeyed, 'escape');
    else open(hotkeyed);
    return;
  }
  onHoverEscape(event);
  keySpeedDial(event);
  focusToaster(event);
  keyToast(event);
  exitFullscreenOnEscape(event);
  if (event.key === 'Enter' || event.key === ' ') startRipple(event);
  if (event.target instanceof HTMLInputElement && event.target.matches('[data-rune-spin]')) spin(event.target, event);
  if (keyCascader(event) || keyMentions(event) || keySortable(event) || keyTree(event) || keyTour(event)) return;
  const menu = event.target instanceof Element ? event.target.closest('[role="menu"][data-rune-overlay]') : null;
  if (menu) moveInMenu(menu, event);
  else openMenuFromTrigger(event);
  // 提及的文本框也是列表宿主（高亮跟着它），但键盘只在列表打开时由 mentions.js 处理
  if (event.target instanceof Element && event.target.matches('[data-rune-list]:not([data-rune-mentions])')) moveInList(event.target, event);
  moveRoving(event);
  keySlider(event);
  keyResizeHandle(event);
  keySplitter(event);
  keyTabs(event);
  // 列表没用掉的按键再转成命令（Enter 选中了高亮项时不再加标签）
  if (event.key === 'Backspace' || event.key === 'Escape' || event.key === 'Enter') sendKeyCommand(event);
}

function onFocusIn(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  noteRovingFocus(event);
  highlightFirstOnFocus(target);
  onCascaderFocusIn(event);
  onOtpFocus(event);
  onHoverFocusIn(event);
  onToasterFocusIn(event);
  // 列表自己获得焦点：还没有高亮项时高亮默认项（触发按钮、输入框做宿主时由打开下拉的一方决定）
  if (target.matches(':is([role="listbox"], [role="tree"])[data-rune-list]') && activeIndex(target, target) < 0) highlightDefault(target, target);
}

function onFocusOut(event) {
  blurSortable(event);
  onOtpFocus(event);
  onMentionsFocusOut(event);
  onHoverFocusOut(event);
  onToasterFocusOut(event);
}
