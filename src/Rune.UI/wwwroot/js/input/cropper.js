// 裁剪图片（data-rune-cropper，同 vue-tsx-pro 的 ImageCropper）：裁剪框固定在区域中间（data-aspect 的宽高比，四周留 data-padding），
// 图片在底下拖动、缩放，缩放下限正好盖满裁剪框（上限是它的 data-max-zoom 倍），框里永远是图：
//   - 拖动平移、滚轮与触控板以指针为中心缩放、双击放大；区域聚焦时 + − 缩放、0 还原、方向键微调
//   - 工具条按钮（data-crop-action="in|out|reset"）当场做；缩放比例（盖满时是 1）发 rune-command crop-zoom，C# 写回 data-zoom 时缩放过去
//   - 导出：C# 换了 data-crop-request（"序号;宽;高;类型;质量;文件名"）时把裁好的图写进隐藏的文件输入框（data-crop-output）并触发 change，
//     C# 照常从 InputFile 拿到文件（不经 Blazor Server 的消息，图大也没关系）
// 换了图、区域大小、宽高比时回到初始：盖满并居中。
import { startMove } from '../core/move.js';
import { sendCommand } from '../core/shared.js';

const STEP = 1.1;
const croppers = new WeakMap();

/** 开始管理一个裁剪区（插入时调用；重复调用无副作用） */
export function watchCropper(root) {
  if (croppers.has(root)) return;
  const viewport = root.querySelector('[data-crop-viewport]');
  const image = viewport?.querySelector('img');
  const frame = viewport?.querySelector('[data-crop-frame]');
  if (!viewport || !image || !frame) return;
  const state = { root, viewport, image, frame, natural: null, size: { width: 0, height: 0 }, x: 0, y: 0, scale: 1, reported: 1, request: root.getAttribute('data-crop-request') };
  croppers.set(root, state);

  const load = () => {
    if (!image.naturalWidth) return;
    state.natural = { width: image.naturalWidth, height: image.naturalHeight };
    reset(state);
  };
  image.addEventListener('load', load);
  if (image.complete) load();
  new ResizeObserver(() => {
    state.size = { width: viewport.clientWidth, height: viewport.clientHeight };
    reset(state);
  }).observe(viewport);
  new MutationObserver((records) => {
    for (const record of records) {
      if (record.attributeName === 'data-aspect' || record.attributeName === 'data-padding') reset(state);
      else if (record.attributeName === 'data-zoom') zoomTo(state, Number(root.getAttribute('data-zoom')) || 1);
      else if (record.attributeName === 'data-crop-request') exportRequested(state);
    }
  }).observe(root, { attributes: true, attributeFilter: ['data-aspect', 'data-padding', 'data-zoom', 'data-crop-request'] });

  viewport.addEventListener('pointerdown', (event) => grab(state, event));
  viewport.addEventListener('wheel', (event) => {
    if (!state.natural) return;
    event.preventDefault();
    const rect = viewport.getBoundingClientRect();
    zoomAt(state, state.scale * (event.deltaY < 0 ? STEP : 1 / STEP), event.clientX - rect.left, event.clientY - rect.top);
  }, { passive: false });
  viewport.addEventListener('dblclick', (event) => {
    const rect = viewport.getBoundingClientRect();
    zoomAt(state, state.scale * STEP * STEP, event.clientX - rect.left, event.clientY - rect.top);
  });
  viewport.addEventListener('keydown', (event) => key(state, event));
  root.addEventListener('click', (event) => {
    const action = event.target instanceof Element ? event.target.closest('[data-crop-action]') : null;
    if (!action || !root.contains(action)) return;
    const name = action.getAttribute('data-crop-action');
    if (name === 'reset') reset(state);
    else zoomAround(state, name === 'in' ? STEP : 1 / STEP);
  });
}

const number = (state, name, fallback) => Number(state.root.getAttribute(name) ?? fallback) || fallback;

/** 裁剪框：按宽高比放进区域中间，四周留 padding */
function frameOf(state) {
  const aspect = number(state, 'data-aspect', 1);
  const padding = number(state, 'data-padding', 24);
  const room = { width: Math.max(state.size.width - padding * 2, 1), height: Math.max(state.size.height - padding * 2, 1) };
  const width = Math.min(room.width, room.height * aspect);
  const height = width / aspect;
  return { x: (state.size.width - width) / 2, y: (state.size.height - height) / 2, width, height };
}

/** 正好盖满裁剪框的缩放 */
function coverOf(state) {
  if (!state.natural) return 1;
  const box = frameOf(state);
  return Math.max(box.width / state.natural.width, box.height / state.natural.height);
}

/** 缩放在上下限之间，位置让图盖住裁剪框 */
function clampView(state) {
  const cover = coverOf(state);
  state.scale = Math.min(Math.max(state.scale, cover), cover * number(state, 'data-max-zoom', 4));
  if (!state.natural) return;
  const box = frameOf(state);
  const width = state.natural.width * state.scale;
  const height = state.natural.height * state.scale;
  state.x = Math.min(box.x, Math.max(state.x, box.x + box.width - width));
  state.y = Math.min(box.y, Math.max(state.y, box.y + box.height - height));
}

