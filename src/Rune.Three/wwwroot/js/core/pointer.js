// 指针相对画布的位置（-1 到 1，向上为正，与 WebGL 的坐标一致）与 strength（指针在画布上时趋向 1，离开后趋向 0），
// 弹簧阻尼（同 vue-tsx-pro 的 usePointer，感知时长取主题的 --rune-durations-xl）。场景每帧读 context.pointer。
// 监听整个窗口：背景效果通常在内容下面，指针落在上层的文字、按钮上时也要跟随；离开画布时位置停在原处，只让 strength 淡出。
// 减少动态效果时直接到位（效果自己决定要不要停下交互）。

import { reducedMotion } from '../../../RuneForBlazor.UI/js/core/shared.js';
import { stepSpring } from '../../../RuneForBlazor.UI/js/core/pointer.js';

let last = null;
addEventListener('pointermove', (event) => (last = { x: event.clientX, y: event.clientY }), { passive: true });
document.documentElement.addEventListener('pointerleave', () => (last = null));

/** 跟随指针的状态：每帧（渲染前）调用 step(dt)；scope() 是量位置的元素 */
export function createPointer(scope) {
  const spring = () => ({ value: 0, velocity: 0, target: 0 });
  const x = spring();
  const y = spring();
  const strength = spring();
  let period = null;

  return {
    get x() {
      return x.value;
    },
    get y() {
      return y.value;
    },
    get strength() {
      return strength.value;
    },
    /** 推进 dt 秒 */
    step(dt) {
      const element = scope();
      if (!element) return;
      const rect = element.getBoundingClientRect();
      const inside = last != null && rect.width > 0 && rect.height > 0
        && last.x >= rect.left && last.x <= rect.right && last.y >= rect.top && last.y <= rect.bottom;
      strength.target = inside ? 1 : 0;
      if (inside) {
        x.target = ((last.x - rect.left) / rect.width) * 2 - 1;
        y.target = 1 - ((last.y - rect.top) / rect.height) * 2;
      }
      period ??= (parseFloat(getComputedStyle(element).getPropertyValue('--rune-durations-xl')) || 0) / 1000;
      const instant = reducedMotion.matches || period === 0;
      for (const item of [x, y, strength]) {
        if (instant) {
          item.value = item.target;
          item.velocity = 0;
        } else if (dt > 0) {
          // 后台标签页回来时两帧相隔很久：按一帧算，不让弹簧一步跨过头
          stepSpring(item, Math.min(dt, 1 / 30), period, 0);
        }
      }
    },
  };
}
