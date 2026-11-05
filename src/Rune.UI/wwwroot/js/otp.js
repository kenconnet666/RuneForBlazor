// 验证码输入框（data-rune-otp，同 vue-tsx-pro 的 OtpField）：格子只是显示，真正的输入框盖在上面。
// 输入时在捕获阶段先过滤（只留数字或字母数字、字母转大写、截到 data-length 位），赶在 Blazor 读值之前；
// 再按输入框的当前值画格子：各格的 data-char（显示成圆点时是 •）、data-state，聚焦时当前格的 data-active。
// 输入框的值由 ValueElement 保护、不会被晚到的渲染写回，所以它就是准的：C# 晚到的渲染改了格子的属性时，按它重画。

const watched = new WeakSet();

const sanitizers = {
  numeric: (text) => text.replace(/\D/g, ''),
  alphanumeric: (text) => text.replace(/[^0-9a-z]/gi, '').toUpperCase(),
};

const inputOf = (root) => root.querySelector(':scope > input');

function setAttribute(element, name, value) {
  if (element.getAttribute(name) !== value) element.setAttribute(name, value);
}

/** 按输入框的当前值画格子 */
function paint(root) {
  const input = inputOf(root);
  if (!input) return;
  const chars = [...input.value];
  const length = Number(root.dataset.length);
  const mask = root.hasAttribute('data-mask');
  const focused = document.activeElement === input && !input.readOnly && !input.disabled;
  const current = Math.min(chars.length, length - 1);
  root.querySelectorAll(':scope > [data-index]').forEach((slot) => {
    const index = Number(slot.dataset.index);
    const char = chars[index] ? (mask ? '•' : chars[index]) : '';
    setAttribute(slot, 'data-char', char);
    setAttribute(slot, 'data-state', char ? 'filled' : 'empty');
    const active = focused && index === current;
    if (slot.hasAttribute('data-active') !== active) slot.toggleAttribute('data-active', active);
  });
}

/** 开始监视一个验证码输入框（插入时调用；重复调用无副作用）：C# 的渲染改了格子时按输入框重画 */
export function watchOtp(root) {
  if (watched.has(root)) return;
  watched.add(root);
  new MutationObserver(() => paint(root)).observe(root, { subtree: true, attributes: true, attributeFilter: ['data-char', 'data-state'] });
  paint(root);
}

const rootOf = (target) => (target instanceof HTMLInputElement && target.parentElement?.hasAttribute('data-rune-otp') ? target.parentElement : null);

/** input（捕获阶段）：过滤后画格子 */
export function onOtpInput(event) {
  const root = rootOf(event.target);
  if (!root) return;
  const input = event.target;
  const next = (sanitizers[root.dataset.charset] ?? sanitizers.numeric)(input.value).slice(0, Number(root.dataset.length));
  if (input.value !== next) input.value = next;
  paint(root);
}

/** 聚焦、失焦：当前格的焦点环与光标 */
export function onOtpFocus(event) {
  const root = rootOf(event.target);
  if (root) paint(root);
}
