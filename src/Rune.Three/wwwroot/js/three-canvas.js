// three.js 画布（ThreeCanvas 的 data-rune-three，同 vue-tsx-pro 的 three/core/renderer.ts 的 useThree）：渲染器的生命周期。
// C# 只输出外层与降级内容，场景、动画循环、canvas 都在这里，参数变了经属性交过来，不往返：
//
//   <div data-rune-three="globe | 自己的模块地址" data-options="{…}" data-quality="auto" data-paused data-resolution="0.5">
//     <template data-part="canvas" class="…">   canvas 的属性（PropsCanvas），复制到这里建的每块 canvas 上
//     <div data-part="fallback">…</div>         降级内容：根元素的 data-state 是 fallback 时才显示
//   </div>
//
// - data-rune-three 是场景模块：内置的写名字（scenes/{名字}.js），自己的写地址（相对页面的 base，如 js/my-scene.js）；
// - 第一次进入视口时才创建渲染器，占用 WebGL 上下文的名额（core/quality.js）；被回收、上下文丢失、换画质档位、
//   要重建场景时换一块新 canvas 再建，所以场景要在 setup 里搭，不要在外面持有渲染器；
// - 所有画布共用一个 requestAnimationFrame 循环；滚出视口、标签页隐藏、data-paused 时停下；
// - 减少动态效果（系统设置，或主题的时长为 0）时只画一帧静止画面，尺寸、主题色变化时（invalidate）重画；
// - 尺寸跟随布局（ResizeObserver），像素比取设备与画质档位上限的较小值，再乘 data-resolution；
// - 外层被移除、被回收时释放场景里的几何体、材质、纹理和渲染器，并立即交还上下文（forceContextLoss），不等浏览器回收；
//   上下文连续丢失三次后改显示降级内容；
// - 渲染状态写在根元素的 data-state 上（idle / running / paused / still / fallback），并发 rune-command three-status 交给 C#。
//
// 场景模块的约定（内置的效果与使用方自己的场景一样）：
//
//   export const keys = ['palette'];        // 可省：data-options 里这些值变了要重建场景（着色器、色板这类建好后不再改的）
//   export function setup(context) {       // 每次创建渲染器时调用一次
//     context.scene.add(…);
//     return {                              // 都可省
//       frame({ time, delta }, context) {}, // 每帧渲染前（time 只在循环运行时增长；delta 最多 0.1 秒）
//       resize(size, context) {},           // 尺寸变了（透视相机的宽高比已更新）
//       update(options, previous, context) {}, // data-options 变了（keys 以外的）：之后会重画一帧
//       dispose() {},                       // 释放前：清理自己加的监听、加载器；场景里的对象由这里统一释放
//     };
//   }
//
// context：three（three.js 的全部导出：自己的模块不用算 import 的路径）、renderer、scene、camera（默认透视相机，视角 45°，位于 z = 5；可以换）、canvas、root（data-rune-three 的元素）、
// host（外层的效果元素，EffectSurface 的根，没有时是 root）、size（width、height、pixelRatio）、quality（level、pixelRatio、density、antialias）、
// still（只画静止画面）、dark（暗色主题）、pointer（x、y、strength，见 core/pointer.js）、options（data-options 解析出的对象，始终是最新的）、
// invalidate()（循环停着时请求重画一帧）、themeColors(sources)、themeColorList(sources, size)（见 core/theme-colors.js）、
// send(name, text)（从 host 发 rune-command，交给外层组件的 C#）。

import * as THREE from '../vendor/three/three.js';
import { reducedMotion, sendCommand } from '../../RuneForBlazor.UI/js/core/shared.js';
import { disposeObject } from './core/dispose.js';
import { createPointer } from './core/pointer.js';
import { acquireContext, releaseContext, resolveThreeQuality, retryWaiting } from './core/quality.js';
import { createThemeColorSet } from './core/theme-colors.js';

/** 上下文连续丢失这么多次后不再重建，改显示降级内容 */
const MAX_CONTEXT_LOSSES = 3;

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

// 所有画布共用一个动画帧循环
const tickers = new Set();
let tickerFrame = 0;

function runTickers(now) {
  tickerFrame = 0;
  for (const tick of [...tickers]) tick(now);
  if (tickers.size) tickerFrame = requestAnimationFrame(runTickers);
}

function subscribe(tick) {
  tickers.add(tick);
  tickerFrame ||= requestAnimationFrame(runTickers);
}

