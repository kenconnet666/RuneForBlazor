// 八个小球按图表色板着色，绕着品牌色的球体转：颜色在画布所在位置读取，局部主题（RuneProvider）同样生效
const CHART = [1, 2, 3, 4, 5, 6, 7, 8].map((n) => `var(--rune-chart-colors-series${n})`);

export function setup({ three, scene, themeColors }) {
  const { AmbientLight, DirectionalLight, Group, Mesh, MeshStandardMaterial, SphereGeometry, TorusGeometry } = three;
  const palette = themeColors({ accent: 'var(--rune-tones-accent-solid)', ring: 'var(--rune-colors-border)' });
  const core = new MeshStandardMaterial({ roughness: 0.25, emissiveIntensity: 0.35 });
  core.color = palette.accent;
  core.emissive = palette.accent;
  const ringMaterial = new MeshStandardMaterial({ roughness: 0.8 });
  ringMaterial.color = palette.ring;

  const orbit = new Group();
  const ball = new SphereGeometry(0.22, 32, 16);
  CHART.forEach((color, index) => {
    const material = new MeshStandardMaterial({ roughness: 0.35 });
    material.color = themeColors({ color }).color;
    const mesh = new Mesh(ball, material);
    const angle = (index / CHART.length) * Math.PI * 2;
    mesh.position.set(Math.cos(angle) * 1.7, Math.sin(angle) * 1.7, 0);
    orbit.add(mesh);
  });
  orbit.add(new Mesh(new TorusGeometry(1.7, 0.012, 8, 160), ringMaterial));
  orbit.rotation.x = -1.05;

  const light = new DirectionalLight(0xffffff, 2.2);
  light.position.set(2, 3, 4);
  scene.add(new Mesh(new SphereGeometry(0.75, 64, 32), core), orbit, new AmbientLight(0xffffff, 0.8), light);
  return { frame: ({ time }) => void (orbit.rotation.z = time * 0.35) };
}
