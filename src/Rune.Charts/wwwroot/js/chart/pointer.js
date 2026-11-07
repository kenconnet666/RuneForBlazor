// 绘图区的指针（同 vue-tsx-pro 的 XYChart 与各图表的悬停）：按 data-chart 里的数据画悬停，不往返。
// - band、line：找出指针所在的行，移动十字线、圆点、浅底（C# 渲染的元素，这里只改属性）；point：最近的点；
//   item：指针下的那一项（data-index）高亮，其余变淡；提示框在容器里克隆原型填字。
// - 点击：data-clickable 时发 rune-command activate（点在柱上按那根柱，其余按悬停的行）；select 时点空白处发 deselect。
import { sendCommand } from '../../../Rune.UI/js/core/shared.js';
import { dataOf, round } from './state.js';

/** 指针移动：找出悬停的行（散点图是最近的点） */
export function onPointerMove(plot, state, event) {
  const data = dataOf(plot, state);
  const svg = plot.querySelector('svg');
  if (!data || !svg) return;
  const box = svg.getBoundingClientRect();
  const x = event.clientX - box.left;
  const y = event.clientY - box.top;
  if (data.mode === 'item') {
    const target = event.target instanceof Element ? event.target.closest('[data-index]') : null;
    return hover(plot, state, target && svg.contains(target) ? { index: Number(target.getAttribute('data-index')), kind: target.getAttribute('data-kind'), x, y } : null);
  }
  const [left, top, width, height] = data.plot;
  const inside = x >= left - 4 && x <= left + width + 4 && y >= top - 4 && y <= top + height + 4;
  if (!inside) return hover(plot, state, null);
  if (data.mode === 'point') {
    let best = null;
    let distance = data.pointReach;
    for (const [px, py, index] of data.points) {
      const d = Math.hypot(px - x, py - y);
      if (d < distance) {
        distance = d;
        best = { index, x: px, y: py };
      }
    }
    return hover(plot, state, best);
  }
  const centers = data.centers;
  if (!centers.length) return hover(plot, state, null);
  const along = data.horizontal ? y : x;
  let nearest = 0;
  centers.forEach((center, index) => {
    if (Math.abs(center - along) < Math.abs(centers[nearest] - along)) nearest = index;
  });
  const center = centers[nearest];
  if (Math.abs(center - along) > data.reach) return hover(plot, state, null);
  hover(plot, state, data.horizontal ? { index: nearest, x, y: center } : { index: nearest, x: center, y });
}

/** 悬停到一行（null 时离开）：十字线、圆点、浅底、提示框 */
export function hover(plot, state, current) {
  const data = dataOf(plot, state);
  state.hovered = current;
  const band = plot.querySelector('[data-part="hover-band"]');
  const crosshair = plot.querySelector('[data-part="crosshair"]');
  const tooltip = plot.querySelector('[data-part="tooltip"]');
  // 散点图：悬停的那一行的点描粗（data-active）
  plot.querySelectorAll('[data-part="point"][data-active]').forEach((point) => point.removeAttribute('data-active'));
  if (current && data?.mode === 'point') plot.querySelectorAll(`[data-part="point"][data-index="${current.index}"]`).forEach((point) => point.setAttribute('data-active', ''));
  if (data?.mode === 'item') highlight(plot, data, current);
  if (!current || !data) {
    band?.setAttribute('display', 'none');
    crosshair?.setAttribute('display', 'none');
    tooltip?.removeAttribute('data-open');
    return;
  }
  if (data.mode === 'band' || data.mode === 'line') {
    const [left, top, width, height] = data.plot;
    const center = data.centers[current.index];
    if (band) {
      const half = data.half;
      const rect = data.horizontal ? [left, center - half, width, half * 2] : [center - half, top, half * 2, height];
      ['x', 'y', 'width', 'height'].forEach((name, i) => band.setAttribute(name, round(rect[i])));
      band.removeAttribute('display');
    }
    if (crosshair) {
      const p = Math.round(center) + 0.5;
      const line = crosshair.querySelector('line');
      line?.setAttribute('x1', p);
      line?.setAttribute('x2', p);
      const ys = data.dots?.[current.index] ?? [];
      crosshair.querySelectorAll('[data-dot]').forEach((dot, i) => {
        const y = ys[i];
        if (y == null) return dot.setAttribute('display', 'none');
        dot.removeAttribute('display');
        dot.setAttribute('cx', round(center));
        dot.setAttribute('cy', y);
      });
      crosshair.removeAttribute('display');
    }
  }
  if (tooltip && data.tips) showTip(plot, state, tooltip, data, current);
}

