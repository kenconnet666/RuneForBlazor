// 速拨（FloatButtonGroup 的 Icon，data-rune-speed-dial="open|closed"）：展开时 Esc 收起、焦点回到主按钮；点到组以外的地方收起。
// 展开状态归 C#：这里只经 rune-command（close）告诉它。

import { sendCommand } from '../core/shared.js';

/** 展开的速拨里按 Esc：收起，焦点回到主按钮 */
export function keySpeedDial(event) {
  if (event.key !== 'Escape' || !(event.target instanceof Element)) return;
  const dial = event.target.closest('[data-rune-speed-dial="open"]');
  if (!dial) return;
  event.preventDefault();
  sendCommand(dial, 'close');
  dial.querySelector('[data-rune-speed-dial-trigger]')?.focus();
}

/** 按在展开的速拨以外的地方：收起 */
export function closeSpeedDialsOutside(event) {
  for (const dial of document.querySelectorAll('[data-rune-speed-dial="open"]')) {
    if (!dial.contains(event.target)) sendCommand(dial, 'close');
  }
}
