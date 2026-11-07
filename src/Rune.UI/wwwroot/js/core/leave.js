// 离开确认（LeaveConfirmation）：页面上有 data-rune-leave-guard 时，点了去本站别的页面的链接先不走，
// 发出冒泡的 rune-command（leave，带目标地址）交给 C# 弹确认对话框，确认了由 C# 跳过去。
// 在 JS 里拦：交互岛（静态 SSR 的路由 + 交互组件）的页面之间走增强导航，NavigationLock 拦不到。

import { sendCommand } from './shared.js';

/** 捕获阶段的点击：赶在 Blazor 的链接拦截（增强导航、交互路由）之前。 */
export function guardLeave(event) {
  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
  const guard = document.querySelector('[data-rune-leave-guard]');
  const link = guard && event.target instanceof Element ? event.target.closest('a[href]') : null;
  if (!link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return;
  const url = new URL(link.href, location.href);
  // 去别的网站由浏览器的 beforeunload 问；只换查询串、锚点还在这一页，不问
  if (url.origin !== location.origin || url.pathname === location.pathname) return;
  event.preventDefault();
  event.stopPropagation();
  sendCommand(guard, 'leave', url.href);
}
