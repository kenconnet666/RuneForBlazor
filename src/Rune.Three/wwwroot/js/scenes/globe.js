// 点阵地球（Globe，同 vue-tsx-pro 的 three/globe/Globe.tsx）：陆地画成一层细密的点，可以标注地点（markers，向外扩散的光圈），
// 两地之间画弧线（arcs，光点沿弧线流动）；缓缓自转，可以拖动转动（松手后带惯性）。参考 GitHub 首页的地球、cobe。
// 陆地来自 Natural Earth 1:110m 的世界地图（1° 的位图，见 land.js）；点的疏密按画质档位。减少动态效果时停在一帧，不自转、弧线不流动。
// options：markers（[{ lat, lng, size?, color? }]）、arcs（[{ from: [lat, lng], to: [lat, lng] }]）、center（{ lat, lng }，一开始正对的地方）、
// rotationSpeed（度 / 秒，向东为正）、draggable、density、landColor、markerColor、glow。

import { BufferAttribute, BufferGeometry, Group, LineSegments, Mesh, PerspectiveCamera, PlaneGeometry, Points, ShaderMaterial, Vector3 } from '../../vendor/three/three.js';
import { createOrbit } from '../core/orbit.js';
import { LAND_BITS, LAND_HEIGHT, LAND_WIDTH } from './land.js';

/** 这些变了重建场景（颜色在 setup 里读一次） */
export const keys = ['density', 'glow', 'landColor', 'markerColor', 'markers', 'arcs'];

const DEG = Math.PI / 180;
/** 地球的轮廓占画面短边的比例：外面留给大气光晕 */
const FIT = 0.8;
const FOV = 40;

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

/** 经纬度 → 球面上的点（半径 radius）：经度 0 朝向 +z（正对相机），北极朝 +y */
export function latLngToVector(lat, lng, radius = 1, target = new Vector3()) {
  const phi = lat * DEG;
  const theta = lng * DEG;
  return target.set(radius * Math.cos(phi) * Math.sin(theta), radius * Math.sin(phi), radius * Math.cos(phi) * Math.cos(theta));
}

let bytes;
/** 陆地位图：某个经纬度是不是陆地 */
function isLand(lat, lng) {
  bytes ??= Uint8Array.from(atob(LAND_BITS), (char) => char.charCodeAt(0));
  const row = clamp(Math.floor(90 - lat), 0, LAND_HEIGHT - 1);
  const column = (((Math.floor(lng + 180) % LAND_WIDTH) + LAND_WIDTH) % LAND_WIDTH) | 0;
  const index = row * LAND_WIDTH + column;
  return (bytes[index >> 3] & (1 << (index & 7))) !== 0;
}

// 点：正面清楚、转到背面淡出；大小随透视缩放
const dotVertex = /* glsl */ `
uniform float uPixelRatio;
uniform float uScale;
attribute float aSize;
attribute float aPhase;
varying float vFacing;
varying float vPhase;
void main() {
  vec4 world = modelMatrix * vec4(position, 1.0);
  vFacing = dot(normalize(world.xyz), normalize(cameraPosition - world.xyz));
  vPhase = aPhase;
  vec4 view = viewMatrix * world;
  gl_PointSize = aSize * uScale * uPixelRatio / -view.z;
  gl_Position = projectionMatrix * view;
}
`;

