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

function start() {
  if (started) return;
  started = true;
  watchClips();
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', collect, { once: true });
  else collect();
}

// “视频”示例的片段是在浏览器里现录的（clip.js，用到时才下载）：交互渲染出来的 video[data-docs-clip] 出现了就录一段给它（预渲染的那一份不录）
function watchClips() {
  const record = (video) => import('./clip.js').then(({ recordClip }) => recordClip(video));
  const seen = new WeakSet();
  const scan = (root) => {
    for (const video of root.querySelectorAll('video[data-docs-clip]')) {
      // 占位里留着的是预渲染的那一份（马上要被换掉）：不给它录
      if (seen.has(video) || video.closest('[data-docs-hold]')) continue;
      seen.add(video);
      void record(video);
    }
  };
  new MutationObserver((records) => {
    for (const { addedNodes } of records)
      for (const node of addedNodes) if (node instanceof Element) scan(node.matches('video[data-docs-clip]') ? node.parentElement ?? node : node);
  }).observe(document.documentElement, { childList: true, subtree: true });
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
  for (const record of records) {
    for (const node of record.addedNodes) {
      if (!(node instanceof Element)) continue;
      if (node.hasAttribute('data-docs-hold')) place(node);
      for (const slot of node.querySelectorAll('[data-docs-hold]')) place(slot);
    }
  }
  if (held.size === 0) stop();
}

function place(slot) {
  const name = slot.getAttribute('data-docs-hold');
  const nodes = held.get(name);
  // 占位里已经有东西（放过了），或没有这一段（不是预渲染出来的页面）：不动
  if (!nodes || slot.firstChild) return;
  held.delete(name);
  slot.append(...nodes);
}

function stop() {
  observer?.disconnect();
  observer = null;
  held.clear();
}
