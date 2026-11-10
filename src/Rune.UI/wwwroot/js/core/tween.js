// 会滚动的数字（AnimatedNumber 的 data-rune-tween）：data-value 变了时从显示中的值过渡到新值，逐帧改数字的文字节点
// （不经过 C# 渲染），停下时换回 C# 格式化好的终值。第一次出现时有 data-from 就从它滚过来。
// 中间帧用 Intl.NumberFormat 按 data-locale 与 data-format（Intl 的选项加 scale、suffix）格式化；时长取元素上的主题
// （--rune-durations-{data-rune-tween}），缓动取 --rune-easings-standard；减少动态效果时直接到位。
// 改的是 C# 渲染出的那个文字节点的 nodeValue（不换掉节点）：C# 之后再渲染新的终值时照常更新它。

import { reducedMotion } from './shared.js';

// 每个数字当前显示的值与正在跑的过渡
const states = new WeakMap();

/** 插入时：记下当前值；有 data-from 时从它滚到 data-value */
export function watchTween(element) {
  if (states.has(element)) return;
  const target = Number(element.dataset.value);
  const state = { value: target, frame: 0 };
  states.set(element, state);
  const from = element.dataset.from;
  if (from != null && from !== '') run(element, state, Number(from), target);
}

/** data-value 变了（C# 渲染了新值，文字已经是新的终值）：从显示中的值接着滚过去 */
export function onTweenValueChanged(element) {
  const state = states.get(element);
  if (!state) {
    watchTween(element);
    return;
  }
  run(element, state, state.value, Number(element.dataset.value));
}

function run(element, state, from, to) {
  cancelAnimationFrame(state.frame);
  const text = element.firstChild;
  const style = getComputedStyle(element);
  const duration = parseFloat(style.getPropertyValue(`--rune-durations-${element.dataset.runeTween || 'lg'}`)) || 0;
  if (!(text instanceof Text) || from === to || !Number.isFinite(from) || !Number.isFinite(to) || duration === 0 || reducedMotion.matches) {
    state.value = to;
    return;
  }
  const final = text.nodeValue;
  const format = formatterOf(element);
  const ease = easing(style.getPropertyValue('--rune-easings-standard'));
  // 中间帧按终值的小数位取整：整数不会滚出一串小数
  const places = 10 ** Math.min(20, (element.dataset.value.split('.')[1] ?? '').length);
  const start = performance.now();
  const step = (now) => {
    const progress = Math.min(1, (now - start) / duration);
    state.value = from + (to - from) * ease(progress);
    if (progress < 1) {
      text.nodeValue = format(Math.round(state.value * places) / places);
      state.frame = requestAnimationFrame(step);
    } else {
      state.value = to;
      text.nodeValue = final;
    }
  };
  state.frame = requestAnimationFrame(step);
}

function formatterOf(element) {
  const { scale = 1, suffix, ...options } = JSON.parse(element.dataset.format || '{}');
  const intl = new Intl.NumberFormat(element.dataset.locale || undefined, options);
  return (value) => {
    const text = intl.format(value / scale);
    return suffix ? `${text} ${suffix}` : text;
  };
}

/** 主题的缓动（cubic-bezier(…)）换成进度函数；认不出时用线性。Rune.Three 的主题色过渡也用它 */
export function easing(value) {
  const match = /cubic-bezier\(([^)]+)\)/.exec(value);
  const points = match?.[1].split(',').map(Number);
  if (!points || points.length !== 4 || points.some(Number.isNaN)) return (t) => t;
  const [x1, y1, x2, y2] = points;
  const curve = (a, b, t) => 3 * a * (1 - t) ** 2 * t + 3 * b * (1 - t) * t ** 2 + t ** 3;
  return (x) => {
    // 二分找到横坐标为 x 的参数 t，再取纵坐标
    let low = 0;
    let high = 1;
    for (let i = 0; i < 20; i++) {
      const mid = (low + high) / 2;
      if (curve(x1, x2, mid) < x) low = mid;
      else high = mid;
    }
    return curve(y1, y2, (low + high) / 2);
  };
}
