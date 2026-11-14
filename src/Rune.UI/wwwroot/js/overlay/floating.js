// 锚定浮层的定位：Popover、Tooltip、Menu、Select 的弹出层贴着触发器，新手引导的卡片贴着目标。

// 同 vue-tsx-pro 的 utils/dom/floating：位置由纯函数 computePlacement 算（想要的一侧放不下换对侧，都放不下选空间大的一侧，
// 再沿对齐方向挪回视口；坐标对齐到设备像素；箭头对准锚点中间），autoUpdate 在滚动、缩放、锚点或浮层变了大小、锚点挪了位置时重新定位。
// 浮层都在顶层（原生 popover、模态对话框里）、position: fixed：坐标就是视口坐标，不用处理定位祖先与裁剪祖先。

import { caretRect } from '../input/caret.js';

// 正在跟随触发元素的浮层 → 停止跟随
const floatingCleanups = new WeakMap();

// 与视口边缘至少留出的距离（px）
const VIEWPORT_PADDING = 8;

// 缩放原点朝向锚点：弹出在下方时从上边缘展开
const TRANSFORM_ORIGIN = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };

// 箭头贴着浮层的哪条边（与弹出方向相反），转多少度才朝向锚点（画了右、下两条边的方块）
const ARROW_EDGE = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };

const ARROW_ROTATION = { top: 45, right: 135, bottom: 225, left: -45 };

const OPPOSITE = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };

const isVertical = (side) => side === 'top' || side === 'bottom';

// 夹在 min–max 之间；放不下（max < min）时贴着 min 那一边
const clampTo = (value, min, max) => Math.max(min, Math.min(value, max));

// 浮层在锚点这一侧、这样对齐时的左上角
function coordsOf(side, align, { anchor, width, height, gap, rtl }) {
  const right = anchor.left + anchor.width;
  const bottom = anchor.top + anchor.height;
  if (isVertical(side)) {
    // start / end 是逻辑方向：从右到左排版时 start 贴锚点的右边
    const atLeft = align === 'center' ? null : (align === 'start') !== rtl;
    const x = atLeft === null ? anchor.left + (anchor.width - width) / 2 : atLeft ? anchor.left : right - width;
    return { x, y: side === 'top' ? anchor.top - height - gap : bottom + gap };
  }
  const y = align === 'start' ? anchor.top : align === 'end' ? bottom - height : anchor.top + (anchor.height - height) / 2;
  return { x: side === 'left' ? anchor.left - width - gap : right + gap, y };
}

/**
 * 算浮层的位置（纯函数，定位的规则都在这里，不碰 DOM）：
 * 1. 按想要的一侧放；放不下换对侧（crossAxisFallback 时再试垂直的两侧）；都放不下选空间最大的一侧；
 * 2. 沿对齐的方向挪回视口里；
 * 3. 箭头对准锚点的中间，但不画到浮层的圆角外；
 * 4. 报告这一侧能用的高度（fitHeight 写成浮层的最大高度）。
 *
 * input：{ anchor, viewport（矩形 left/top/width/height）, width, height（浮层的布局尺寸）, placement, gap, rtl, padding,
 * crossAxisFallback?, arrow?: { size, padding } }。
 * 返回 { x, y, side, align, availableHeight, arrowX?（上下弹出时）, arrowY?（左右弹出时）}。
 */
