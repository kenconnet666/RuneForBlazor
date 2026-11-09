// 手写签名（data-rune-signature，同 vue-tsx-pro 的 SignaturePad）：canvas 上用指针写，笔画按速度粗细（写得快变细，参考 signature_pad），
// 撤销上一笔、清空；每写完一笔把透明底的 PNG（data URL）写进表单的隐藏输入框，并发 rune-command signature 交给 C#（静态 SSR 只写输入框）。
// C# 换了值（读回的旧签名、别处清空）写在 data-value 上：与自己最后交出去的不同时清掉笔画，把它画在最底下。
// 根元素的 data-signed 标出有没有签（占位文字、工具栏按它显示），data-writable 标出能不能写。
import { startMove } from '../core/move.js';
import { sendCommand } from '../core/shared.js';

/** 速度的平滑权重：越大越稳（signature_pad 的 velocityFilterWeight） */
const VELOCITY_WEIGHT = 0.7;
/** 比这更近的点不记（CSS px）：手抖、高频事件 */
const MIN_DISTANCE = 1.5;

const pads = new WeakMap();

const mid = (a, b) => [(a.x + b.x) / 2, (a.y + b.y) / 2];

/** 一笔拆成二次贝塞尔的段：穿过相邻两点的中点，控制点是原来的点；笔宽取两端的平均 */
function segmentsOf(stroke) {
  const result = [];
  let from = [stroke[0].x, stroke[0].y];
  for (let i = 1; i < stroke.length; i++) {
    const point = stroke[i];
    const last = i === stroke.length - 1;
    const to = last ? [point.x, point.y] : mid(point, stroke[i + 1]);
    result.push({ from, control: last ? to : [point.x, point.y], to, width: (stroke[i - 1].width + point.width) / 2 });
    from = to;
  }
  return result;
}

/** 开始管理一块签名区（插入时调用；重复调用无副作用） */
export function watchSignature(root) {
  if (pads.has(root)) return;
  const canvas = root.querySelector('canvas');
  if (!canvas) return;
  const pad = { root, canvas, strokes: [], base: null, emitted: root.getAttribute('data-value') || null, drawing: null };
  pads.set(root, pad);

  new ResizeObserver(() => redraw(pad)).observe(canvas);
  new MutationObserver(() => receive(pad)).observe(root, { attributes: true, attributeFilter: ['data-value'] });
  canvas.addEventListener('pointerdown', (event) => begin(pad, event));
  root.addEventListener('click', (event) => {
    const action = event.target instanceof Element ? event.target.closest('[data-signature-action]') : null;
    if (!action || !root.contains(action)) return;
    if (action.getAttribute('data-signature-action') === 'undo') undo(pad);
    else clear(pad);
  });
  if (pad.emitted) loadBase(pad, pad.emitted);
}

const writable = (pad) => pad.root.hasAttribute('data-writable');
const empty = (pad) => pad.strokes.length === 0 && pad.base === null;
const number = (pad, name, fallback) => Number(pad.root.getAttribute(name) ?? fallback) || fallback;

function context(pad) {
  const context = pad.canvas.getContext('2d');
  if (!context) return null;
  context.lineCap = 'round';
  context.lineJoin = 'round';
  context.strokeStyle = context.fillStyle = pad.root.getAttribute('data-pen-color') || getComputedStyle(pad.canvas).color;
  return context;
}

function drawSegments(context, segments) {
  for (const { from, control, to, width } of segments) {
    context.lineWidth = width;
    context.beginPath();
    context.moveTo(from[0], from[1]);
    context.quadraticCurveTo(control[0], control[1], to[0], to[1]);
    context.stroke();
  }
}

function drawDot(context, point) {
  context.beginPath();
  context.arc(point.x, point.y, point.width / 2, 0, Math.PI * 2);
  context.fill();
}

/** 整张重画：换了大小、撤销、读回旧签名时 */
function redraw(pad) {
  const ratio = window.devicePixelRatio || 1;
  const { width, height } = pad.canvas.getBoundingClientRect();
  pad.canvas.width = Math.round(width * ratio);
  pad.canvas.height = Math.round(height * ratio);
  const context2d = context(pad);
  if (!context2d) return;
  context2d.setTransform(ratio, 0, 0, ratio, 0, 0);
  if (pad.base) context2d.drawImage(pad.base, 0, 0, width, height);
  for (const stroke of pad.strokes) {
    if (stroke.length === 1) drawDot(context2d, stroke[0]);
    else drawSegments(context2d, segmentsOf(stroke));
  }
  pad.root.toggleAttribute('data-signed', !empty(pad));
}

