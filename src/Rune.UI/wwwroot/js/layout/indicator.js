// 跟着当前项走的指示条（同 vue-tsx-pro 的 useActiveIndicator）：ToggleGroup 的选中滑块、Tabs 的下划线。
// 指示条元素写 data-rune-indicator="当前项的选择器"（在它的父元素里找），data-axis="horizontal|vertical|both"（默认 both）。
// 量出当前项相对父元素的位置写到指示条上（transform 与宽高）：选中变了时滑过去（过渡写在指示条的样式里，取主题时长，
// 减少动态效果时时长为 0），第一次与尺寸变化（字体加载、容器变宽）时直接对齐。父元素要是指示条的定位容器。

const watched = new WeakSet();
const resizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const { target } of entries) {
    for (const indicator of target.querySelectorAll(':scope > [data-rune-indicator]')) place(indicator, false);
  }
});

/** 开始跟随（插入时调用；重复调用无副作用）。 */
export function watchIndicator(indicator) {
  const container = indicator.parentElement;
  if (!container || watched.has(indicator)) return;
  watched.add(indicator);
  resizeObserver?.observe(container);
  // 选中状态、项的增删变了：滑到新的当前项
  new MutationObserver(() => place(indicator, true)).observe(container, {
    childList: true,
    subtree: true,
    attributes: true,
    attributeFilter: ['aria-pressed', 'aria-selected', 'aria-current', 'data-state'],
  });
  requestAnimationFrame(() => place(indicator, false));
}

function place(indicator, animated) {
  if (!indicator.isConnected) return;
  const item = indicator.parentElement.querySelector(indicator.getAttribute('data-rune-indicator'));
  if (!item) return;
  const axis = indicator.getAttribute('data-axis') ?? 'both';
  const x = axis === 'vertical' ? 0 : item.offsetLeft;
  const y = axis === 'horizontal' ? 0 : item.offsetTop;
  const values = { transform: `translate(${x}px, ${y}px)` };
  if (axis !== 'vertical') values.width = `${item.offsetWidth}px`;
  if (axis !== 'horizontal') values.height = `${item.offsetHeight}px`;
  if (Object.entries(values).every(([name, value]) => indicator.style[name] === value)) return;
  if (animated) {
    Object.assign(indicator.style, values);
    return;
  }
  // 直接到位：先关掉过渡，写完、读一次布局让它生效，再恢复
  indicator.style.transition = 'none';
  Object.assign(indicator.style, values);
  void indicator.offsetWidth;
  indicator.style.transition = '';
}
