// 粒子场（ParticleField，同 vue-tsx-pro 的 three/effects/ParticleField.tsx）：柔和的光点在区域里缓缓漂移，指针靠近时向四周排开、
// 离开后回到原来的漂移；links 打开时相近的粒子连成星座般的细线，越近越清楚。背景透明。
// 粒子数默认按面积与画质档位决定（连线时更稀疏），尺寸变化时增减；颜色从色板里随机分配，连线用色板的第一个颜色。
// options：count（粒子数，不写时按面积）、palette、size（直径，CSS 像素）、speed、links、linkDistance、interactive、repelRadius。

import { BufferAttribute, BufferGeometry, LineSegments, OrthographicCamera, Points, ShaderMaterial } from '../../vendor/three/three.js';
import { THEME_PALETTE_SIZE } from '../core/shaders.js';

export const keys = ['palette', 'links'];

/** 粒子数上限（不连线 / 连线）：连线的计算量随粒子数增长 */
const MAX_PARTICLES = 3000;
const MAX_LINKED = 420;
/** 每个粒子最多画几条线 */
const LINKS_PER_PARTICLE = 6;

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

const pointsVertex = /* glsl */ `
uniform vec3 uColors[${THEME_PALETTE_SIZE}];
uniform float uPixelRatio;
attribute float aSize;
attribute float aColor;
varying vec3 vColor;
void main() {
  vColor = uColors[int(aColor)];
  gl_PointSize = aSize * uPixelRatio;
  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
}
`;

