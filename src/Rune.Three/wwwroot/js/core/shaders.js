// 着色器的公共片段（GLSL 字符串，同 vue-tsx-pro 的 three/core/shaders.ts），拼进自己的片段着色器：
//
//   const fragmentShader = `
//     ${shaderChunks.simplexNoise}
//     ${shaderChunks.themePalette}
//     void main() { gl_FragColor = vec4(themePalette(snoise(vec3(vUv * 3.0, uTime)) * 0.5 + 0.5), 1.0); }
//   `;
//
// themePalette 用的 uColors、uColorCount 由 ShaderBackground 自动传入（主题色板）。

/** 色板最多几个颜色（uColors 数组的长度） */
export const THEME_PALETTE_SIZE = 6;

/**
 * 三维 simplex 噪声 `float snoise(vec3 v)`，返回 -1 到 1。
 * 来自 Ashima Arts 的 webgl-noise（Ian McEwan、Stefan Gustavson，MIT 许可）
 */
const simplexNoise = /* glsl */ `
vec3 mod289(vec3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec4 mod289(vec4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec4 permute(vec4 x) { return mod289(((x * 34.0) + 10.0) * x); }
vec4 taylorInvSqrt(vec4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

float snoise(vec3 v) {
  const vec2 C = vec2(1.0 / 6.0, 1.0 / 3.0);
  const vec4 D = vec4(0.0, 0.5, 1.0, 2.0);
  vec3 i = floor(v + dot(v, C.yyy));
  vec3 x0 = v - i + dot(i, C.xxx);
  vec3 g = step(x0.yzx, x0.xyz);
  vec3 l = 1.0 - g;
  vec3 i1 = min(g.xyz, l.zxy);
  vec3 i2 = max(g.xyz, l.zxy);
  vec3 x1 = x0 - i1 + C.xxx;
  vec3 x2 = x0 - i2 + C.yyy;
  vec3 x3 = x0 - D.yyy;
  i = mod289(i);
  vec4 p = permute(permute(permute(
    i.z + vec4(0.0, i1.z, i2.z, 1.0))
    + i.y + vec4(0.0, i1.y, i2.y, 1.0))
    + i.x + vec4(0.0, i1.x, i2.x, 1.0));
  float n_ = 0.142857142857;
  vec3 ns = n_ * D.wyz - D.xzx;
  vec4 j = p - 49.0 * floor(p * ns.z * ns.z);
  vec4 x_ = floor(j * ns.z);
  vec4 y_ = floor(j - 7.0 * x_);
  vec4 x = x_ * ns.x + ns.yyyy;
  vec4 y = y_ * ns.x + ns.yyyy;
  vec4 h = 1.0 - abs(x) - abs(y);
  vec4 b0 = vec4(x.xy, y.xy);
  vec4 b1 = vec4(x.zw, y.zw);
  vec4 s0 = floor(b0) * 2.0 + 1.0;
  vec4 s1 = floor(b1) * 2.0 + 1.0;
  vec4 sh = -step(h, vec4(0.0));
  vec4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
  vec4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
  vec3 p0 = vec3(a0.xy, h.x);
  vec3 p1 = vec3(a0.zw, h.y);
  vec3 p2 = vec3(a1.xy, h.z);
  vec3 p3 = vec3(a1.zw, h.w);
  vec4 norm = taylorInvSqrt(vec4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;
  vec4 m = max(0.5 - vec4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
  m = m * m;
  return 105.0 * dot(m * m, vec4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}
`;

/** 分形噪声 `float fbm(vec3 p)`：几层 simplex 噪声叠加，返回约 -1 到 1；依赖 simplexNoise */
const fbm = /* glsl */ `
float fbm(vec3 p) {
  float sum = 0.0;
  float amplitude = 0.5;
  for (int octave = 0; octave < 4; octave++) {
    sum += amplitude * snoise(p);
    p = p * 2.02 + vec3(17.0);
    amplitude *= 0.5;
  }
  return sum;
}
`;

/** 主题色板 `vec3 themePalette(float t)`：t 从 0 到 1 依次经过 uColors 的各个颜色；uColors 的长度是 THEME_PALETTE_SIZE */
const themePalette = /* glsl */ `
#define THEME_PALETTE_SIZE ${THEME_PALETTE_SIZE}
uniform vec3 uColors[THEME_PALETTE_SIZE];
uniform int uColorCount;

vec3 themePalette(float t) {
  float scaled = clamp(t, 0.0, 1.0) * float(max(uColorCount - 1, 1));
  vec3 color = uColors[0];
  for (int i = 1; i < THEME_PALETTE_SIZE; i++) {
    if (i >= uColorCount) break;
    color = mix(color, uColors[i], clamp(scaled - float(i - 1), 0.0, 1.0));
  }
  return color;
}
`;

/** 公共着色器片段：simplexNoise、fbm、themePalette */
export const shaderChunks = Object.freeze({ simplexNoise, fbm, themePalette });
