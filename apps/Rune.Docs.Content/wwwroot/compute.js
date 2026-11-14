// Popover 页“自己算位置”的示例（data-docs-compute）：拖动锚点、按方向键移动，用组件库的 computePlacement 算浮层的位置并画出来。
// 想要的位置与 crossAxisFallback 由 C# 写在框上（data-placement、data-fallback），变了就重算。坐标全是框里的坐标。

import { computePlacement } from '../RuneForBlazor.UI/js/overlay/floating.js';

const ANCHOR = { width: 64, height: 32 };
const PANEL = { width: 148, height: 72 };
const ARROW = 10;
/** 方向键一次挪多少 */
const STEP = 12;

const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

/** 示例出现时调用（文档站的初始化器）。 */
export function watchCompute(stage) {
  const anchor = stage.querySelector('[data-docs-compute-anchor]');
  const panel = stage.querySelector('[data-docs-compute-panel]');
  const label = stage.querySelector('[data-docs-compute-label]');
  const arrow = stage.querySelector('[data-docs-compute-arrow]');
  const result = stage.parentElement.querySelector('[data-docs-compute-result]');
  // 锚点的左上角（框里的坐标）
  let at = { x: 120, y: 60 };
  let grab = null;

  const draw = () => {
    const width = stage.clientWidth;
    const height = stage.clientHeight;
    at = { x: clamp(at.x, 0, Math.max(0, width - ANCHOR.width)), y: clamp(at.y, 0, Math.max(0, height - ANCHOR.height)) };
    const placement = stage.dataset.placement || 'bottom-start';
    const { x, y, side, align, arrowX, arrowY } = computePlacement({
      anchor: { left: at.x, top: at.y, ...ANCHOR },
      ...PANEL,
      viewport: { left: 0, top: 0, width, height },
      placement,
      gap: 6 + ARROW * 0.7,
      rtl: false,
      padding: 8,
      crossAxisFallback: stage.dataset.fallback === 'true',
      arrow: { size: ARROW, padding: ARROW },
    });
    Object.assign(anchor.style, { left: `${at.x}px`, top: `${at.y}px` });
    Object.assign(panel.style, { left: `${x}px`, top: `${y}px` });
    // 箭头在浮层朝着锚点的那条边上
    Object.assign(arrow.style, {
      left: arrowX != null ? `${arrowX}px` : side === 'right' ? `${-ARROW / 2}px` : '',
      right: side === 'left' ? `${-ARROW / 2}px` : '',
      top: arrowY != null ? `${arrowY}px` : side === 'bottom' ? `${-ARROW / 2}px` : '',
      bottom: side === 'top' ? `${-ARROW / 2}px` : '',
    });
    label.textContent = `${side}-${align}`;
    const flipped = side !== placement.split('-')[0];
    result.textContent = `结果：x ${Math.round(x)}、y ${Math.round(y)}，落在 ${side} 侧${flipped ? '（原来的一侧放不下，换了）' : ''}`;
  };

  anchor.addEventListener('pointerdown', (event) => {
    anchor.setPointerCapture(event.pointerId);
    grab = { x: event.clientX - at.x, y: event.clientY - at.y };
  });
  anchor.addEventListener('pointermove', (event) => {
    if (!grab) return;
    at = { x: event.clientX - grab.x, y: event.clientY - grab.y };
    draw();
  });
  const release = () => (grab = null);
  anchor.addEventListener('pointerup', release);
  anchor.addEventListener('pointercancel', release);
  anchor.addEventListener('keydown', (event) => {
    const delta = { ArrowLeft: [-STEP, 0], ArrowRight: [STEP, 0], ArrowUp: [0, -STEP], ArrowDown: [0, STEP] }[event.key];
    if (!delta) return;
    event.preventDefault();
    at = { x: at.x + delta[0], y: at.y + delta[1] };
    draw();
  });
  new ResizeObserver(draw).observe(stage);
  new MutationObserver(draw).observe(stage, { attributes: true, attributeFilter: ['data-placement', 'data-fallback'] });
  draw();
}
