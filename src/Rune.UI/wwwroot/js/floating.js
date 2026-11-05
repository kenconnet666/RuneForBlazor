// 锚定浮层的定位（Floating UI，用到时才加载）。

// 同 vue-tsx-pro 的 utils/dom/floating：Floating UI 计算位置（offset、flip、shift，可选 arrow 与 size），autoUpdate 在滚动、缩放、
// 锚点变化时重新定位。库第一次打开浮层时才加载（wwwroot/vendor，随 Rune.UI 分发）。第一次算好位置之前浮层不可见，不会在默认位置闪一下。

let floatingUi = null;

const loadFloatingUi = () => (floatingUi ??= import('../vendor/floating-ui.dom.mjs'));

// 正在跟随触发元素的浮层 → 停止跟随
const floatingCleanups = new WeakMap();

// 与视口边缘至少留出的距离（px）
const VIEWPORT_PADDING = 8;

// 缩放原点朝向锚点：弹出在下方时从上边缘展开
const TRANSFORM_ORIGIN = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };

// 箭头贴着浮层的哪条边（与弹出方向相反），转多少度才朝向锚点（画了右、下两条边的方块）
const ARROW_EDGE = { top: 'bottom', right: 'left', bottom: 'top', left: 'right' };

const ARROW_ROTATION = { top: 45, right: 135, bottom: 225, left: -45 };

/**
 * 浮层的锚点：data-point-x / -y 给的视口里的点（右键菜单的指针位置），data-anchor 指定的元素（Select 的输入框外壳），
 * 或触发元素（popovertarget、data-rune-tooltip、data-rune-hovercard 指向它）。
 */
function floatingAnchorOf(overlay) {
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

/** 贴着触发元素定位并持续跟随；第一次定位完成（可见）时完成。 */
export async function anchorFloating(overlay) {
  const anchor = floatingAnchorOf(overlay);
  if (!anchor) return;
  overlay.style.visibility = 'hidden';
  const [{ computePosition, autoUpdate, offset, flip, shift, arrow: arrowMiddleware, size }] = await Promise.all([loadFloatingUi(), contentRendered(overlay)]);
  // 加载期间又关上了
  if (!overlay.matches(':popover-open')) {
    overlay.style.visibility = '';
    return;
  }
  releaseFloating(overlay);
  const arrow = overlay.querySelector(':scope > [data-rune-arrow]');
  // 箭头伸出浮层一半（旋转后对角线的一半约等于边长的 0.7）：距离加上这一段，尖角才不压在锚点上
  const arrowSize = arrow?.offsetWidth ?? 0;
  const middleware = [
    offset(Number(overlay.dataset.offset ?? 6) + arrowSize * 0.7),
    flip({ padding: VIEWPORT_PADDING }),
    shift({ padding: VIEWPORT_PADDING }),
  ];
  // 箭头离浮层的圆角留出距离，不会画到圆角外面
  if (arrow) middleware.push(arrowMiddleware({ element: arrow, padding: arrowSize }));
  const matchWidth = overlay.hasAttribute('data-match-width');
  const fitHeight = overlay.hasAttribute('data-fit-height');
  if (matchWidth || fitHeight) {
    middleware.push(size({
      padding: VIEWPORT_PADDING,
      apply({ rects, availableHeight, elements }) {
        if (matchWidth) elements.floating.style.minWidth = `${rects.reference.width}px`;
        if (fitHeight) elements.floating.style.maxHeight = `${Math.max(0, availableHeight)}px`;
      },
    }));
  }
  let placement = overlay.dataset.runeFloating;
  // 子菜单往书写方向的结束边弹出：Floating UI 的 left / right 是物理方向，从右到左排版时换过来
  if (overlay.hasAttribute('data-rune-submenu') && anchor instanceof Element && getComputedStyle(anchor).direction === 'rtl') {
    placement = placement.replace(/^right/, 'left');
  }
  let stopped = false;
  return new Promise((resolve) => {
    const update = () => computePosition(anchor, overlay, { placement, strategy: 'fixed', middleware }).then(({ x, y, placement: final, middlewareData }) => {
      if (stopped) return;
      const [side, align = 'center'] = final.split('-');
      if (arrow && middlewareData.arrow) {
        const { x: arrowX, y: arrowY } = middlewareData.arrow;
        Object.assign(arrow.style, {
          left: arrowX != null ? `${arrowX}px` : '',
          top: arrowY != null ? `${arrowY}px` : '',
          right: '',
          bottom: '',
          [ARROW_EDGE[side]]: `${-arrowSize / 2}px`,
          transform: `rotate(${ARROW_ROTATION[side]}deg)`,
        });
      }
      Object.assign(overlay.style, { left: `${x}px`, top: `${y}px`, visibility: '', transformOrigin: TRANSFORM_ORIGIN[side] });
      overlay.dataset.side = side;
      overlay.dataset.align = align;
      resolve();
    });
    const cleanup = autoUpdate(anchor, overlay, update);
    floatingCleanups.set(overlay, () => {
      stopped = true;
      cleanup();
      resolve();
    });
  });
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

