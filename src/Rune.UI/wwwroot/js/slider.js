// 滑块（data-rune-slider，同 vue-tsx-pro 的 Slider）：拖动、点轨道、键盘都在这里算（夹在范围内、对齐步长、范围时不交叉），
// 当场画出来（位置变量、aria-valuenow 与取值范围、刻度的 data-active、隐藏 input），再发出冒泡的 rune-slide 交给 C#
// （index、value、commit：松开或按键时为 true；seq：逐次加一的序号）。静态 SSR 下没有 C#，这里画的就是最终状态，表单照样提交。
//
// C# 处理完把序号写回外层的 data-ack、它认定的值写在 data-values（JS 从不改这个属性）。Server 模式下拖动中 C# 的渲染会晚到，
// 把旧值写回滑块的属性：C# 还没追上（正在拖，或 data-ack 小于发出的序号）时立刻改回本地的值（MutationObserver 在绘制前回调，看不到闪动）；
// 追上之后按 data-values 画（父级没采纳改动时，Blazor 的旧树里值没变、不会重写被这里改过的属性，要靠这一步复原）。

import { startMove } from './move.js';

/** 本地改过、C# 还没追上的值：root → { values, dragging } */
const pending = new WeakMap();
/** 每个滑块发出过的最大序号 */
const sequences = new WeakMap();
const watched = new WeakSet();

const decimals = (number) => {
  const text = String(number);
  const dot = text.indexOf('.');
  return dot < 0 ? 0 : text.length - dot - 1;
};

function configOf(root) {
  const min = Number(root.dataset.min);
  const max = Number(root.dataset.max);
  const step = Number(root.dataset.step) || 1;
  const big = root.dataset.bigStep ? Number(root.dataset.bigStep) : Math.max(step, (max - min) / 10);
  return { min, max, step, big, vertical: root.dataset.orientation === 'vertical' };
}

const thumbsOf = (root) => [...root.querySelectorAll(':scope > [role="slider"]')];

const renderedValues = (root) => (root.dataset.values ?? '').split(' ').filter(Boolean).map(Number);

const valuesOf = (root) => pending.get(root)?.values ?? renderedValues(root);

const isOff = (root) => root.hasAttribute('data-disabled') || root.hasAttribute('data-readonly');

/** 夹在范围内并对齐到 min + n × step（小数位取 step、min 里多的那个，避免 0.1 + 0.2 这类误差） */
function snap(value, { min, max, step }) {
  const stepped = min + Math.round((value - min) / step) * step;
  return Number(Math.min(Math.max(stepped, min), max).toFixed(Math.max(decimals(step), decimals(min))));
}

/** 第 index 个滑块能取的范围：范围时不能越过另一个 */
const bounds = (values, index, { min, max }) => (values.length < 2 ? [min, max] : index === 0 ? [min, values[1]] : [values[0], max]);

const percent = (value, { min, max }) =>
  max > min ? Math.round(((Math.min(Math.max(value, min), max) - min) / (max - min)) * 1e6) / 1e4 : 0;

function setAttribute(element, name, value) {
  if (element.getAttribute(name) !== value) element.setAttribute(name, value);
}

function setVariable(element, name, value) {
  if (element.style.getPropertyValue(name) !== value) element.style.setProperty(name, value);
}

/** 按这组值画：位置、各滑块的取值与范围、刻度、隐藏 input；与 C# 渲染出的一致 */
function paint(root, values) {
  const config = configOf(root);
  const range = values.length > 1;
  setVariable(root, '--rune-slider-start', `${range ? percent(values[0], config) : 0}%`);
  setVariable(root, '--rune-slider-end', `${percent(values.at(-1), config)}%`);
  thumbsOf(root).forEach((thumb, index) => {
    const [lower, upper] = bounds(values, index, config);
    const now = String(values[index]);
    // 取值文字（aria-valuetext）由 C# 按 ValueText 算：值变了就先去掉，免得念旧的
    if (thumb.getAttribute('aria-valuenow') !== now) thumb.removeAttribute('aria-valuetext');
    setAttribute(thumb, 'aria-valuenow', now);
    setAttribute(thumb, 'aria-valuemin', String(lower));
    setAttribute(thumb, 'aria-valuemax', String(upper));
  });
  const [low, high] = range ? values : [config.min, values[0]];
  root.querySelectorAll(':scope > * > [data-value]').forEach((mark) => {
    const value = Number(mark.dataset.value);
    mark.toggleAttribute('data-active', value >= low && value <= high);
  });
  root.querySelectorAll(':scope > input[type="hidden"]').forEach((input, index) => {
    const value = String(values[index]);
    if (input.value !== value) input.value = value;
  });
}

