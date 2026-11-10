// 3D 效果的画质档位与 WebGL 上下文的数量上限（同 vue-tsx-pro 的 three/core/quality.ts）。
//
// 档位决定像素比上限、粒子等元素的密度、是否抗锯齿；auto 按设备判断（触屏、核数、内存、省流量）；off 不渲染，显示降级内容。
// 浏览器同时能保持的 WebGL 上下文有限（Chrome 约 16 个，超出时最早的上下文被强制丢失），
// 页面上的效果多时由这里分配：超过上限时，回收最久没出现在视口里的那个，它再滚回视口时重建。

const LEVELS = {
  low: { pixelRatio: 1, density: 0.4, antialias: false },
  medium: { pixelRatio: 1.5, density: 0.7, antialias: true },
  high: { pixelRatio: 2, density: 1, antialias: true },
};

/** auto 的判断：省流量或很弱的设备 low，触屏或 4 核以下 medium，其余 high */
function deviceLevel() {
  const cores = navigator.hardwareConcurrency || 4;
  // deviceMemory、connection 只有 Chromium 有
  const memory = navigator.deviceMemory ?? 8;
  if (navigator.connection?.saveData || cores <= 2 || memory <= 2) return 'low';
  const coarse = matchMedia('(pointer: coarse)').matches;
  if (coarse || cores <= 4 || memory <= 4) return 'medium';
  return 'high';
}

/** 档位名（auto、low、medium、high）→ { level, pixelRatio, density, antialias } */
export function resolveThreeQuality(quality = 'auto') {
  const level = quality in LEVELS ? quality : deviceLevel();
  return { level, ...LEVELS[level] };
}

let limit = 8;
const holders = new Set();
const waiting = new Set();

/**
 * 页面上同时保持的 WebGL 上下文上限，默认 8（给宿主自己的 canvas 留余量）。
 * 只影响之后申请的效果；已经在渲染的不会因为调低而被回收
 */
export function setMaxThreeContexts(count) {
  limit = Math.max(1, Math.floor(count));
}

/** 当前占用的 WebGL 上下文数（测试、性能面板用） */
export function activeThreeContexts() {
  return holders.size;
}

/**
 * 申请一个上下文：有空位直接占用；满了回收一个不在视口里的；都在视口里时排队，返回 false。
 * holder：{ visible（现在是否在视口里）, lastSeen（上次在视口里的时间）, evict()（被回收）, retry()（有空位了再试） }
 */
export function acquireContext(holder) {
  if (holders.has(holder)) return true;
  if (holders.size >= limit) {
    let oldest;
    for (const other of holders) {
      if (!other.visible && (!oldest || other.lastSeen < oldest.lastSeen)) oldest = other;
    }
    if (!oldest) {
      waiting.add(holder);
      return false;
    }
    holders.delete(oldest);
    oldest.evict();
  }
  waiting.delete(holder);
  holders.add(holder);
  return true;
}

/** 释放上下文（卸载、被回收、上下文丢失），排队的效果依次再试 */
export function releaseContext(holder) {
  waiting.delete(holder);
  if (holders.delete(holder)) retryWaiting();
}

/** 排队的效果依次再试：有了空位，或有效果滚出视口（可以被回收）时调用 */
export function retryWaiting() {
  for (const next of [...waiting]) next.retry();
}
