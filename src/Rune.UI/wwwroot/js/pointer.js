// 跟随指针的装饰效果（同 vue-tsx-pro 的 usePointer）：带 data-rune-pointer 的元素上写三个变量，CSS 拿它们算倾斜、光的位置。
//   --rune-pointer-x         横向位置：左边 -1，右边 1
//   --rune-pointer-y         纵向位置：底边 -1，顶边 1（向上为正）
//   --rune-pointer-strength  指针在元素上时趋向 1，离开后趋向 0
// 三个值都经过弹簧阻尼：data-rune-pointer 的值是弹簧的感知时长档位（sm / md / lg……，取主题的 --rune-durations-*），
// data-rune-pointer-bounce 是回弹程度（0 不回弹）。监听整个窗口：指针落在卡片上层的按钮、文字上也跟着走；
// 离开元素时位置停在原处，只让 strength 淡出，效果不会一下子跳回中心。减少动态效果时直接到位。

import { reducedMotion } from './shared.js';

const SELECTOR = '[data-rune-pointer]';
/** 每个元素的弹簧状态：当前值、速度、目标 */
const springs = new WeakMap();
let pointer = null;
let frame = 0;
let lastTime = 0;

/** pointermove：记下指针位置，下一帧更新 */
export function trackPointer(event) {
  pointer = { x: event.clientX, y: event.clientY };
  schedule();
}

/** 指针离开窗口：所有效果淡出 */
export function releasePointer() {
  pointer = null;
  schedule();
}

function schedule() {
  if (frame) return;
  lastTime = performance.now();
  frame = requestAnimationFrame(step);
}

function step(now) {
  frame = 0;
  // 后台标签页回来时两帧相隔很久：按一帧算，不让弹簧一步跨过头
  const dt = Math.min((now - lastTime) / 1000, 1 / 30);
  lastTime = now;
  let moving = false;
  for (const element of document.querySelectorAll(SELECTOR)) {
    if (advance(element, dt)) moving = true;
  }
  if (moving) {
    frame = requestAnimationFrame(step);
  }
}

/** 推进一个元素的弹簧并写回变量；还在动时返回 true */
function advance(element, dt) {
  const state = springs.get(element) ?? createState(element);
  aim(element, state);
  const instant = reducedMotion.matches || state.period === 0;
  let moving = false;
  for (const key of ['x', 'y', 'strength']) {
    const spring = state[key];
    if (instant) {
      spring.value = spring.target;
      spring.velocity = 0;
    } else {
      // 阻尼弹簧：固有频率按感知时长（2π / T），阻尼比 1 - bounce；分几小步积分，大步长时也稳定
      const omega = (2 * Math.PI) / state.period;
      const zeta = 1 - state.bounce;
      const steps = 4;
      const h = dt / steps;
      for (let i = 0; i < steps; i++) {
        const acceleration = -omega * omega * (spring.value - spring.target) - 2 * zeta * omega * spring.velocity;
        spring.velocity += acceleration * h;
        spring.value += spring.velocity * h;
      }
      if (Math.abs(spring.value - spring.target) < 0.001 && Math.abs(spring.velocity) < 0.001) {
        spring.value = spring.target;
        spring.velocity = 0;
      } else {
        moving = true;
      }
    }
  }
  element.style.setProperty('--rune-pointer-x', state.x.value.toFixed(4));
  element.style.setProperty('--rune-pointer-y', state.y.value.toFixed(4));
  element.style.setProperty('--rune-pointer-strength', state.strength.value.toFixed(4));
  return moving;
}

function createState(element) {
  const style = getComputedStyle(element);
  const token = element.getAttribute('data-rune-pointer') || 'xl';
  const period = (parseFloat(style.getPropertyValue(`--rune-durations-${token}`)) || 0) / 1000;
  const bounce = Math.min(Math.max(Number(element.getAttribute('data-rune-pointer-bounce')) || 0, 0), 0.99);
  const spring = () => ({ value: 0, velocity: 0, target: 0 });
  const state = { period, bounce, x: spring(), y: spring(), strength: spring() };
  springs.set(element, state);
  return state;
}

/** 按指针位置定目标：在元素里时更新位置、strength 趋向 1；在外面时位置不动、strength 趋向 0 */
function aim(element, state) {
  const rect = element.getBoundingClientRect();
  const inside = pointer != null && rect.width > 0 && rect.height > 0
    && pointer.x >= rect.left && pointer.x <= rect.right && pointer.y >= rect.top && pointer.y <= rect.bottom;
  state.strength.target = inside ? 1 : 0;
  if (!inside) return;
  state.x.target = ((pointer.x - rect.left) / rect.width) * 2 - 1;
  state.y.target = 1 - ((pointer.y - rect.top) / rect.height) * 2;
}
