// 漫游焦点（WAI-ARIA 的 roving tabindex，同 vue-tsx-pro 的 rovingFocus）：一组项里只有一个能用 Tab 进入（tabindex=0），其余为 -1；
// 组内用方向键移动，Home / End 到两端。ToggleGroup、Tabs、Toolbar 共用。
// 容器写 data-rune-roving="horizontal|vertical|both"（可加 " noloop"：到两端不循环），项是容器里的 data-rune-roving-items 选择器（默认 button）；
// 嵌套的组各管各的，容器里的弹层（popover、dialog）不归它。tabindex 由这里写（组件不渲染 tabindex），项增删、选中变化时重新定 Tab 进入点。

const watched = new WeakSet();
// 每组上次聚焦的项：Tab 回来时落在它上面
const lastFocused = new WeakMap();

const isEnabled = (item) => !item.matches(':disabled, [aria-disabled="true"]') && !item.closest('[hidden], [inert]');

// 选中或当前的项：Tab 进入时优先落在它上面
const isMarked = (item) =>
  ['aria-selected', 'aria-pressed', 'aria-checked'].some((name) => item.getAttribute(name) === 'true')
  || (item.hasAttribute('aria-current') && item.getAttribute('aria-current') !== 'false');

function itemsOf(container) {
  const selector = container.getAttribute('data-rune-roving-items') || 'button';
  return [...container.querySelectorAll(selector)].filter((item) => {
    if (item.parentElement?.closest('[data-rune-roving]') !== container) return false;
    const layer = item.closest('[popover], dialog');
    return !layer || layer === container || !container.contains(layer);
  });
}

/** 开始管理一个组（插入时调用；重复调用无副作用）。 */
export function watchRoving(container) {
  if (watched.has(container)) return;
  watched.add(container);
  new MutationObserver(() => syncRoving(container)).observe(container, {
    childList: true,
    subtree: true,
    attributes: true,
    attributeFilter: ['disabled', 'aria-disabled', 'aria-selected', 'aria-pressed', 'aria-checked', 'aria-current', 'hidden'],
  });
  syncRoving(container);
}

/** 定下哪一项是 Tab 进入点，其余设为 -1。 */
function syncRoving(container) {
  const all = itemsOf(container);
  const enabled = all.filter(isEnabled);
  const last = lastFocused.get(container);
  const stop = (last && enabled.includes(last) ? last : undefined) ?? enabled.find(isMarked) ?? enabled[0];
  for (const item of all) {
    const value = item === stop ? '0' : '-1';
    if (item.getAttribute('tabindex') !== value) item.setAttribute('tabindex', value);
  }
}

/** 组里按方向键、Home / End：焦点移到相邻（或两端）的项；不改变选择。 */
export function moveRoving(event) {
  if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || !(event.target instanceof Element)) return;
  const container = event.target.closest('[data-rune-roving]');
  if (!container) return;
  const items = itemsOf(container).filter(isEnabled);
  const index = items.indexOf(event.target);
  if (index < 0) return;
  const [orientation = 'horizontal', ...flags] = container.getAttribute('data-rune-roving').split(' ');
  const rtl = getComputedStyle(container).direction === 'rtl';
  const horizontal = orientation !== 'vertical';
  const vertical = orientation !== 'horizontal';
  const steps = {
    ArrowRight: horizontal ? (rtl ? -1 : 1) : undefined,
    ArrowLeft: horizontal ? (rtl ? 1 : -1) : undefined,
    ArrowDown: vertical ? 1 : undefined,
    ArrowUp: vertical ? -1 : undefined,
    Home: 'first',
    End: 'last',
  };
  const step = steps[event.key];
  if (step === undefined) return;
  event.preventDefault();
  let next;
  if (step === 'first') next = 0;
  else if (step === 'last') next = items.length - 1;
  else if (flags.includes('noloop')) next = Math.min(Math.max(index + step, 0), items.length - 1);
  else next = (index + step + items.length) % items.length;
  items[next].focus();
}

/** 组里的项获得焦点：记下来，它成为 Tab 进入点。 */
export function noteRovingFocus(event) {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const container = target.closest('[data-rune-roving]');
  if (!container || !itemsOf(container).includes(target)) return;
  lastFocused.set(container, target);
  syncRoving(container);
}
