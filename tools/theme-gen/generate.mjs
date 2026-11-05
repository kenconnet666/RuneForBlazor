#!/usr/bin/env node
// 从 vue-tsx-pro 的主题生成 C# 的内置主题（Themes.g.cs），两边的颜色、阴影、字体、圆角、动效取值保持一致。
//
// 用法：node tools/theme-gen/generate.mjs [--source <vue-tsx-pro 根目录>] [--check]
//   --source  vue-tsx-pro 仓库根目录，默认 ../../IdeaProjects/vue-tsx-pro（相对本仓库根）
//   --check   只比较不写入；生成文件与现有内容不一致时以非零状态退出（CI 用）
//
// 数据：packages/ui/src/config/theme/
//   tokens.ts   各组 token 的默认值（css.defineVars）：主题之外的组（字号、间距、尺寸、动效……）取这里，作为 Base；
//               值写成 { default, '@media …': … } 的（动效时长在减少动态效果时为 0）生成条件块
//   *.ts        每套主题：createTheme(颜色 | 阴影 | 字体 | 圆角, { … }) 与 { name, colorScheme, styles: [四组] }
// 对象字面量按 JS 求值（只含字符串，没有别的依赖）；每个 vue 的 token 都要能对上 C# 的 token（TOKEN_MAP），对不上就报错，
// 以免 vue-tsx-pro 加了 token 这边悄悄漏掉。
//
// 零依赖：只用 node: 内置模块。

import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const outputPath = resolve(repoRoot, 'src/Rune.UI/Config/Theme/Themes.g.cs');

const { values } = parseArgs({
  options: {
    source: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    check: { type: 'boolean', default: false },
  },
});
const themeDir = resolve(values.source, 'packages/ui/src/config/theme');
if (!existsSync(themeDir)) {
  console.error(`找不到 ${themeDir}；用 --source 指定 vue-tsx-pro 根目录`);
  process.exit(1);
}

/** 本库独有、vue-tsx-pro 没有的 token（不在顶层里的浮动元素的层级），写进 Base */
const RUNE_ONLY = [
  ['ZIndices.Toast', '1200'],
  ['ZIndices.Tooltip', '1300'],
];

/** 主题里会换的组（每套主题写全）；其余的组只取 tokens.ts 的默认值 */
const THEMED_GROUPS = new Set(['colors', 'shadows', 'fonts', 'radii']);

/** 减少动态效果的媒体查询 → C# 的条件 */
const CONDITIONS = { '@media (prefers-reduced-motion: reduce)': 'Media.ReducedMotion' };

const TONES = ['accent', 'danger', 'success', 'warning', 'info'];
const TONE_USES = { '': 'Solid', Hover: 'SolidHover', Contrast: 'Contrast', Text: 'Text', Soft: 'Soft', SoftHover: 'SoftHover', Border: 'Border' };

const pascal = (name) => name.charAt(0).toUpperCase() + name.slice(1);

/** vue 的（组, token）→ C# 的 token 表达式；对不上时返回 null */
function csharpToken(group, key) {
  if (group === 'colors') {
    for (const tone of TONES) {
      if (!key.startsWith(tone)) continue;
      const use = TONE_USES[key.slice(tone.length)];
      if (use) return `Tones.${pascal(tone)}.${use}`;
    }
    const code = /^code([A-Z]\w*)$/.exec(key);
    if (code) return `CodeColors.${code[1]}`;
    const chart = /^chart(\d+)$/.exec(key);
    if (chart) return `ChartColors.Series${chart[1]}`;
    return `Colors.${pascal(key)}`;
  }
  return `${pascal(group)}.${pascal(key)}`;
}

/** 从源码里截出 `start` 之后配对的 { … } 并按 JS 求值 */
function evaluateObject(source, start, scope = {}) {
  const open = source.indexOf('{', start);
  let depth = 0;
  for (let i = open; i < source.length; i++) {
    if (source[i] === '{') depth++;
    else if (source[i] === '}' && --depth === 0) {
      const literal = source.slice(open, i + 1);
      return new Function(...Object.keys(scope), `return (${literal});`)(...Object.values(scope));
    }
  }
  throw new Error(`对象字面量没有闭合：${source.slice(start, start + 80)}`);
}