/**
 * 按项悬停：指针下那一项（同一 data-index 的元素）写 data-active，其余写 data-dimmed 变淡（dim 时）；
 * 带 data-hover-d 的图形（环形图的扇区）换成悬停时的路径。
 * 离开时复原
 */
function highlight(plot, data, current) {
  // 一起高亮的项：groups 里写了的（桑基图的节点连着它的带子），没写时只有自己
  const related = current ? new Set((data.groups?.[current.index] ?? [current.index]).map(String)) : null;
  for (const element of plot.querySelectorAll('svg [data-index]')) {
    const active = related !== null && related.has(element.getAttribute('data-index'));
    // data-dim-on：只在悬停这一种项时变淡（弦图的弧：悬停弧时其余的弧变淡，悬停弦时弧不变）
    const dimOn = element.getAttribute('data-dim-on');
    if (data.dim && current && !active && (!dimOn || dimOn === current.kind)) element.setAttribute('data-dimmed', '');
    else element.removeAttribute('data-dimmed');
    if (active) element.setAttribute('data-active', '');
    else element.removeAttribute('data-active');
    const hoverD = element.getAttribute('data-hover-d');
    if (!hoverD) continue;
    if (active && !element.hasAttribute('data-base-d')) {
      element.setAttribute('data-base-d', element.getAttribute('d'));
      element.setAttribute('d', hoverD);
    } else if (!active && element.hasAttribute('data-base-d')) {
      element.setAttribute('d', element.getAttribute('data-base-d'));
      element.removeAttribute('data-base-d');
    }
  }
}

/**
 * 提示框：放在指针右侧，过了绘图区的一半放到左侧；纵向以指针为中线。内容按这一行的数据克隆原型（[data-tip-proto]）填字；
 * 同一行时不重建。消失时保留上一次的内容淡出
 */
function showTip(plot, state, tooltip, data, current) {
  const tip = data.tips[current.index];
  // 这一项没有提示（地图上没有数据的区域）
  if (!tip) return tooltip.removeAttribute('data-open');
  if (state.tipRow !== current.index || state.tipData !== data || !tooltip.hasChildNodes()) {
    state.tipRow = current.index;
    state.tipData = data;
    const proto = plot.querySelector('[data-tip-proto]');
    const [title, items] = tip;
    const nodes = [];
    if (title) {
      const heading = proto.querySelector('[data-tip-title]').cloneNode(false);
      heading.removeAttribute('data-tip-title');
      heading.textContent = title;
      nodes.push(heading);
    }
    for (const [label, color, value] of items) {
      const row = proto.querySelector('[data-tip-row]').cloneNode(true);
      row.removeAttribute('data-tip-row');
      const [swatch, name, text] = row.children;
      if (color) swatch.style.setProperty('--rune-chart-color', color);
      else swatch.style.visibility = 'hidden';
      name.textContent = label;
      text.textContent = value;
      nodes.push(row);
    }
    tooltip.replaceChildren(...nodes);
  }
  const flip = current.x > data.width / 2;
  const left = flip ? current.x - 14 : current.x + 14;
  tooltip.style.transform = `translate(${Math.round(left)}px, ${Math.round(current.y)}px) translate(${flip ? '-100%' : '0'}, -50%)`;
  tooltip.setAttribute('data-open', '');
  if (!tooltip.hasAttribute('data-placed')) requestAnimationFrame(() => tooltip.setAttribute('data-placed', ''));
}

/**
 * 点击下钻：点在柱上按那根柱（data-index），其余按悬停的行。悬停数据写了 select 时（网络图）点项总是交 activate（选中），
 * 点空白处交 deselect；拖动画布之后的那次点击不算
 */
export function onClick(plot, state, event) {
  if (state.dragged) {
    state.dragged = false;
    return;
  }
  const select = dataOf(plot, state)?.select;
  if (!plot.hasAttribute('data-clickable') && !select) return;
  const target = event.target instanceof Element ? event.target.closest('[data-index]') : null;
  const index = target ? Number(target.getAttribute('data-index')) : select ? undefined : state.hovered?.index;
  if (index === undefined || Number.isNaN(index)) {
    if (select) sendCommand(plot, 'deselect');
    return;
  }
  sendCommand(plot, 'activate', String(index));
}
