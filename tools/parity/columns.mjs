// 对比文档页示例的分列（同 vue-tsx-pro 的 docs.tsx：哪一列、什么顺序），列出与 vue 不同的页；--apply 照 vue 的分列重排本库的页面。
// 只看两边都有的示例：本库多出来的示例留在原来那一列的末尾，vue 有而本库还没有的跳过。
// 用法：node tools/parity/columns.mjs [--apply] [--vue=<vue-tsx-pro 目录>]
import { readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options } = parseArgs({
  options: {
    vue: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    apply: { type: 'boolean', default: false },
  },
});

const files = (dir, test) => readdirSync(dir).flatMap((name) => {
  const path = join(dir, name);
  return statSync(path).isDirectory() ? files(path, test) : test(name) ? [path] : [];
});

/** 一页的分列：[[id, …], [id, …]]；没有 ExampleColumn 时 null */
function columnsOf(text, idPattern) {
  const columns = [];
  const columnPattern = /<ExampleColumn>([\s\S]*?)<\/ExampleColumn>/g;
  for (let match; (match = columnPattern.exec(text)); ) columns.push([...match[1].matchAll(idPattern)].map((m) => m[1]));
  return columns.length ? columns : null;
}

const kebab = (name) => name.replace(/([a-z0-9])([A-Z])/g, '$1-$2').replace(/([A-Z])([A-Z][a-z])/g, '$1-$2').toLowerCase();
const vue = new Map();
for (const path of files(join(options.vue, 'apps/docs/src/components'), (name) => name === 'docs.tsx')) {
  const columns = columnsOf(readFileSync(path, 'utf8'), /<Example\s+id="([^"]+)"/g);
  if (columns) vue.set(kebab(path.split(/[\\/]/).at(-2)), columns);
}

let differ = 0;
for (const path of files(join(repoRoot, 'apps/Rune.Docs.Content/Components'), (name) => name.endsWith('Page.razor'))) {
  const text = readFileSync(path, 'utf8');
  const slug = /@page "\/components\/([a-z0-9-]+)"/.exec(text)?.[1];
  const target = slug && vue.get(slug);
  const ours = columnsOf(text, /<Example\s+Id="([^"]+)"/g);
  if (!target || !ours || target.length !== ours.length) continue;
  const known = new Set(ours.flat());
  // 照 vue 的分列：两边都有的按 vue 的列与顺序，本库多出来的接在原来那一列后面
  const wanted = target.map((column) => column.filter((id) => known.has(id)));
  const placed = new Set(wanted.flat());
  ours.forEach((column, index) => wanted[index].push(...column.filter((id) => !placed.has(id))));
  if (JSON.stringify(wanted) === JSON.stringify(ours)) continue;
  differ++;
  console.log(`${slug}\n  rune: ${ours.map((c) => c.join(' ')).join(' | ')}\n  vue:  ${wanted.map((c) => c.join(' ')).join(' | ')}`);
  if (options.apply) writeFileSync(path, rearrange(text, wanted));
}
console.log(`\n${differ} 页的分列与 vue 不同${options.apply ? '，已照 vue 重排' : '；加 --apply 照 vue 重排'}`);

/** 把各个 Example 块按 wanted 的分列放回 ExampleColumn 里（块的内容、缩进原样） */
function rearrange(original, wanted) {
  // 按 LF 处理，原来是 CRLF 的写回时换回去
  const crlf = original.includes('\r\n');
  const text = original.replaceAll('\r\n', '\n');
  const blocks = new Map();
  const blockPattern = /([ \t]*)<Example\s+Id="([^"]+)"[\s\S]*?<\/Example>\n/g;
  for (let match; (match = blockPattern.exec(text)); ) blocks.set(match[2], match[0]);
  let column = 0;
  const result = text.replace(/(<ExampleColumn>\n)[\s\S]*?(\n?[ \t]*<\/ExampleColumn>)/g, (_, open, close) => {
    const body = wanted[column++].map((id) => blocks.get(id)).join('');
    return open + body.replace(/\n$/, '') + close;
  });
  return crlf ? result.replaceAll('\n', '\r\n') : result;
}
