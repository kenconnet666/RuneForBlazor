// 选文件的区域（data-rune-file-drop，FileDrop）：拖着文件经过时在区域上写 data-dragging。
// 文件本身由盖满区域的原生 file input 接住（放下就是选中，C# 的 InputFile 收到 change），这里只管高亮。
// dragenter / dragleave 在子元素之间也会触发：按区域计数，回到 0 才算离开。
//
// Upload 的图片墙另有两样：
//   - data-rune-upload-previews：选到的图片先用本地地址（object URL）显示，不等上传。选文件（change）时给每张图片建一个，
//     经 rune-command（file-previews，JSON：“文件名|大小|修改时间” → 地址）交给 C#；属性值是 C# 还在用的地址（空格分隔），
//     不再用的（移除了、没加进列表的）收回，组件不在了全部收回
//   - data-rune-paste-files：焦点在里面时粘贴文件（截图、复制的图片），塞进里面 FileDrop 的 input 并发 change，与选文件一样交给 C#

import { sendCommand } from '../core/shared.js';

const depths = new WeakMap();

function zoneOf(event) {
  const zone = event.target instanceof Element ? event.target.closest('[data-rune-file-drop]') : null;
  if (!zone || zone.hasAttribute('data-disabled') || zone.hasAttribute('data-readonly')) return null;
  return event.dataTransfer?.types.includes('Files') ? zone : null;
}

/** 文件拖进区域（或区域里的子元素） */
export function onFileDragEnter(event) {
  const zone = zoneOf(event);
  if (!zone) return;
  depths.set(zone, (depths.get(zone) ?? 0) + 1);
  zone.setAttribute('data-dragging', '');
}

/** 文件拖出区域（或区域里的子元素） */
export function onFileDragLeave(event) {
  const zone = zoneOf(event);
  if (!zone) return;
  const depth = Math.max(0, (depths.get(zone) ?? 0) - 1);
  depths.set(zone, depth);
  if (depth === 0) zone.removeAttribute('data-dragging');
}

/** 放下了：收起高亮（选中交给原生 input） */
export function onFileDrop(event) {
  const zone = event.target instanceof Element ? event.target.closest('[data-rune-file-drop]') : null;
  if (!zone) return;
  depths.set(zone, 0);
  zone.removeAttribute('data-dragging');
}

// ───────── 图片墙的本地地址 ─────────

// 图片墙 → 为它建的地址（还没收回的）
const created = new Map();
// 刚建、还没在属性里出现过的地址 → 建的时间：C# 还没收到（或收到了还没渲染）时属性先因别的原因变了，不能当成不用了
const pending = new Map();
/** 建好多久还没用上就算没加进列表（类型、大小不符，超出数量） */
const ADOPT_WINDOW = 10_000;

/** C# 认这个键找文件（IBrowserFile 的 Name、Size、LastModified 毫秒数） */
const keyOf = (file) => `${file.name}|${file.size}|${file.lastModified}`;

/** 选到文件（捕获阶段，赶在 InputFile 之前）：给图片建本地地址交给 C# */
export function createFilePreviews(event) {
  const input = event.target;
  if (!(input instanceof HTMLInputElement) || input.type !== 'file' || !input.files?.length) return;
  const wall = input.closest('[data-rune-upload-previews]');
  if (!wall) return;
  const urls = {};
  for (const file of input.files) {
    if (!file.type.startsWith('image/')) continue;
    urls[keyOf(file)] = URL.createObjectURL(file);
  }
  const entries = Object.values(urls);
  if (entries.length === 0) return;
  if (!created.has(wall)) created.set(wall, new Set());
  entries.forEach((url) => {
    created.get(wall).add(url);
    pending.set(url, Date.now());
  });
  sendCommand(wall, 'file-previews', JSON.stringify(urls));
}

/** C# 用着的地址变了：不在里面的收回 */
export function releaseFilePreviews(wall) {
  const urls = created.get(wall);
  if (!urls) return;
  const used = new Set((wall.getAttribute('data-rune-upload-previews') ?? '').split(' '));
  for (const url of urls) {
    if (used.has(url)) {
      pending.delete(url);
      continue;
    }
    if (Date.now() - (pending.get(url) ?? 0) < ADOPT_WINDOW) continue;
    URL.revokeObjectURL(url);
    urls.delete(url);
    pending.delete(url);
  }
}

/** 文档里删掉了元素：不在文档里的图片墙，地址全部收回 */
export function releaseDetachedPreviews() {
  for (const [wall, urls] of created) {
    if (wall.isConnected) continue;
    urls.forEach((url) => {
      URL.revokeObjectURL(url);
      pending.delete(url);
    });
    created.delete(wall);
  }
}

// ───────── 粘贴文件 ─────────

/** 粘贴的内容里有文件：交给里面的 FileDrop，同选文件 */
export function pasteFiles(event) {
  const host = event.target instanceof Element ? event.target.closest('[data-rune-paste-files]') : null;
  const files = [...(event.clipboardData?.files ?? [])];
  if (!host || files.length === 0) return;
  const input = host.querySelector('[data-rune-file-drop] input[type=file]');
  if (!input || input.disabled) return;
  event.preventDefault();
  const transfer = new DataTransfer();
  files.forEach((file) => transfer.items.add(file));
  input.files = transfer.files;
  input.dispatchEvent(new Event('change', { bubbles: true }));
}
