// 文档站的 JS 初始化器（文件名必须是 {程序集名}.lib.module.js，Blazor 启动时自动加载）。
//
// 首屏分批接管：页面是预渲染的 HTML，WebAssembly 启动后 Blazor 把根组件第一次渲染的结果换上去（不复用已有的 DOM）。
// 整页一口气渲染完要好几秒，所以重的部分（示例的效果、API 表，见 Queued.razor）排队渲染：第一批只有外壳与各小节的外框，
// 重的部分先是一个空的占位 <div data-docs-hold="名字">。这里把预渲染时那一段节点原样放回占位里：
//   - 预渲染的 HTML 里，每段两头是注释 <!--docs-hold:名字--> … <!--/docs-hold:名字-->，页面解析完就把中间的节点记下来；
//   - Blazor 换上新的 DOM 时旧节点被摘掉（还记着，没有释放），新 DOM 里出现占位就把它们放进去。
//     这发生在同一个任务里（DOM 变动的回调先于绘制），画面不闪；
//   - 轮到这一段时 Blazor 把占位（连同里面的旧节点）换成真正渲染的内容。
// 旧节点是静态的 HTML，Rune 的 JS（浮层、主题切换）在它上面照常工作，C# 的事件要等换成真正的内容。

/** 名字 → 预渲染时那一段的节点 */
const held = new Map();
let observer = null;

/** Blazor Web App 的初始化入口 */
export function afterWebStarted() {
  start();
}

/** 独立的 Blazor Server / WebAssembly、Hybrid 的初始化入口（没有预渲染的内容，什么都不做） */
export function afterStarted() {
  start();
}

let started = false;

/**
 * C#（RenderQueue）问：这些占位里现在该渲染哪几个，按离视口的远近排（带着 #锚点打开、已经滚下去了时先换眼前的）。
 * onlyNear 时只给视口上下各一屏以内的；一个都不在附近就等到有一个滚到附近再回答。占位都不在页面上了回答空（C# 不再等）。
 */
let stopWaiting = null;

globalThis.runeDocsNearHolds = (names, onlyNear) => new Promise((resolve) => {
  // 上一次还在等滚动的作废（C# 那边有了新的占位，重新问）
  stopWaiting?.();
  const slots = names.map((name) => [name, document.querySelector(`[data-docs-hold="${CSS.escape(name)}"]`)]).filter(([, slot]) => slot);
  // 占位不占格子（display: contents）：量里面留着的第一块内容，没有内容时量外层
  const boxOf = (slot) => (slot.firstElementChild ?? slot.parentElement)?.getBoundingClientRect();
  const ranked = () => slots
    .filter(([, slot]) => slot.isConnected)
    .map(([name, slot]) => {
      const box = boxOf(slot);
      return [name, box ? Math.max(0, box.top - innerHeight, -box.bottom) : 0];
    })
    .sort((a, b) => a[1] - b[1]);
  const near = (list) => list.filter(([, distance]) => distance <= innerHeight).map(([name]) => name);

  const first = ranked();
  if (!onlyNear || first.length === 0 || near(first).length > 0) {
    resolve(onlyNear ? near(first) : first.map(([name]) => name));
    return;
  }
  // 都在远处：等滚动；占位被换页带走了也要收场（每两秒看一眼）
  let frame = 0;
  const stop = () => {
    removeEventListener('scroll', schedule, true);
    removeEventListener('resize', schedule);
    clearInterval(alive);
    if (stopWaiting === stop) stopWaiting = null;
  };
  const check = () => {
    frame = 0;
    const now = ranked();
    if (now.length > 0 && near(now).length === 0) return;
    stop();
    resolve(near(now));
  };
  const schedule = () => (frame ||= requestAnimationFrame(check));
  addEventListener('scroll', schedule, { passive: true, capture: true });
  addEventListener('resize', schedule);
  const alive = setInterval(check, 2000);
  stopWaiting = stop;
});

function start() {
  if (started) return;
  started = true;
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', collect, { once: true });
  else collect();
}

/** 记下每一段：两个注释之间的兄弟节点 */
function collect() {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_COMMENT);
  const starts = new Map();
  for (let comment = walker.nextNode(); comment; comment = walker.nextNode()) {
    const text = comment.data;
    if (text.startsWith('docs-hold:')) starts.set(text.slice('docs-hold:'.length), comment);
    else if (text.startsWith('/docs-hold:')) {
      const name = text.slice('/docs-hold:'.length);
      const begin = starts.get(name);
      if (!begin || begin.parentNode !== comment.parentNode) continue;
      const nodes = [];
      for (let node = begin.nextSibling; node && node !== comment; node = node.nextSibling) nodes.push(node);
      held.set(name, nodes);
    }
  }
  if (held.size === 0) return;
  observer = new MutationObserver(fill);
  observer.observe(document.body, { childList: true, subtree: true });
  // 没轮到就换了页的那几段用不上了（新页面排队时不留占位）：过两分钟放掉
  setTimeout(stop, 120_000);
}

/** 新 DOM 里出现了占位：把那一段旧节点放进去 */
function fill(records) {
  let placed = false;
  for (const record of records) {
    for (const node of record.addedNodes) {
      if (!(node instanceof Element)) continue;
      if (node.hasAttribute('data-docs-hold')) placed = place(node) || placed;
      for (const slot of node.querySelectorAll('[data-docs-hold]')) placed = place(slot) || placed;
    }
  }
  if (placed) pinAnchor();
  if (held.size === 0) stop();
}

let pinning = false;

/**
 * 带着 #锚点打开的页面：Blazor 换上新的 DOM 时占位还是空的，页面一下子变矮，它滚到锚点（或被浏览器夹住）的位置就不对了；
 * 之后上面的小节一个个换成真正的内容，高度有出入，锚点还会被顶开。所以从第一批预渲染的内容放回占位起（页面恢复原来的高度，还没绘制），
 * 页面每变一次就把锚点对回去，直到用户自己滚动、按键、点了什么（或过了 20 秒）
 */
function pinAnchor() {
  if (pinning || location.hash.length < 2) return;
  pinning = true;
  const id = decodeURIComponent(location.hash.slice(1));
  const align = () => document.getElementById(id)?.scrollIntoView();
  let frame = 0;
  const pin = new MutationObserver(() => (frame ||= requestAnimationFrame(() => {
    frame = 0;
    align();
  })));
  const release = () => {
    pin.disconnect();
    cancelAnimationFrame(frame);
    for (const type of INTENT) removeEventListener(type, release, true);
  };
  const INTENT = ['wheel', 'touchstart', 'keydown', 'pointerdown'];
  for (const type of INTENT) addEventListener(type, release, { capture: true, passive: true });
  setTimeout(release, 20_000);
  pin.observe(document.body, { childList: true, subtree: true });
  align();
}

function place(slot) {
  const name = slot.getAttribute('data-docs-hold');
  const nodes = held.get(name);
  // 占位里已经有东西（放过了），或没有这一段（不是预渲染出来的页面）：不动
  if (!nodes || slot.firstChild) return false;
  held.delete(name);
  slot.append(...nodes);
  return true;
}

function stop() {
  observer?.disconnect();
  observer = null;
  held.clear();
}
