// 悬停层：提示（data-rune-tooltip）与悬停卡片（data-rune-hovercard）的悬停意图与显示、隐藏（同 vue-tsx-pro 的 hover-intent 与 useHoverLayer）。

// 指针停留 data-delay 后显示，离开 data-close-delay 后隐藏（可以移到浮层上，WCAG 1.4.13）；键盘聚焦（:focus-visible）立即显示，
// 按下触发元素、Esc 时隐藏；触屏不显示。提示失焦就隐藏；卡片里可以有链接、按钮，焦点移进卡片时不隐藏，离开触发元素与卡片才隐藏。
// 提示一组、卡片一组：同组同一时间只显示一个，刚关掉一个（300ms 内）时下一个直接显示，在一排按钮上移动不用每次等。

const SKIP_DELAY = 300;

const TRIGGER = '[data-rune-tooltip], [data-rune-hovercard]';

const PANEL = '[data-rune-hover-panel]';

const timers = new WeakMap();

/** 每组正显示的浮层与上次关掉的时间：tooltip、card */
const groups = { tooltip: { shown: null, closedAt: 0 }, card: { shown: null, closedAt: 0 } };

function groupOf(panel) {
  return groups[panel.dataset.runeHoverPanel] ?? groups.tooltip;
}

/** 触发元素对应的浮层。 */
export function hoverPanelOf(trigger) {
  // 截断的文字（Ellipsis）：真的截断了才弹出全文
  if (trigger.hasAttribute('data-rune-ellipsis-text') && !trigger.closest('[data-truncated]')) return null;
  const id = trigger.getAttribute('data-rune-tooltip') ?? trigger.getAttribute('data-rune-hovercard');
  return id ? document.getElementById(id) : null;
}

/** 浮层对应的触发元素（定位、判断焦点去向用）。 */
export function hoverTriggerOf(panel) {
  const id = CSS.escape(panel.id);
  return document.querySelector(`[data-rune-tooltip="${id}"], [data-rune-hovercard="${id}"]`);
}

function schedule(panel, show, wait) {
  clearTimeout(timers.get(panel));
  const run = () => (show ? showPanel(panel) : hidePanel(panel));
  if (wait <= 0) run();
  else timers.set(panel, setTimeout(run, wait));
}

function showPanel(panel) {
  if (!panel.isConnected || panel.matches(':popover-open')) return;
  const group = groupOf(panel);
  if (group.shown && group.shown !== panel) hidePanel(group.shown);
  group.shown = panel;
  panel.showPopover();
}

function hidePanel(panel) {
  clearTimeout(timers.get(panel));
  const group = groupOf(panel);
  if (panel.matches(':popover-open')) {
    panel.hidePopover();
    group.closedAt = Date.now();
  }
  if (group.shown === panel) group.shown = null;
}

function closeDelayOf(panel) {
  return Number(panel.dataset.closeDelay ?? 100);
}

// 指针进出：pointerover / pointerout 冒泡，按 relatedTarget 判断是不是真的进出了触发元素（或浮层）
export function onHoverPointerOver(event) {
  if (event.pointerType === 'touch' || !(event.target instanceof Element)) return;
  const trigger = event.target.closest(TRIGGER);
  if (trigger && !trigger.contains(event.relatedTarget)) {
    const panel = hoverPanelOf(trigger);
    if (!panel) return;
    const group = groupOf(panel);
    const warm = group.shown !== null || Date.now() - group.closedAt < SKIP_DELAY;
    schedule(panel, true, warm ? 0 : Number(panel.dataset.delay ?? 500));
    return;
  }
  // 移到浮层上：取消正要做的隐藏
  const panel = event.target.closest(PANEL);
  if (panel) clearTimeout(timers.get(panel));
}

export function onHoverPointerOut(event) {
  if (!(event.target instanceof Element)) return;
  const owner = event.target.closest(`${TRIGGER}, ${PANEL}`);
  if (!owner || owner.contains(event.relatedTarget)) return;
  const panel = owner.matches(PANEL) ? owner : hoverPanelOf(owner);
  if (panel) schedule(panel, false, closeDelayOf(panel));
}

/** 按下触发元素时隐藏它的浮层。 */
export function onHoverPointerDown(event) {
  const trigger = event.target instanceof Element ? event.target.closest(TRIGGER) : null;
  const panel = trigger && hoverPanelOf(trigger);
  if (panel) hidePanel(panel);
}

/** Esc 隐藏正显示的提示与卡片。 */
export function onHoverEscape(event) {
  if (event.key !== 'Escape') return;
  for (const group of Object.values(groups)) if (group.shown) hidePanel(group.shown);
}

/** 只在键盘聚焦时显示；点击聚焦（按钮被按下）时不显示。 */
export function onHoverFocusIn(event) {
  const target = event.target;
  if (target instanceof Element && target.matches(TRIGGER) && target.matches(':focus-visible')) {
    const panel = hoverPanelOf(target);
    if (panel) schedule(panel, true, 0);
  }
}

/** 焦点离开触发元素或卡片：提示直接隐藏；卡片在焦点移进卡片（或回到触发元素）时保留。 */
export function onHoverFocusOut(event) {
  if (!(event.target instanceof Element)) return;
  const owner = event.target.matches(TRIGGER) ? event.target : event.target.closest(PANEL);
  if (!owner) return;
  const panel = owner.matches(PANEL) ? owner : hoverPanelOf(owner);
  if (!panel) return;
  const next = event.relatedTarget;
  const stays = panel.dataset.runeHoverPanel === 'card' && next instanceof Node
    && (panel.contains(next) || hoverTriggerOf(panel)?.contains(next));
  if (!stays) hidePanel(panel);
}