/** C# 的渲染改了属性：没追上时改回本地的值，追上了按 C# 的值画 */
function reconcile(root) {
  const local = pending.get(root);
  const acknowledged = Number(root.dataset.ack ?? 0);
  if (local && (local.dragging || acknowledged < (sequences.get(root) ?? 0))) {
    paint(root, local.values);
    return;
  }
  pending.delete(root);
  paint(root, renderedValues(root));
}

/** 开始监视一个滑块（插入时调用；重复调用无副作用）。 */
export function watchSlider(root) {
  if (watched.has(root)) return;
  watched.add(root);
  new MutationObserver(() => reconcile(root)).observe(root, {
    subtree: true,
    attributes: true,
    attributeFilter: ['data-ack', 'data-values', 'style', 'aria-valuenow', 'aria-valuemin', 'aria-valuemax', 'data-active'],
  });
}

function send(root, index, commit) {
  const seq = (sequences.get(root) ?? 0) + 1;
  sequences.set(root, seq);
  const value = String(valuesOf(root)[index]);
  root.dispatchEvent(new CustomEvent('rune-slide', { bubbles: true, detail: { index, value, commit, seq } }));
}

/** 把第 index 个滑块改成 raw（夹住、对齐）并画出来，返回是否变了；发不发给 C# 由调用方决定 */
function slide(root, index, raw, dragging) {
  const config = configOf(root);
  const values = [...valuesOf(root)];
  const [lower, upper] = bounds(values, index, config);
  const next = Math.min(Math.max(snap(raw, config), lower), upper);
  const changed = next !== values[index];
  values[index] = next;
  pending.set(root, { values, dragging });
  paint(root, values);
  return changed;
}

/** 指针位置对应的值 */
function valueAt(root, track, clientX, clientY) {
  const config = configOf(root);
  const rect = track.getBoundingClientRect();
  let ratio = config.vertical ? (rect.bottom - clientY) / rect.height : (clientX - rect.left) / rect.width;
  if (!config.vertical && getComputedStyle(root).direction === 'rtl') ratio = 1 - ratio;
  return snap(config.min + Math.min(Math.max(ratio, 0), 1) * (config.max - config.min), config);
}

/** 离 target 最近的滑块；两个重合时按方向选（往高处去的拿高的那个） */
function nearest(values, target) {
  if (values.length < 2) return 0;
  const [low, high] = values;
  if (low === high) return target > high ? 1 : 0;
  return Math.abs(target - low) <= Math.abs(target - high) ? 0 : 1;
}

/** 按在滑块上（轨道、滑块、空白处）：移动离得近的滑块到这里并接着拖，焦点交给它 */
export function grabSlider(event) {
  if (event.button !== 0 || !(event.target instanceof Element)) return;
  const root = event.target.closest('[data-rune-slider]');
  if (!root || isOff(root)) return;
  const track = root.firstElementChild;
  if (!track) return;
  // 不让浏览器把焦点放到别处、开始选中文字；焦点交给滑块，键盘接着可以微调
  event.preventDefault();
  const target = valueAt(root, track, event.clientX, event.clientY);
  const index = nearest(valuesOf(root), target);
  thumbsOf(root)[index]?.focus({ preventScroll: true });
  if (slide(root, index, target, true)) send(root, index, false);
  startMove(event, {
    target: root,
    onMove: ({ clientX, clientY }) => {
      if (slide(root, index, valueAt(root, track, clientX, clientY), true)) send(root, index, false);
    },
    onEnd: () => {
      const local = pending.get(root);
      if (local) local.dragging = false;
      send(root, index, true);
    },
  });
}

/** 滑块上的按键：方向键 ±step、Page Up / Down ±大步长、Home / End 到能取的两端；从右到左时横向的方向键反过来 */
export function keySlider(event) {
  const thumb = event.target;
  if (!(thumb instanceof Element) || thumb.getAttribute('role') !== 'slider') return;
  const root = thumb.parentElement;
  if (!root?.hasAttribute('data-rune-slider') || isOff(root) || event.altKey || event.ctrlKey || event.metaKey) return;
  const config = configOf(root);
  const index = Number(thumb.dataset.index ?? 0);
  const values = valuesOf(root);
  const current = values[index];
  const [lower, upper] = bounds(values, index, config);
  const rtl = !config.vertical && getComputedStyle(root).direction === 'rtl';
  const next = {
    [rtl ? 'ArrowLeft' : 'ArrowRight']: current + config.step,
    [rtl ? 'ArrowRight' : 'ArrowLeft']: current - config.step,
    ArrowUp: current + config.step,
    ArrowDown: current - config.step,
    PageUp: current + config.big,
    PageDown: current - config.big,
    Home: lower,
    End: upper,
  }[event.key];
  if (next === undefined) return;
  event.preventDefault();
  slide(root, index, next, false);
  send(root, index, true);
}