const pointsFragment = /* glsl */ `
varying vec3 vColor;
void main() {
  float d = length(gl_PointCoord - 0.5);
  gl_FragColor = vec4(vColor, smoothstep(0.5, 0.1, d));
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;

const linesVertex = /* glsl */ `
attribute float aAlpha;
varying float vAlpha;
void main() {
  vAlpha = aAlpha;
  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
}
`;

const linesFragment = /* glsl */ `
uniform vec3 uColor;
varying float vAlpha;
void main() {
  gl_FragColor = vec4(uColor, vAlpha);
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;

/** 透明叠加的着色器材质 */
const overlayMaterial = (params) => new ShaderMaterial({ ...params, transparent: true, premultipliedAlpha: true, depthTest: false, depthWrite: false });

export function setup(context) {
  const { scene, size: area, quality, pointer, themeColorList } = context;
  const option = (name, fallback) => context.options[name] ?? fallback;
  const links = option('links', false);
  const camera = new OrthographicCamera(0, 1, 1, 0, -1, 1);
  const max = links ? MAX_LINKED : MAX_PARTICLES;
  const sources = option('palette', []).slice(0, THEME_PALETTE_SIZE);
  const list = themeColorList(sources.length ? sources : ['var(--rune-tones-accent-solid)'], THEME_PALETTE_SIZE);

  // 粒子状态：位置、速度、各自的漂移速度
  const position = new Float32Array(max * 3);
  const velocity = new Float32Array(max * 2);
  const drift = new Float32Array(max * 2);
  const sizes = new Float32Array(max);
  const tints = new Float32Array(max);
  let active = 0;
  const seed = (index) => {
    position[index * 3] = Math.random() * area.width;
    position[index * 3 + 1] = Math.random() * area.height;
    const angle = Math.random() * Math.PI * 2;
    const pace = 6 + Math.random() * 14;
    drift[index * 2] = Math.cos(angle) * pace;
    drift[index * 2 + 1] = Math.sin(angle) * pace;
    velocity[index * 2] = drift[index * 2];
    velocity[index * 2 + 1] = drift[index * 2 + 1];
    sizes[index] = option('size', 3) * (0.6 + Math.random() * 0.8);
    tints[index] = Math.floor(Math.random() * Math.max(1, sources.length));
  };

  const geometry = new BufferGeometry();
  const positionAttribute = new BufferAttribute(position, 3);
  geometry.setAttribute('position', positionAttribute);
  geometry.setAttribute('aSize', new BufferAttribute(sizes, 1));
  geometry.setAttribute('aColor', new BufferAttribute(tints, 1));
  const pointsUniforms = { uColors: { value: list }, uPixelRatio: { value: 1 } };
  const points = new Points(geometry, overlayMaterial({ vertexShader: pointsVertex, fragmentShader: pointsFragment, uniforms: pointsUniforms }));
  points.frustumCulled = false;
  scene.add(points);

  const maxSegments = links ? max * LINKS_PER_PARTICLE : 0;
  const linePositions = new Float32Array(maxSegments * 6);
  const lineAlphas = new Float32Array(maxSegments * 2);
  const lineGeometry = new BufferGeometry();
  const linePositionAttribute = new BufferAttribute(linePositions, 3);
  const lineAlphaAttribute = new BufferAttribute(lineAlphas, 1);
  lineGeometry.setAttribute('position', linePositionAttribute);
  lineGeometry.setAttribute('aAlpha', lineAlphaAttribute);
  if (links) {
    const lines = new LineSegments(lineGeometry, overlayMaterial({ vertexShader: linesVertex, fragmentShader: linesFragment, uniforms: { uColor: { value: list[0] } } }));
    lines.frustumCulled = false;
    scene.add(lines);
  }

  // 按连线距离分格，只比较相邻的格子；格子里的粒子串成链表（head 是每格第一个，next 是同格的下一个，-1 结束），
  // 每帧都要算，不分配新内存
  const next = new Int32Array(max);
  let head = new Int32Array(0);
  function connect() {
    const linkDistance = option('linkDistance', 120);
    const cell = linkDistance;
    const columns = Math.ceil(area.width / cell) + 1;
    const rows = Math.ceil(area.height / cell) + 1;
    if (head.length < columns * rows) head = new Int32Array(columns * rows);
    head.fill(-1, 0, columns * rows);
    // 漂出边界的粒子归到最近的格子
    const cellOf = (value, count) => clamp(Math.floor(value / cell), 0, count - 1);
    for (let index = 0; index < active; index++) {
      const key = cellOf(position[index * 3 + 1], rows) * columns + cellOf(position[index * 3], columns);
      next[index] = head[key];
      head[key] = index;
    }
    let segments = 0;
    for (let index = 0; index < active && segments < maxSegments; index++) {
      const x = position[index * 3];
      const y = position[index * 3 + 1];
      const column = cellOf(x, columns);
      const row = cellOf(y, rows);
      let own = 0;
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          const r = row + dy;
          const c = column + dx;
          if (r < 0 || r >= rows || c < 0 || c >= columns) continue;
          for (let other = head[r * columns + c]; other !== -1; other = next[other]) {
            if (other <= index || own >= LINKS_PER_PARTICLE || segments >= maxSegments) continue;
            const ox = position[other * 3];
            const oy = position[other * 3 + 1];
            const distance = Math.hypot(ox - x, oy - y);
            if (distance > linkDistance) continue;
            const offset = segments * 6;
            linePositions[offset] = x;
            linePositions[offset + 1] = y;
            linePositions[offset + 3] = ox;
            linePositions[offset + 4] = oy;
            const alpha = (1 - distance / linkDistance) * 0.45;
            lineAlphas[segments * 2] = alpha;
            lineAlphas[segments * 2 + 1] = alpha;
            segments++;
            own++;
          }
        }
      }
    }
    lineGeometry.setDrawRange(0, segments * 2);
    linePositionAttribute.needsUpdate = true;
    lineAlphaAttribute.needsUpdate = true;
  }

  /** 按面积与画质档位（或给定的 count）增减粒子：新增的撒在整个区域，已有的落在区域外时重新撒 */
  function populate() {
    const perParticle = links ? 9000 : 2600;
    const wanted = context.options.count ?? Math.round(((area.width * area.height) / perParticle) * quality.density);
    const target = clamp(wanted, 0, max);
    for (let index = 0; index < target; index++) {
      const outside = position[index * 3] > area.width || position[index * 3 + 1] > area.height;
      if (index >= active || outside) seed(index);
    }
    active = target;
    geometry.setDrawRange(0, active);
    positionAttribute.needsUpdate = true;
    geometry.attributes.aSize.needsUpdate = true;
    geometry.attributes.aColor.needsUpdate = true;
    if (links) connect();
  }

  return {
    resize: (size, context) => {
      camera.right = size.width;
      camera.top = size.height;
      camera.updateProjectionMatrix();
      context.camera = camera;
      pointsUniforms.uPixelRatio.value = size.pixelRatio;
      populate();
    },
    update: (options, previous) => {
      // 粒子数、大小变了：重新撒（大小只对新撒的粒子生效，全部重撒）
      if (options.size !== previous.size) active = 0;
      if (options.count !== previous.count || options.size !== previous.size) populate();
    },
    frame: ({ delta }) => {
      if (delta === 0) return;
      const speed = option('speed', 1);
      const repelRadius = option('repelRadius', 140);
      const step = delta * speed;
      const mx = ((pointer.x + 1) / 2) * area.width;
      const my = ((pointer.y + 1) / 2) * area.height;
      const push = option('interactive', true) ? pointer.strength : 0;
      const margin = option('size', 3) * 2;
      for (let index = 0; index < active; index++) {
        let x = position[index * 3];
        let y = position[index * 3 + 1];
        let vx = velocity[index * 2];
        let vy = velocity[index * 2 + 1];
        if (push > 0.01) {
          const dx = x - mx;
          const dy = y - my;
          const distance = Math.hypot(dx, dy) || 1;
          if (distance < repelRadius) {
            const force = (1 - distance / repelRadius) ** 2 * 1400 * push * delta;
            vx += (dx / distance) * force;
            vy += (dy / distance) * force;
          }
        }
        // 排开后慢慢回到自己的漂移速度
        const settle = Math.min(1, delta * 1.6);
        vx += (drift[index * 2] - vx) * settle;
        vy += (drift[index * 2 + 1] - vy) * settle;
        x += vx * step;
        y += vy * step;
        // 出了边界从对边回来
        if (x < -margin) x = area.width + margin;
        else if (x > area.width + margin) x = -margin;
        if (y < -margin) y = area.height + margin;
        else if (y > area.height + margin) y = -margin;
        position[index * 3] = x;
        position[index * 3 + 1] = y;
        velocity[index * 2] = vx;
        velocity[index * 2 + 1] = vy;
      }
      positionAttribute.needsUpdate = true;
      if (links) connect();
    },
  };
}
