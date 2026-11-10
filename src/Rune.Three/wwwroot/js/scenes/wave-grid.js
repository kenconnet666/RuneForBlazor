// 线框地形（WaveGrid，同 vue-tsx-pro 的 three/effects/WaveGrid.tsx）：一张向远处延伸的网格随噪声起伏，像缓慢呼吸的山丘；
// 指针所在的位置隆起。颜色按高度在色板里取（低处第一个颜色、高处最后一个），远处渐隐到透明。背景透明；细分程度随画质档位变化。
// options：type（grid 网格线 / dots 只画交点）、palette、speed、amplitude（起伏的高度）、interactive。

import { BufferAttribute, BufferGeometry, LineSegments, Points, ShaderMaterial, Vector3 } from '../../vendor/three/three.js';
import { shaderChunks, THEME_PALETTE_SIZE } from '../core/shaders.js';

export const keys = ['type', 'palette'];

/** 网格的大小（世界单位）与最高画质下每边的格数 */
const WIDTH = 16;
const DEPTH = 12;
const CELLS = 72;

const vertexShader = /* glsl */ `
uniform float uTime;
uniform float uAmplitude;
uniform vec3 uPointer;
uniform float uPointSize;
varying float vHeight;
varying float vDepth;
${shaderChunks.simplexNoise}
${shaderChunks.fbm}
void main() {
  vec3 p = position;
  float wave = fbm(vec3(p.x * 0.18, p.y * 0.22 - uTime * 0.15, uTime * 0.05));
  float bump = exp(-distance(p.xy, uPointer.xy) * 0.6) * uPointer.z * 1.4;
  p.z = (wave * 1.6 + bump) * uAmplitude;
  vHeight = clamp(wave * 0.9 + 0.5 + bump * 0.4, 0.0, 1.0);
  vec4 view = modelViewMatrix * vec4(p, 1.0);
  vDepth = -view.z;
  gl_PointSize = uPointSize * (8.0 / vDepth);
  gl_Position = projectionMatrix * view;
}
`;

const fragmentShader = /* glsl */ `
varying float vHeight;
varying float vDepth;
${shaderChunks.themePalette}
void main() {
  float fade = 1.0 - smoothstep(9.0, 17.0, vDepth);
  gl_FragColor = vec4(themePalette(vHeight), fade * (0.35 + vHeight * 0.65));
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;

/** 网格的顶点：dots 时是交点，grid 时是横竖两组线段 */
function gridGeometry(cells, type) {
  const columns = Math.round(cells * (WIDTH / DEPTH));
  const at = (column, row) => [(column / columns - 0.5) * WIDTH, (row / cells - 0.5) * DEPTH, 0];
  const vertices = [];
  for (let row = 0; row <= cells; row++) {
    for (let column = 0; column <= columns; column++) {
      if (type === 'dots') vertices.push(...at(column, row));
      else {
        if (column < columns) vertices.push(...at(column, row), ...at(column + 1, row));
        if (row < cells) vertices.push(...at(column, row), ...at(column, row + 1));
      }
    }
  }
  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new BufferAttribute(new Float32Array(vertices), 3));
  return geometry;
}

export function setup(context) {
  const { scene, camera, quality, size, pointer, themeColorList } = context;
  const kind = context.options.type ?? 'grid';
  const sources = (context.options.palette ?? []).slice(0, THEME_PALETTE_SIZE);
  const list = themeColorList(sources.length ? sources : ['var(--rune-tones-accent-solid)'], THEME_PALETTE_SIZE);
  const uniforms = {
    uTime: { value: 0 },
    uAmplitude: { value: context.options.amplitude ?? 1 },
    uPointer: { value: new Vector3() },
    uPointSize: { value: 2 },
    uColors: { value: list },
    uColorCount: { value: Math.max(1, sources.length) },
  };
  const material = new ShaderMaterial({ vertexShader, fragmentShader, uniforms, transparent: true, premultipliedAlpha: true, depthWrite: false });
  const geometry = gridGeometry(Math.round(CELLS * quality.density), kind);
  const mesh = kind === 'dots' ? new Points(geometry, material) : new LineSegments(geometry, material);
  mesh.rotation.x = -Math.PI * 0.42;
  mesh.position.y = -0.6;
  scene.add(mesh);
  camera.position.set(0, 1.4, 6.5);
  camera.lookAt(0, -0.4, 0);
  return {
    frame: ({ time }) => {
      const { speed = 1, amplitude = 1, interactive = true } = context.options;
      uniforms.uTime.value = time * speed;
      uniforms.uAmplitude.value = amplitude;
      // 指针在网格平面上的落点：近似为按屏幕位置映射到网格的范围
      uniforms.uPointer.value.set(pointer.x * WIDTH * 0.35, (pointer.y + 0.3) * DEPTH * 0.45, interactive ? pointer.strength : 0);
    },
    resize: (next) => {
      uniforms.uPointSize.value = 2.2 * next.pixelRatio * (size.height / 400 || 1);
    },
  };
}
