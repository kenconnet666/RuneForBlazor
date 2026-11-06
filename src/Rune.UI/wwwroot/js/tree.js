// 树（data-rune-tree，role="tree"，同 vue-tsx-pro 的 Tree）：↑↓、Home / End、类型搜索与高亮照列表的走（lists.js，宿主就是树）；
// 这里管 → ←（展开收起、进入第一个子节点、回到父节点）、Enter、空格、Alt + 方向键与指针拖动（移动节点），要改状态的经 rune-command 交给 C#。
// 看不见的行（虚拟滚动、没渲染）不在 DOM 里：C# 把各行的父行、能否展开、是否展开写在 data-rune-tree-rows 上。

import { activeIndex, highlight } from './lists.js';
import { startMove } from './move.js';
import { isRtl, sendCommand } from './shared.js';

// data-rune-tree-rows 解析一次：值没变时直接用
const structures = new WeakMap();

function structureOf(tree) {
  const json = tree.getAttribute('data-rune-tree-rows') ?? '{"parents":[],"expandable":[],"expanded":[]}';
  const cached = structures.get(tree);
  if (cached?.json === json) return cached.structure;
  const { parents, expandable, expanded } = JSON.parse(json);
  const structure = { parents, expandable: new Set(expandable), expanded: new Set(expanded) };
  structures.set(tree, { json, structure });
  return structure;
}

/** 按键：处理了（不必再交给列表）时返回 true */
export function keyTree(event) {
  const tree = event.target;
  if (!(tree instanceof Element) || !tree.matches('[data-rune-tree]') || event.ctrlKey || event.metaKey) return false;
  const index = activeIndex(tree, tree);
  const forward = isRtl(tree) ? 'ArrowLeft' : 'ArrowRight';
  const backward = isRtl(tree) ? 'ArrowRight' : 'ArrowLeft';
  if (event.altKey) {
    const direction = { ArrowUp: 'up', ArrowDown: 'down', [forward]: 'in', [backward]: 'out' }[event.key];
    if (!direction || index < 0 || tree.getAttribute('data-rune-tree') !== 'move') return false;
    event.preventDefault();
    sendCommand(tree, 'move', `${index}:${direction}`);
    return true;
  }
  if (index < 0) return false;
  const { parents, expandable, expanded } = structureOf(tree);
  switch (event.key) {
    case forward:
      if (!expandable.has(index)) break;
      if (!expanded.has(index)) sendCommand(tree, 'expand', String(index));
      // 已展开：进入第一个子节点（子节点还没加载出来时不动）
      else if (parents[index + 1] === index) highlight(tree, tree, index + 1, true);
      break;
    case backward:
      if (expanded.has(index)) sendCommand(tree, 'collapse', String(index));
      else if (parents[index] >= 0) highlight(tree, tree, parents[index], true);
      break;
    case 'Enter':
      sendCommand(tree, 'enter', String(index));
      break;
    case ' ':
      sendCommand(tree, 'space', String(index));
      break;
    default:
      return false;
  }
  event.preventDefault();
  return true;
}

/**
 * 按下一行：焦点留在（移到）树上，这一行成为当前项；点击本身（选中、勾选、展开）由 C# 处理。
 * 能移动节点（data-rune-tree="move"）时按住拖动：落点写在行上（data-drop），松开时经命令交给 C#（是否能放由 C# 的 CanDrop 判断）。
 */
export function pressTreeItem(event) {
  const item = event.target instanceof Element ? event.target.closest('[role="treeitem"]') : null;
  const tree = item?.closest('[data-rune-tree]');
  if (!tree) return;
  event.preventDefault();
  tree.focus({ preventScroll: true });
  const from = Number(item.getAttribute('data-index'));
  highlight(tree, tree, from, false);
  // 触摸时照常滚动，不拖
  if (tree.getAttribute('data-rune-tree') === 'move' && event.pointerType !== 'touch') dragFrom(tree, from, event);
}

// 移动超过这么多像素才算拖动（之下是点击）
const DRAG_THRESHOLD = 4;

// 拖到收起的节点里停这么久就展开它，好放到更深的地方
const EXPAND_DELAY = 600;

function dragFrom(tree, from, event) {
  const rowAt = (index) => document.getElementById(`${tree.id}-${index}`);
  let dragging = false;
  let drop = null;
  let expandTimer = 0;

  const mark = (next) => {
    if (drop && (drop.index !== next?.index || drop.zone !== next?.zone)) rowAt(drop.index)?.removeAttribute('data-drop');
    drop = next;
    if (drop) rowAt(drop.index)?.setAttribute('data-drop', drop.zone);
  };

  startMove(event, {
    // 虚拟滚动会换掉行元素：捕获在不动的树上
    target: tree,
    onMove: ({ clientX, clientY, deltaX, deltaY }) => {
      if (!dragging && Math.hypot(deltaX, deltaY) < DRAG_THRESHOLD) return;
      dragging = true;
      rowAt(from)?.setAttribute('data-dragging', '');
      // 先按当前的位置算落点，再滚动：滚动后指针下面换成了别的项
      const next = spotAt(tree, from, clientX, clientY);
      autoScroll(tree, clientY);
      if (next?.index === drop?.index && next?.zone === drop?.zone) return;
      mark(next);
      clearTimeout(expandTimer);
      const { expandable, expanded } = structureOf(tree);
      if (next?.zone === 'inside' && expandable.has(next.index) && !expanded.has(next.index)) {
        expandTimer = setTimeout(() => sendCommand(tree, 'expand', String(next.index)), EXPAND_DELAY);
      }
    },
    onEnd: ({ cancelled }) => {
      clearTimeout(expandTimer);
      rowAt(from)?.removeAttribute('data-dragging');
      const spot = drop;
      mark(null);
      if (dragging && !cancelled && spot) sendCommand(tree, 'drop', `${from}:${spot.index}:${spot.zone}`);
    },
  });
}

/** 指针下的落点：一项的上四分之一放在它前面，下四分之一放在后面，中间放进它里面；放进被拖的节点自己或子孙里时没有落点 */
function spotAt(tree, from, clientX, clientY) {
  const item = document.elementFromPoint(clientX, clientY)?.closest('[role="treeitem"]');
  if (!item || !tree.contains(item)) return null;
  const index = Number(item.getAttribute('data-index'));
  const { parents } = structureOf(tree);
  for (let at = index; at >= 0; at = parents[at]) if (at === from) return null;
  const rect = item.getBoundingClientRect();
  const ratio = (clientY - rect.top) / rect.height;
  return { index, zone: ratio < 0.25 ? 'before' : ratio > 0.75 ? 'after' : 'inside' };
}

/** 拖到靠近上下边缘时滚动 */
function autoScroll(tree, clientY) {
  const rect = tree.getBoundingClientRect();
  if (clientY < rect.top + 32) tree.scrollTop -= 12;
  else if (clientY > rect.bottom - 32) tree.scrollTop += 12;
}
