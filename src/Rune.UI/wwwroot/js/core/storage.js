// 记在浏览器里的状态（LocalSetting 的 template[data-rune-local-setting]，同 vue-tsx-pro 的 usePersistentState）：
// - 出现时读存储：有记下的值、而且与 C# 的不同时交给 C#（rune-command restore，值是 JSON 文字）；没有记下的不写（同 vue：改了才存）。
// - C# 改了 data-value（值变了）时写进存储；内容没变（刚读出来的、别的标签页刚写进来的）不再写。
// - 别的标签页改了同一个键（storage 事件）：交给 C#。
// 隐私模式、禁用存储、配额满时读写会抛错：只在这次打开有效。
import { sendCommand } from './shared.js';

const watching = new WeakSet();
const elements = new Set();

const storageOf = (element) => {
  try {
    return element.dataset.storage === 'session' ? sessionStorage : localStorage;
  } catch {
    return null;
  }
};

function read(element) {
  try {
    return storageOf(element)?.getItem(element.dataset.runeLocalSetting) ?? null;
  } catch {
    return null;
  }
}

function write(element) {
  const value = element.dataset.value;
  if (value === undefined || value === read(element)) return;
  try {
    storageOf(element)?.setItem(element.dataset.runeLocalSetting, value);
  } catch {
    // 存不了：只在这次打开有效
  }
}

function restore(element) {
  const stored = read(element);
  if (stored !== null && stored !== element.dataset.value) sendCommand(element, 'restore', stored);
}

let listening = false;

function listen() {
  if (listening) return;
  listening = true;
  window.addEventListener('storage', (event) => {
    for (const element of elements) {
      if (!element.isConnected) {
        elements.delete(element);
        continue;
      }
      if (event.key === element.dataset.runeLocalSetting && event.storageArea === storageOf(element)) restore(element);
    }
  });
}

/** LocalSetting 出现时：读出记下的值，之后值变了就写回 */
export function watchLocalSetting(element) {
  if (watching.has(element)) return;
  watching.add(element);
  elements.add(element);
  listen();
  restore(element);
  new MutationObserver(() => {
    if (element.isConnected) write(element);
  }).observe(element, { attributes: true, attributeFilter: ['data-value'] });
}
