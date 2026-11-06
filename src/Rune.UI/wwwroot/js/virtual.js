// 虚拟列表（data-rune-virtual，同 vue-tsx-pro 的 utils/virtual/virtualizer.ts，算法参考 @tanstack/virtual-core）：
// 先按估计大小排出位置表，渲染后测量真实大小并按 key 缓存；可见范围用二分查找；可视区上方的项大小变化时同步调整滚动位置，可见内容不跳。
// 分工：C# 只渲染 JS 报告的那一段（rune-command range），每项带 data-index 与 data-key；位置（transform）、列表的总大小、
// 测量都在 JS 里，不等往返。滚动到指定项由 C# 写 data-rune-virtual-scroll（index:序号:对齐:序列号 或 offset:像素:auto:序列号）。
// 表格（选项 table，DataTable）：列表是 tbody，行不能用 transform 排位置，改为上下两个占位行（data-rune-virtual-before / after）撑出滚动高度；
// 吸顶的表头、吸底的合计行挡住的部分不算可见，滚动到某一行时停在它们之间（同 vue 的 scrollPaddingStart / End）。

import { sendCommand } from './shared.js';

// 动态大小下 scrollToIndex 重新对准的最多次数
const MAX_SCROLL_ATTEMPTS = 10;

const states = new WeakMap();

/** 按大小依次排出每项的位置 */
function computeMeasurements(count, sizeOf) {
  const items = new Array(count);
  let start = 0;
  for (let index = 0; index < count; index++) {
    const size = sizeOf(index);
    items[index] = { index, start, size, end: start + size };
    start += size;
  }
  return items;
}

/** 与 [offset, offset + viewport) 相交的项（两端都包含）；没有项时为 null */
function findVisibleRange(items, offset, viewport) {
  if (!items.length) return null;
  // 二分查找第一个 end > offset 的项
  let low = 0;
  let high = items.length - 1;
  while (low < high) {
    const middle = (low + high) >> 1;
    if (items[middle].end > offset) high = middle;
    else low = middle + 1;
  }
  let endIndex = low;
  while (endIndex < items.length - 1 && items[endIndex].end < offset + viewport) endIndex++;
  return { startIndex: low, endIndex };
}

const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

class Virtualizer {
  constructor(host) {
    this.host = host;
    this.list = null;
    /** key → 测量到的大小；序号 → key（只知道渲染过的项的） */
    this.sizes = new Map();
    this.keys = new Map();
    this.measurements = null;
    this.valid = 0;
    this.sizeVersion = 0;
    this.offset = 0;
    this.viewport = 0;
    this.range = null;
    this.sent = '';
    this.pendingScroll = 0;
    this.lastScrollRequest = '';
    this.readOptions();
  }

  readOptions() {
    const { count = 0, estimate = 40, overscan = 4, horizontal = false, window: followWindow = false, table = false } =
      JSON.parse(this.host.getAttribute('data-rune-virtual') || '{}');
    const changed = count !== this.options?.count || estimate !== this.options?.estimate;
    this.options = { count, estimate, overscan, horizontal, followWindow, table };
    // 总数变了：序号与 key 的对应作废（数据换了、过滤了），测量过的大小按 key 保留
    if (changed) {
      this.keys.clear();
      this.measurements = null;
      this.valid = 0;
    }
  }

  get scrollElement() {
    return this.options.followWindow ? window : this.host;
  }

