// 新手引导（data-rune-tour 的模态 dialog，同 vue-tsx-pro 的 Tour）：打开、换步骤时找到这一步的目标（data-rune-tour-target 是选择器），
// 滚到视口中间，镂空（data-rune-tour-spotlight）盖在目标上、卡片（data-rune-tour-panel）贴着目标，滚动、缩放、目标大小变化时跟着动；
// 找不到目标时整页遮罩、卡片居中（data-dimmed、data-centered）。→ / ← 经命令交给 C# 换步骤（焦点在输入框里时不处理）。
// 打开、关闭、焦点圈在里面、Esc 同对话框（overlays.js）。

import { followElement, loadFloatingUi } from './floating.js';
import { TYPING } from './hotkeys.js';
import { isRtl, sendCommand } from './shared.js';

// 正在跟随目标的引导 → 停止跟随
const following = new WeakMap();

/** 引导出现时：打开、换步骤（data-step、目标、开关）时重新跟随 */
export function watchTour(dialog) {
  if (following.has(dialog)) return;
  following.set(dialog, null);
  new MutationObserver(() => follow(dialog)).observe(dialog, {
    attributes: true,
    attributeFilter: ['open', 'data-step', 'data-rune-tour-target'],
  });
  follow(dialog);
}

async function follow(dialog) {
  following.get(dialog)?.();
  following.set(dialog, null);
  const spotlight = dialog.querySelector(':scope > [data-rune-tour-spotlight]');
  const panel = dialog.querySelector(':scope > [data-rune-tour-panel]');
  if (!dialog.open || !spotlight || !panel) return;
  const selector = dialog.getAttribute('data-rune-tour-target');
  const target = selector ? document.querySelector(selector) : null;
  spotlight.toggleAttribute('data-dimmed', !target);
  panel.toggleAttribute('data-centered', !target);
  if (!target) {
    // 上一步定位时写的位置清掉，卡片回到居中
    Object.assign(spotlight.style, { left: '', top: '', width: '', height: '' });
    Object.assign(panel.style, { left: '', top: '', transformOrigin: '' });
    panel.focus({ preventScroll: true });
    return;
  }
  target.scrollIntoView({ block: 'center', inline: 'nearest' });
  const padding = Number(dialog.getAttribute('data-padding') ?? 6);
  const card = followElement(target, panel, dialog.getAttribute('data-placement') ?? 'bottom', padding + 8);
  const { autoUpdate } = await loadFloatingUi();
  const stopSpotlight = autoUpdate(target, spotlight, () => {
    const box = target.getBoundingClientRect();
    Object.assign(spotlight.style, {
      left: `${box.left - padding}px`,
      top: `${box.top - padding}px`,
      width: `${box.width + padding * 2}px`,
      height: `${box.height + padding * 2}px`,
    });
  });
  following.set(dialog, () => {
    card.stop();
    stopSpotlight();
  });
  await card.ready;
  panel.focus({ preventScroll: true });
}

/** → / ← 下一步 / 上一步（从右到左排版时反过来）；处理了时返回 true */
export function keyTour(event) {
  if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return false;
  if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return false;
  const dialog = event.target instanceof Element ? event.target.closest('dialog[data-rune-tour]') : null;
  if (!dialog || event.target.closest(TYPING)) return false;
  const forward = (event.key === 'ArrowRight') !== isRtl(dialog);
  event.preventDefault();
  sendCommand(dialog, forward ? 'next' : 'previous');
  return true;
}
