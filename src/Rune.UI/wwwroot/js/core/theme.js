// 主题切换（ThemeSwitcher）：选择记在 localStorage 里，根元素写上解析出的主题名（data-theme，RuneProvider 的主题类据此取那一套）
// 与选择（data-theme-preference），再按配置设好根元素的底色、文字色与配色方案。配置是 ThemeBootScript 输出的 script#rune-theme-config。
// 跟随系统时系统明暗或对比度变了、其他标签页改了选择、增强导航换了页之后都重新应用。切换时新主题从按钮处圆形展开（View Transitions；
// 不支持或减少动态效果时直接换）。菜单里的单选项（data-rune-theme-choice）的 aria-checked 与触发按钮的图标（data-rune-theme-icon，
// 换成选中项的图标）在这里同步：C# 不知道选了哪套。显示 token 取值的元素（data-rune-token-value）换主题后重新读。

import { reducedMotion } from './shared.js';

const SYSTEM = 'system';
const darkQuery = matchMedia('(prefers-color-scheme: dark)');
const contrastQuery = matchMedia('(prefers-contrast: more)');

function readConfig() {
  const element = document.getElementById('rune-theme-config');
  if (!element) return null;
  try {
    return JSON.parse(element.textContent);
  } catch {
    return null;
  }
}

/** 记住的选择；隐私模式、禁用存储时 localStorage 会抛错，当作没记过 */
function storedPreference(config) {
  let stored = null;
  try {
    stored = localStorage.getItem(config.key);
  } catch {
    // 当作没记过
  }
  return stored === SYSTEM || config.themes[stored] ? stored : config.fallback;
}

function resolve(config, preference) {
  if (preference !== SYSTEM) return preference;
  const pair = config.system.contrast && contrastQuery.matches ? config.system.contrast : config.system;
  return darkQuery.matches ? pair.dark : pair.light;
}

/** 按选择写根元素，并同步页面上的主题切换 */
function apply(config, preference) {
  const root = document.documentElement;
  const name = resolve(config, preference);
  root.dataset.themePreference = preference;
  const colors = config.themes[name];
  if (colors) {
    root.dataset.theme = name;
    root.style.colorScheme = colors.colorScheme;
    root.style.backgroundColor = colors.background;
    root.style.color = colors.text;
  }
  document.querySelectorAll('[data-rune-theme-switcher]').forEach((switcher) => syncSwitcher(switcher, preference));
  refreshTokenValues();
}

/** 显示 token 取值的元素（ColorSwatch，data-rune-token-value="--变量名"）：按当前主题读出来写进去 */
export function fillTokenValue(element) {
  element.textContent = getComputedStyle(element).getPropertyValue(element.getAttribute('data-rune-token-value')).trim();
}

function refreshTokenValues() {
  document.querySelectorAll('[data-rune-token-value]').forEach(fillTokenValue);
}

/** 重新按记住的选择应用（增强导航换页后根元素的属性可能被换掉、其他标签页改了选择） */
export function reapplyTheme() {
  const config = readConfig();
  if (config) apply(config, storedPreference(config));
}

/** 装上全局的监听：系统明暗或对比度变了（跟随系统时）、其他标签页改了选择 */
export function installTheme() {
  const followSystem = () => {
    const config = readConfig();
    if (config && storedPreference(config) === SYSTEM) apply(config, SYSTEM);
    // 没有可选主题时跟随系统的主题由媒体查询切换：token 的取值也变了
    else if (!config) refreshTokenValues();
  };
  darkQuery.addEventListener('change', followSystem);
  contrastQuery.addEventListener('change', followSystem);
  window.addEventListener('storage', (event) => {
    const config = readConfig();
    if (config && event.key === config.key) apply(config, storedPreference(config));
  });
}

/** 新插入的主题切换：按当前的选择标出选中项、换上它的图标 */
export function watchThemeSwitcher(switcher) {
  const config = readConfig();
  if (config) syncSwitcher(switcher, storedPreference(config));
}

// 主题切换（菜单，data-rune-theme-switcher）里的单选项与触发按钮的图标
function syncSwitcher(switcher, preference) {
  let chosen = null;
  for (const item of switcher.querySelectorAll('[data-rune-theme-choice]')) {
    const checked = item.getAttribute('data-rune-theme-choice') === preference;
    item.setAttribute('aria-checked', String(checked));
    item.dataset.state = checked ? 'checked' : 'unchecked';
    if (checked) chosen = item;
  }
  const icon = document.querySelector(`[popovertarget="${switcher.id}"] [data-rune-theme-icon]`);
  const source = chosen?.querySelector('svg[data-rune-choice-icon]');
  if (icon && source) {
    icon.setAttribute('viewBox', source.getAttribute('viewBox'));
    icon.innerHTML = source.innerHTML;
  }
}

/** 点了主题切换里的一项：记下、应用，从触发按钮处圆形展开 */
export function chooseTheme(target) {
  const item = target.closest('[data-rune-theme-choice]');
  if (!item || item.matches('[aria-disabled="true"]')) return;
  const config = readConfig();
  const preference = item.getAttribute('data-rune-theme-choice');
  if (!config || (preference !== SYSTEM && !config.themes[preference])) return;
  try {
    localStorage.setItem(config.key, preference);
  } catch {
    // 记不住也照样切换
  }
  const switcher = item.closest('[data-rune-theme-switcher]');
  const trigger = switcher && document.querySelector(`[popovertarget="${switcher.id}"]`);
  reveal(() => apply(config, preference), trigger);
}

// 整页切换的过渡：从元素中心圆形展开新画面（同 vue-tsx-pro 的 revealTransition）
function reveal(update, from) {
  if (!document.startViewTransition || reducedMotion.matches || !from) {
    update();
    return;
  }
  const root = document.documentElement;
  const rect = from.getBoundingClientRect();
  const x = rect.left + rect.width / 2;
  const y = rect.top + rect.height / 2;
  insertRevealStyle();
  root.dataset.runeReveal = '';
  // 时长与缓动取主题的 token（在主题类所在的元素里才有）
  const styles = getComputedStyle(from);
  const duration = parseFloat(styles.getPropertyValue('--rune-durations-lg')) || 0;
  const easing = styles.getPropertyValue('--rune-easings-standard').trim() || 'ease';
  let updated = false;
  const run = () => {
    if (updated) return;
    updated = true;
    update();
  };
  const transition = document.startViewTransition(run);
  // 页面没在绘制（后台标签页、被遮住的窗口）时截不到画面，回调迟迟不执行：跳过转场直接换
  setTimeout(() => {
    if (updated) return;
    transition.skipTransition();
    run();
  }, 1000);
  transition.finished.finally(() => delete root.dataset.runeReveal).catch(() => undefined);
  transition.ready.then(() => {
    const radius = Math.hypot(Math.max(x, innerWidth - x), Math.max(y, innerHeight - y));
    root.animate(
      { clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${radius}px at ${x}px ${y}px)`] },
      { duration, easing, pseudoElement: '::view-transition-new(root)' },
    );
  }).catch(() => undefined);
}

// 圆形展开时去掉浏览器默认的淡入淡出：只对带 data-rune-reveal 的根元素生效（转场期间才带），不影响页面自己的 View Transitions
let revealStyleInserted = false;
function insertRevealStyle() {
  if (revealStyleInserted) return;
  revealStyleInserted = true;
  const style = document.createElement('style');
  style.textContent = ':root[data-rune-reveal]::view-transition-old(root),:root[data-rune-reveal]::view-transition-new(root){animation:none;mix-blend-mode:normal}';
  document.head.append(style);
}
