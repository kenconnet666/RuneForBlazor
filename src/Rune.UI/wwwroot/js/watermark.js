// 水印（Watermark 的 data-rune-watermark，值是选项的 JSON）：用 canvas 画一块平铺单元，写成那一层的背景；选项变了重画。
// 一块单元是两行两列的格子，斜对角各画一份（同 antd），平铺后相邻两行错开半格、铺得匀；每份居中、旋转，四周留出 gap 的一半。
// 按设备像素比放大，高分屏不糊。

const DEFAULT_COLOR = 'rgba(128, 128, 128, 0.16)';
const drawing = new WeakMap();

export async function drawWatermark(layer) {
  const options = JSON.parse(layer.dataset.runeWatermark || '{}');
  const current = (drawing.get(layer) ?? 0) + 1;
  drawing.set(layer, current);
  const { lines = [], image, width = 120, height = 40, rotate = -22, gap = [120, 100], fontSize = 14, color = DEFAULT_COLOR } = options;
  const ratio = window.devicePixelRatio || 1;
  const canvas = document.createElement('canvas');
  const context = canvas.getContext('2d');
  if (!context) return;
  const font = `${fontSize}px sans-serif`;
  context.font = font;
  const lineHeight = fontSize * 1.4;
  const [contentWidth, contentHeight] = image
    ? [width, height]
    : [Math.max(1, ...lines.map((line) => context.measureText(line).width)), lines.length * lineHeight];
  const cellWidth = contentWidth + gap[0];
  const cellHeight = contentHeight + gap[1];
  let picture;
  if (image) {
    picture = new Image();
    picture.crossOrigin = 'anonymous';
    picture.src = image;
    await picture.decode().catch(() => undefined);
    // 等图片时选项又变了：交给后一次
    if (drawing.get(layer) !== current) return;
  }
  canvas.width = cellWidth * 2 * ratio;
  canvas.height = cellHeight * 2 * ratio;
  context.scale(ratio, ratio);
  // canvas 的尺寸改过之后要重新设字体
  context.font = font;
  context.fillStyle = color;
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  for (const [x, y] of [[cellWidth / 2, cellHeight / 2], [cellWidth * 1.5, cellHeight * 1.5]]) {
    context.save();
    context.translate(x, y);
    context.rotate((rotate * Math.PI) / 180);
    if (picture?.naturalWidth) context.drawImage(picture, -width / 2, -height / 2, width, height);
    else if (!image) lines.forEach((line, index) => context.fillText(line, 0, (index - (lines.length - 1) / 2) * lineHeight));
    context.restore();
  }
  layer.style.backgroundImage = `url(${canvas.toDataURL()})`;
  layer.style.backgroundSize = `${cellWidth * 2}px ${cellHeight * 2}px`;
}
