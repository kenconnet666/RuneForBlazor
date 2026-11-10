// 极光（AuroraBackground，同 vue-tsx-pro 的 three/background/AuroraBackground.tsx）：几层半透明的光幕在区域上方缓缓飘动，
// 下缘清晰、向上渐隐，带细密的竖向光丝。背景透明：暗色主题里像夜空的极光，亮色主题里是一层淡淡的彩色雾。
// options：palette、speed、interactive（见 shader-background.js），intensity（亮度 0–1.5）、height（光幕下缘的高度，0 底部、1 顶部）。

import { shaderScene } from './shader-background.js';

export const keys = ['palette'];

const SHADER = /* glsl */ `
uniform float uIntensity;
uniform float uHeight;

vec4 shade(vec2 uv) {
  float aspect = uResolution.x / max(uResolution.y, 1.0);
  float x = uv.x * aspect + uPointer.x * 0.12 * uPointer.z;
  float t = uTime * 0.08;
  vec3 color = vec3(0.0);
  float alpha = 0.0;
  for (int band = 0; band < 3; band++) {
    float fb = float(band);
    float wave = fbm(vec3(x * (0.7 + fb * 0.25) + fb * 3.1, fb * 1.7, t * (1.0 + fb * 0.35)));
    float edge = uHeight + fb * 0.07 + wave * 0.2;
    float d = uv.y - edge;
    // 下缘清晰、向上指数渐隐
    float curtain = smoothstep(-0.025, 0.015, d) * exp(-max(d, 0.0) * (3.2 + fb * 1.6));
    float rays = 0.55 + 0.45 * snoise(vec3(x * 9.0 + fb * 11.0, t * 3.0, fb));
    float a = curtain * rays * (0.75 - fb * 0.18);
    color += themePalette(clamp(0.15 + fb * 0.3 + wave * 0.35, 0.0, 1.0)) * a;
    alpha += a;
  }
  // 暗色主题里光更亮，亮色主题里收敛成雾
  float strength = uIntensity * mix(0.55, 1.0, uDark);
  return vec4(color / max(alpha, 1e-4), clamp(alpha * strength, 0.0, 1.0));
}
`;

export function setup(context) {
  return shaderScene(context, SHADER, (options) => ({ uIntensity: options.intensity ?? 1, uHeight: options.height ?? 0.55 }));
}