export function computePlacement(input) {
  const { anchor, width, height, viewport, placement, padding, rtl, arrow } = input;
  const [wanted, align = 'center'] = placement.split('-');
  const bounds = {
    left: viewport.left + padding,
    top: viewport.top + padding,
    right: viewport.left + viewport.width - padding,
    bottom: viewport.top + viewport.height - padding,
  };
  // 这一侧还差多少放不下（≤ 0 放得下）
  const overflowOf = (side) => {
    const { x, y } = coordsOf(side, align, input);
    if (side === 'top') return bounds.top - y;
    if (side === 'bottom') return y + height - bounds.bottom;
    if (side === 'left') return bounds.left - x;
    return x + width - bounds.right;
  };
  const perpendicular = isVertical(wanted) ? (rtl ? ['left', 'right'] : ['right', 'left']) : ['bottom', 'top'];
  const candidates = [wanted, OPPOSITE[wanted], ...(input.crossAxisFallback ? perpendicular : [])]
    .map((side) => ({ side, overflow: overflowOf(side) }));
  const { side } = candidates.find((candidate) => candidate.overflow <= 0)
    ?? candidates.reduce((best, candidate) => (candidate.overflow < best.overflow ? candidate : best));

  let { x, y } = coordsOf(side, align, input);
  if (isVertical(side)) x = clampTo(x, bounds.left, bounds.right - width);
  else y = clampTo(y, bounds.top, bounds.bottom - height);

  const availableHeight = side === 'bottom' ? bounds.bottom - y : side === 'top' ? y + height - bounds.top : bounds.bottom - bounds.top;
  const result = { x, y, side, align, availableHeight: Math.max(0, availableHeight) };
  if (arrow) {
    // 对准锚点的中间；离浮层两端留出距离，尖角不画到圆角外
    const along = (center, start, length) => clampTo(center - start - arrow.size / 2, arrow.padding, length - arrow.size - arrow.padding);
    if (isVertical(side)) result.arrowX = along(anchor.left + anchor.width / 2, x, width);
    else result.arrowY = along(anchor.top + anchor.height / 2, y, height);
  }
  return result;
}

// 看得见的视口：手机上键盘弹出、双指放大时，是 visualViewport 那一块
function viewportRect() {
  const view = window.visualViewport;
  if (view) return { left: view.offsetLeft, top: view.offsetTop, width: view.width, height: view.height };
  const root = document.documentElement;
  return { left: 0, top: 0, width: root.clientWidth, height: root.clientHeight };
}

// 元素挪了位置（没有滚动、没有变大小：上面的内容变高了，把它挤下去）时调用 onMove。
// 做法同 floating-ui 的 layoutShift：交叉观察器的根正好收到元素的边上，元素一挪交叉比例就变；每次变了按新位置重建。比每帧量一次省
function observeMove(element, onMove) {
  const root = document.documentElement;
  let observer;
  let timer;
  const cleanup = () => {
    clearTimeout(timer);
    observer?.disconnect();
    observer = undefined;
  };
  const watch = (moved, threshold = 1) => {
    cleanup();
    const { left, top, width, height } = element.getBoundingClientRect();
    if (moved) onMove();
    if (!width || !height) return;
    const inset = (value) => `${-Math.floor(value)}px`;
    const rootMargin = [inset(top), inset(root.clientWidth - (left + width)), inset(root.clientHeight - (top + height)), inset(left)].join(' ');
    let first = true;
    observer = new IntersectionObserver((entries) => {
      const ratio = entries[0].intersectionRatio;
      if (ratio !== threshold) {
        if (!first) return watch(true);
        // 元素有一部分在视口外：按现在的比例重新观察；完全在外面时过一会儿再看
        if (ratio) watch(false, ratio);
        else timer = setTimeout(() => watch(false, 1e-7), 1000);
      }
      first = false;
    }, { rootMargin, threshold: clampTo(threshold, 0, 1) || 1 });
    observer.observe(element);
  };
  watch(false);
  return cleanup;
}

/**
 * 锚点、浮层的位置或大小可能变了的时候调用 update：页面或锚点的祖先滚动、窗口与可视视口变了，锚点或浮层变了大小，锚点挪了位置。
 * 先调用一次；返回停止的函数。anchor 是元素，或 { getBoundingClientRect, contextElement? } 这样的虚拟锚点。
 */
