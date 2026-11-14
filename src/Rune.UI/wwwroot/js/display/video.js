// Video 的自绘控制条（data-rune-video，同 vue-tsx-pro 的 Video controls="custom"）。控制条由 C# 按状态渲染，这里做两件事：
// 1. 操作 video：点按钮（data-rune-video-action）、拖进度与音量（rune-slide）、选字幕与倍速（菜单项上的 data-rune-video-caption / -speed）、
//    点画面、键盘（空格 / K、← →、↑ ↓、M、F）；
// 2. video 的状态变了（播放、时间、时长、音量、倍速、字幕、全屏）时发 rune-command（video-state）交给 C# 重新渲染。
//    接管时先按 video 现在的状态发一次：预渲染页面上元数据、自动播放的事件可能在这之前就过去了。
// 控制条的收起只在这里（外层的 data-idle、控制条的 data-state），C# 不渲染这两个属性：播放中鼠标离开画面、或 2.5 秒不动就收起，
// 菜单开着、鼠标停在控制条上时留着；键盘聚焦到控制条里时样式让它照样出现。

import { sendCommand } from '../core/shared.js';
import { TYPING } from '../core/hotkeys.js';

/** 方向键一次快进、快退多少秒 */
const SEEK_STEP = 5;
/** 播放中多久不动就收起控制条（ms） */
const IDLE = 2500;

const watched = new WeakSet();
const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

/**
 * 总时长（秒）。直播、边录边播的片段没有时长（Infinity）：取能跳到的最远处；也没有时是 0——
 * 进度条不可用，时间只显示已经播了多久（同 vue 的 lengthOf）
 */
function lengthOf(video) {
  if (Number.isFinite(video.duration)) return video.duration;
  const { seekable } = video;
  const end = seekable?.length ? seekable.end(seekable.length - 1) : 0;
  return Number.isFinite(end) ? end : 0;
}