const landFragment = /* glsl */ `
uniform vec3 uColor;
varying float vFacing;
void main() {
  float d = length(gl_PointCoord - 0.5);
  float alpha = smoothstep(0.5, 0.3, d) * smoothstep(-0.05, 0.35, vFacing) * 0.85;
  gl_FragColor = vec4(uColor, alpha);
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;

// 标注：实心的点加一圈向外扩散、渐淡的光圈
const markerFragment = /* glsl */ `
uniform vec3 uColor;
uniform float uTime;
varying float vFacing;
varying float vPhase;
void main() {
  float d = length(gl_PointCoord - 0.5) * 2.0;
  float core = smoothstep(0.3, 0.2, d);
  float wave = fract(uTime * 0.6 + vPhase);
  float ring = smoothstep(0.08, 0.0, abs(d - wave)) * (1.0 - wave);
  float alpha = max(core, ring * 0.9) * smoothstep(-0.1, 0.25, vFacing);
  gl_FragColor = vec4(uColor, alpha);
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;

// 弧线：整条淡淡的底色，加一段从起点流向终点的亮光
const arcVertex = /* glsl */ `
attribute float aT;
attribute float aPhase;
varying float vT;
varying float vPhase;
varying float vFacing;
void main() {
  vec4 world = modelMatrix * vec4(position, 1.0);
  vFacing = dot(normalize(world.xyz), normalize(cameraPosition - world.xyz));
  vT = aT;
  vPhase = aPhase;
  gl_Position = projectionMatrix * viewMatrix * world;
}
`;

const arcFragment = /* glsl */ `
uniform vec3 uColor;
uniform float uTime;
varying float vT;
varying float vPhase;
varying float vFacing;
void main() {
  float head = fract(uTime * 0.35 + vPhase) * 1.4 - 0.2;
  float glow = exp(-pow((vT - head) * 7.0, 2.0));
  float alpha = (0.18 + 0.82 * glow) * smoothstep(-0.25, 0.2, vFacing);
  gl_FragColor = vec4(uColor, alpha);
  #include <colorspace_fragment>
  #include <premultiplied_alpha_fragment>
}
`;

// 大气：正对相机的一块方形，按到球心的距离画径向的光（单位是地球半径，uEdge 是轮廓所在的半径）：
// 球面里只有一层淡淡的雾，轮廓外一圈亮边向外淡出
const glowVertex = /* glsl */ `
varying vec2 vPosition;
void main() {
  vPosition = position.xy;
  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
}
`;

const glowFragment = /* glsl */ `
uniform vec3 uColor;
uniform float uEdge;
uniform float uLimit;
varying vec2 vPosition;
void main() {
  float r = length(vPosition) / uEdge;
  float inner = smoothstep(0.55, 1.0, r) * 0.22;
  // 到画布边缘（短边方向在 uLimit 处）之前淡完，不留方形的边
  float outer = exp(-(r - 1.0) * 9.0) * smoothstep(uLimit, uLimit - 0.12, r);
  float intensity = r < 1.0 ? inner : outer * 0.75;
  // 先转色彩空间再乘强度（预乘）：反过来的话，sRGB 会把很淡的颜色提亮，方形的边缘就露出来了
  gl_FragColor = vec4(uColor, 1.0);
  #include <colorspace_fragment>
  gl_FragColor = vec4(gl_FragColor.rgb, 1.0) * intensity;
}
`;

/** 透明叠加的着色器材质 */
const overlay = (params) => new ShaderMaterial({ ...params, transparent: true, premultipliedAlpha: true, depthWrite: false });

/** 两地之间的大圆弧，抬离地面（越远抬得越高），采样成 segments 段 */
function arcPoints({ from, to }, segments) {
  const a = latLngToVector(from[0], from[1]);
  const b = latLngToVector(to[0], to[1]);
  const angle = a.angleTo(b);
  const lift = 0.06 + angle * 0.22;
  const points = [];
  for (let i = 0; i <= segments; i++) {
    const t = i / segments;
    // 球面线性插值（slerp），再按 sin 抬高
    const point = angle < 1e-6
      ? a.clone()
      : a.clone().multiplyScalar(Math.sin((1 - t) * angle) / Math.sin(angle)).add(b.clone().multiplyScalar(Math.sin(t * angle) / Math.sin(angle)));
    points.push(point.normalize().multiplyScalar(1 + lift * Math.sin(Math.PI * t)));
  }
  return points;
}

// 朝向跟着外层元素走：重建场景（换了标注、颜色）时不回到一开始的角度
const orbits = new WeakMap();

export function setup(context) {
  const { scene, quality, themeColors, host } = context;
  const options = context.options;
  const landColor = options.landColor ?? 'var(--rune-colors-text-muted)';
  const markerColor = options.markerColor ?? 'var(--rune-tones-accent-solid)';
  const markers = options.markers ?? [];
  const arcs = options.arcs ?? [];
  const center = options.center ?? { lat: 20, lng: 100 };
  // 一开始正对 center；拖动左右改 yaw、上下改 pitch，松手后带惯性回到自转
  let orbit = orbits.get(host);
  if (!orbit) {
    orbit = createOrbit({ yaw: -center.lng * DEG, pitch: center.lat * DEG });
    orbits.set(host, orbit);
  }
  let detach = null;
  const syncDraggable = (draggable) => {
    if (draggable && !detach) detach = orbit.attach(host, context.invalidate);
    else if (!draggable && detach) {
      detach();
      detach = null;
    }
  };
  syncDraggable(options.draggable ?? true);

  const camera = new PerspectiveCamera(FOV, 1, 0.1, 100);
  const palette = themeColors({ land: landColor, marker: markerColor });
  // 标注按颜色分组：同色的一次画完
  const byColor = new Map();
  for (const marker of markers) {
    const color = marker.color ?? markerColor;
    byColor.set(color, [...(byColor.get(color) ?? []), marker]);
  }
  const markerTints = themeColors(Object.fromEntries([...byColor.keys()].map((color, index) => [index, color])));

  const globe = new Group();
  scene.add(globe);
  const uniforms = { uPixelRatio: { value: 1 }, uScale: { value: 1 }, uTime: { value: 0 } };

  // 陆地：斐波那契球面上均匀撒点，落在陆地上的留下
  const candidates = Math.round(24_000 * (options.density ?? 1) * Math.min(1, quality.density));
  const land = [];
  const golden = Math.PI * (3 - Math.sqrt(5));
  const at = new Vector3();
  for (let i = 0; i < candidates; i++) {
    const y = 1 - (2 * (i + 0.5)) / candidates;
    const lat = Math.asin(y) / DEG;
    const lng = (((((i * golden) / DEG) % 360) + 360) % 360) - 180;
    if (!isLand(lat, lng)) continue;
    latLngToVector(lat, lng, 1, at);
    land.push(at.x, at.y, at.z);
  }
  const landGeometry = new BufferGeometry();
  landGeometry.setAttribute('position', new BufferAttribute(new Float32Array(land), 3));
  const landCount = land.length / 3;
  landGeometry.setAttribute('aSize', new BufferAttribute(new Float32Array(landCount).fill(2.2), 1));
  landGeometry.setAttribute('aPhase', new BufferAttribute(new Float32Array(landCount), 1));
  globe.add(new Points(landGeometry, overlay({ vertexShader: dotVertex, fragmentShader: landFragment, uniforms: { ...uniforms, uColor: { value: palette.land } } })));

  // 标注：实心点加扩散的光圈
  [...byColor.values()].forEach((list, group) => {
    const positions = new Float32Array(list.length * 3);
    list.forEach((marker, i) => latLngToVector(marker.lat, marker.lng, 1.005, at).toArray(positions, i * 3));
    const geometry = new BufferGeometry();
    geometry.setAttribute('position', new BufferAttribute(positions, 3));
    geometry.setAttribute('aSize', new BufferAttribute(Float32Array.from(list, (marker) => 22 * (marker.size ?? 1)), 1));
    geometry.setAttribute('aPhase', new BufferAttribute(Float32Array.from(list, (_, i) => (i * 0.37) % 1), 1));
    globe.add(new Points(geometry, overlay({ vertexShader: dotVertex, fragmentShader: markerFragment, uniforms: { ...uniforms, uColor: { value: markerTints[group] } } })));
  });

  // 弧线：所有弧线放在同一组线段里，一次画完
  if (arcs.length) {
    const SEGMENTS = 64;
    const positions = [];
    const ts = [];
    const phases = [];
    arcs.forEach((arc, index) => {
      const points = arcPoints(arc, SEGMENTS);
      const phase = (index * 0.29) % 1;
      for (let i = 0; i < SEGMENTS; i++) {
        positions.push(...points[i].toArray(), ...points[i + 1].toArray());
        ts.push(i / SEGMENTS, (i + 1) / SEGMENTS);
        phases.push(phase, phase);
      }
    });
    const geometry = new BufferGeometry();
    geometry.setAttribute('position', new BufferAttribute(new Float32Array(positions), 3));
    geometry.setAttribute('aT', new BufferAttribute(new Float32Array(ts), 1));
    geometry.setAttribute('aPhase', new BufferAttribute(new Float32Array(phases), 1));
    globe.add(new LineSegments(geometry, overlay({ vertexShader: arcVertex, fragmentShader: arcFragment, uniforms: { uColor: { value: palette.marker }, uTime: uniforms.uTime } })));
  }

  // 透视下球的轮廓比半径大一点：相机距离 d 时在 z = 0 平面上是 d / √(d² − 1)
  const edge = { value: 1 };
  if (options.glow ?? true) {
    // 不随地球转：相机在 z 轴上，xy 平面上的方形总是正对相机
    const atmosphere = new Mesh(new PlaneGeometry(3, 3), overlay({ vertexShader: glowVertex, fragmentShader: glowFragment, uniforms: { uColor: { value: palette.marker }, uEdge: edge, uLimit: { value: 1 / FIT } } }));
    atmosphere.renderOrder = -1;
    scene.add(atmosphere);
  }

  const apply = () => globe.rotation.set(orbit.pitch, orbit.yaw, 0, 'XYZ');
  apply();

  return {
    resize: (size, context) => {
      camera.aspect = size.width / Math.max(size.height, 1);
      // 轮廓的直径占短边的 FIT：相机距离 d 处的轮廓半径是 d / √(d² − 1)，要它等于 FIT · d · tan(fov / 2)，解出 d（宽比高窄时再拉远）
      const t = FIT * Math.tan((FOV * DEG) / 2);
      const fitHeight = Math.sqrt(1 + 1 / (t * t));
      camera.position.z = fitHeight * Math.max(1, 1 / camera.aspect);
      camera.updateProjectionMatrix();
      context.camera = camera;
      const d = camera.position.z;
      edge.value = d / Math.sqrt(d * d - 1);
      uniforms.uPixelRatio.value = size.pixelRatio;
      // 点的大小（CSS 像素）在 z = 相机距离处不变：/ -view.z 的透视缩放乘回来，再按画布大小缩放
      uniforms.uScale.value = (camera.position.z * Math.min(size.width, size.height)) / 360;
    },
    frame: ({ time, delta }) => {
      uniforms.uTime.value = time;
      // 静止画面里拖动照样转（拖动时请求重画），只是不自转
      orbit.step(delta, context.still ? 0 : (context.options.rotationSpeed ?? 6) * DEG);
      apply();
    },
    update: (next) => syncDraggable(next.draggable ?? true),
    dispose: () => detach?.(),
  };
}
