// 一片方块随波起伏，指针所在处隆起：context.pointer 是指针相对画布的位置（-1 到 1）与 strength（离开时淡出）
const COLUMNS = 18;
const ROWS = 9;
const GAP = 0.36;

export function setup({ three, scene, camera, pointer, themeColors }) {
  const { AmbientLight, BoxGeometry, Color, DirectionalLight, InstancedMesh, MeshStandardMaterial, Object3D } = three;
  const palette = themeColors({ low: 'var(--rune-chart-colors-series2)', high: 'var(--rune-tones-accent-solid)' });
  const material = new MeshStandardMaterial({ roughness: 0.4 });
  const cubes = new InstancedMesh(new BoxGeometry(0.28, 0.28, 0.28), material, COLUMNS * ROWS);
  const dummy = new Object3D();
  const tint = new Color();
  camera.position.set(0, -3.4, 6.2);
  camera.lookAt(0, 0, 0);
  const light = new DirectionalLight(0xffffff, 2);
  light.position.set(-2, -3, 5);
  scene.add(cubes, new AmbientLight(0xffffff, 0.8), light);
  return {
    frame: ({ time }) => {
      // 指针在网格平面上的位置
      const px = pointer.x * (COLUMNS * GAP) * 0.5;
      const py = pointer.y * (ROWS * GAP) * 0.5;
      for (let row = 0; row < ROWS; row++) {
        for (let column = 0; column < COLUMNS; column++) {
          const index = row * COLUMNS + column;
          const x = (column - (COLUMNS - 1) / 2) * GAP;
          const y = (row - (ROWS - 1) / 2) * GAP;
          const near = Math.exp(-((x - px) ** 2 + (y - py) ** 2) * 1.6) * pointer.strength;
          const wave = Math.sin(time * 1.4 + x * 1.2 + y * 0.8) * 0.5 + 0.5;
          const height = 0.15 + wave * 0.25 + near * 1.6;
          dummy.position.set(x, y, height * 0.14);
          dummy.scale.set(1, 1, 0.4 + height * 2);
          dummy.updateMatrix();
          cubes.setMatrixAt(index, dummy.matrix);
          cubes.setColorAt(index, tint.lerpColors(palette.low, palette.high, Math.min(1, near + wave * 0.3)));
        }
      }
      cubes.instanceMatrix.needsUpdate = true;
      cubes.instanceColor.needsUpdate = true;
    },
  };
}
