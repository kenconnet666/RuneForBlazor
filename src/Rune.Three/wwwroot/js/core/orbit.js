// 拖动转动（Globe、ModelViewer 共用，同 vue-tsx-pro 的 three/core/orbit.ts）：指针左右拖改 yaw、上下拖改 pitch（夹在 ±pitchLimit 内），
// 松手后带着惯性慢慢回到自转；方向键也能转（nudge）。场景每帧调用 step 取用。
//
//   const orbit = createOrbit({ yaw: 0, pitch: 0.3 });
//   const detach = orbit.attach(host);         // 指针事件挂在外层元素上；拖动时外层带 data-dragging（光标换成 grabbing）
//   frame: ({ delta }) => { orbit.step(delta, speed); camera… orbit.yaw / orbit.pitch }

/** 松手后惯性转动的最快速度（弧度 / 秒） */
const MAX_SPIN = 3;
/** 拖动一像素转多少弧度 */
const PER_PIXEL = 0.006;

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

/** 拖动转动：yaw、pitch 是弧度，pitchLimit 默认 1 */
export function createOrbit({ yaw, pitch, pitchLimit = 1 }) {
  const pointer = { x: 0, y: 0, dx: 0, dy: 0 };
  let dragging = false;
  let velocity = 0;
  const clampPitch = (value) => clamp(value, -pitchLimit, pitchLimit);

  return {
    get yaw() {
      return yaw;
    },
    get pitch() {
      return pitch;
    },
    get dragging() {
      return dragging;
    },
    /** 在 host 上挂指针事件（拖动时 host 带 data-dragging）；返回取消挂载的函数 */
    attach(host, onChange) {
      const setDragging = (value) => {
        dragging = value;
        host.toggleAttribute('data-dragging', value);
      };
      const down = (event) => {
        if (event.button !== 0) return;
        host.setPointerCapture(event.pointerId);
        pointer.x = event.clientX;
        pointer.y = event.clientY;
        setDragging(true);
      };
      const move = (event) => {
        if (!dragging) return;
        pointer.dx += event.clientX - pointer.x;
        pointer.dy += event.clientY - pointer.y;
        pointer.x = event.clientX;
        pointer.y = event.clientY;
        onChange?.();
      };
      const up = () => setDragging(false);
      host.addEventListener('pointerdown', down);
      host.addEventListener('pointermove', move);
      host.addEventListener('pointerup', up);
      host.addEventListener('pointercancel', up);
      return () => {
        host.removeEventListener('pointerdown', down);
        host.removeEventListener('pointermove', move);
        host.removeEventListener('pointerup', up);
        host.removeEventListener('pointercancel', up);
        setDragging(false);
      };
    },
    /** 直接转一下（键盘）：弧度 */
    nudge(byYaw, byPitch) {
      yaw += byYaw;
      pitch = clampPitch(pitch + byPitch);
    },
    /** 每帧调用：用掉拖动的量；没在拖时惯性回到 spin（弧度 / 秒）的自转。返回这一帧有没有变化 */
    step(delta, spin) {
      if (pointer.dx || pointer.dy) {
        const turn = pointer.dx * PER_PIXEL;
        yaw += turn;
        pitch = clampPitch(pitch + pointer.dy * PER_PIXEL);
        // 松手时的速度：几帧平滑一下，再封顶（一帧里拖了一大段时不至于疯转）
        const speed = delta > 0 ? turn / delta : 0;
        velocity = clamp(velocity * 0.5 + speed * 0.5, -MAX_SPIN, MAX_SPIN);
        pointer.dx = 0;
        pointer.dy = 0;
        return true;
      }
      if (dragging) return false;
      // 松手后的惯性慢慢回到自转
      velocity += (spin - velocity) * Math.min(1, delta * 1.5);
      yaw += velocity * delta;
      return velocity !== 0;
    },
  };
}