/** 正在管理的画布：根元素 → 控制器 */
const controllers = new Map();

const visibility = typeof IntersectionObserver === 'undefined' ? null : new IntersectionObserver((entries) => {
  for (const entry of entries) controllers.get(entry.target)?.setVisible(entry.isIntersecting);
});
const sizes = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver((entries) => {
  for (const entry of entries) controllers.get(entry.target)?.resize();
});

// 外层被移除（换页、条件渲染）：释放。主题变了（根元素的 data-theme、局部 RuneProvider 的主题类、主题定制写的变量）：重新读颜色。
// 同一帧里的变化攒到一起处理
let changedThemes = new Set();
let pending = 0;
function onDocumentMutations(mutations) {
  for (const mutation of mutations) if (mutation.type === 'attributes') changedThemes.add(mutation.target);
  pending ||= requestAnimationFrame(() => {
    pending = 0;
    const changed = [...changedThemes];
    changedThemes = new Set();
    for (const [root, controller] of controllers) {
      if (!root.isConnected) controller.destroy();
      else if (changed.some((target) => target !== root && target.contains(root))) controller.onTheme();
    }
  });
}
let documentObserver = null;

reducedMotion.addEventListener('change', () => controllers.forEach((controller) => controller.onTheme()));
document.addEventListener('visibilitychange', () => controllers.forEach((controller) => controller.update()));

const BUILTIN = /^[a-z][a-z-]*$/;
const sceneUrl = (value) => BUILTIN.test(value) ? new URL(`./scenes/${value}.js`, import.meta.url).href : new URL(value, document.baseURI).href;

/** 主题的时长为 0（主题编辑器关掉了动效，或系统要求减少动态效果）：读不到主题时当作有动效 */
function themeMotionOff(element) {
  const value = getComputedStyle(element).getPropertyValue('--rune-durations-md').trim();
  return value !== '' && parseFloat(value) === 0;
}

/** 元素所在位置是不是暗色主题（color-scheme 只写了 dark，或写了 light dark 而系统是暗色） */
function isDark(element) {
  const scheme = getComputedStyle(element).colorScheme;
  return scheme.includes('dark') && (!scheme.includes('light') || matchMedia('(prefers-color-scheme: dark)').matches);
}

