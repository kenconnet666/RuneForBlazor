#!/usr/bin/env node
// 对比两边文档站每一页的标题、侧栏里的顺序、示例的 id 与标题（读源码，不用开站点），列出不同的地方：
// vue 改了示例的 id、标题，或挪了页面的顺序时在这里看得出来。有意不同的（Blazor 的参数名、本库多出来的示例）也会列出，逐条判断。
// 示例的排布（分列、顺序）用 columns.mjs。
// 用法：node tools/parity/titles.mjs [--vue=<vue-tsx-pro 目录>]
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values: options } = parseArgs({ options: { vue: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') } } });

const files = (dir, test) => readdirSync(dir).flatMap((name) => {
  const path = join(dir, name);
  return statSync(path).isDirectory() ? files(path, test) : test(name) ? [path] : [];
});
const kebab = (name) => name.replace(/(?<=[a-z0-9])([A-Z])/g, '-$1').toLowerCase();

const vue = new Map();
for (const path of files(join(options.vue, 'apps/docs/src/components'), (name) => name === 'docs.tsx')) {
  const text = readFileSync(path, 'utf8');
  const parts = path.split(/[\\/]/);
  vue.set(kebab(parts.at(-2)), {
    name: parts.at(-2),
    category: parts.at(-3).replaceAll('-', ''),
    title: /title:\s*'([^']*)'/.exec(text)?.[1],
    order: Number(/order:\s*(\d+)/.exec(text)?.[1] ?? Infinity),
    examples: new Map([...text.matchAll(/<Example\s+id="([^"]+)"\s+title="([^"]*)"/g)].map((match) => [match[1], match[2]])),
  });
}

const rune = new Map();
for (const path of files(join(repoRoot, 'apps/Rune.Docs.Content/Components'), (name) => name.endsWith('Page.razor'))) {
  const text = readFileSync(path, 'utf8');
  const slug = /@page "\/components\/([a-z0-9-]+)"/.exec(text)?.[1];
  const meta = /\[DocMeta\("([^"]*)",\s*"(?:\.|[^"])*",\s*DocCategory\.(\w+)(?:,\s*Order\s*=\s*(\d+))?/.exec(text);
  if (!slug || !meta) continue;
  rune.set(slug, {
    title: meta[1],
    category: meta[2].toLowerCase(),
    order: Number(meta[3] ?? Infinity),
    examples: new Map([...text.matchAll(/<Example\s+Id="([^"]+)"\s+Title="([^"]*)"/g)].map((match) => [match[1], match[2]])),
  });
}

const shared = [...vue.keys()].filter((slug) => rune.has(slug)).sort();
console.log(`只有 vue 的页面：${[...vue.keys()].filter((slug) => !rune.has(slug)).sort().join(' ') || '无'}`);
console.log(`只有 rune 的页面：${[...rune.keys()].filter((slug) => !vue.has(slug)).sort().join(' ') || '无'}\n`);

for (const slug of shared) {
  const [theirs, ours] = [vue.get(slug), rune.get(slug)];
  const lines = [];
  if (theirs.title !== ours.title) lines.push(`  标题：vue「${theirs.title}」 rune「${ours.title}」`);
  if (theirs.category !== ours.category) lines.push(`  分类：vue ${theirs.category} rune ${ours.category}`);
  const missing = [...theirs.examples.keys()].filter((id) => !ours.examples.has(id));
  const extra = [...ours.examples.keys()].filter((id) => !theirs.examples.has(id));
  if (missing.length) lines.push(`  还没有的示例：${missing.join(' ')}`);
  if (extra.length) lines.push(`  多出来的示例：${extra.join(' ')}`);
  for (const [id, title] of theirs.examples) {
    if (ours.examples.has(id) && ours.examples.get(id) !== title) lines.push(`  示例 ${id}：vue「${title}」 rune「${ours.examples.get(id)}」`);
  }
  if (lines.length) console.log(`${slug}\n${lines.join('\n')}`);
}

// 侧栏里每个分类的顺序：两边都按 order、再按名字排
const sequence = (pages, nameOf) => (category) => shared.filter((slug) => pages.get(slug).category === category)
  .sort((a, b) => pages.get(a).order - pages.get(b).order || nameOf(a).localeCompare(nameOf(b)));
const [theirs, ours] = [sequence(vue, (slug) => vue.get(slug).name), sequence(rune, (slug) => rune.get(slug).title)];
for (const category of new Set(shared.map((slug) => vue.get(slug).category))) {
  if (theirs(category).join(' ') === ours(category).join(' ')) continue;
  console.log(`\n${category} 的顺序不同\n  vue:  ${theirs(category).join(' ')}\n  rune: ${ours(category).join(' ')}`);
}
