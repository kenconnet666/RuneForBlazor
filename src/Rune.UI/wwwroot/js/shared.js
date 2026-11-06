// 几个模块共用的小工具：可聚焦元素的选择器、减少动态效果、读屏播报、等过渡结束。


export const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');

/** 让读屏念出一句话：共用一个礼貌的播报区；先清空、稍后再写入，同一句话连说两次也会念 */
export function speak(message) {
  if (!message) return;
  let region = document.querySelector('[data-rune-announcer]');
  if (!region) {
    region = document.createElement('div');
    region.setAttribute('data-rune-announcer', '');
    region.setAttribute('aria-live', 'polite');
    region.setAttribute('aria-atomic', 'true');
    Object.assign(region.style, {
      position: 'absolute', width: '1px', height: '1px', margin: '-1px', padding: '0',
      overflow: 'hidden', clipPath: 'inset(50%)', whiteSpace: 'nowrap', border: '0',
    });
  }
  // 模态对话框打开时放进对话框里：对话框外的内容读屏读不到
  const host = document.querySelector('dialog[open]:modal') ?? document.body;
  if (region.parentElement !== host) host.append(region);
  region.textContent = '';
  clearTimeout(speakTimer);
  speakTimer = setTimeout(() => (region.textContent = message), 100);
}

let speakTimer;

export const TABBABLE = 'a[href], button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"])';

/** 把按键交给 C#：发出冒泡的 rune-command（Blazor 里是 @onrunecommand），带命令名与可选的文字（粘贴的内容）。 */
export function sendCommand(target, name, text = null) {
  target.dispatchEvent(new CustomEvent('rune-command', { bubbles: true, detail: { name, text } }));
}

/** 过渡结束后执行：没有过渡（减少动态效果时时长为 0）立即执行；transitionend 没来（页面在后台）时按时长兜底 */
export function afterTransition(element, done) {
  const seconds = (value) => Math.max(0, ...value.split(',').map((part) => parseFloat(part) * (part.trim().endsWith('ms') ? 0.001 : 1)));
  const style = getComputedStyle(element);
  const total = seconds(style.transitionDuration) + seconds(style.transitionDelay);
  if (total === 0) {
    requestAnimationFrame(done);
    return;
  }
  let finished = false;
  const finish = () => {
    if (finished) return;
    finished = true;
    element.removeEventListener('transitionend', onEnd);
    done();
  };
  const onEnd = (event) => event.target === element && finish();
  element.addEventListener('transitionend', onEnd);
  setTimeout(finish, total * 1000 + 50);
}