// tokens.ts：各组的默认值
const tokensSource = readFileSync(resolve(themeDir, 'tokens.ts'), 'utf8');
const reduce = /const reduce = '([^']+)'/.exec(tokensSource)?.[1];
const defaults = new Map();
for (const match of tokensSource.matchAll(/export const (\w+) = css\.defineVars\('vtp-[\w-]+',/g)) {
  defaults.set(match[1], evaluateObject(tokensSource, match.index, { reduce }));
}

// 各主题文件：createTheme 的各组取值，与主题对象（名字、亮暗、用哪四组）
const groupValues = new Map();
const themes = [];
const themeFiles = readdirSync(themeDir).filter((f) => f.endsWith('.ts') && !['tokens.ts', 'theme.ts', 'token-names.ts', 'builtin.ts', 'boot.ts', 'preference.ts'].includes(f));
for (const file of themeFiles) {
  const source = readFileSync(resolve(themeDir, file), 'utf8');
  for (const match of source.matchAll(/export const (\w+) = css\.createTheme\((\w+),/g)) {
    groupValues.set(match[1], { group: match[2], values: evaluateObject(source, match.index) });
  }
  for (const match of source.matchAll(/export const (\w+): Theme = \{/g)) {
    const body = source.slice(match.index + match[0].length - 1, source.indexOf('};', match.index) + 1);
    const name = /name: '([^']+)'/.exec(body)?.[1];
    const scheme = /colorScheme: '([^']+)'/.exec(body)?.[1];
    const styles = /styles: \[([^\]]+)\]/.exec(body)?.[1].split(',').map((s) => s.trim()).filter(Boolean);
    // 紧挨在主题对象前面的文档注释
    const comment = /\/\*\*((?:(?!\*\/)[\s\S])*)\*\/\s*$/.exec(source.slice(0, match.index))?.[1] ?? '';
    const doc = comment.split('\n').map((l) => l.replace(/^\s*\*\s?/, '').trim()).filter(Boolean).join(' ');
    if (!name || !scheme || !styles) throw new Error(`${file}：读不出主题 ${match[1]} 的 name / colorScheme / styles`);
    themes.push({ name, dark: scheme === 'dark', styles, doc, file });
  }
}

// 主题的顺序同 builtin.ts（ThemeSwitcher 列出的顺序）
const builtinSource = readFileSync(resolve(themeDir, 'builtin.ts'), 'utf8');
const order = [...evaluateObjectKeys(builtinSource, builtinSource.indexOf('export const builtinThemes'))];
themes.sort((a, b) => order.indexOf(a.name) - order.indexOf(b.name));

function evaluateObjectKeys(source, start) {
  const open = source.indexOf('{', start);
  const close = source.indexOf('}', open);
  return source.slice(open + 1, close).split('\n').map((l) => /^\s*'?([\w-]+)'?:/.exec(l)?.[1]).filter(Boolean);
}

const unmapped = [];
const csharpString = (text) => `"${String(text).replaceAll('\\', '\\\\').replaceAll('"', '\\"')}"`;
function assignment(group, key, value, indent) {
  const token = csharpToken(group, key);
  if (!token) unmapped.push(`${group}.${key}`);
  return `${indent}[${token}] = ${csharpString(value)},`;
}

