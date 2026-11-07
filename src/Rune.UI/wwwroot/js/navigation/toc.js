// 本页目录的滚动高亮（data-rune-toc，同 vue-tsx-pro 的 useScrollSpy）：页面滚到哪一节，就给那一节的链接写 aria-current="location"，
// 侧边的指示条（indicator.js）跟着滑过去。当前节 = 顶部已经越过阅读线的最后一个标题；阅读线是标题自己的 scroll-margin-top
// （标题让开粘性顶栏的距离），点目录跳过去之后，高亮的正好是跳到的那一节。滚到底时最后几节可能永远到不了阅读线：这时高亮视口里的最后一个。
// 滚动容器是 data-scroll-root 指定 id 的元素，没有时是窗口。

const watched = new WeakSet();

/** 开始管理一个目录（插入时调用；重复调用无副作用）。 */
export function watchToc(nav) {
  if (watched.has(nav)) return;
  watched.add(nav);
  let frame = 0;
  const schedule = () => {
    cancelAnimationFrame(frame);
    frame = requestAnimationFrame(() => update(nav));
  };
  const rootId = nav.getAttribute('data-scroll-root');
  const root = rootId ? document.getElementById(rootId) : null;
  (root ?? window).addEventListener('scroll', schedule, { passive: true });
  window.addEventListener('resize', schedule, { passive: true });
  // 目录项变了（换页、标题增减）：重新算
  new MutationObserver(schedule).observe(nav, { childList: true, subtree: true });
  schedule();
}

function update(nav) {
  if (!nav.isConnected) return;
  const links = [...nav.querySelectorAll('a[href*="#"]')];
  const rootId = nav.getAttribute('data-scroll-root');
  const root = rootId ? document.getElementById(rootId) : null;
  const top = root ? root.getBoundingClientRect().top : 0;
  const bottom = root ? root.getBoundingClientRect().bottom : window.innerHeight;
  const scroller = root ?? document.scrollingElement ?? document.documentElement;
  const atBottom = scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 1;
  let current = null;
  for (const link of links) {
    const id = decodeURIComponent(link.hash.slice(1));
    const heading = id && document.getElementById(id);
    if (!heading) continue;
    const offset = heading.getBoundingClientRect().top - top;
    const line = parseFloat(getComputedStyle(heading).scrollMarginTop) || 0;
    // +1：跳转后标题正好停在阅读线上，取整误差不该让它算作还没到
    if (offset <= line + 1 || (atBottom && offset < bottom - top)) current = link;
  }
  for (const link of links) {
    if (link === current) {
      if (link.getAttribute('aria-current') !== 'location') link.setAttribute('aria-current', 'location');
    } else if (link.hasAttribute('aria-current')) {
      link.removeAttribute('aria-current');
    }
  }
}
