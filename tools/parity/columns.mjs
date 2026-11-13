// 对比文档页示例的排布（同 vue-tsx-pro 的 docs.tsx：哪些示例占整行、哪些分列、哪一列、宽列、什么顺序），列出与 vue 不同的页与缺的示例；
// --apply 照 vue 的排布重排本库的页面。只看两边都有的示例：本库多出来的示例接在原来所在那一组的末尾（整行的接在最后），
// vue 有而本库还没有的列出来、跳过。
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

const TOKEN = /<ExampleColumn(\s+wide|\s+Wide="true")?\s*>|<\/ExampleColumn>|<Example\s+(?:id|Id)="([^"]+)"|<\/?Examples>/g;

/**
 * 一页的排布：整行的示例是 id，一组分列是 { columns: [{ wide, ids }] }
 */
function layoutOf(text) {
  const items = [];
  let group = null;
  let column = null;
  for (const match of text.matchAll(TOKEN)) {
    const tag = match[0];
    if (tag === '<Examples>') group = { columns: [] };
    else if (tag === '</Examples>') items.push(group), (group = null);
    else if (tag.startsWith('<ExampleColumn')) column = { wide: !!match[1], ids: [] };
    else if (tag === '</ExampleColumn>') group.columns.push(column), (column = null);
    else (column ? column.ids : items).push(match[2]);
  }
  return items;
}

const idsOf = (layout) => layout.flatMap((item) => (typeof item === 'string' ? [item] : item.columns.flatMap((c) => c.ids)));

/** 只留下 keep 里的示例（空的列、空的组去掉） */
function only(layout, keep) {
  return layout.flatMap((item) => {
    if (typeof item === 'string') return keep.has(item) ? [item] : [];
    const columns = item.columns.map((c) => ({ wide: c.wide, ids: c.ids.filter((id) => keep.has(id)) })).filter((c) => c.ids.length);
    return columns.length ? [{ columns }] : [];
  });
}

// 同 vue 文档站 nav.ts 的 kebab：Button → button，IconButton → icon-button，QRCode → qrcode
const kebab = (name) => name.replace(/(?<=[a-z0-9])([A-Z])/g, '-$1').toLowerCase();
const vue = new Map();
for (const path of files(join(options.vue, 'apps/docs/src/components'), (name) => name === 'docs.tsx'))
  vue.set(kebab(path.split(/[\\/]/).at(-2)), layoutOf(readFileSync(path, 'utf8')));

let differ = 0;
for (const path of files(join(repoRoot, 'apps/Rune.Docs.Content/Components'), (name) => name.endsWith('Page.razor'))) {
  const text = readFileSync(path, 'utf8');
  const slug = /@page "\/components\/([a-z0-9-]+)"/.exec(text)?.[1];
  const target = slug && vue.get(slug);
  if (!target) continue;
  const ours = layoutOf(text);
  const have = new Set(idsOf(ours));
  const missing = idsOf(target).filter((id) => !have.has(id));
  if (missing.length) console.log(`${slug} 还没有的示例：${missing.join(' ')}`);

  const shared = new Set(idsOf(target).filter((id) => have.has(id)));
  const wanted = only(target, shared);
  if (JSON.stringify(wanted) === JSON.stringify(only(ours, shared))) continue;
  differ++;
  const show = (layout) => layout.map((item) => (typeof item === 'string' ? item : `[${item.columns.map((c) => (c.wide ? '宽 ' : '') + c.ids.join(' ')).join(' | ')}]`)).join('  ');
  console.log(`${slug}\n  rune: ${show(only(ours, shared))}\n  vue:  ${show(wanted)}`);
  if (options.apply) writeFileSync(path, rearrange(text, withExtras(wanted, ours, shared)));
}
console.log(`\n${differ} 页的排布与 vue 不同${options.apply ? '，已照 vue 重排' : '；加 --apply 照 vue 重排'}`);

/** 本库多出来的示例放回去：原来在某一组里的接在对应组的最后一列，整行的接在最后 */
function withExtras(wanted, ours, shared) {
  const result = structuredClone(wanted);
  const groups = result.filter((item) => typeof item !== 'string');
  let groupIndex = -1;
  for (const item of ours) {
    if (typeof item === 'string') {
      if (!shared.has(item)) result.push(item);
      continue;
    }
    groupIndex++;
    const extras = item.columns.flatMap((c) => c.ids).filter((id) => !shared.has(id));
    if (!extras.length) continue;
    const group = groups[Math.min(groupIndex, groups.length - 1)];
    if (group) group.columns.at(-1).ids.push(...extras);
    else result.push(...extras);
  }
  return result;
}

/** 把各个 Example 块按排布放回去：DocPage 开头到键盘说明 / API 之前的这一段重写，块的内容原样、缩进按层级 */
function rearrange(original, layout) {
  // 按 LF 处理，原来是 CRLF 的写回时换回去
  const crlf = original.includes('\r\n');
  const text = original.replaceAll('\r\n', '\n');
  const start = text.indexOf('\n', text.indexOf('<DocPage')) + 1;
  const end = Math.min(...['    <KeyboardReference', '    <ApiReference', '</DocPage>'].map((tag) => text.indexOf(tag, start)).filter((i) => i >= 0));
  const blocks = new Map();
  for (const match of text.slice(start, end).matchAll(/[ \t]*<Example\s+Id="([^"]+)"(?:[^>]*\/>|[\s\S]*?<\/Example>)\n/g)) blocks.set(match[1], match[0]);
  const indent = (block, depth) => {
    const lines = block.replace(/\n$/, '').split('\n');
    const base = lines[0].length - lines[0].trimStart().length;
    return lines.map((line) => (line.trim() ? ' '.repeat(depth) + line.slice(base) : '')).join('\n') + '\n';
  };
  const body = layout.map((item) => typeof item === 'string'
    ? indent(blocks.get(item), 4)
    : '    <Examples>\n' + item.columns.map((c) => (c.wide ? '        <ExampleColumn Wide="true">\n' : '        <ExampleColumn>\n')
        + c.ids.map((id) => indent(blocks.get(id), 12)).join('') + '        </ExampleColumn>\n').join('') + '    </Examples>\n').join('');
  // 夹在示例之间的其他内容（说明代码块等）不动：只在这一段全是示例时重写
  const rest = text.slice(start, end).replace(/[ \t]*<Example\s+Id="[^"]+"(?:[^>]*\/>|[\s\S]*?<\/Example>)\n/g, '').replace(/[ \t]*<\/?Examples>\n|[ \t]*<ExampleColumn[^>]*>\n|[ \t]*<\/ExampleColumn>\n/g, '').trim();
  if (rest) {
    console.log(`  （示例之间还有别的内容，没有自动重排，请手动调整）`);
    return original;
  }
  const result = text.slice(0, start) + body + text.slice(end);
  return crlf ? result.replaceAll('\n', '\r\n') : result;
}