function apply(state) {
  clampView(state);
  const box = frameOf(state);
  state.image.style.transform = `translate(${state.x}px, ${state.y}px) scale(${state.scale})`;
  Object.assign(state.frame.style, { left: `${box.x}px`, top: `${box.y}px`, width: `${box.width}px`, height: `${box.height}px` });
  // 缩放比例交给 C#（滑块、按钮的可用性）：只在变了时发
  const zoom = Math.round((state.scale / coverOf(state)) * 100) / 100;
  if (zoom !== state.reported) {
    state.reported = zoom;
    sendCommand(state.root, 'crop-zoom', String(zoom));
  }
}

/** 回到初始：盖满裁剪框、居中 */
function reset(state) {
  if (!state.natural) return;
  const box = frameOf(state);
  state.scale = coverOf(state);
  state.x = box.x + (box.width - state.natural.width * state.scale) / 2;
  state.y = box.y + (box.height - state.natural.height * state.scale) / 2;
  apply(state);
}

/** 以区域里的 (cx, cy) 为中心缩放到 scale */
function zoomAt(state, scale, cx, cy) {
  if (!state.natural) return;
  const before = state.scale;
  const limited = Math.min(Math.max(scale, coverOf(state)), coverOf(state) * number(state, 'data-max-zoom', 4));
  state.x = cx - ((cx - state.x) * limited) / before;
  state.y = cy - ((cy - state.y) * limited) / before;
  state.scale = limited;
  apply(state);
}

/** 以裁剪框中心缩放 factor 倍 */
function zoomAround(state, factor) {
  const box = frameOf(state);
  zoomAt(state, state.scale * factor, box.x + box.width / 2, box.y + box.height / 2);
}

/** C# 给的缩放比例（盖满时是 1） */
function zoomTo(state, zoom) {
  if (Math.abs(zoom - state.scale / coverOf(state)) < 0.005) return;
  zoomAround(state, (coverOf(state) * zoom) / state.scale);
}

function grab(state, event) {
  if (event.button !== 0 || !state.natural) return;
  event.preventDefault();
  state.viewport.focus({ preventScroll: true });
  const from = { x: state.x, y: state.y };
  state.viewport.toggleAttribute('data-dragging', true);
  startMove(event, {
    target: state.viewport,
    onMove: ({ deltaX, deltaY }) => {
      state.x = from.x + deltaX;
      state.y = from.y + deltaY;
      apply(state);
    },
    onEnd: () => state.viewport.removeAttribute('data-dragging'),
  });
}

function key(state, event) {
  const moves = { ArrowLeft: [10, 0], ArrowRight: [-10, 0], ArrowUp: [0, 10], ArrowDown: [0, -10] };
  if (event.key === '+' || event.key === '=') zoomAround(state, STEP);
  else if (event.key === '-') zoomAround(state, 1 / STEP);
  else if (event.key === '0') reset(state);
  else if (moves[event.key]) {
    state.x += moves[event.key][0];
    state.y += moves[event.key][1];
    apply(state);
  } else return;
  event.preventDefault();
}

/** 裁好的画布：只写宽或高时另一边按宽高比，都不写时按图片原本的像素；圆形时裁成椭圆、四角透明 */
function toCanvas(state, width, height) {
  if (!state.natural) return null;
  const box = frameOf(state);
  const aspect = number(state, 'data-aspect', 1);
  const source = { x: (box.x - state.x) / state.scale, y: (box.y - state.y) / state.scale, width: box.width / state.scale, height: box.height / state.scale };
  const outputWidth = Math.round(width || (height ? height * aspect : source.width));
  const outputHeight = Math.round(height || outputWidth / aspect);
  const canvas = document.createElement('canvas');
  canvas.width = outputWidth;
  canvas.height = outputHeight;
  const context = canvas.getContext('2d');
  if (!context) return null;
  context.imageSmoothingQuality = 'high';
  if (state.root.getAttribute('data-shape') === 'round') {
    context.beginPath();
    context.ellipse(outputWidth / 2, outputHeight / 2, outputWidth / 2, outputHeight / 2, 0, 0, Math.PI * 2);
    context.clip();
  }
  context.drawImage(state.image, source.x, source.y, source.width, source.height, 0, 0, outputWidth, outputHeight);
  return canvas;
}

/** C# 要一张裁好的图：写进隐藏的文件输入框，触发 change */
function exportRequested(state) {
  const request = state.root.getAttribute('data-crop-request');
  if (!request || request === state.request) return;
  state.request = request;
  const [, width, height, type, quality, name] = request.split(';');
  const input = state.root.querySelector('input[type="file"][data-crop-output]');
  const canvas = toCanvas(state, Number(width) || 0, Number(height) || 0);
  const deliver = (blob) => {
    const transfer = new DataTransfer();
    if (blob) transfer.items.add(new File([blob], name || 'cropped.png', { type: blob.type }));
    input.files = transfer.files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
  };
  if (!canvas || !input) {
    if (input) deliver(null);
    return;
  }
  canvas.toBlob(deliver, type || 'image/png', quality ? Number(quality) : undefined);
}
