// 提示（data-rune-tooltip）的悬停意图与显示、隐藏。

// 同 vue-tsx-pro 的 hover-intent：指针停留 delay 后显示，离开 100ms 后隐藏（可以移到提示上，WCAG 1.4.13）；
// 键盘聚焦（:focus-visible）立即显示，按下触发元素、失焦、Esc 时隐藏；触屏不显示。所有提示一组：同一时间只显示一个，
// 刚关掉一个（300ms 内）时下一个直接显示，在一排按钮上移动不用每次等。

const TOOLTIP_CLOSE_DELAY = 100;

const TOOLTIP_SKIP_DELAY = 300;

const tooltipTimers = new WeakMap();

export let shownTooltip = null;

let tooltipClosedAt = 0;

export function tooltipOf(trigger) {
  const id = trigger.getAttribute('data-rune-tooltip');
  return id ? document.getElementById(id) : null;
}

export function scheduleTooltip(tooltip, show, wait) {
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

export function hideTooltip(tooltip) {
  clearTimeout(tooltipTimers.get(tooltip));
  if (tooltip.matches(':popover-open')) {
    tooltip.hidePopover();
    tooltipClosedAt = Date.now();
  }
  if (shownTooltip === tooltip) shownTooltip = null;
}

// 指针进出：pointerover / pointerout 冒泡，按 relatedTarget 判断是不是真的进出了触发元素（或提示）
export function onPointerOver(event) {
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

export function onPointerOut(event) {
  if (!(event.target instanceof Element)) return;
  const owner = event.target.closest('[data-rune-tooltip], [role="tooltip"][popover]');
  if (!owner || owner.contains(event.relatedTarget)) return;
  const tooltip = owner.matches('[data-rune-tooltip]') ? tooltipOf(owner) : owner;
  if (tooltip) scheduleTooltip(tooltip, false, TOOLTIP_CLOSE_DELAY);
}

export function hideTooltipOf(element) {
  const trigger = element instanceof Element ? element.closest('[data-rune-tooltip]') : null;
  const tooltip = trigger && tooltipOf(trigger);
  if (tooltip) hideTooltip(tooltip);
}