  mount() {
    const onScroll = () => {
      if (!this.host.isConnected) return this.unmount();
      this.readScroll();
    };
    const onResize = () => {
      this.readViewport();
      this.readScroll();
    };
    this.onScroll = onScroll;
    this.onResize = onResize;
    this.scrollElement.addEventListener('scroll', onScroll, { passive: true });
    if (this.options.followWindow) window.addEventListener('resize', onResize);
    else {
      this.viewportObserver = new ResizeObserver(onResize);
      this.viewportObserver.observe(this.host);
    }
    // 测量结果下一帧再用：在回调里同步改布局会报 ResizeObserver loop
    this.itemObserver = new ResizeObserver((entries) => {
      const sizes = entries.map((entry) => [entry.target, this.borderBoxSize(entry)]);
      requestAnimationFrame(() => {
        for (const [target, size] of sizes) this.resizeItem(target, size);
      });
    });
    // C# 换了渲染的那一段：新插进来的项先量，再排位置（在绘制之前）
    this.domObserver = new MutationObserver(() => this.syncRendered());
    this.domObserver.observe(this.host, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-rune-virtual', 'data-rune-virtual-scroll'] });
    this.readViewport();
    this.syncRendered();
  }

  unmount() {
    this.scrollElement.removeEventListener('scroll', this.onScroll);
    window.removeEventListener('resize', this.onResize);
    this.viewportObserver?.disconnect();
    this.itemObserver?.disconnect();
    this.domObserver?.disconnect();
    cancelAnimationFrame(this.pendingScroll);
    states.delete(this.host);
  }

  /** DOM 变了：读选项与滚动请求，量新渲染的项，排位置，重算可见范围 */
  syncRendered() {
    if (!this.host.isConnected) return this.unmount();
    this.readOptions();
    this.list = this.host.querySelector('[data-rune-virtual-list]');
    const fresh = [];
    for (const element of this.list?.children ?? []) {
      if (!element.hasAttribute('data-index')) continue;
      this.keys.set(Number(element.getAttribute('data-index')), element.getAttribute('data-key'));
      if (element.__runeMeasured) continue;
      element.__runeMeasured = true;
      this.itemObserver.observe(element);
      fresh.push(element);
    }
    // 先全部读完再更新：边读边改时每次读取都强制重新布局
    const sizes = fresh.map((element) => [element, this.options.horizontal ? element.offsetWidth : element.offsetHeight]);
    for (const [element, size] of sizes) this.storeSize(element, size);
    this.readScroll();
    this.applyScrollRequest();
  }

  keyOf(index) {
    return this.keys.get(index) ?? `#${index}`;
  }

  /** 位置表补算到第 index 项为止：只从第一个变了大小的项往后重排 */
  measureUpTo(index) {
    const { count, estimate } = this.options;
    const sizeOf = (i) => this.sizes.get(this.keyOf(i)) ?? estimate;
    if (!this.measurements || this.measurements.length !== count) {
      this.measurements = computeMeasurements(count, sizeOf);
      this.valid = count;
      return this.measurements;
    }
    const items = this.measurements;
    const end = Math.min(index, count - 1);
    let start = this.valid > 0 ? items[this.valid - 1].end : 0;
    for (let i = this.valid; i <= end; i++) {
      const size = sizeOf(i);
      items[i] = { index: i, start, size, end: start + size };
      start += size;
    }
    this.valid = Math.max(this.valid, end + 1);
    return items;
  }

  getMeasurements() {
    return this.measureUpTo(this.options.count - 1);
  }

  totalSize() {
    return this.getMeasurements().at(-1)?.end ?? 0;
  }

  storeSize(element, size) {
    // 0 是还没排版（display: none、刚离开文档），保留原来的值
    if (!size || !element.isConnected) return false;
    const index = Number(element.getAttribute('data-index'));
    const item = this.measureUpTo(index)[index];
    if (!item || item.size === size) return false;
    // 可视区上方的项变大或变小：同步调整滚动位置，可见内容不跳
    if (item.start < this.offset) this.scrollBy(size - item.size);
    this.sizes.set(this.keyOf(index), size);
    this.sizeVersion++;
    this.valid = Math.min(this.valid, index);
    return true;
  }

  resizeItem(element, size) {
    if (this.storeSize(element, size)) this.update();
  }

  borderBoxSize(entry) {
    const box = entry.borderBoxSize?.[0];
    if (!box) return this.options.horizontal ? entry.contentRect.width : entry.contentRect.height;
    return this.options.horizontal ? box.inlineSize : box.blockSize;
  }

  /** 列表开头距滚动内容开头的距离：随页面滚动时是列表在页面里的位置；表格里是表头的高度 */
  scrollMargin() {
    if (!this.list) return 0;
    const rect = this.list.getBoundingClientRect();
    if (this.options.followWindow) return this.options.horizontal ? rect.left + window.scrollX : rect.top + window.scrollY;
    if (!this.options.table) return 0;
    return rect.top - this.host.getBoundingClientRect().top + this.host.scrollTop;
  }

  /** 吸顶的表头、吸底的合计行挡住的高度：滚动到某一行时让过它们 */
  scrollPadding() {
    const table = this.options.table ? this.list?.closest('table') : null;
    return { before: table?.tHead?.offsetHeight ?? 0, after: table?.tFoot?.offsetHeight ?? 0 };
  }

  scrollBy(delta) {
    this.offset += delta;
    this.scrollElement.scrollBy(this.options.horizontal ? { left: delta } : { top: delta });
  }

  readViewport() {
    const { horizontal, followWindow } = this.options;
    if (followWindow) this.viewport = horizontal ? window.innerWidth : window.innerHeight;
    else this.viewport = horizontal ? this.host.clientWidth : this.host.clientHeight;
  }

  readScroll() {
    const { horizontal, followWindow } = this.options;
    const position = followWindow
      ? (horizontal ? window.scrollX : window.scrollY)
      : (horizontal ? this.host.scrollLeft : this.host.scrollTop);
    this.offset = position - this.scrollMargin();
    this.update();
  }

  /** 排位置、撑开总大小；可见范围或要渲染的那一段变了时告诉 C# */
  update() {
    const measurements = this.getMeasurements();
    const { horizontal, overscan, count } = this.options;
    if (this.list && this.options.table) this.placeRows(measurements);
    else if (this.list) {
      this.list.style[horizontal ? 'width' : 'height'] = `${this.totalSize()}px`;
      for (const element of this.list.children) {
        const item = measurements[Number(element.getAttribute('data-index'))];
        if (item) element.style.transform = horizontal ? `translateX(${item.start}px)` : `translateY(${item.start}px)`;
      }
    }
    this.range = findVisibleRange(measurements, this.offset, this.viewport);
    if (!this.range) return;
    const from = Math.max(0, this.range.startIndex - overscan);
    const to = Math.min(count - 1, this.range.endIndex + overscan);
    const message = `${from}:${to}:${this.range.startIndex}:${this.range.endIndex}`;
    if (message === this.sent) return;
    this.sent = message;
    sendCommand(this.host, 'range', message);
  }

  /** 表格：上方的占位行高到第一个渲染的行，下方的占位行补足剩下的总高度 */
  placeRows(measurements) {
    let first = Infinity;
    let last = -1;
    for (const element of this.list.children) {
      if (!element.hasAttribute('data-index')) continue;
      const index = Number(element.getAttribute('data-index'));
      first = Math.min(first, index);
      last = Math.max(last, index);
    }
    const before = last < 0 ? 0 : (measurements[first]?.start ?? 0);
    const after = last < 0 ? 0 : this.totalSize() - (measurements[last]?.end ?? 0);
    const size = (selector, height) => {
      const cell = this.list.querySelector(`:scope > ${selector} > td`);
      if (!cell) return;
      cell.parentElement.hidden = height <= 0;
      cell.style.height = `${height}px`;
    };
    size('[data-rune-virtual-before]', before);
    size('[data-rune-virtual-after]', after);
  }

  /** C# 写的滚动请求：每个序列号只执行一次 */
  applyScrollRequest() {
    const request = this.host.getAttribute('data-rune-virtual-scroll');
    if (!request || request === this.lastScrollRequest) return;
    this.lastScrollRequest = request;
    const [kind, value, align] = request.split(':');
    if (kind === 'offset') this.scrollToOffset(Number(value));
    else this.scrollToIndex(Number(value), align);
  }

  scrollToIndex(index, align = 'auto') {
    cancelAnimationFrame(this.pendingScroll);
    const target = () => this.offsetForIndex(index, align);
    const first = target();
    if (first == null) return;
    this.scrollToOffset(first);
    // 滚过去之后周围的项才被测量，位置可能变化：下一帧重新对准；对准了、而且这一帧没有新的测量才算稳定
    let attempts = 0;
    let version = this.sizeVersion;
    const settle = () => {
      if (++attempts > MAX_SCROLL_ATTEMPTS) return;
      const next = target();
      const measured = version !== this.sizeVersion;
      version = this.sizeVersion;
      if (next != null && Math.abs(next - this.offset) >= 1) this.scrollToOffset(next);
      else if (!measured) return;
      this.pendingScroll = requestAnimationFrame(settle);
    };
    this.pendingScroll = requestAnimationFrame(settle);
  }

  scrollToOffset(offset) {
    const position = offset + this.scrollMargin();
    this.scrollElement.scrollTo(this.options.horizontal ? { left: position } : { top: position });
  }

  /** 第 index 项按 align 对齐时的滚动位置；auto 且已经完整可见时为 null */
  offsetForIndex(index, align) {
    const item = this.getMeasurements()[clamp(index, 0, this.options.count - 1)];
    if (!item) return null;
    const { offset, viewport } = this;
    const { before, after } = this.scrollPadding();
    let target;
    if (align === 'auto') {
      if (item.start >= offset + before && item.end <= offset + viewport - after) return null;
      target = item.start < offset + before ? item.start - before : item.end - viewport + after;
    } else if (align === 'start') target = item.start - before;
    else if (align === 'end') target = item.end - viewport + after;
    else target = item.start + item.size / 2 - (viewport + before - after) / 2;
    // 开头可以负到 -before：第一行对齐开头时回到最顶上，表头下面就是它
    return clamp(target, -before, Math.max(0, this.totalSize() - viewport + after));
  }
}

/** 虚拟列表出现时开始跟踪滚动与测量 */
export function watchVirtual(host) {
  if (states.has(host)) return;
  const virtualizer = new Virtualizer(host);
  states.set(host, virtualizer);
  virtualizer.mount();
}
