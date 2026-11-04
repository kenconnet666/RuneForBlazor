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
//   data-indeterminate       勾选框的半选：原生的 indeterminate 只能用 JS 设置，按这个属性同步
//   data-rune-focus="id"     点击时聚焦这个 id 的控件并滚到它（表单错误汇总里的链接）
//   data-rune-spin           数字输入框：↑↓ 加减步长、Page Up / Down 加减大步长、Home / End 到最小 / 最大，改完发 change（C# 再校正）
//   data-rune-ripple         按钮、菜单项：按下时从指针位置（键盘 Enter / 空格时从中心）扩散一圈波纹，松开后淡出
//   data-rune-inert          加载中、可聚焦的不可用按钮：拦下点击的默认动作（提交表单）
//   data-rune-copy="文字"     复制按钮：点击时（data-rune-copy-on-click）或 data-rune-copy-request 变了时把文字写入剪贴板，
//                            成功后按钮的 data-state 变成 copied（data-rune-copy-timeout 毫秒后复原）、播报 data-rune-copied-text，
//                            发出冒泡的 rune-copied（Blazor 里是 @onrunecopied）
//   data-rune-grid-span="n"  网格里跨 n 列的项：网格实际的列比 n 少时（按宽度自动换列的网格变窄）改成跨当前的列数
//   data-rune-control        输入框的外壳：点在空白处（内边距、图标、前后缀）时聚焦里面的输入元素
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
  document.addEventListener('focusout', clearTooltipDismissal);
  document.addEventListener('pointerout', clearTooltipDismissal);

  // 组件改属性（data-open、data-indeterminate），这里同步原生状态；新插入的元素（第一次渲染就带着状态）一并处理
  new MutationObserver(onMutations).observe(document.documentElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeOldValue: true,
    attributeFilter: ['data-open', 'data-indeterminate', 'data-rune-draw', 'data-rune-copy-request', 'data-rune-autosize'],
  });
  syncWithin(document);
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
  if (event.key === 'Escape') dismissTooltips();
  if (event.key === 'Enter' || event.key === ' ') startRipple(event);
  if (event.target instanceof HTMLInputElement && event.target.matches('[data-rune-spin]')) spin(event.target, event);
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
  if (!shell || target.closest('input, textarea, select, button, a')) return;
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
