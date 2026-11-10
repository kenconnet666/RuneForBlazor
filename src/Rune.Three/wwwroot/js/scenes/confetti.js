// 彩纸（Confetti，同 vue-tsx-pro 的 three/effects/Confetti.tsx）：彩纸从 origin 喷出，翻转着飘落后消失。参考 canvas-confetti 的 origin、angle、spread。
// C# 只在彩纸还在飘的时候才输出画布（不放的时候不占 WebGL 上下文）：每触发一次往 options.bursts 里加一批（带递增的 id），
// 这里放出还没放过的批次；全部落完后发 rune-command confetti-done（文字是放过的最大 id：之后才加的批次 C# 留着再放），C# 收起画布。
// 减少动态效果时不放，直接发 confetti-done。
// options：bursts（[{ id, origin: { x, y }（0–1，x 从左到右、y 从上到下）, angle（度，90 正上方）, spread, count, velocity, gravity }]）、palette。

import { Color, DoubleSide, InstancedMesh, Matrix4, MeshBasicMaterial, OrthographicCamera, PlaneGeometry, Quaternion, Vector3 } from '../../vendor/three/three.js';

export const keys = ['palette'];

/** 同时在飘的彩纸上限 */
const MAX_PIECES = 900;
/** 空气阻力（每秒速度剩下 e^-DRAG）：喷出后很快慢下来，落得轻飘 */
const DRAG = 2.6;
/** 一片彩纸飘多久（秒），最后一段缩小消失 */
const LIFETIME = [3.2, 4.6];
const SHRINK = 0.6;
/** 重力：每秒加速多少个区域高度 */
const GRAVITY = 1.1;

const random = (min, max) => min + Math.random() * (max - min);

// 放过的批次（最大的 id）跟着外层元素走：换色板重建场景时不重放
const launched = new WeakMap();

