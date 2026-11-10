// 释放 three.js 对象占用的 GPU 资源：几何体、材质、材质与 uniform 里的纹理（同 vue-tsx-pro 的 three/core/dispose.ts）。
// three.js 不会随对象被垃圾回收而释放显存，移除模型、卸载场景时要显式调用。

const isTexture = (value) => value?.isTexture === true;

/** 释放材质，连同它的贴图与 ShaderMaterial uniform 里的纹理 */
export function disposeMaterial(material) {
  for (const value of Object.values(material)) {
    if (isTexture(value)) value.dispose();
  }
  for (const uniform of Object.values(material.uniforms ?? {})) {
    if (isTexture(uniform.value)) uniform.value.dispose();
  }
  material.dispose();
}

/** 释放对象及其子孙的几何体、材质与纹理；场景的背景与环境贴图一并释放 */
export function disposeObject(root) {
  root.traverse((object) => {
    object.geometry?.dispose();
    for (const item of [object.material ?? []].flat()) disposeMaterial(item);
  });
  if (isTexture(root.background)) root.background.dispose();
  if (isTexture(root.environment)) root.environment.dispose();
}
