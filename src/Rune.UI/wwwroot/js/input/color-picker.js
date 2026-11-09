// 取色器（data-rune-color-picker，同 vue-tsx-pro 的 ColorPicker）：
//   - 饱和度 / 明度色板（data-color-area）：指针按下即跳过去并接着拖，←→ 改饱和度、↑↓ 改明度（Shift 一次 10%）。
//     把手当场挪（写在把手自己的 --rune-color-s / --rune-color-v 上，不等往返），每帧最多发一次 rune-command color-area（"s v"），
//     拖完、松开方向键时发 color-area-end（记进最近使用）；C# 渲染回来后去掉把手上的临时值
//   - 吸管：支持 EyeDropper 的浏览器才把按钮（data-color-eyedropper）显示出来，取到的颜色发 color-pick
import { startMove } from '../core/move.js';
import { sendCommand } from '../core/shared.js';

const watched = new WeakSet();
const clamp = (value) => Math.min(1, Math.max(0, value));

/** 开始管理一个取色器（插入时调用；重复调用无副作用） */
export function watchColorPicker(root) {
  if (watched.has(root)) return;
  watched.add(root);
  const area = root.querySelector('[data-color-area]');
  const thumb = area?.querySelector('[role="slider"]');
  if (area && thumb) {
    area.addEventListener('pointerdown', (event) => grab(root, area, thumb, event));
    thumb.addEventListener('keydown', (event) => key(root, area, thumb, event));
    thumb.addEventListener('keyup', (event) => {
      if (event.key.startsWith('Arrow')) sendCommand(root, 'color-area-end');
    });
    // C# 按新值渲染了：去掉拖动时的临时值
    new MutationObserver(() => {
      if (!area.hasAttribute('data-dragging')) release(thumb);
    }).observe(area, { attributes: true, attributeFilter: ['style'] });
  }
  const dropper = root.querySelector('[data-color-eyedropper]');
  if (dropper && 'EyeDropper' in window) {
    dropper.hidden = false;
    dropper.addEventListener('click', async () => {
      try {
        const { sRGBHex } = await new window.EyeDropper().open();
        sendCommand(root, 'color-pick', sRGBHex);
      } catch {
        // 用户按 Esc 取消
      }
    });
  }
}

const disabled = (root) => root.hasAttribute('data-disabled');
const current = (area, name) => Number(getComputedStyle(area.querySelector('[role="slider"]')).getPropertyValue(name)) || 0;

/** 把手挪到 s、v，并交给 C#（每帧最多一次） */
function place(root, thumb, s, v) {
  thumb.style.setProperty('--rune-color-s', String(s));
  thumb.style.setProperty('--rune-color-v', String(v));
  if (thumb._pending) {
    thumb._pending = `${s} ${v}`;
    return;
  }
  thumb._pending = `${s} ${v}`;
  requestAnimationFrame(() => {
    sendCommand(root, 'color-area', thumb._pending);
    thumb._pending = null;
  });
}

function release(thumb) {
  thumb.style.removeProperty('--rune-color-s');
  thumb.style.removeProperty('--rune-color-v');
}

function pickAt(root, area, thumb, clientX, clientY) {
  const rect = area.getBoundingClientRect();
  place(root, thumb, clamp((clientX - rect.left) / rect.width), clamp(1 - (clientY - rect.top) / rect.height));
}

function grab(root, area, thumb, event) {
  if (disabled(root) || event.button !== 0) return;
  event.preventDefault();
  area.toggleAttribute('data-dragging', true);
  pickAt(root, area, thumb, event.clientX, event.clientY);
  thumb.focus();
  startMove(event, {
    target: area,
    onMove: ({ clientX, clientY }) => pickAt(root, area, thumb, clientX, clientY),
    onEnd: () => {
      area.removeAttribute('data-dragging');
      sendCommand(root, 'color-area-end');
    },
  });
}

function key(root, area, thumb, event) {
  const step = event.shiftKey ? 0.1 : 0.01;
  const moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, step], ArrowDown: [0, -step] };
  const move = moves[event.key];
  if (!move || disabled(root)) return;
  event.preventDefault();
  place(root, thumb, clamp(current(area, '--rune-color-s') + move[0]), clamp(current(area, '--rune-color-v') + move[1]));
}
