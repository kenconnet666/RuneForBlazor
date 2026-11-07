// 图表的绘图区（[data-rune-chart]，同 vue-tsx-pro 的 usePlotArea、XYChart 的悬停）：图形由 C# 画，这里只做量尺寸与悬停。
// - 量尺寸：ResizeObserver 量出绘图区的内容尺寸，进入过视口附近（IntersectionObserver，提前 200px；打印前全部算看见）之后
//   以 rune-command size（"宽,高"，取整，变了才发）交给 C#，C# 按尺寸重排。一页很多图表时先画看得见的。
// - 悬停：按 data-chart 里的数据（各行的类目坐标、各系列圆点的坐标、每行提示框的文字）找出指针所在的行（散点图是最近的点），
//   移动十字线、圆点、浅底（C# 渲染的元素，这里只改属性），填提示框（容器里的节点是这里克隆的）。不往返。
// - 点击（data-clickable）：点在柱上按那根柱（data-index），其余按悬停的行，发 rune-command activate。
import { sendCommand } from '../../Rune.UI/js/core/shared.js';

// 绘图区离视口多近就算看见了（px）：滚到之前先画好，进来时已经是画好的
const SEEN_MARGIN = 200;

const states = new WeakMap();

const sizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target, contentRect } of entries) {
    const state = states.get(target);
    if (!state) continue;
    state.size = [Math.round(contentRect.width), Math.round(contentRect.height)];
    report(target, state);
  }
});

const seenObserver = typeof IntersectionObserver === 'undefined' ? null : new IntersectionObserver((entries) => {
  for (const entry of entries) {
    if (!entry.isIntersecting) continue;
    seenObserver.unobserve(entry.target);
    const state = states.get(entry.target);
    if (!state) continue;
    state.seen = true;
    report(entry.target, state);
  }
}, { rootMargin: `${SEEN_MARGIN}px` });

addEventListener('beforeprint', () => {
  for (const plot of document.querySelectorAll('[data-rune-chart]')) {
    const state = states.get(plot);
    if (state && !state.seen) {
      state.seen = true;
      report(plot, state);
    }
  }
});

/** 看见了、量到了大小、和上次交出去的不同：交给 C# */
function report(plot, state) {
  if (!state.seen || !state.size) return;
  const [width, height] = state.size;
  if (width === 0 || height === 0) return;
  const text = `${width},${height}`;
  if (text === state.reported) return;
  state.reported = text;
  sendCommand(plot, 'size', text);
}

const near = (element) => {
  const box = element.getBoundingClientRect();
  return box.top < innerHeight + SEEN_MARGIN && box.bottom > -SEEN_MARGIN && box.left < innerWidth + SEEN_MARGIN && box.right > -SEEN_MARGIN;
};

/** 开始管理一个绘图区（插入时调用；重复调用无副作用） */
export function watchChart(plot) {
  if (states.has(plot)) return;
  // 没有 IntersectionObserver 的环境、挂上时就在视口附近的：立即算看见（不等观察器的第一次回调）
  const state = { seen: !seenObserver || near(plot), size: null, reported: null, data: null, raw: null, hovered: null };
  states.set(plot, state);
  if (!state.seen) seenObserver.observe(plot);
  if (sizeObserver) sizeObserver.observe(plot);
  else {
    state.size = [Math.round(plot.clientWidth), Math.round(plot.clientHeight)];
    report(plot, state);
  }
  plot.addEventListener('pointermove', (event) => onPointerMove(plot, state, event));
  plot.addEventListener('pointerleave', () => hover(plot, state, null));
  plot.addEventListener('click', (event) => onClick(plot, state, event));
}

/** data-chart 里的数据：C# 重画后会变，按原文缓存 */
function dataOf(plot, state) {
  const raw = plot.dataset.chart ?? '';
  if (raw !== state.raw) {
    state.raw = raw;
    try {
      state.data = raw ? JSON.parse(raw) : null;
    } catch {
      state.data = null;
    }
  }
  return state.data;
}