export function autoUpdate(anchor, floating, update) {
  const element = anchor instanceof Element ? anchor : (anchor.contextElement ?? null);
  // 滚动不冒泡，在 document 上捕获能收到任何元素的滚动：只管页面与锚点的祖先的（浮层里面、别处的容器滚，位置都不变）
  const onScroll = (event) => {
    const target = event.target;
    if (target === document || (target instanceof Node && element && target.contains(element))) update();
  };
  const view = window.visualViewport;
  document.addEventListener('scroll', onScroll, { capture: true, passive: true });
  window.addEventListener('resize', update);
  view?.addEventListener('resize', update);
  view?.addEventListener('scroll', update);
  const resize = new ResizeObserver(update);
  if (element) resize.observe(element);
  resize.observe(floating);
  const stopMove = anchor instanceof Element ? observeMove(anchor, update) : undefined;
  update();
  return () => {
    document.removeEventListener('scroll', onScroll, { capture: true });
    window.removeEventListener('resize', update);
    view?.removeEventListener('resize', update);
    view?.removeEventListener('scroll', update);
    resize.disconnect();
    stopMove?.();
  };
}

// 对齐到设备像素：小数的坐标会让文字、细边框发虚
const snap = (value) => {
  const ratio = window.devicePixelRatio || 1;
  return Math.round(value * ratio) / ratio;
};

/**
 * 把 position: fixed 的 element 贴着 anchor 定位并持续跟随。
 * options：placement（默认 bottom-start）、offset（与锚点的距离，默认 6）、matchWidth（至少与锚点一样宽）、
 * fitHeight（最大高度限制在这一侧剩下的空间）、arrow（箭头元素，随翻转换边）、crossAxisFallback（主方向与对侧都放不下时再试垂直的两侧）。
 * 返回 { ready（第一次定位完成）, stop（停止跟随，重复调用无副作用）}。
 */
export function followElement(anchor, element, { placement = 'bottom-start', offset = 6, matchWidth, fitHeight, arrow, crossAxisFallback } = {}) {
  // 箭头伸出浮层一半（旋转后对角线的一半约等于边长的 0.7）：距离加上这一段，尖角才不压在锚点上
  const arrowSize = arrow?.offsetWidth ?? 0;
  let stopped = false;
  let resolveReady;
  const ready = new Promise((resolve) => (resolveReady = resolve));

  const compute = (rect) => computePlacement({
    anchor: rect,
    // 布局尺寸：进出场的缩放动画不影响它
    width: element.offsetWidth,
    height: element.offsetHeight,
    viewport: viewportRect(),
    placement,
    gap: offset + arrowSize * 0.7,
    rtl: getComputedStyle(element).direction === 'rtl',
    padding: VIEWPORT_PADDING,
    crossAxisFallback,
    // 箭头离浮层的圆角留出距离，不会画到圆角外面
    arrow: arrow ? { size: arrowSize, padding: arrowSize } : undefined,
  });

  const update = () => {
    if (stopped) return;
    const rect = anchor.getBoundingClientRect();
    if (matchWidth) element.style.minWidth = `${rect.width}px`;
    let result = compute(rect);
    if (fitHeight) {
      const height = element.offsetHeight;
      element.style.maxHeight = `${result.availableHeight}px`;
      // 限高之后变矮了：放在上方时的顶边要按新的高度再算一次
      if (element.offsetHeight !== height) result = compute(rect);
    }
    const { x, y, side, align, arrowX, arrowY } = result;
    if (arrow) {
      Object.assign(arrow.style, {
        left: arrowX != null ? `${arrowX}px` : '',
        top: arrowY != null ? `${arrowY}px` : '',
        right: '',
        bottom: '',
        [ARROW_EDGE[side]]: `${-arrowSize / 2}px`,
        transform: `rotate(${ARROW_ROTATION[side]}deg)`,
      });
    }
    Object.assign(element.style, { left: `${snap(x)}px`, top: `${snap(y)}px`, transformOrigin: TRANSFORM_ORIGIN[side] });
    element.dataset.side = side;
    element.dataset.align = align;
    resolveReady();
  };
  const cleanup = autoUpdate(anchor, element, update);
  return {
    ready,
    stop: () => {
      if (stopped) return;
      stopped = true;
      cleanup();
      resolveReady();
    },
  };
}