/** 写完一笔、撤销、清空：交出新的值 */
function commit(pad) {
  pad.emitted = empty(pad) ? null : exportPng(pad);
  const input = pad.root.querySelector('input[type="hidden"]');
  if (input) input.value = pad.emitted ?? '';
  sendCommand(pad.root, 'signature', pad.emitted ?? '');
}

/**
 * 按 CSS 尺寸（1 倍）导出 PNG：高分屏上画布是 2、3 倍大，原样导出的图大好几倍；
 * 值经 Blazor Server 的连接交给 C#，单条消息默认上限 32 KB，签名按 1 倍导出一般在十几 KB
 */
function exportPng(pad) {
  const { width, height } = pad.canvas.getBoundingClientRect();
  const copy = document.createElement('canvas');
  copy.width = Math.round(width);
  copy.height = Math.round(height);
  copy.getContext('2d')?.drawImage(pad.canvas, 0, 0, copy.width, copy.height);
  return copy.toDataURL('image/png');
}

function undo(pad) {
  if (pad.strokes.length > 0) pad.strokes = pad.strokes.slice(0, -1);
  else pad.base = null;
  redraw(pad);
  commit(pad);
}

function clear(pad) {
  pad.strokes = [];
  pad.base = null;
  redraw(pad);
  commit(pad);
}

/** C# 换了值：与自己交出去的不同时清掉笔画，旧签名画在底下 */
function receive(pad) {
  const next = pad.root.getAttribute('data-value') || null;
  if (next === pad.emitted) return;
  pad.emitted = next;
  pad.strokes = [];
  pad.base = null;
  if (next) loadBase(pad, next);
  else redraw(pad);
}

function loadBase(pad, source) {
  const image = new Image();
  image.onload = () => {
    if (pad.emitted !== source) return;
    pad.base = image;
    redraw(pad);
  };
  image.src = source;
}

/** 指针在画布上的位置，和按速度算出的笔宽 */
function pointOf(pad, clientX, clientY, time) {
  const rect = pad.canvas.getBoundingClientRect();
  const x = clientX - rect.left;
  const y = clientY - rect.top;
  const min = number(pad, 'data-min-width', 0.75);
  const max = number(pad, 'data-max-width', 2.75);
  const drawing = pad.drawing;
  const previous = drawing?.points.at(-1);
  if (!drawing || !previous) return { x, y, width: (min + max) / 2 };
  const elapsed = Math.max(time - drawing.time, 1);
  const speed = Math.hypot(x - previous.x, y - previous.y) / elapsed;
  drawing.velocity = VELOCITY_WEIGHT * speed + (1 - VELOCITY_WEIGHT) * drawing.velocity;
  drawing.time = time;
  return { x, y, width: Math.max(max / (drawing.velocity + 1), min) };
}

/** 按下开始一笔；之后的移动、抬笔（取消也一样收下已写的部分）由 startMove 跟踪 */
function begin(pad, event) {
  if (!writable(pad) || event.button !== 0) return;
  event.preventDefault();
  pad.drawing = { points: [], velocity: 0, time: performance.now() };
  pad.drawing.points.push(pointOf(pad, event.clientX, event.clientY, pad.drawing.time));
  const context2d = context(pad);
  if (context2d) drawDot(context2d, pad.drawing.points[0]);
  startMove(event, {
    target: pad.canvas,
    onMove: ({ clientX, clientY }) => extend(pad, clientX, clientY),
    onEnd: () => finish(pad),
  });
}

/** 接上新的点，只画新多出来的一段：从上上个点与上个点的中点，经过上个点，到上个点与这个点的中点（最后半段等抬笔时补上） */
function extend(pad, clientX, clientY) {
  const drawing = pad.drawing;
  if (!drawing) return;
  const previous = drawing.points.at(-1);
  const point = pointOf(pad, clientX, clientY, performance.now());
  if (Math.hypot(point.x - previous.x, point.y - previous.y) < MIN_DISTANCE) return;
  const points = drawing.points;
  points.push(point);
  const context2d = context(pad);
  if (!context2d || points.length < 3) return;
  const [a, b, c] = points.slice(-3);
  drawSegments(context2d, [{ from: points.length === 3 ? [a.x, a.y] : mid(a, b), control: [b.x, b.y], to: mid(b, c), width: (a.width + b.width) / 2 }]);
}

function finish(pad) {
  const drawing = pad.drawing;
  pad.drawing = null;
  if (!drawing) return;
  pad.strokes = [...pad.strokes, drawing.points];
  // 收尾：补上最后半段（并把整笔按最终的形状重画一次）
  redraw(pad);
  commit(pad);
}
