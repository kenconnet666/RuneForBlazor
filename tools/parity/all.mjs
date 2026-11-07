// 对照全部页面（两边都有的组件页），亮色主题各跑一遍 parity.mjs，汇总每页的差异数到 artifacts/parity/summary.md。
// 用法：node tools/parity/all.mjs [--from=<演示名>]（从某一页接着跑）[--shots]（各示例的截图也存下来，给 shots.py 比较）[--mode=server]（同 parity.mjs，默认 static）[--states]（另比悬停、聚焦、按下）；准备同 README.md（两边的服务都开着）。
import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options } = parseArgs({
  options: {
    vue: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    from: { type: 'string' },
    shots: { type: 'boolean', default: false },
    mode: { type: 'string', default: 'static' },
    states: { type: 'boolean', default: false },
  },
});

// 本库文档页的地址：/components/<演示名>
const runePages = new Set();
const walk = (dir) => {
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) walk(path);
    else if (name.endsWith('Page.razor')) {
      const match = /@page "\/components\/([a-z0-9-]+)"/.exec(readFileSync(path, 'utf8'));
      if (match) runePages.add(match[1]);
    }
  }
};
walk(join(repoRoot, 'apps/Rune.Docs.Content/Components'));

// vue 文档站的组件页：apps/docs/src/components/<分类>/<组件>，地址是组件名的短横线写法
const kebab = (name) => name.replace(/([a-z0-9])([A-Z])/g, '$1-$2').replace(/([A-Z])([A-Z][a-z])/g, '$1-$2').toLowerCase();
const vueRoot = join(options.vue, 'apps/docs/src/components');
const vuePages = new Set();
for (const category of readdirSync(vueRoot)) {
  const dir = join(vueRoot, category);
  if (!statSync(dir).isDirectory()) continue;
  for (const name of readdirSync(dir)) if (statSync(join(dir, name)).isDirectory()) vuePages.add(kebab(name));
}

const pages = [...runePages].filter((page) => vuePages.has(page)).sort();
const start = options.from ? pages.indexOf(options.from) : 0;
const rows = [];
for (const page of pages.slice(Math.max(start, 0))) {
  let output = '';
  try {
    output = execFileSync('node', [join(repoRoot, 'tools/parity/parity.mjs'), page, '--themes=light', `--mode=${options.mode}`, ...(options.shots ? ['--shots'] : []), ...(options.states ? ['--states'] : [])], { cwd: repoRoot, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], timeout: 180_000 });
  } catch (error) {
    output = `${error.stdout ?? ''}${error.stderr ?? ''}`;
  }
  const total = /共 (\d+) 处差异/.exec(output)?.[1] ?? '?';
  const missing = output.split('\n').filter((line) => /没有示例|找不到|Error|错误/.test(line)).slice(0, 2).join(' / ');
  rows.push(`| ${page} | ${total} | ${missing.replaceAll('|', '/')} |`);
  console.log(`${page}: ${total}${missing ? ` (${missing})` : ''}`);
}
const report = join(repoRoot, 'artifacts/parity/summary.md');
const previous = options.from && existsSync(report) ? readFileSync(report, 'utf8').trimEnd().split('\n') : ['| 页面 | 差异 | 备注 |', '| --- | --- | --- |'];
writeFileSync(report, [...previous, ...rows].join('\n') + '\n');
console.log(`\n${pages.length} 页（两边都有），汇总在 ${report}；只在一边的页：rune ${[...runePages].filter((p) => !vuePages.has(p)).join(', ')}`);
