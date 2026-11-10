// 着色器背景（ShaderBackground，同 vue-tsx-pro 的 three/background/ShaderBackground.tsx）：使用方写 `vec4 shade(vec2 uv)`，
// 主题色、时间、尺寸、指针自动作为 uniform 传入，画满整块区域。MeshGradient、AuroraBackground 也用这里的 shaderScene。
//
// options：fragmentShader（shade 函数）、uniforms（名字 → 数值，改了实时生效）、palette（token 或 CSS 颜色，最多 6 个）、
// speed（uTime = 秒数 × speed）、interactive（跟随指针，关掉时 uPointer.z 始终为 0）。
// shade 里可以直接用：uTime、uResolution（CSS 像素）、uPointer（xy 是 -1–1 的指针位置，z 是指针在区域内的程度 0–1）、
// uDark（暗色主题为 1）、uBackground（主题背景色）、uColors / uColorCount 与 themePalette(t)、snoise、fbm。
// 颜色在线性空间里计算，输出时按渲染器的色彩空间转换。

import { Mesh, PlaneGeometry, ShaderMaterial, Vector2, Vector3 } from '../../vendor/three/three.js';
import { shaderChunks, THEME_PALETTE_SIZE } from '../core/shaders.js';

/** 着色器与色板建好后不再改：变了重建 */
export const keys = ['fragmentShader', 'palette'];

const vertexShader = /* glsl */ `
varying vec2 vUv;
void main() {
  vUv = uv;
  gl_Position = vec4(position.xy, 0.0, 1.0);
}
`;

/** 把 shade 函数包成完整的片段着色器 */
function fragment(source) {
  return /* glsl */ `
uniform float uTime;
uniform vec2 uResolution;
uniform vec3 uPointer;
uniform float uDark;
uniform vec3 uBackground;
varying vec2 vUv;
${shaderChunks.simplexNoise}
${shaderChunks.fbm}
${shaderChunks.themePalette}
${source}
void main() {
  gl_FragColor = shade(vUv);
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;
}

/**
 * 全屏片段着色器的场景：shade 是着色器源码，extra(options) 给出自己的 uniform（名字 → 数值），options 变了时重新取值
 */
export function shaderScene(context, shade, extra = () => ({})) {
  const { scene, size, pointer, themeColors, themeColorList } = context;
  const background = 'var(--rune-colors-background)';
  const palette = context.options.palette ?? [];
  const { background: backgroundColor } = themeColors({ background });
  // 没给颜色时色板全是背景色
  const list = themeColorList(palette.length > 0 ? palette : [background], THEME_PALETTE_SIZE);
  const builtin = {
    uTime: { value: 0 },
    uResolution: { value: new Vector2(size.width, size.height) },
    uPointer: { value: new Vector3() },
    uDark: { value: 0 },
    uBackground: { value: backgroundColor },
    uColors: { value: list },
    uColorCount: { value: Math.min(Math.max(palette.length, 1), THEME_PALETTE_SIZE) },
  };
  const own = Object.fromEntries(Object.entries(extra(context.options)).map(([name, value]) => [name, { value }]));
  const material = new ShaderMaterial({
    vertexShader,
    fragmentShader: fragment(shade),
    uniforms: { ...builtin, ...own },
    transparent: true,
    premultipliedAlpha: true,
    depthTest: false,
    depthWrite: false,
  });
  const quad = new Mesh(new PlaneGeometry(2, 2), material);
  quad.frustumCulled = false;
  scene.add(quad);
  return {
    frame: ({ time }) => {
      const { speed = 1, interactive = true } = context.options;
      builtin.uTime.value = time * speed;
      builtin.uPointer.value.set(pointer.x, pointer.y, interactive ? pointer.strength : 0);
      builtin.uDark.value = context.dark ? 1 : 0;
    },
    resize: (next) => builtin.uResolution.value.set(next.width, next.height),
    update: (options) => {
      for (const [name, value] of Object.entries(extra(options))) {
        if (own[name]) own[name].value = value;
      }
    },
  };
}

export function setup(context) {
  return shaderScene(context, context.options.fragmentShader ?? 'vec4 shade(vec2 uv) { return vec4(0.0); }', (options) => options.uniforms ?? {});
}
