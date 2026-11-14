// 列表：类型搜索、高亮与键盘（data-rune-list）、选项弹出层（Select）的开关与焦点、搜索词变化后的高亮与播报。
// 树（role="tree"，Tree）也是这样的列表：各行是 treeitem，↑↓、Home / End、类型搜索照列表的走，展开、进出层级在 tree.js。

import { sendCommand, speak } from '../core/shared.js';


// 每个菜单、列表各有一个输入缓冲（同 vue-tsx-pro 的 createTypeahead）
const typeaheadQueries = new WeakMap();

/**
 * 类型搜索（WAI-ARIA 的 type-ahead）：在菜单、列表里直接按字母，跳到以它开头的项。
 * 连续输入拼成一个词（“ex” 跳到 Export）；停顿 500 毫秒后重新开始；反复按同一个字母在以它开头的项之间轮换。
 * labels 是各项的文字，current 是当前项的序号（没有时 -1）；返回该落到的项的序号，不是可打印字符或没有匹配时 -1。
 */
export function typeahead(owner, key, labels, current) {
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

// 列表里的一项：选项，或树的一行
const ITEM = '[role="option"], [role="treeitem"]';

/** 宿主控制的列表：宿主本身是 listbox（或 tree），或经 aria-controls 指向它。 */
function listOf(host) {
  const role = host.getAttribute('role');
  if (role === 'listbox' || role === 'tree') return host;
  const id = host.getAttribute('aria-controls');
  return id ? document.getElementById(id) : null;
}

/** 列表的键盘宿主（焦点所在、写 aria-activedescendant 的元素）：有几个时（触发按钮与搜索框）取有焦点的那个。 */
export function hostOf(list) {
  if (list.hasAttribute('data-rune-list')) return list;
  const active = document.activeElement;
  if (active?.matches('[data-rune-list]') && listOf(active) === list) return active;
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

// 树的行按 data-index 编号（aria-posinset 是在同级里的位置）
function indexOfOption(option) {
  const row = option.getAttribute('data-index');
  return row !== null ? Number(row) : Number(option.getAttribute('aria-posinset')) - 1;
}

/** 高亮项的序号；没有、或选项变少后已经不在了时 -1。 */
export function activeIndex(host, list) {
  const index = Number(host.getAttribute('data-rune-active') ?? -1);
  return index >= 0 && index < optionTable(list).length ? index : -1;
}

/** 高亮一项（-1 取消）；scroll 时滚进可见范围（键盘移动；指针移过的本来就看得见）。 */
export function highlight(host, list, index, scroll) {
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
export function syncHighlight(host, list) {
  const index = activeIndex(host, list);
  const option = optionAt(list, index);
  for (const other of list.querySelectorAll(':is([role="option"], [role="treeitem"])[data-highlighted]')) {
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
export function highlightDefault(host, list) {
  const table = optionTable(list);
  // 虚拟滚动时已选项可能没渲染：C# 把它的序号写在 data-rune-selected 上
  const attribute = list.getAttribute('data-rune-selected');
  const selected = attribute !== null
    ? Number(attribute)
    : [...list.querySelectorAll(':is([role="option"], [role="treeitem"])[aria-selected="true"]')].map(indexOfOption).find((index) => !table[index]?.disabled) ?? -1;
  highlight(host, list, selected >= 0 ? selected : table.findIndex((option) => !option.disabled), true);
}

export function moveInList(host, event) {
  if (event.altKey || event.ctrlKey || event.metaKey) return;
  if (host.getAttribute('aria-disabled') === 'true') return;
  const list = listOf(host);
  if (!list) return;
  const flags = flagsOf(host);
  const table = optionTable(list);
  // 可用项的序号：方向键、类型搜索都跳过不可用的项
  const usable = table.flatMap((option, index) => (option.disabled ? [] : [index]));
  const panel = popoverOfHost(host);
  if (panel && !panel.matches(':popover-open')) {
    keyOnClosedTrigger(host, panel, list, event, flags, usable.map((index) => table[index].label), usable);
    return;
  }
  if (event.key === 'Tab') {
    // 弹层里不用 Tab 切换：关掉，焦点照常移到下一个
    list.closest('[popover]')?.hidePopover();
    return;
  }
  const current = usable.indexOf(activeIndex(host, list));
  const step = listMoves[event.key];
  // 输入框里 Home / End 移动光标
  if (step !== undefined && !(flags.includes('text') && typeof step === 'string')) {
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
export function keepFocusOnListHost(event) {
  if (event.target instanceof Element && event.target.closest('[role="listbox"] [role="option"]')) event.preventDefault();
}

export function highlightUnderPointer(event) {
  const option = event.target instanceof Element ? event.target.closest('[role="option"]') : null;
  if (!option || option.hasAttribute('data-highlighted') || option.getAttribute('aria-disabled') === 'true') return;
  const list = option.closest('[role="listbox"]');
  const host = list && hostOf(list);
  if (host) highlight(host, list, indexOfOption(option), false);
}

// 触发按钮（popovertarget 指向弹层的列表宿主）打开弹层；焦点一直在触发按钮上（可搜索时在弹层里的搜索框上），
// 高亮经 aria-activedescendant 告诉读屏。选中由 C# 处理（点击选项），这里只管开关、高亮与焦点。

// 用 ↑ 打开的触发按钮：打开后高亮最后一项
const highlightLastOnOpen = new WeakSet();

// 按下时弹层开着的外壳：按下已经把弹层当作点在外面关掉了（popover="auto"），松开后的点击不要再打开
export const pressedWhileOpen = new WeakSet();

// 用户输入过的弹层（data-rune-hide-empty 的）：有建议时才显示，Esc、选中、失焦关上后清掉
const typedPanels = new WeakSet();

// 因输入而打开的弹层：打开后不高亮已选项（Enter 会把它取消掉），等过滤结果渲染出来再高亮第一项（onQueryChanged）
const openedByTyping = new WeakSet();

/** 宿主的弹层：触发按钮的 popovertarget，或输入框（不能做 popovertarget）上的 data-rune-popover。 */
export function popoverOfHost(host) {
  if (host.popoverTargetElement) return host.popoverTargetElement;
  const id = host.getAttribute('data-rune-popover');
  return id ? document.getElementById(id) : null;
}

/** 弹层外面的列表宿主（Select 的触发按钮、Combobox 的输入框）；弹层里的搜索框不算。 */
function listTriggerOf(panel) {
  const list = panel.querySelector('[role="listbox"]');
  if (!list?.id) return null;
  return [...document.querySelectorAll(`[data-rune-list][aria-controls="${CSS.escape(list.id)}"]`)].find((host) => !panel.contains(host)) ?? null;
}

/** 关闭时在触发按钮上按键：↓ / Enter / 空格打开，↑ 打开并高亮最后一项；单选时按字母直接选中以它开头的项（同原生 select）。 */
function keyOnClosedTrigger(host, panel, list, event, flags, labels, usable) {
  if (flags.includes('text')) {
    // 输入框：只有 ↑↓ 打开，并显示全部选项（C# 清掉过滤词）；Enter、空格、字母照常输入
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    event.preventDefault();
    sendCommand(host, 'show-all');
    if (event.key === 'ArrowUp') highlightLastOnOpen.add(host);
    panel.showPopover();
    return;
  }
  if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    panel.showPopover();
  } else if (event.key === 'ArrowUp') {
    event.preventDefault();
    highlightLastOnOpen.add(host);
    panel.showPopover();
  } else if (flags.includes('typeahead') && list.getAttribute('aria-multiselectable') !== 'true') {
    const selected = list.querySelector('[role="option"][aria-selected="true"]');
    const match = typeahead(list, event.key, labels, selected ? usable.indexOf(indexOfOption(selected)) : -1);
    if (match < 0) return;
    event.preventDefault();
    const option = optionAt(list, usable[match]);
    if (option && option.getAttribute('aria-selected') !== 'true') option.click();
  }
}

/** 定位好、焦点移进去之后：在当前宿主（搜索框或触发按钮）上高亮已选的项（没有就第一项），用 ↑ 打开时是最后一项。 */
export function listPopoverOpened(panel) {
  const trigger = listTriggerOf(panel);
  const list = panel.querySelector('[role="listbox"]');
  if (!trigger || !list || !panel.matches(':popover-open')) return;
  if (openedByTyping.delete(panel)) return;
  const host = hostOf(list) ?? trigger;
  if (!highlightLastOnOpen.delete(trigger)) {
    highlightDefault(host, list);
    return;
  }
  const table = optionTable(list);
  highlight(host, list, table.findLastIndex((option) => !option.disabled), true);
}

/** 关闭后：清掉高亮；焦点在弹层里（搜索框，关上后落到 body）时还给触发按钮。 */
export function listPopoverClosed(panel) {
  const trigger = listTriggerOf(panel);
  const list = panel.querySelector('[role="listbox"]');
  typedPanels.delete(panel);
  // 打开后还没定位完（第一次要加载 Floating UI）就关了时 listPopoverOpened 没来得及清它：留着会吞掉下一次打开的默认高亮
  openedByTyping.delete(panel);
  if (!trigger || !list) return;
  highlightLastOnOpen.delete(trigger);
  for (const host of [trigger, ...panel.querySelectorAll('[data-rune-list]')]) highlight(host, list, -1, false);
  const active = document.activeElement;
  if (!active || active === document.body || panel.contains(active)) trigger.focus();
}

/**
 * 弹层外面控制列表的元素（触发按钮、输入框、展开按钮）的 aria-expanded、外壳的 data-state 跟着弹层
 * （C# 收到 rune-toggle 后写的是同样的值；静态 SSR 只有这里写）。
 */
export function syncTriggerExpanded(panel, opened) {
  const list = panel.querySelector('[role="listbox"]');
  if (!list?.id) return;
  const controls = [...document.querySelectorAll(`[aria-controls="${CSS.escape(list.id)}"]`)].filter((element) => !panel.contains(element));
  for (const element of controls) element.setAttribute('aria-expanded', String(opened));
  listTriggerOf(panel)?.closest('[data-rune-control]')?.setAttribute('data-state', opened ? 'open' : 'closed');
}

// 搜索框（data-rune-list 含 text）、分组显示的输入框（TextField 的 Format）、验证码
const COMPOSING_HOST = 'input[data-rune-list~="text"], input[data-rune-format], [data-rune-otp] > input';

/**
 * 输入法组合期间这些输入框的 input 不往下传（捕获阶段，赶在 Blazor 与改写输入框的处理之前）：
 * 不拿拼音字母去过滤，也不在组合中改写输入框（会打断组合）；组合结束时补发一次
 */
export function holdComposingInput(event) {
  if (event.isComposing && event.target instanceof Element && event.target.matches(COMPOSING_HOST)) event.stopImmediatePropagation();
}

/** 组合结束：补发一次 input，按最终的文字过滤、重排（验证码、分组显示的输入框这时才改写） */
export function releaseComposedInput(event) {
  if (event.target instanceof Element && event.target.matches(COMPOSING_HOST)) event.target.dispatchEvent(new Event('input', { bubbles: true }));
}

/**
 * 在输入框宿主（data-rune-popover）里输入：打开弹层。data-rune-hide-empty 的弹层（Autocomplete）先记下“输入过”，
 * 等 C# 渲染出建议后由 syncHideEmpty 决定显示不显示（没有建议时不弹出）。
 */
export function onHostInput(event) {
  const host = event.target;
  if (!(host instanceof HTMLInputElement) || event.isComposing || !host.matches('[data-rune-list]')) return;
  // 旧的高亮指着上一批结果：C# 渲染出新结果之前按 Enter 不该选中它（新结果渲染后 onQueryChanged 高亮第一项）。
  // 命令面板的输入框没有弹层，也一样
  const list = listOf(host);
  if (list && flagsOf(host).includes('text')) highlight(host, list, -1, false);
  if (!host.hasAttribute('data-rune-popover')) return;
  const panel = popoverOfHost(host);
  if (!panel) return;
  if (panel.hasAttribute('data-rune-hide-empty')) {
    typedPanels.add(panel);
    syncHideEmpty(panel.querySelector('[role="listbox"]'));
  } else if (!panel.matches(':popover-open')) {
    openedByTyping.add(panel);
    panel.showPopover();
  }
}

/** data-rune-hide-empty 的弹层：输入过且有建议（或正在加载）时显示，否则藏起来；建议、加载状态、搜索词变了时调用。 */
export function syncHideEmpty(list) {
  const panel = list?.closest('[popover][data-rune-hide-empty]');
  if (!panel) return;
  const trigger = listTriggerOf(panel);
  const wanted = typedPanels.has(panel) && trigger === document.activeElement
    && (optionTable(list).length > 0 || list.getAttribute('aria-busy') === 'true');
  if (wanted && !panel.matches(':popover-open')) {
    openedByTyping.add(panel);
    panel.showPopover();
  }
  else if (!wanted && panel.matches(':popover-open')) panel.hidePopover();
}

/** 单选列表在弹层里选中了一项（点击，或 Enter 转成的点击）：关闭弹层；有下一级的项（级联选择）只是展开，不关。 */
export function closeAfterPick(target) {
  const option = target.closest('[role="option"]:not([aria-disabled="true"]):not([aria-haspopup])');
  const list = option?.closest('[role="listbox"]');
  if (!list || list.getAttribute('aria-multiselectable') === 'true') return;
  const panel = list.closest('[popover]');
  if (panel?.matches(':popover-open')) panel.hidePopover();
}

/** 点在下拉外壳的空白处（含已选标签的文字）：与点触发按钮一样切换打开。 */
export function toggleFromShell(target) {
  const shell = target.closest('[data-rune-control]');
  const trigger = shell?.querySelector(':scope > button[role="combobox"][popovertarget]');
  if (!trigger || trigger.disabled || target.closest('button, a, input')) return;
  if (pressedWhileOpen.has(shell)) return;
  trigger.click();
}

// 搜索词变了（C# 渲染了过滤后的选项）：有词时高亮第一个匹配项，清空时回到默认项；停顿后播报结果数
const resultTimers = new WeakMap();

const RESULT_ANNOUNCE_DELAY = 500;

/**
 * 弹层里的列表换了一批选项（远程结果晚到、虚拟滚动）：有搜索词且还没有高亮项时高亮第一项
 * （搜索词变化时可能还是骨架行，那时没有可高亮的）。
 */
export function onOptionsChanged(list) {
  syncHideEmpty(list);
  if (!isShown(list)) return;
  const host = hostOf(list);
  // 有搜索词，或宿主要一直高亮第一项（命令面板）：还没有高亮项时高亮第一项
  if (!host || (!list.getAttribute('data-rune-query') && !flagsOf(host).includes('first'))) return;
  // 宿主说了不自动高亮（manual：允许自己输入的标签输入框）
  if (flagsOf(host).includes('manual')) return;
  if (activeIndex(host, list) < 0) highlight(host, list, optionTable(list).findIndex((option) => !option.disabled), true);
}

/** 列表在打开着的浮层里：弹出层打开着，或在打开的对话框里（命令面板）。 */
function isShown(list) {
  const panel = list.closest('[popover]');
  return panel ? panel.matches(':popover-open') : !!list.closest('dialog[open]');
}

const flagsOf = (host) => (host.getAttribute('data-rune-list') ?? '').split(' ');

/** 宿主写了 first（命令面板的输入框）：聚焦时高亮第一项，打开就能直接回车。 */
export function highlightFirstOnFocus(host) {
  if (!host.matches('[data-rune-list]') || !flagsOf(host).includes('first')) return;
  const list = listOf(host);
  if (list) highlight(host, list, optionTable(list).findIndex((option) => !option.disabled), true);
}

export function onQueryChanged(list) {
  syncHideEmpty(list);
  if (!isShown(list)) return;
  const host = hostOf(list);
  const query = list.getAttribute('data-rune-query') ?? '';
  if (host) {
    // manual：打了字也不高亮第一项（回车交给宿主自己，加的是打的字），上一次的高亮清掉
    if (query && flagsOf(host).includes('manual')) highlight(host, list, -1, false);
    else if (query || flagsOf(host).includes('first')) highlight(host, list, optionTable(list).findIndex((option) => !option.disabled), true);
    else highlightDefault(host, list);
  }
  clearTimeout(resultTimers.get(list));
  if (!query) return;
  resultTimers.set(list, setTimeout(() => {
    const results = list.getAttribute('data-rune-results');
    if (results) speak(results);
  }, RESULT_ANNOUNCE_DELAY));
}

/** 双击一个选项（data-rune-option-dblclick 的列表，穿梭框双击直接移过去）：发出 rune-command（move，带选项的序号）。 */
export function doubleClickOption(event) {
  const option = event.target instanceof Element ? event.target.closest('[role="option"]:not([aria-disabled="true"])') : null;
  const list = option?.closest('[role="listbox"][data-rune-option-dblclick]');
  if (list) sendCommand(list, 'move', String(indexOfOption(option)));
}

// 远程分页的列表末尾的哨兵（data-rune-list-end，ListboxView 的 OnReachEnd）：进入滚动容器可见范围下方约 5 行以内时，
// 在列表上发 rune-command（more），C# 取下一页。哨兵按项数换一个元素：接上一页后它还在范围里时新元素再报一次
// 按滚动容器各一个观察者（容器没了跟着回收）；没有滚动容器时按视口
const endObservers = new WeakMap();
let viewportEndObserver;

export function watchListEnd(sentinel) {
  const list = sentinel.closest('[role="listbox"]');
  if (!list) return;
  const root = sentinel.closest('[data-rune-scroll-viewport]');
  let observer = root ? endObservers.get(root) : viewportEndObserver;
  if (!observer) {
    observer = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting || !entry.target.isConnected) continue;
        const owner = entry.target.closest('[role="listbox"]');
        if (owner) sendCommand(owner, 'more');
      }
    }, { root, rootMargin: '0px 0px 180px 0px' });
    if (root) endObservers.set(root, observer);
    else viewportEndObserver = observer;
  }
  observer.observe(sentinel);
}
