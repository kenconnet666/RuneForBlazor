// 页面顶部的加载条（data-rune-loading-bar，同 vue-tsx-pro 的 createLoadingBar）：C# 只写状态（data-status、data-run），动画在这里做。
// 开始后过 data-delay（默认 200ms）才显示，先快后慢地涨，停在 90% 前等完成；完成走满后淡出，出错变成危险色走满后淡出，淡出后归零。
// 带 data-navigation 的加载条跟着 Blazor 的增强导航（enhancednavigationstart / end）自己开始、结束。

const states = new WeakMap();

/** 每次涨多少：离 90% 越近涨得越慢 */
const trickle = (progress) => (90 - progress) * 0.12 + 0.5;

function stateOf(bar) {
  let state = states.get(bar);
  if (!state) {
    state = { progress: 0, visible: false, failed: false, run: null, status: 'idle', timers: [] };
    states.set(bar, state);
  }
  return state;
}

function render(bar, state) {
  bar.toggleAttribute('data-visible', state.visible);
  const phase = state.failed ? 'error' : state.visible ? 'loading' : 'idle';
  bar.setAttribute('data-state', phase);
  bar.setAttribute('aria-valuenow', String(Math.round(state.progress)));
  if (state.visible) bar.removeAttribute('aria-hidden');
  else bar.setAttribute('aria-hidden', 'true');
  bar.setAttribute('aria-busy', String(state.visible && state.progress < 100));
  const line = bar.firstElementChild;
  if (line) {
    line.style.transform = `scaleX(${state.progress / 100})`;
    line.setAttribute('data-state', phase);
  }
}

function stop(state) {
  for (const timer of state.timers) {
    clearTimeout(timer);
    clearInterval(timer);
  }
  state.timers = [];
}

function start(bar) {
  const state = stateOf(bar);
  stop(state);
  state.failed = false;
  if (!state.visible) state.progress = 0;
  render(bar, state);
  state.timers.push(setTimeout(() => {
    state.visible = true;
    state.progress = Math.max(state.progress, 8);
    render(bar, state);
  }, Number(bar.dataset.delay ?? 200)));
  state.timers.push(setInterval(() => {
    if (!state.visible) return;
    state.progress = Math.min(state.progress + trickle(state.progress), 90);
    render(bar, state);
  }, 300));
}

function end(bar, failed) {
  const state = stateOf(bar);
  stop(state);
  if (!state.visible) {
    state.progress = 0;
    render(bar, state);
    return;
  }
  state.failed = failed;
  state.progress = 100;
  render(bar, state);
  // 走满的过渡结束后淡出，淡出后归零
  state.timers.push(setTimeout(() => {
    state.visible = false;
    render(bar, state);
    state.timers.push(setTimeout(() => {
      state.progress = 0;
      state.failed = false;
      render(bar, state);
    }, 300));
  }, 300));
}

// 按 C# 写的状态走：开始次数（data-run）变了就重新开始
function sync(bar) {
  const state = stateOf(bar);
  const status = bar.dataset.status ?? 'idle';
  const run = bar.dataset.run ?? '0';
  if (status === state.status && run === state.run) return;
  const restarted = run !== state.run;
  state.status = status;
  state.run = run;
  if (status === 'loading' && (restarted || !state.timers.length)) start(bar);
  else if (status === 'done') end(bar, false);
  else if (status === 'failed') end(bar, true);
}

/** 开始管理一个加载条（插入时调用）。 */
export function watchLoadingBar(bar) {
  if (states.has(bar)) return;
  const state = stateOf(bar);
  // 第一次渲染时就在加载：照常开始；否则只记下当前的状态
  state.status = bar.dataset.status === 'loading' ? 'idle' : bar.dataset.status ?? 'idle';
  state.run = bar.dataset.status === 'loading' ? null : bar.dataset.run ?? '0';
  new MutationObserver(() => sync(bar)).observe(bar, { attributes: true, attributeFilter: ['data-status', 'data-run'] });
  sync(bar);
}

const navigationBars = () => document.querySelectorAll('[data-rune-loading-bar][data-navigation]');

/** Blazor 的增强导航开始。 */
export function navigationStarted() {
  for (const bar of navigationBars()) start(bar);
}

/** Blazor 的增强导航结束。 */
export function navigationEnded() {
  for (const bar of navigationBars()) end(bar, false);
}
