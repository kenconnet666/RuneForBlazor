// 文档里“视频”示例的片段：在浏览器里现录一段两秒的动画（小球沿波浪线移动），示例不依赖外部的视频文件。
// 实际使用时 Src 就是视频文件的地址。由文档站的 JS 初始化器在 video[data-docs-clip] 出现时调用。

/** 给这个 video 录一段：先放上第一帧当封面，录完换上片段的地址 */
export async function recordClip(video) {
  if (typeof MediaRecorder === 'undefined') return;
  const canvas = document.createElement('canvas');
  canvas.width = 640;
  canvas.height = 360;
  const context = canvas.getContext('2d');
  const draw = (progress) => {
    context.fillStyle = '#0f172a';
    context.fillRect(0, 0, 640, 360);
    context.fillStyle = '#6366f1';
    context.beginPath();
    context.arc(80 + progress * 480, 180 + Math.sin(progress * Math.PI * 4) * 60, 36, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = '#e2e8f0';
    context.font = '20px system-ui, sans-serif';
    context.fillText(`${(progress * 2).toFixed(1)} s`, 24, 40);
  };
  draw(0);
  video.poster = canvas.toDataURL('image/png');
  const recorder = new MediaRecorder(canvas.captureStream(30));
  const chunks = [];
  recorder.ondataavailable = (event) => chunks.push(event.data);
  const stopped = new Promise((resolve) => (recorder.onstop = resolve));
  recorder.start();
  const started = performance.now();
  await new Promise((resolve) => {
    const frame = (now) => {
      const progress = Math.min((now - started) / 2000, 1);
      draw(progress);
      if (progress < 1) requestAnimationFrame(frame);
      else resolve();
    };
    requestAnimationFrame(frame);
  });
  recorder.stop();
  await stopped;
  if (video.isConnected) video.src = URL.createObjectURL(new Blob(chunks, { type: recorder.mimeType }));
}