/** 开始管理一个画布（插入时调用；重复调用无副作用） */
export function watchThree(root) {
  if (controllers.has(root)) return;
  documentObserver ??= new MutationObserver(onDocumentMutations);
  if (controllers.size === 0) {
    documentObserver.observe(document.documentElement, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-theme', 'class', 'style'] });
  }
  controllers.set(root, createController(root));
}

function readOptions(root) {
  try {
    return JSON.parse(root.dataset.options || '{}');
  } catch {
    return {};
  }
}

function createController(root) {
  const host = root.closest('[data-rune-three-host]') ?? root;
  let options = readOptions(root);
  let module = null;
  let canvas = null;
  let instance = null;
  let status = null;
  let visible = visibility === null;
  let lastSeen = 0;
  let losses = 0;
  let destroyed = false;
  // 创建失败或上下文反复丢失：换画质也不再重试（区别于 off 的降级）
  let unsupported = false;
  let themeStill = themeMotionOff(root);
  let dark = isDark(root);
  const pointer = createPointer(() => canvas);

  const quality = () => root.dataset.quality || 'auto';
  const paused = () => root.hasAttribute('data-paused');
  const resolution = () => clamp(Number(root.dataset.resolution) || 1, 0.1, 1);
  const still = () => reducedMotion.matches || themeStill;

  const holder = {
    get visible() {
      return visible;
    },
    get lastSeen() {
      return lastSeen;
    },
    evict() {
      teardown();
      mountCanvas();
      setStatus('idle');
    },
    retry: () => start(),
  };

  function setStatus(next) {
    if (status === next) return;
    status = next;
    root.dataset.state = next;
    sendCommand(root, 'three-status', next);
  }

  /** 换一块新 canvas（丢失过上下文的 canvas 拿不回新的上下文，抗锯齿也只能在建上下文时定）：属性取自 template[data-part=canvas] */
  function mountCanvas() {
    canvas?.remove();
    canvas = document.createElement('canvas');
    for (const { name, value } of template()?.attributes ?? []) canvas.setAttribute(name, value);
    canvas.setAttribute('aria-hidden', 'true');
    root.prepend(canvas);
  }

  const template = () => root.querySelector(':scope > template[data-part="canvas"]');

  function toFallback() {
    teardown();
    canvas?.remove();
    canvas = null;
    setStatus('fallback');
  }

  function draw(target, delta) {
    target.handle.frame?.({ time: target.time, delta }, target.context);
    target.renderer.render(target.context.scene, target.context.camera);
  }

  function tick(now) {
    const target = instance;
    if (!target) return;
    // 外层已经不在页面上（下一帧才轮到移除的检查）：不再画
    if (!root.isConnected) return;
    const delta = target.last ? Math.min((now - target.last) / 1000, 0.1) : 0;
    target.last = now;
    target.time += delta;
    pointer.step(delta);
    draw(target, delta);
  }

  function invalidate() {
    const target = instance;
    if (!target || target.ticking || target.pending) return;
    target.pending = requestAnimationFrame(() => {
      target.pending = 0;
      if (instance === target) draw(target, 0);
    });
  }

  function resize() {
    const target = instance;
    if (!target) return;
    const { context, renderer, handle } = target;
    const size = context.size;
    size.width = canvas.clientWidth;
    size.height = canvas.clientHeight;
    size.pixelRatio = Math.min(devicePixelRatio || 1, context.quality.pixelRatio) * resolution();
    renderer.setPixelRatio(size.pixelRatio);
    renderer.setSize(size.width, size.height, false);
    const camera = context.camera;
    if (camera.isPerspectiveCamera && size.height > 0) {
      camera.aspect = size.width / size.height;
      camera.updateProjectionMatrix();
    }
    handle.resize?.(size, context);
    invalidate();
  }

  function create(level) {
    const resolved = resolveThreeQuality(level);
    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: resolved.antialias });
    renderer.setClearColor(0x000000, 0);
    const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 100);
    camera.position.z = 5;
    const colorSets = [];
    const context = {
      three: THREE,
      renderer,
      scene: new THREE.Scene(),
      camera,
      canvas,
      root,
      host,
      size: { width: 0, height: 0, pixelRatio: 1 },
      quality: resolved,
      pointer,
      get still() {
        return still();
      },
      get dark() {
        return dark;
      },
      get options() {
        return options;
      },
      invalidate,
      themeColors(sources) {
        const set = createThemeColorSet(invalidate);
        set.refresh(canvas, sources, false);
        colorSets.push({ set, sources });
        return set.colors;
      },
      themeColorList(sources, size) {
        const used = size == null ? sources : sources.slice(0, size);
        const themed = context.themeColors(Object.fromEntries(used.map((color, index) => [index, color])));
        const list = used.map((_, index) => themed[index]);
        while (size != null && list.length < size) list.push(list.at(-1));
        return list;
      },
      send(name, text = null) {
        sendCommand(host, name, text);
      },
    };
    const target = { renderer, context, handle: {}, canvas, time: 0, last: 0, pending: 0, ticking: false, colorSets };
    target.onLost = (event) => lose(target, event);
    canvas.addEventListener('webglcontextlost', target.onLost);
    return target;
  }

  function start() {
    if (destroyed || instance || !module || !canvas || !visible || status === 'fallback') return;
    const level = quality();
    if (level === 'off') return;
    if (!acquireContext(holder)) return;
    try {
      instance = create(level);
    } catch {
      // 没有 WebGL（浏览器禁用、驱动黑名单、测试环境）
      releaseContext(holder);
      unsupported = true;
      toFallback();
      return;
    }
    try {
      instance.handle = module.setup(instance.context) ?? {};
    } catch (error) {
      unsupported = true;
      toFallback();
      reportError(error);
      return;
    }
    resize();
    update();
  }

  function teardown() {
    const target = instance;
    if (!target) return;
    instance = null;
    tickers.delete(tick);
    cancelAnimationFrame(target.pending);
    target.canvas.removeEventListener('webglcontextlost', target.onLost);
    for (const { set } of target.colorSets) set.stop();
    try {
      target.handle.dispose?.();
    } catch (error) {
      reportError(error);
    }
    disposeObject(target.context.scene);
    target.renderer.dispose();
    if (!target.renderer.getContext().isContextLost()) target.renderer.forceContextLoss();
    releaseContext(holder);
  }

  function lose(target) {
    if (instance !== target) return;
    teardown();
    losses++;
    unsupported = losses >= MAX_CONTEXT_LOSSES;
    if (unsupported) {
      toFallback();
      return;
    }
    mountCanvas();
    setStatus('idle');
    start();
  }

  /** 按可见性、暂停、静止决定循环开还是停 */
  function update() {
    const target = instance;
    if (!target) return;
    const mode = still() ? 'still' : paused() || !visible || document.hidden ? 'paused' : 'running';
    target.ticking = mode === 'running';
    if (target.ticking) {
      target.last = 0;
      subscribe(tick);
    } else {
      tickers.delete(tick);
      if (mode === 'still') invalidate();
    }
    setStatus(mode);
  }

  /** 场景要重建：换一块 canvas 从头建（setup 再调用一次） */
  function rebuild() {
    if (status === 'fallback') return;
    teardown();
    mountCanvas();
    setStatus('idle');
    start();
  }

  function load(name) {
    import(sceneUrl(name)).then(
      (loaded) => {
        if (destroyed || root.dataset.runeThree !== name) return;
        module = loaded;
        start();
      },
      (error) => {
        if (destroyed) return;
        unsupported = true;
        toFallback();
        reportError(error);
      },
    );
  }

  function onAttribute(name) {
    if (name === 'data-paused') update();
    else if (name === 'data-resolution') resize();
    else if (name === 'data-rune-three') {
      module = null;
      rebuild();
      load(root.dataset.runeThree);
    } else if (name === 'data-quality') {
      if (unsupported) return;
      if (quality() === 'off') toFallback();
      else if (instance || status === 'fallback') {
        // 换档重建（抗锯齿只能在建上下文时定）；从 off 回来时重新挂上 canvas，再按可见性创建
        teardown();
        mountCanvas();
        setStatus('idle');
        start();
      }
    } else if (name === 'data-options') {
      const previous = options;
      options = readOptions(root);
      const changed = (key) => JSON.stringify(options[key]) !== JSON.stringify(previous[key]);
      if (module?.keys?.some(changed)) rebuild();
      else if (instance) {
        instance.handle.update?.(options, previous, instance.context);
        invalidate();
      }
    }
  }

  const attributes = new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      // canvas 的属性（PropsCanvas）变了：当前的 canvas 跟着改
      if (mutation.target !== root) {
        if (canvas && mutation.attributeName !== 'aria-hidden') {
          const value = mutation.target.getAttribute(mutation.attributeName);
          if (value == null) canvas.removeAttribute(mutation.attributeName);
          else canvas.setAttribute(mutation.attributeName, value);
        }
        continue;
      }
      onAttribute(mutation.attributeName);
    }
  });
  attributes.observe(root, { attributes: true, attributeFilter: ['data-rune-three', 'data-options', 'data-quality', 'data-paused', 'data-resolution'] });
  const canvasTemplate = template();
  if (canvasTemplate) attributes.observe(canvasTemplate, { attributes: true });

  // 顶层显示的效果（Confetti 铺满视口）：外层是原生 popover，打开了画布才看得见、才进入视口。
  // 顶层里后进的在上面：同一刻打开的对话框会盖住它，等这一帧过去再进顶层
  const layer = root.closest('[data-rune-three-layer][popover]');
  if (layer) {
    requestAnimationFrame(() => {
      if (layer.isConnected && !layer.matches(':popover-open')) layer.showPopover();
    });
  }

  if (quality() === 'off') setStatus('fallback');
  else {
    mountCanvas();
    setStatus('idle');
  }
  visibility?.observe(root);
  sizes?.observe(root);
  load(root.dataset.runeThree);

  return {
    setVisible(shown) {
      visible = shown;
      lastSeen = performance.now();
      if (instance) update();
      else start();
      // 滚出视口的效果可以被回收：排队的效果再试
      if (!shown && instance) retryWaiting();
    },
    resize,
    update,
    onTheme() {
      themeStill = themeMotionOff(root);
      dark = isDark(root);
      for (const { set, sources } of instance?.colorSets ?? []) set.refresh(canvas, sources, true);
      invalidate();
      update();
    },
    destroy() {
      destroyed = true;
      teardown();
      releaseContext(holder);
      attributes.disconnect();
      visibility?.unobserve(root);
      sizes?.unobserve(root);
      controllers.delete(root);
      if (controllers.size === 0) documentObserver?.disconnect();
    },
  };
}
