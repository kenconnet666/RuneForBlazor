// 按钮一类的效果：复制到剪贴板、波纹、图标描线、可移除元素的离场、加载中按钮拦下点击。

import { TABBABLE, afterTransition, reducedMotion, speak } from './shared.js';


export function blockInert(event) {
  if (event.target instanceof Element && event.target.closest('[data-rune-inert]')) event.preventDefault();
}

/** 复制按钮：写入剪贴板（点击当下同步开始写，Safari 才认），成功后显示对勾、播报，过一会儿复原 */
export function copyFrom(button) {
  const text = button.getAttribute('data-rune-copy') ?? '';
  const finish = (success) => {
    if (success) {
      button.setAttribute('data-state', 'copied');
      speak(button.getAttribute('data-rune-copied-text') || '');
      clearTimeout(copyTimers.get(button));
      const timeout = Number(button.getAttribute('data-rune-copy-timeout')) || 2000;
      copyTimers.set(button, setTimeout(() => button.removeAttribute('data-state'), timeout));
    }
    button.dispatchEvent(new CustomEvent('rune-copied', { bubbles: true, detail: { success } }));
  };
  if (!navigator.clipboard?.writeText) {
    finish(false);
    return;
  }
  navigator.clipboard.writeText(text).then(() => finish(true), () => finish(false));
}

const copyTimers = new WeakMap();

/** 图标描线：每一笔的长度统一成 1，虚线从 0 画到 1（时长、缓动取主题；减少动态效果时直接画好） */
/** 用户点击勾上了复选框（data-rune-check-draw）：后面的对勾一笔画出（初次渲染、外部改值不经过 change，不画） */
export function drawCheckOnChange(event) {
  const target = event.target;
  if (!(target instanceof HTMLInputElement) || !target.checked || !target.hasAttribute('data-rune-check-draw')) return;
  const check = target.nextElementSibling;
  if (check instanceof SVGSVGElement) drawIcon(check);
}

export function drawIcon(svg) {
  if (reducedMotion.matches) return;
  const style = getComputedStyle(svg);
  const duration = parseFloat(style.getPropertyValue('--rune-durations-xl')) || 500;
  const easing = style.getPropertyValue('--rune-easings-enter').trim() || 'ease-out';
  for (const stroke of svg.children) {
    stroke.setAttribute('pathLength', '1');
    // 圆头的线长度为 0 时还会画出一个点：开头用平头
    stroke.animate(
      [{ strokeDasharray: '0 1', strokeLinecap: 'butt' }, { strokeDasharray: '1 1', strokeLinecap: 'round' }],
      { duration, easing },
    );
  }
}

/** 可移除元素的离场：先播放离场过渡，结束后交给 Blazor 移除；等一会儿还在（使用方没移除）就重新显示 */
export function startDismiss(button) {
  const target = button.closest('[data-rune-dismissible]');
  if (!target || target.hasAttribute('data-dismissing')) return;
  const candidates = focusCandidatesAfterRemoval(target);
  target.setAttribute('data-dismissing', '');
  afterTransition(target, () => {
    target.dispatchEvent(new CustomEvent('rune-dismiss', { bubbles: true }));
    // Server 模式要等一次往返：每 50ms 看一次，最多 2 秒
    let checks = 0;
    const timer = setInterval(() => {
      if (!target.isConnected) {
        clearInterval(timer);
        const active = document.activeElement;
        if (!active || active === document.body) candidates.find((item) => item.isConnected)?.focus();
      } else if (++checks >= 40) {
        clearInterval(timer);
        target.removeAttribute('data-dismissing');
      }
    }, 50);
  });
}

/** 元素被移除后接手焦点的候选：同组（父元素）里在它后面的可聚焦元素，再是前面的；焦点不在它里面时不接管 */
function focusCandidatesAfterRemoval(element) {
  const group = element.parentElement;
  if (!group || !element.contains(document.activeElement)) return [];
  const others = [...group.querySelectorAll(TABBABLE)].filter((item) => !element.contains(item));
  const following = others.filter((item) => element.compareDocumentPosition(item) & Node.DOCUMENT_POSITION_FOLLOWING);
  const preceding = others.filter((item) => !following.includes(item)).reverse();
  return [...following, ...preceding];
}

/**
 * 在带 data-rune-ripple 的元素上开始一个波纹（同 vue-tsx-pro 的 ripple）：半透明的当前文字颜色，
 * 实底按钮上是浅色、描边按钮上是深色；松开、移出、取消或失去焦点时淡出并移除。
 * 宿主要有 position: relative 与 overflow: hidden（SharedStyles.RippleHost）。减少动效时不扩散，只是按下时变色。
 */
export function startRipple(event) {
  const keyboard = event instanceof KeyboardEvent;
  const host = event.target instanceof Element ? event.target.closest('[data-rune-ripple]') : null;
  if (!host || host.matches(':disabled, [aria-disabled="true"], [aria-busy="true"]')) return;
  if (keyboard ? event.repeat || event.target !== host : event.button !== 0) return;

  const rect = host.getBoundingClientRect();
  const x = keyboard ? rect.width / 2 : event.clientX - rect.left;
  const y = keyboard ? rect.height / 2 : event.clientY - rect.top;
  // 圆心到最远的角的距离：扩散完正好盖满整个元素
  const radius = Math.hypot(Math.max(x, rect.width - x), Math.max(y, rect.height - y));
  const ripple = document.createElement('span');
  ripple.setAttribute('aria-hidden', 'true');
  Object.assign(ripple.style, {
    position: 'absolute',
    left: `${x - radius}px`,
    top: `${y - radius}px`,
    width: `${radius * 2}px`,
    height: `${radius * 2}px`,
    borderRadius: '50%',
    pointerEvents: 'none',
    backgroundColor: 'color-mix(in srgb, currentColor 22%, transparent)',
  });
  host.append(ripple);
  const motion = !reducedMotion.matches;
  const easing = getComputedStyle(host).getPropertyValue('--rune-easings-standard').trim() || 'ease-out';
  ripple.animate([{ transform: 'scale(0)' }, { transform: 'scale(1)' }], { duration: motion ? 450 : 0, easing, fill: 'forwards' });

  const releaseEvents = keyboard
    ? [['keyup', host], ['blur', host]]
    : [['pointerup', window], ['pointercancel', window], ['pointerleave', host]];
  const release = () => {
    for (const [type, target] of releaseEvents) target.removeEventListener(type, release);
    const fade = motion ? 250 : 0;
    ripple.animate([{ opacity: 1 }, { opacity: 0 }], { duration: fade, easing: 'ease-in', fill: 'forwards' }).finished.then(() => ripple.remove());
    // 页面不渲染时（后台标签页）动画不推进、finished 不会完成：到时间兜底移除
    setTimeout(() => ripple.remove(), fade + 100);
  };
  for (const [type, target] of releaseEvents) target.addEventListener(type, release);
}
