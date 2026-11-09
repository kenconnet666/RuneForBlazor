// 折叠内容区（data-rune-collapse，同 vue-tsx-pro 的 Collapse）：收起（data-state="closed"）的过渡结束后加 hidden="until-found"，
// 页内查找、锚点命中时浏览器自动展开并发 beforematch，这里转成冒泡的 rune-toggle（open: true）交给 C#；展开时立刻去掉 hidden。
// 不支持 until-found 的浏览器（Safari）只靠组件渲染的 inert。
// 完全展开（过渡结束）后加 data-expanded：内容层不再裁剪，贴着边的焦点环、阴影不被裁掉；过渡中与收起时照旧裁，内容跟着高度出现。
//
// 触发器（data-rune-disclose + aria-controls，Collapsible、CodePreview 的“显示代码”）在这里当场切换，再发 rune-toggle 让 C# 写回：
// WebAssembly 还在启动时 C# 收不到点击，切换不能等它。WebAssembly 接管时会把预渲染的内容整片重画成初始状态，
// 所以接管前在这里切换过的（id 稳定的，data-rune-disclose="keep"）记下来，同一个 id 的内容区重画出来时恢复一次。

const watched = new WeakSet();
const supportsUntilFound = 'onbeforematch' in HTMLElement.prototype;
/** 接管前切换过的内容区：地址 + id → 是否展开（恢复一次后删掉） */
const pending = new Map();

/** 等元素自己的过渡都结束（被打断也算结束） */
const afterTransitions = (element) => Promise.allSettled(element.getAnimations().map((animation) => animation.finished));

const keyOf = (root) => `${location.pathname}#${root.id}`;

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

/** 展开或收起：改 data-state、inert 与触发器的 aria-expanded，再告诉 C#（C# 写回的是同样的值） */
function setOpen(root, open) {
  root.dataset.state = open ? 'open' : 'closed';
  root.inert = !open;
  for (const trigger of document.querySelectorAll(`[aria-controls="${CSS.escape(root.id)}"]`)) trigger.setAttribute('aria-expanded', String(open));
  root.dispatchEvent(new CustomEvent('rune-toggle', { bubbles: true, detail: { open } }));
}

/** 开始管理一个折叠内容区（插入时调用；重复调用无副作用） */
export function watchCollapse(root) {
  if (watched.has(root)) return;
  watched.add(root);
  new MutationObserver(() => sync(root, true)).observe(root, { attributes: true, attributeFilter: ['data-state'] });
  root.addEventListener('beforematch', () => root.dispatchEvent(new CustomEvent('rune-toggle', { bubbles: true, detail: { open: true } })));
  const restored = root.id ? pending.get(keyOf(root)) : undefined;
  if (restored !== undefined) {
    pending.delete(keyOf(root));
    if (restored !== (root.dataset.state === 'open')) setOpen(root, restored);
  }
  // 一开始就是收起的：没有过渡要等
  sync(root, false);
}

/** 点了触发器（data-rune-disclose）：切换它 aria-controls 指的内容区 */
export function toggleDisclosure(target) {
  const trigger = target.closest('[data-rune-disclose]');
  if (!trigger || trigger.matches(':disabled, [aria-disabled="true"]')) return;
  const root = document.getElementById(trigger.getAttribute('aria-controls') ?? '');
  if (!root?.matches('[data-rune-collapse]')) return;
  const open = root.dataset.state !== 'open';
  if (trigger.dataset.runeDisclose === 'keep') pending.set(keyOf(root), open);
  setOpen(root, open);
}
