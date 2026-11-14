// 嵌入的页面（Embed 的 iframe[data-rune-embed]）与自动播放的视频（Video 的 video[data-rune-autoplay]）。

/** 外层（AspectRatio）：加载中、加载完写在它的 data-state 上，骨架与 iframe 的显隐跟着它。 */
function rootOf(frame) {
  return frame.closest('[data-scope="embed"][data-part="root"]') ?? frame.parentElement;
}

/**
 * iframe 出现了：这时才给地址。地址直接写在标记里的话，预渲染出来的 iframe 在脚本接管之前就可能加载完了，
 * load 事件听不到，骨架会一直盖着。换了地址 C# 重建 iframe，再走一遍。
 */
export function startEmbed(frame) {
  const src = frame.getAttribute('data-rune-embed');
  if (!src || frame.getAttribute('src') === src) return;
  const root = rootOf(frame);
  if (root) root.dataset.state = 'loading';
  frame.src = src;
  // 内联的页面（srcdoc）不等 src：标记里就有，静态页面上在脚本接管之前已经加载完，之后写 src 也不会重新加载（有 srcdoc 时 src 不起作用）。
  // 重写一次让它在 load 接上之后再加载一遍
  const inline = frame.getAttribute('srcdoc');
  if (inline !== null) frame.setAttribute('srcdoc', inline);
}

/** iframe 加载完了（load 不冒泡，在捕获阶段接住）。 */
export function markEmbed(frame) {
  if (!(frame instanceof HTMLIFrameElement) || !frame.hasAttribute('data-rune-embed')) return;
  // 还没给地址时浏览器也会为空白页发一次 load：不算
  if (!frame.getAttribute('src')) return;
  const root = rootOf(frame);
  if (root) root.dataset.state = 'loaded';
}

/**
 * 自动播放的视频：系统要求减少动态效果时不自动播放，并显示控制条，让人自己决定要不要播。
 * 标记里不写 autoplay：浏览器会在脚本判断之前就开始播。
 */
export function startVideo(video) {
  if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
    video.controls = true;
    return;
  }
  // 没静音的自动播放会被浏览器拒绝：不报错，留给人自己点
  video.play().catch(() => {});
}