/** 指针移动：找出悬停的行（散点图是最近的点） */
function onPointerMove(plot, state, event) {
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
function hover(plot, state, current) {
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

const round = (value) => Math.round(value * 1000) / 1000;

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
  if (!tip) return;
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

/** 点击下钻：点在柱上按那根柱（data-index），其余按悬停的行 */
function onClick(plot, state, event) {
  if (!plot.hasAttribute('data-clickable')) return;
  const target = event.target instanceof Element ? event.target.closest('[data-index]') : null;
  const index = target ? Number(target.getAttribute('data-index')) : state.hovered?.index;
  if (index === undefined || Number.isNaN(index)) return;
  sendCommand(plot, 'activate', String(index));
}

// ───────── 导出（同 vue-tsx-pro 的 export.ts）：菜单项带 data-rune-chart-export，图表外层是它所在的 figure ─────────

document.addEventListener('click', (event) => {
  const item = event.target instanceof Element ? event.target.closest('[data-rune-chart-export]') : null;
  const figure = item?.closest('[role="figure"]');
  if (!figure) return;
  const name = item.dataset.name || 'chart';
  if (item.dataset.runeChartExport === 'image') {
    const svg = figure.querySelector('[data-rune-chart] svg');
    if (svg) void exportImage(svg, name);
  } else {
    const table = figure.querySelector(':scope > table');
    if (table) download(`${name}.csv`, new Blob(['﻿', tableToCsv(table)], { type: 'text/csv;charset=utf-8' }));
  }
});

// 写回到复制品上的样式：画图要用到的这些（主题的 CSS 变量在算出来的样式里已经解析）
const INLINED = ['fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'stroke-dasharray', 'stroke-linecap', 'stroke-linejoin',
  'opacity', 'visibility', 'display', 'font-family', 'font-size', 'font-weight', 'text-anchor', 'dominant-baseline', 'paint-order'];

/** SVG → 带内联样式的 SVG 文字（宽高写成像素）；悬停的十字线、浅底不在图里 */
function serializeSvg(svg) {
  const clone = svg.cloneNode(true);
  const sources = [svg, ...svg.querySelectorAll('*')];
  const targets = [clone, ...clone.querySelectorAll('*')];
  sources.forEach((source, index) => {
    const target = targets[index];
    if (!target?.style) return;
    const style = getComputedStyle(source);
    for (const property of INLINED) target.style.setProperty(property, style.getPropertyValue(property));
    // 进场动画的终态：复制品不播动画
    target.style.setProperty('stroke-dashoffset', '0');
    target.style.setProperty('scale', 'none');
  });
  clone.querySelectorAll('[data-part="crosshair"], [data-part="hover-band"]').forEach((element) => element.remove());
  const { width, height } = svg.getBoundingClientRect();
  clone.setAttribute('width', String(width));
  clone.setAttribute('height', String(height));
  clone.removeAttribute('viewBox');
  clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
  return { text: new XMLSerializer().serializeToString(clone), width, height };
}

/** 往外找第一个不透明的背景色：图片的底色 */
function backgroundOf(element) {
  for (let node = element; node instanceof Element; node = node.parentElement) {
    const color = getComputedStyle(node).backgroundColor;
    if (color && color !== 'transparent' && !/rgba\(.*,\s*0\)$/.test(color)) return color;
  }
  return '#ffffff';
}

/** 绘图区的 SVG 存成 PNG（两倍分辨率），底色取外层的背景色 */
async function exportImage(svg, name) {
  const { text, width, height } = serializeSvg(svg);
  const image = new Image();
  image.src = URL.createObjectURL(new Blob([text], { type: 'image/svg+xml' }));
  try {
    await image.decode();
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(width * 2);
    canvas.height = Math.round(height * 2);
    const context = canvas.getContext('2d');
    context.scale(2, 2);
    context.fillStyle = backgroundOf(svg);
    context.fillRect(0, 0, width, height);
    context.drawImage(image, 0, 0, width, height);
    const blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/png'));
    if (blob) download(`${name}.png`, blob);
  } finally {
    URL.revokeObjectURL(image.src);
  }
}

/** 数据表 → CSV：每格按 RFC 4180 加引号，开头写 = + - @ 的格子前加 '（Excel 不当公式执行） */
function tableToCsv(table) {
  const cell = (text) => {
    const value = /^[=+\-@]/.test(text) && Number.isNaN(Number(text)) ? `'${text}` : text;
    return /[",\r\n]/.test(value) ? `"${value.replaceAll('"', '""')}"` : value;
  };
  return [...table.rows].map((row) => [...row.cells].map((c) => cell(c.textContent.trim())).join(',')).join('\r\n');
}

function download(fileName, blob) {
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = fileName;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 1000);
}
