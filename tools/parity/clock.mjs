// 对照时两边的“现在”固定在同一时刻：浏览器里（vue、本库的 WebAssembly）用 Playwright 的 clock，
// 服务端渲染（本库的静态 SSR、Server）由 .claude/launch.json 给 bench、docs 传 --FixedNow=同一时刻（见 DocsServices.AddDocsContent）。
// 取 vue 示例里写定的“今天”（2026-10-21，周三）上午十点（东八区）。
export const FIXED_NOW = '2026-10-21T02:00:00Z';

/** 新开的上下文里，页面的 Date 都返回 FIXED_NOW；计时器照常走 */
export async function fixClock(context) {
  await context.clock.setFixedTime(new Date(FIXED_NOW));
}