// Base：主题之外的组
const baseLines = [];
const conditional = new Map();
for (const [group, tokens] of defaults) {
  if (THEMED_GROUPS.has(group)) continue;
  for (const [key, value] of Object.entries(tokens)) {
    if (typeof value === 'object') {
      baseLines.push(assignment(group, key, value.default, '        '));
      for (const [condition, conditionalValue] of Object.entries(value)) {
        if (condition === 'default') continue;
        if (!CONDITIONS[condition]) throw new Error(`不认识的条件 ${condition}（${group}.${key}）`);
        if (!conditional.has(condition)) conditional.set(condition, []);
        conditional.get(condition).push(assignment(group, key, conditionalValue, '            '));
      }
    } else {
      baseLines.push(assignment(group, key, value, '        '));
    }
  }
}
for (const [token, value] of RUNE_ONLY) baseLines.push(`        [${token}] = ${csharpString(value)},`);
for (const [condition, lines] of conditional) {
  baseLines.push(`        [${CONDITIONS[condition]}] = new("${CONDITIONS[condition].split('.').pop().replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()}")`, '        {', ...lines, '        },');
}

const propertyName = (name) => name.split('-').map(pascal).join('');

const themeBlocks = themes.map((theme) => {
  const lines = [];
  for (const style of theme.styles) {
    const entry = groupValues.get(style);
    if (!entry) throw new Error(`${theme.file}：主题 ${theme.name} 用到的 ${style} 找不到`);
    lines.push(`        // ${entry.group}（${style}）`);
    for (const [key, value] of Object.entries(entry.values)) lines.push(assignment(entry.group, key, value, '        '));
  }
  const doc = theme.doc || `${theme.name} 主题`;
  const summary = (/[。．.]$/.test(doc) ? doc : `${doc}。`).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
  return [
    `    /// <summary>${summary}</summary>`,
    `    public static Theme ${propertyName(theme.name)} => Css.Define(static () => new Theme(${csharpString(theme.name)}, basedOn: Base)`,
    '    {',
    ...(theme.dark ? ['        IsDark = true,'] : []),
    ...lines,
    '    });',
  ].join('\n');
});

if (unmapped.length) {
  console.error(`这些 token 在 C# 里没有对应（在 Tokens.cs 里加上，或在 csharpToken 里写映射）：\n  ${[...new Set(unmapped)].join('\n  ')}`);
  process.exit(1);
}

const output = [
  '// <auto-generated>',
  '// 由 tools/theme-gen/generate.mjs 从 vue-tsx-pro 的 packages/ui/src/config/theme 生成，请勿手改。',
  '// </auto-generated>',
  '#nullable enable',
  '',
  'using Rune.Styles;',
  '',
  'namespace Rune.UI;',
  '',
  '/// <summary>',
  `/// 内置主题（${themes.length} 套，同 vue-tsx-pro）。自定义主题以它们为基础，只写不同的 token：`,
  '/// <code>new Theme("brand", basedOn: Themes.Light) { [Tones.Accent.Solid] = "#7c3aed" }</code>',
  '/// 亮暗跟随系统：<c>AddRuneUI(o => { o.Theme = Themes.Light; o.DarkTheme = Themes.Dark; })</c>；也可以固定用一套（如 Cyberpunk）。',
  '/// </summary>',
  'public static partial class Themes',
  '{',
  '    /// <summary>所有内置主题共用的部分：主题之外的组（字号、间距、尺寸、动效……），取 vue-tsx-pro tokens.ts 的默认值。</summary>',
  '    public static Theme Base => Css.Define(static () => new Theme("base")',
  '    {',
  ...baseLines,
  '    });',
  '',
  themeBlocks.join('\n\n'),
  '',
  '    /// <summary>全部内置主题，按 vue-tsx-pro 主题切换器的顺序。</summary>',
  `    public static IReadOnlyList<Theme> All => [${themes.map((t) => propertyName(t.name)).join(', ')}];`,
  '}',
  '',
].join('\n');

if (values.check) {
  const current = existsSync(outputPath) ? readFileSync(outputPath, 'utf8').replaceAll('\r\n', '\n') : null;
  if (current !== output) {
    console.error(`已过期：${outputPath}`);
    process.exit(1);
  }
  console.log('生成文件与数据源一致');
} else {
  writeFileSync(outputPath, output);
  console.log(`已写入 ${outputPath}：${themes.length} 套主题`);
}