/** 自绘控制条的播放器出现时（交互接管后）调用。 */
export function watchVideo(root) {
  if (watched.has(root)) return;
  watched.add(root);
  const media = () => root.querySelector(':scope > video');
  const controls = () => root.querySelector(':scope > [data-rune-video-controls]');
  let sent = '';
  let idleTimer;

  const send = () => {
    const video = media();
    if (!video || !root.isConnected) return;
    const showing = [...video.textTracks].findIndex((track) => track.mode === 'showing');
    // 直播能跳到的范围边播边变：每次 timeupdate 都重算
    const duration = lengthOf(video);
    const state = [
      !video.paused && !video.ended ? 1 : 0,
      video.currentTime.toFixed(1),
      duration.toFixed(1),
      video.volume.toFixed(2),
      video.muted ? 1 : 0,
      video.playbackRate,
      showing,
      document.fullscreenElement === root ? 1 : 0,
      document.fullscreenEnabled ? 1 : 0,
    ].join(' ');
    if (state === sent) return;
    sent = state;
    sendCommand(root, 'video-state', state);
  };

  const playing = () => {
    const video = media();
    return !!video && !video.paused && !video.ended;
  };
  // 正在用控制条：菜单开着，或鼠标停在控制条上
  const busy = () => !!controls()?.matches(':hover, :has([aria-expanded="true"])');
  const setShown = (shown) => {
    controls()?.setAttribute('data-state', shown ? 'visible' : 'hidden');
    root.toggleAttribute('data-idle', !shown);
  };
  // 露出控制条；播放中过一会儿自动收起（正在用控制条时再等一轮）
  const wake = () => {
    setShown(true);
    clearTimeout(idleTimer);
    if (playing()) idleTimer = setTimeout(() => (!root.isConnected ? undefined : busy() ? wake() : setShown(false)), IDLE);
  };

  const toggle = () => {
    const video = media();
    if (!video) return;
    if (video.paused || video.ended) video.play().catch(() => {});
    else video.pause();
  };
  const seekBy = (seconds) => {
    const video = media();
    // 不知道时长（直播）时没有可以跳的范围
    const duration = video ? lengthOf(video) : 0;
    if (duration > 0) video.currentTime = clamp(video.currentTime + seconds, 0, duration);
  };
  const setVolume = (value) => {
    const video = media();
    if (!video) return;
    video.volume = clamp(value, 0, 1);
    // 拖到有声音就取消静音，拖到 0 就是静音
    video.muted = video.volume === 0;
  };
  const toggleMute = () => {
    const video = media();
    if (video) video.muted = !video.muted;
  };
  const toggleFullscreen = () => {
    if (document.fullscreenElement === root) document.exitFullscreen().catch(() => {});
    else root.requestFullscreen?.().catch(() => {});
  };
  const setCaption = (index) => {
    const video = media();
    if (!video) return;
    [...video.textTracks].forEach((track, position) => (track.mode = position === index ? 'showing' : 'disabled'));
    send();
  };

  // 媒体事件不冒泡：在外层的捕获阶段接住（换了视频源、video 重建时也接得到）
  for (const type of ['play', 'pause', 'ended', 'timeupdate', 'durationchange', 'loadedmetadata', 'volumechange', 'ratechange', 'seeked']) {
    root.addEventListener(type, () => {
      send();
      if (type === 'play' || type === 'pause') wake();
    }, true);
  }
  root.addEventListener('fullscreenchange', send);
  root.addEventListener('pointermove', wake);
  root.addEventListener('focusin', wake);
  root.addEventListener('pointerleave', () => {
    // 鼠标离开画面：不用等，直接收起
    if (playing() && !busy()) {
      clearTimeout(idleTimer);
      setShown(false);
    }
  });

  root.addEventListener('rune-slide', (event) => {
    const slider = event.target;
    const video = media();
    if (!video) return;
    if (slider.closest('[data-rune-video-seek]')) video.currentTime = event.detail.value;
    else if (slider.closest('[data-rune-video-volume]')) setVolume(event.detail.value);
  });

  root.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target : null;
    if (!target) return;
    const caption = target.closest('[data-rune-video-caption]');
    if (caption) return setCaption(Number(caption.getAttribute('data-rune-video-caption')));
    const speed = target.closest('[data-rune-video-speed]');
    if (speed) {
      const video = media();
      if (video) video.playbackRate = Number(speed.getAttribute('data-rune-video-speed'));
      return;
    }
    const action = target.closest('[data-rune-video-action]')?.getAttribute('data-rune-video-action');
    if (action === 'play') toggle();
    else if (action === 'mute') toggleMute();
    else if (action === 'fullscreen') toggleFullscreen();
    // 点控制条（按钮之间的空白、滑块、菜单）不算点画面；点画面：播放 / 暂停，焦点落在播放器上（键盘才有地方接）
    else if (!target.closest('[data-rune-video-controls]')) {
      toggle();
      root.focus({ preventScroll: true });
    }
  });

  // 焦点在播放器里时的键盘：空格 / K 播放暂停，← → 快退快进，↑ ↓ 音量，M 静音，F 全屏
  root.addEventListener('keydown', (event) => {
    if (event.altKey || event.ctrlKey || event.metaKey || event.defaultPrevented) return;
    const target = event.target instanceof Element ? event.target : root;
    // 菜单里的按键（类型搜索的字母、方向键）都是菜单的
    if (target.closest(TYPING) || target.closest('[role="menu"]')) return;
    // 焦点在滑块上时，方向键、空格是它的
    const inControl = target.closest('[role="slider"]');
    const onButton = target.closest('button');
    const video = media();
    const actions = {
      k: toggle,
      ' ': onButton || inControl ? undefined : toggle,
      m: toggleMute,
      f: toggleFullscreen,
      arrowleft: inControl ? undefined : () => seekBy(-SEEK_STEP),
      arrowright: inControl ? undefined : () => seekBy(SEEK_STEP),
      arrowup: inControl || !video ? undefined : () => setVolume(video.volume + 0.1),
      arrowdown: inControl || !video ? undefined : () => setVolume(video.volume - 0.1),
    };
    const action = actions[event.key.toLowerCase()];
    if (!action) return;
    event.preventDefault();
    action();
    wake();
  });

  send();
  wake();
}
