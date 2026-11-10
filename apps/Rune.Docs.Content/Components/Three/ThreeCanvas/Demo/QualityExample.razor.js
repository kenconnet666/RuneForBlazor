// 斐波那契球面上的点云：粒子数按画质档位的密度系数决定；建好、尺寸变了时把档位、粒子数与像素比报告给 C#
function spherePoints(count) {
  const positions = new Float32Array(count * 3);
  const golden = Math.PI * (3 - Math.sqrt(5));
  for (let index = 0; index < count; index++) {
    const y = 1 - (index / (count - 1)) * 2;
    const radius = Math.sqrt(1 - y * y);
    const angle = index * golden;
    const scale = 1.6 + Math.sin(index * 0.37) * 0.08;
    positions.set([Math.cos(angle) * radius * scale, y * scale, Math.sin(angle) * radius * scale], index * 3);
  }
  return positions;
}

export function setup({ three, scene, quality, themeColors, send }) {
  const { AdditiveBlending, BufferAttribute, BufferGeometry, Points, PointsMaterial } = three;
  const count = Math.round(12000 * quality.density);
  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new BufferAttribute(spherePoints(count), 3));
  const material = new PointsMaterial({ size: 0.03, transparent: true, opacity: 0.9, blending: AdditiveBlending, depthWrite: false });
  material.color = themeColors({ accent: 'var(--rune-tones-accent-solid)' }).accent;
  const points = new Points(geometry, material);
  scene.add(points);
  return {
    frame: ({ time }) => points.rotation.set(time * 0.12, time * 0.2, 0),
    resize: (size) => send('scene-info', `${quality.level} · ${count} 个粒子 · 像素比 ${size.pixelRatio.toFixed(2)}`),
  };
}
