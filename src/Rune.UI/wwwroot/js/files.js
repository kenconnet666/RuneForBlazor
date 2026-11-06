// 选文件的区域（data-rune-file-drop，FileDrop）：拖着文件经过时在区域上写 data-dragging。
// 文件本身由盖满区域的原生 file input 接住（放下就是选中，C# 的 InputFile 收到 change），这里只管高亮。
// dragenter / dragleave 在子元素之间也会触发：按区域计数，回到 0 才算离开。

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
