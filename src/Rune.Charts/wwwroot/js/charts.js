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
  if (!current || !data) {
    band?.setAttribute('display', 'none');
    crosshair?.setAttribute('display', 'none');
    tooltip?.removeAttribute('data-open');
    return;
  }
  if (data.mode !== 'point') {
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
