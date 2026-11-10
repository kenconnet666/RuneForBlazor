// 写自己的场景模块（ThreeCanvas 的 Setup）时可以用的工具，地址是 _content/RuneForBlazor.Three/js/index.js；
// three.js 本身在 _content/RuneForBlazor.Three/vendor/three/three.js（与内置效果同一份，不要另装一份 three）。
//
//   import { Mesh, IcosahedronGeometry, MeshStandardMaterial } from '../_content/RuneForBlazor.Three/vendor/three/three.js';
//   import { shaderChunks } from '../_content/RuneForBlazor.Three/js/index.js';

export { disposeMaterial, disposeObject } from './core/dispose.js';
export { createOrbit } from './core/orbit.js';
export { activeThreeContexts, resolveThreeQuality, setMaxThreeContexts } from './core/quality.js';
export { shaderChunks, THEME_PALETTE_SIZE } from './core/shaders.js';
export { readThemeColor } from './core/theme-colors.js';
export { latLngToVector } from './scenes/globe.js';
