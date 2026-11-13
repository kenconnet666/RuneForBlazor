// 模型查看器（ModelViewer，同 vue-tsx-pro 的 three/model/ModelViewer.tsx）：加载 glTF / GLB（或一个 JS 模块生成的 three 对象），
// 自动居中、缩放到合适的大小，拖动环绕查看（松手带惯性），方向键也能转；没在拖时缓缓自转。参考 Google 的 <model-viewer>。
// 光照是一间柔和的摄影棚（RoomEnvironment 的环境光），exposure 调整体亮度。减少动态效果时不自转，拖动仍可用。
// options：src（模型地址）、object（JS 模块的地址：默认导出一个函数，参数是 three 的全部导出，返回 Object3D 或它的 Promise；
// 地址后面写 #名字 时用那个导出）、
// rotationSpeed（度 / 秒）、controls（拖动、方向键）、zoom（滚轮、+ / - 键）、orbit（{ yaw, pitch }，度）、exposure。
// 加载状态写在外层（host）的 data-status（loading / loaded / error）与 aria-busy 上，C# 输出的转圈与出错提示按它显示；
// 加载完、失败时发 rune-command model-load、model-error（文字是原因）。

import * as THREE from '../../vendor/three/three.js';
import { createOrbit } from '../core/orbit.js';

const { ACESFilmicToneMapping, Box3, Group, PerspectiveCamera, PMREMGenerator, Sphere, Vector3 } = THREE;

export const keys = ['src', 'object'];

const DEG = Math.PI / 180;
const FOV = 35;
/** 模型（外接球）占画面短边的比例 */
const FIT = 0.85;
/** 缩放的范围：相对合适距离的倍数 */
const ZOOM_RANGE = [0.5, 2.5];

// 视角与缩放跟着外层元素走：重建场景（换了模型）时不回到一开始的角度
const views = new WeakMap();

function loadModel({ src, object }) {
  if (object) {
    // 地址后面可以跟 #导出名：一个模块里放几个对象
    const [path, name = 'default'] = object.split('#');
    return import(new URL(path, document.baseURI).href).then((module) => module[name](THREE));
  }
  return import('../../vendor/three/addons.js').then(({ GLTFLoader }) => new GLTFLoader().loadAsync(src ?? '')).then((gltf) => gltf.scene);
}

export function setup(context) {
  const { renderer, scene, host, invalidate } = context;
  const options = context.options;
  let view = views.get(host);
  if (!view) {
    const start = options.orbit ?? { yaw: 30, pitch: 15 };
    view = { orbit: createOrbit({ yaw: start.yaw * DEG, pitch: start.pitch * DEG, pitchLimit: 1.4 }), distance: 1 };
    views.set(host, view);
  }
  const { orbit } = view;

  renderer.toneMapping = ACESFilmicToneMapping;
  renderer.toneMappingExposure = options.exposure ?? 1;
  let disposed = false;
  // 摄影棚的环境光：把一间屋子渲染一遍再逐级模糊。没有显卡（软件渲染）时这一步要几秒、占住主线程，一页几个查看器排着做，
  // 后面的连“地址错了”都迟迟报不出来。等模型到了再做：加载失败的不做，加载中的不挡着别人
  let environment = null;
  const lightStudio = () => import('../../vendor/three/addons.js').then(({ RoomEnvironment }) => {
    if (disposed) return;
    environment = new PMREMGenerator(renderer);
    scene.environment = environment.fromScene(new RoomEnvironment(), 0.04).texture;
  });

  const camera = new PerspectiveCamera(FOV, 1, 0.01, 100);
  // 合适的距离：外接球（半径 1）正好占短边的 FIT
  let fit = 1 / Math.sin((FOV * DEG) / 2) / FIT;
  const holder = new Group();
  scene.add(holder);

  const setStatus = (status) => {
    host.dataset.status = status;
    if (status === 'loading') host.setAttribute('aria-busy', 'true');
    else host.removeAttribute('aria-busy');
  };
  setStatus('loading');
  loadModel(options).then(
    async (model) => {
      if (disposed) return;
      await lightStudio();
      if (disposed) return;
      // 居中，缩放到外接球半径为 1
      const sphere = new Box3().setFromObject(model).getBoundingSphere(new Sphere());
      const scale = sphere.radius > 0 ? 1 / sphere.radius : 1;
      holder.scale.setScalar(scale);
      holder.position.copy(sphere.center).multiplyScalar(-scale);
      holder.add(model);
      setStatus('loaded');
      context.send('model-load');
      invalidate();
    },
    (error) => {
      if (disposed) return;
      setStatus('error');
      context.send('model-error', String(error?.message ?? error));
    },
  );

  const eye = new Vector3();
  const place = () => {
    const d = fit * view.distance;
    eye.set(Math.sin(orbit.yaw) * Math.cos(orbit.pitch), Math.sin(orbit.pitch), Math.cos(orbit.yaw) * Math.cos(orbit.pitch));
    camera.position.copy(eye.multiplyScalar(d));
    camera.lookAt(0, 0, 0);
  };

  const zoomBy = (factor) => {
    view.distance = Math.min(ZOOM_RANGE[1], Math.max(ZOOM_RANGE[0], view.distance * factor));
  };
  const onKeydown = (event) => {
    const { controls = true, zoom = false } = context.options;
    if (!controls) return;
    const step = 15 * DEG;
    const moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
    const move = moves[event.key];
    if (move) orbit.nudge(...move);
    else if (zoom && (event.key === '+' || event.key === '=')) zoomBy(1 / 1.15);
    else if (zoom && event.key === '-') zoomBy(1.15);
    else return;
    event.preventDefault();
    invalidate();
  };
  const onWheel = (event) => {
    if (!context.options.zoom) return;
    event.preventDefault();
    zoomBy(Math.exp(event.deltaY * 0.001));
    invalidate();
  };
  host.addEventListener('keydown', onKeydown);
  host.addEventListener('wheel', onWheel, { passive: false });
  let detach = null;
  const syncControls = (controls) => {
    if (controls && !detach) detach = orbit.attach(host, invalidate);
    else if (!controls && detach) {
      detach();
      detach = null;
    }
  };
  syncControls(options.controls ?? true);

  return {
    resize: (size, context) => {
      camera.aspect = size.width / Math.max(size.height, 1);
      fit = 1 / Math.sin((FOV * DEG) / 2) / FIT / Math.min(1, camera.aspect);
      camera.updateProjectionMatrix();
      context.camera = camera;
      place();
    },
    frame: ({ delta }) => {
      // 自转是相机绕模型反向转：看起来模型朝 rotationSpeed 的方向转
      orbit.step(delta, context.still ? 0 : -(context.options.rotationSpeed ?? 20) * DEG);
      place();
    },
    update: (next) => {
      renderer.toneMappingExposure = next.exposure ?? 1;
      syncControls(next.controls ?? true);
    },
    dispose: () => {
      disposed = true;
      host.removeEventListener('keydown', onKeydown);
      host.removeEventListener('wheel', onWheel);
      detach?.();
      scene.environment?.dispose();
      environment?.dispose();
    },
  };
}
