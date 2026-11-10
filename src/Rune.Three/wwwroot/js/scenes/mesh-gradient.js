// 流动的网格渐变（MeshGradient，同 vue-tsx-pro 的 three/background/MeshGradient.tsx）：色板里的每个颜色是一团缓慢游走的光斑，
// 彼此柔和地融合，再加一层噪声扭曲与细颗粒；指针靠近时品牌色那一团被轻轻吸过去。画面是不透明的。
// options：palette、speed、interactive（见 shader-background.js），distortion（噪声扭曲 0–1）、grain（颗粒感）。

import { shaderScene } from './shader-background.js';

export const keys = ['palette'];

const SHADER = /* glsl */ `
uniform float uDistortion;
uniform float uGrain;

vec4 shade(vec2 uv) {
  float aspect = uResolution.x / max(uResolution.y, 1.0);
  vec2 p = (uv - 0.5) * vec2(aspect, 1.0);
  float t = uTime * 0.12;
  p += uDistortion * 0.3 * vec2(snoise(vec3(p * 1.3, t)), snoise(vec3(p * 1.3 + 7.3, t)));
  vec3 sum = vec3(0.0);
  float total = 0.0;
  for (int i = 0; i < THEME_PALETTE_SIZE; i++) {
    if (i >= uColorCount) break;
    float fi = float(i);
    vec2 center = vec2(
      sin(t * (0.9 + fi * 0.23) + fi * 2.4) * 0.42 * aspect,
      cos(t * (0.7 + fi * 0.31) + fi * 1.7) * 0.38
    );
    // 品牌色（第一个）被指针吸过去，其余跟着动一点
    float pull = (i == 0 ? 0.35 : 0.08) * uPointer.z;
    center = mix(center, uPointer.xy * 0.5 * vec2(aspect, 1.0), pull);
    float d = distance(p, center);
    float weight = exp(-d * d * 5.0) + 0.002;
    sum += uColors[i] * weight;
    total += weight;
  }
  vec3 color = sum / total;
  float noise = fract(sin(dot(uv * uResolution, vec2(12.9898, 78.233))) * 43758.5453) - 0.5;
  return vec4(color + noise * uGrain, 1.0);
}
`;

export function setup(context) {
  return shaderScene(context, SHADER, (options) => ({ uDistortion: options.distortion ?? 0.6, uGrain: options.grain ?? 0.04 }));
}