/**
 * 浮层的锚点：data-caret-anchor 给的文本框里的光标（@ 提及），data-point-x / -y 给的视口里的点（右键菜单的指针位置），
 * data-anchor 指定的元素（Select 的输入框外壳），或触发元素（popovertarget、data-rune-tooltip、data-rune-hovercard 指向它）。
 */
function floatingAnchorOf(overlay) {
  // 位置是文本框的 data-mention-at（没有时是光标）
  const caretFor = overlay.dataset.caretAnchor && document.getElementById(overlay.dataset.caretAnchor);
  if (caretFor) {
    return {
      getBoundingClientRect: () => caretRect(caretFor, Number(caretFor.dataset.mentionAt ?? caretFor.selectionStart)),
      contextElement: caretFor,
    };
  }
  if (overlay.dataset.pointX !== undefined) {
    const x = Number(overlay.dataset.pointX);
    const y = Number(overlay.dataset.pointY);
    return { getBoundingClientRect: () => new DOMRect(x, y, 0, 0) };
  }
  const anchor = overlay.dataset.anchor && document.getElementById(overlay.dataset.anchor);
  if (anchor) return anchor;
  const id = CSS.escape(overlay.id);
  return document.querySelector(`[popovertarget="${id}"], [data-rune-tooltip="${id}"], [data-rune-hovercard="${id}"]`);
}

/** 浮层（data-rune-floating 写位置）贴着它的锚点定位并持续跟随；第一次定位完成（可见）时完成。 */
export async function anchorFloating(overlay) {
  const anchor = floatingAnchorOf(overlay);
  if (!anchor) return;
  // 定位好之前看不见、点不着（不在旧位置闪一下）。用透明而不是 visibility: hidden：后者让已经聚焦的元素失焦
  // （原生 popover 打开时就按 autofocus 聚焦了搜索框），定位期间打的字落到页面上丢了
  Object.assign(overlay.style, { opacity: '0', pointerEvents: 'none' });
  await contentRendered(overlay);
  releaseFloating(overlay);
  // 等内容期间又关上了
  if (!overlay.matches(':popover-open')) {
    Object.assign(overlay.style, { opacity: '', pointerEvents: '' });
    return;
  }
  let placement = overlay.dataset.runeFloating;
  // 子菜单往书写方向的结束边弹出：left / right 是物理方向，从右到左排版时换过来
  if (overlay.hasAttribute('data-rune-submenu') && anchor instanceof Element && getComputedStyle(anchor).direction === 'rtl') {
    placement = placement.replace(/^right/, 'left');
  }
  const following = followElement(anchor, overlay, {
    placement,
    offset: Number(overlay.dataset.offset ?? 6),
    matchWidth: overlay.hasAttribute('data-match-width'),
    fitHeight: overlay.hasAttribute('data-fit-height'),
    arrow: overlay.querySelector(':scope > [data-rune-arrow]'),
  });
  floatingCleanups.set(overlay, following.stop);
  await following.ready;
  Object.assign(overlay.style, { opacity: '', pointerEvents: '' });
}

// 第一次打开才渲染内容的浮层（Popover 的 Lazy）：打开时还是空的，C# 收到 rune-toggle 才渲染。
// 等内容到了再定位、再显示，不先按空盒子定位闪一下（最多等 1 秒，静态渲染、内容本来就空时不卡住）
function contentRendered(overlay) {
  const hasContent = () => [...overlay.childNodes].some((node) =>
    node instanceof Element ? !node.hasAttribute('data-rune-arrow') : node.textContent.trim() !== '');
  if (hasContent()) return Promise.resolve();
  return new Promise((resolve) => {
    const done = () => {
      observer.disconnect();
      clearTimeout(timer);
      resolve();
    };
    const observer = new MutationObserver(() => hasContent() && done());
    const timer = setTimeout(done, 1000);
    observer.observe(overlay, { childList: true });
  });
}

/** 停止跟随（关闭时）；重复调用无副作用。 */
export function releaseFloating(overlay) {
  floatingCleanups.get(overlay)?.();
  floatingCleanups.delete(overlay);
}
