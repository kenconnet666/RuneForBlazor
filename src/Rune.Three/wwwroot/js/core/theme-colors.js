// 主题色 → THREE.Color（同 vue-tsx-pro 的 three/core/theme-colors.ts）：3D 场景拿不到 CSS 变量，这里在元素上读出颜色的当前取值，
// 转成 three.js 的颜色。场景里经上下文的 themeColors 取用：
//
//   setup({ themeColors }) {
//     const palette = themeColors({ accent: 'var(--rune-tones-accent-solid)', glow: 'var(--rune-chart-colors-series3)' });
//     material.color = palette.accent;          // 直接赋同一个对象；构造参数里传会被复制一份
//   }
//
// 切换主题、改品牌色时重新读取，按主题的 lg 时长与 standard 缓动从旧色过渡到新色（减少动态效果时直接换）；
// Color 对象始终是同一批，材质与 uniform 不用重建。
//
// 颜色可以是 token（var(--rune-tones-accent-solid)）或任意 CSS 颜色（#e5484d、color-mix(…)、oklch(…)）：交给一个隐藏的探针元素，
// 由浏览器解析出最终的颜色，再画到 1×1 的画布上读出 sRGB 值（浏览器负责各种色彩空间的换算）。透明度不保留。

import { Color, SRGBColorSpace } from '../../vendor/three/three.js';
import { reducedMotion } from '../../../RuneForBlazor.UI/js/core/shared.js';
import { easing } from '../../../RuneForBlazor.UI/js/core/tween.js';

let pixel;

/** 在 scope 元素上读出 CSS 颜色，写进 target（sRGB 转成 three.js 的线性工作空间）；解析不了时保持原值，返回 false */
export function readThemeColor(scope, value, target) {
  const probe = document.createElement('span');
  probe.style.display = 'none';
  probe.style.color = value;
  scope.append(probe);
  const computed = getComputedStyle(probe).color;
  probe.remove();
  if (!computed) return false;
  pixel ??= document.createElement('canvas').getContext('2d', { willReadFrequently: true });
  pixel.clearRect(0, 0, 1, 1);
  pixel.fillStyle = computed;
  pixel.fillRect(0, 0, 1, 1);
  const [r, g, b, a] = pixel.getImageData(0, 0, 1, 1).data;
  // 完全透明（transparent、解析失败）时没有颜色可取
  if (a === 0) return false;
  // getImageData 给的是没有预乘的值：半透明的颜色也读得出原来的颜色
  target.setRGB(r / 255, g / 255, b / 255, SRGBColorSpace);
  return true;
}

/** 主题过渡的时长（毫秒）与缓动：主题的 lg 时长、standard 缓动；减少动态效果或主题的时长为 0 时直接换 */
function transitionOf(scope) {
  if (reducedMotion.matches) return null;
  const style = getComputedStyle(scope);
  const duration = parseFloat(style.getPropertyValue('--rune-durations-lg')) || 0;
  return duration > 0 ? { duration, ease: easing(style.getPropertyValue('--rune-easings-standard')) } : null;
}

/**
 * 一组跟随主题的颜色：colors 是名字 → THREE.Color（换主题时原地改值）；refresh(scope, sources, transition) 在 scope 上重新读取
 * （transition 为 false 时直接换），stop() 取消还在跑的过渡。onChange 在颜色变了（第一次读到、过渡的每一帧）时调用
 */
export function createThemeColorSet(onChange) {
  const colors = {};
  let frame = 0;
  return {
    colors,
    refresh(scope, sources, transition) {
      cancelAnimationFrame(frame);
      const from = {};
      const to = {};
      for (const [name, value] of Object.entries(sources)) {
        const color = (colors[name] ??= new Color());
        from[name] = color.clone();
        to[name] = color.clone();
        readThemeColor(scope, value, to[name]);
      }
      const apply = (progress) => {
        for (const name of Object.keys(to)) colors[name].lerpColors(from[name], to[name], progress);
        onChange?.();
      };
      const timing = transition ? transitionOf(scope) : null;
      if (!timing) {
        apply(1);
        return;
      }
      const start = performance.now();
      const step = (now) => {
        const progress = Math.min(1, (now - start) / timing.duration);
        apply(timing.ease(progress));
        if (progress < 1) frame = requestAnimationFrame(step);
      };
      frame = requestAnimationFrame(step);
    },
    stop() {
      cancelAnimationFrame(frame);
    },
  };
}
