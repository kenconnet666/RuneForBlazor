// 程序生成的模型：每个导出是一个函数，参数是 three.js 的全部导出，返回 three 的对象（ModelViewer 的 ObjectModule 写 地址#名字）

/** 清漆的环面结 */
export function knot({ Mesh, MeshPhysicalMaterial, TorusKnotGeometry }) {
  return new Mesh(
    new TorusKnotGeometry(1, 0.32, 220, 32),
    new MeshPhysicalMaterial({ color: 0x8b7cf6, metalness: 0.2, roughness: 0.25, clearcoat: 1 }),
  );
}

/** 透光的二十面体 */
export function gem({ IcosahedronGeometry, Mesh, MeshPhysicalMaterial }) {
  return new Mesh(
    new IcosahedronGeometry(1, 0),
    new MeshPhysicalMaterial({ color: 0x5ed3c3, metalness: 0, roughness: 0.05, transmission: 0.6, thickness: 1 }),
  );
}
