// 环面结加一层线框外壳：颜色取主题色，换主题时原地过渡
export function setup({ three, scene, themeColors }) {
  const { AmbientLight, DirectionalLight, IcosahedronGeometry, LineBasicMaterial, LineSegments, Mesh, MeshStandardMaterial, TorusKnotGeometry, WireframeGeometry } = three;
  const palette = themeColors({
    accent: 'var(--rune-tones-accent-solid)',
    glow: 'var(--rune-chart-colors-series3)',
    line: 'var(--rune-colors-border-strong)',
  });
  const knotMaterial = new MeshStandardMaterial({ roughness: 0.3, metalness: 0.25 });
  // 直接赋主题色对象：换主题时原地过渡，不用重建材质
  knotMaterial.color = palette.accent;
  knotMaterial.emissive = palette.glow;
  knotMaterial.emissiveIntensity = 0.2;
  const knot = new Mesh(new TorusKnotGeometry(0.9, 0.3, 240, 32), knotMaterial);

  const shellMaterial = new LineBasicMaterial({ transparent: true, opacity: 0.4 });
  shellMaterial.color = palette.line;
  const shell = new LineSegments(new WireframeGeometry(new IcosahedronGeometry(1.75, 1)), shellMaterial);

  const key = new DirectionalLight(0xffffff, 2.4);
  key.position.set(3, 4, 5);
  scene.add(knot, shell, new AmbientLight(0xffffff, 0.7), key);
  return {
    frame: ({ time }) => {
      knot.rotation.set(time * 0.3, time * 0.45, 0);
      shell.rotation.set(time * 0.05, -time * 0.12, 0);
    },
  };
}