export function setup(context) {
  const { scene, size: area, themeColorList, host } = context;
  if (context.still) {
    launched.set(host, Math.max(launched.get(host) ?? 0, ...(context.options.bursts ?? []).map((burst) => burst.id)));
    context.send('confetti-done', String(launched.get(host)));
    return;
  }
  const camera = new OrthographicCamera(0, 1, 1, 0, -500, 500);
  const palette = context.options.palette ?? [];
  const tints = themeColorList(palette.length ? palette : ['var(--rune-tones-accent-solid)']);

  const mesh = new InstancedMesh(new PlaneGeometry(1, 1), new MeshBasicMaterial({ side: DoubleSide }), MAX_PIECES);
  mesh.frustumCulled = false;
  mesh.count = 0;
  scene.add(mesh);

  // 每片的状态：位置、速度、尺寸、翻转轴与角度、摆动、寿命、颜色
  const position = new Float32Array(MAX_PIECES * 2);
  const speed = new Float32Array(MAX_PIECES * 2);
  const shape = new Float32Array(MAX_PIECES * 2);
  const axis = new Float32Array(MAX_PIECES * 3);
  const turn = new Float32Array(MAX_PIECES * 2);
  const sway = new Float32Array(MAX_PIECES * 2);
  const life = new Float32Array(MAX_PIECES * 2);
  const fall = new Float32Array(MAX_PIECES);
  const tint = new Uint8Array(MAX_PIECES);
  let alive = 0;
  let finished = false;

  function launch(burst) {
    const x0 = burst.origin.x * area.width;
    const y0 = (1 - burst.origin.y) * area.height;
    // 初速度与重力都按区域高度（H）：大小不同的区域里看起来一样，最快的升到 0.9H 左右、最慢的 0.3H
    const base = area.height * 3.2 * burst.velocity;
    const direction = new Vector3();
    for (let n = 0; n < burst.count && alive < MAX_PIECES; n++, alive++) {
      const i = alive;
      const angle = ((burst.angle + random(-burst.spread / 2, burst.spread / 2)) * Math.PI) / 180;
      const v = base * random(0.45, 1);
      position[i * 2] = x0;
      position[i * 2 + 1] = y0;
      speed[i * 2] = Math.cos(angle) * v;
      speed[i * 2 + 1] = Math.sin(angle) * v;
      const width = random(7, 12);
      shape[i * 2] = width;
      shape[i * 2 + 1] = width * random(0.45, 0.8);
      direction.set(random(-1, 1), random(-1, 1), random(-1, 1)).normalize();
      axis.set([direction.x, direction.y, direction.z], i * 3);
      turn[i * 2] = random(0, Math.PI * 2);
      turn[i * 2 + 1] = random(6, 14) * (Math.random() < 0.5 ? -1 : 1);
      sway[i * 2] = random(0, Math.PI * 2);
      sway[i * 2 + 1] = random(20, 60);
      life[i * 2] = 0;
      life[i * 2 + 1] = random(...LIFETIME);
      fall[i] = burst.gravity;
      tint[i] = Math.floor(Math.random() * tints.length);
    }
  }

  /** 还没放过的批次：画布刚建好的第一帧可能还没量到尺寸，量到了再放 */
  function launchPending() {
    if (area.height <= 0) return;
    const last = launched.get(host) ?? 0;
    let latest = last;
    for (const burst of context.options.bursts ?? []) {
      if (burst.id <= last) continue;
      launch(burst);
      latest = Math.max(latest, burst.id);
    }
    if (latest > last) {
      launched.set(host, latest);
      finished = false;
    }
  }

  /** 把第 to 片换成第 from 片（落完的那片用最后一片补上，活着的始终在前面） */
  function move(from, to) {
    for (const [array, size] of [[position, 2], [speed, 2], [shape, 2], [axis, 3], [turn, 2], [sway, 2], [life, 2]]) {
      array.copyWithin(to * size, from * size, from * size + size);
    }
    fall[to] = fall[from];
    tint[to] = tint[from];
  }

  const matrix = new Matrix4();
  const rotation = new Quaternion();
  const scale = new Vector3();
  const at = new Vector3();
  const normal = new Vector3();
  const shade = new Color();

  return {
    resize: (size, context) => {
      camera.right = size.width;
      camera.top = size.height;
      camera.updateProjectionMatrix();
      context.camera = camera;
    },
    frame: ({ delta }) => {
      launchPending();
      const damping = Math.exp(-DRAG * delta);
      for (let i = 0; i < alive; i++) {
        life[i * 2] += delta;
        const age = life[i * 2];
        const lifetime = life[i * 2 + 1];
        if (age >= lifetime || position[i * 2 + 1] < -40) {
          move(--alive, i--);
          continue;
        }
        speed[i * 2] *= damping;
        speed[i * 2 + 1] = speed[i * 2 + 1] * damping - area.height * GRAVITY * fall[i] * delta;
        // 左右摆动：像纸片在空气里飘
        sway[i * 2] += delta * 4;
        position[i * 2] += (speed[i * 2] + Math.sin(sway[i * 2]) * sway[i * 2 + 1]) * delta;
        position[i * 2 + 1] += speed[i * 2 + 1] * delta;
        turn[i * 2] += turn[i * 2 + 1] * delta;

        rotation.setFromAxisAngle(at.set(axis[i * 3], axis[i * 3 + 1], axis[i * 3 + 2]), turn[i * 2]);
        const fade = Math.min(1, (lifetime - age) / SHRINK);
        scale.set(shape[i * 2] * fade, shape[i * 2 + 1] * fade, 1);
        matrix.compose(at.set(position[i * 2], position[i * 2 + 1], 0), rotation, scale);
        mesh.setMatrixAt(i, matrix);
        // 翻到侧面时暗一些：正对屏幕最亮，看起来是在翻转的纸片
        const facing = Math.abs(normal.set(0, 0, 1).applyQuaternion(rotation).z);
        mesh.setColorAt(i, shade.copy(tints[tint[i]]).multiplyScalar(0.55 + 0.45 * facing));
      }
      mesh.count = alive;
      mesh.instanceMatrix.needsUpdate = true;
      if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
      // 都落完了（并且放过东西）：交给 C# 收起画布
      if (alive === 0 && !finished && area.height > 0 && delta > 0) {
        finished = true;
        context.send('confetti-done', String(launched.get(host) ?? 0));
      }
    },
  };
}
