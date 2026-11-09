// 折叠内容区（data-rune-collapse，同 vue-tsx-pro 的 Collapse）：收起（data-state="closed"）的过渡结束后加 hidden="until-found"，
// 页内查找、锚点命中时浏览器自动展开并发 beforematch，这里转成冒泡的 rune-toggle（open: true）交给 C#；展开时立刻去掉 hidden。
// 不支持 until-found 的浏览器（Safari）只靠组件渲染的 inert。
// 完全展开（过渡结束）后加 data-expanded：内容层不再裁剪，贴着边的焦点环、阴影不被裁掉；过渡中与收起时照旧裁，内容跟着高度出现。

const watched = new WeakSet();
const supportsUntilFound = 'onbeforematch' in HTMLElement.prototype;

/** 等元素自己的过渡都结束（被打断也算结束） */
const afterTransitions = (element) => Promise.allSettled(element.getAnimations().map((animation) => animation.finished));

function sync(root, animated) {
  if (root.dataset.state === 'open') {
    root.removeAttribute('hidden');
    const expand = () => root.toggleAttribute('data-expanded', root.dataset.state === 'open');
    if (animated) afterTransitions(root).then(expand);
    else expand();
    return;
  }
  root.removeAttribute('data-expanded');
  if (!supportsUntilFound) return;
  const hide = () => {
    if (root.dataset.state !== 'open') root.setAttribute('hidden', 'until-found');
  };
  if (animated) afterTransitions(root).then(hide);
  else hide();
}

/** 开始管理一个折叠内容区（插入时调用；重复调用无副作用） */
export function watchCollapse(root) {
  if (watched.has(root)) return;
  watched.add(root);
  new MutationObserver(() => sync(root, true)).observe(root, { attributes: true, attributeFilter: ['data-state'] });
  root.addEventListener('beforematch', () => root.dispatchEvent(new CustomEvent('rune-toggle', { bubbles: true, detail: { open: true } })));
  // 一开始就是收起的：没有过渡要等
  sync(root, false);
}
