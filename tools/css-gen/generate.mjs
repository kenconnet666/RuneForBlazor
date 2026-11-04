#!/usr/bin/env node
// 从 vue-tsx-pro 已生成的 CSS 属性说明（documented.ts）生成 C# 的强类型样式属性表。
//
// 用法：node tools/css-gen/generate.mjs [--source <vue-tsx-pro 根目录>] [--check]
//   --source  vue-tsx-pro 仓库根目录，默认 ../../IdeaProjects/vue-tsx-pro（相对本仓库根）
//   --check   只比较不写入；生成结果与现有文件不一致时以非零状态退出
//
// 零依赖：只用 node: 内置模块，按固定的行格式解析 .ts 文件，不引 TypeScript 编译器。
// 数据来源与许可见 NOTICE.md。

import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const outputPath = resolve(repoRoot, 'src/Rune/Styles/Style.Properties.g.cs');

/** Style 里已有的成员名，生成的属性不能与之重名 */
const RESERVED_MEMBERS = new Set(['Set', 'Declarations', 'Conditions', 'Compiled', 'Item']);

/** 带这些措辞的条目在数据里被标为“不是真实 CSS 属性”，不生成 */
const NOT_A_PROPERTY = /非标准键|不是元素样式属性|无前缀.*尚未支持/;

/** 值类型里出现这些 token 组时视为长度（数字补 px，也可以是间距、尺寸类 token） */
const LENGTH_TOKEN_GROUPS = new Set([
  'Space',
  'Sizes',
  'Radii',
  'BorderWidths',
  'FontSizes',
  'LetterSpacings',
  'StrokeWidths',
]);

const VENDOR_PREFIXES = { Webkit: '-webkit-', Moz: '-moz-', Ms: '-ms-' };

const { values } = parseArgs({
  options: {
    source: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    check: { type: 'boolean', default: false },
  },
});
const sourceRoot = resolve(values.source);
const stylesDir = resolve(sourceRoot, 'packages/tsx/src/styles');

/** 读文件并统一成 LF，缺文件时给出明确的报错 */
function readSource(name) {
  const path = resolve(stylesDir, name);
  if (!existsSync(path)) {
    console.error(`找不到 ${path}；用 --source 指定 vue-tsx-pro 根目录`);
    process.exit(2);
  }
  return readFileSync(path, 'utf8').replaceAll('\r\n', '\n');
}

// ---------- units.ts：数字值的单位规则 ----------

function parseUnits(text) {
  const unitless = text.match(/UNITLESS = new Set\(\s*'([^']+)'/);
  const milliseconds = text.match(/MILLISECONDS = new Set\(\[([^\]]+)\]/);
  if (!unitless || !milliseconds) throw new Error('units.ts 的格式变了：找不到 UNITLESS / MILLISECONDS');
  return {
    unitless: new Set(unitless[1].split(' ')),
    milliseconds: new Set([...milliseconds[1].matchAll(/'(\w+)'/g)].map((m) => m[1])),
  };
}

// ---------- documented.ts：属性与 JSDoc ----------

/**
 * 解析 `interface DocumentedStyles` 里的属性。
 * 每项是一段 2 空格缩进的 JSDoc，后面跟一条以 `;` 结束的声明（可跨多行）。
 * 伪元素（`'::after'?: …`）不是属性，直接忽略。
 */
function parseDocumented(text) {
  const lines = text.split('\n');
  const start = lines.findIndex((l) => l.startsWith('export interface DocumentedStyles'));
  if (start < 0) throw new Error('documented.ts 的格式变了：找不到 DocumentedStyles');
  const entries = [];
  for (let i = start + 1; lines[i] !== '}'; i++) {
    if (lines[i] !== '  /**') continue;
    const doc = [];
    for (i++; lines[i] !== '   */'; i++) doc.push(lines[i].replace(/^ {3}\* ?/, ''));
    let declaration = '';
    for (i++; ; i++) {
      declaration += lines[i].trim() + ' ';
      if (lines[i].trimEnd().endsWith(';')) break;
    }
    const head = declaration.match(/^(\w+)\?: StyleValue</);
    if (head) entries.push({ name: head[1], doc, declaration });
  }
  return entries;
}

/** 值类型里引用的主题 token 组，如 `keyof QualifiedColorsTokens` → Colors */
function tokenGroups(declaration) {
  const groups = [...declaration.matchAll(/keyof (?:Qualified)?(\w+)Tokens/g)].map((m) => m[1]);
  return new Set(groups);
}

// ---------- 命名 ----------

const csharpName = (name) => name[0].toUpperCase() + name.slice(1);

function cssName(name) {
  const kebab = (s) => s.replace(/[A-Z]/g, (c) => '-' + c.toLowerCase());
  const vendor = Object.keys(VENDOR_PREFIXES).find((p) => name.startsWith(p) && /^[A-Z]/.test(name.slice(p.length)));
  if (vendor) return VENDOR_PREFIXES[vendor] + kebab(name.slice(vendor.length)).replace(/^-/, '');
  return kebab(name);
}

// ---------- 文档注释 ----------

const escapeXml = (s) => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');

/** 行内文本：转义 XML，反引号包起来的部分变成 <c> */
function inline(text) {
  return text
    .split('`')
    .map((part, i) => (i % 2 === 1 ? `<c>${escapeXml(part)}</c>` : escapeXml(part)))
    .join('');
}

/**
 * JSDoc 行 → 段落、列表、@see 链接。
 * 空行分段；`- ` 开头的连续行是列表（缩进的后续行并入上一项）；`@see` 之外的 `@` 开头行当普通文字
 * （如 “@font-face 中字体…”）。
 */
function parseDoc(lines) {
  const blocks = [];
  const links = [];
  let paragraph = null;
  let list = null;
  const flush = () => {
    paragraph = null;
    list = null;
  };
  for (const line of lines) {
    const see = line.match(/^@see (\S+)\s*$/);
    if (see) {
      links.push(see[1]);
      flush();
    } else if (line.trim() === '') {
      flush();
    } else if (line.startsWith('- ')) {
      paragraph = null;
      if (!list) blocks.push((list = { list: [] }));
      list.list.push(line.slice(2));
    } else if (list && line.startsWith(' ')) {
      list.list[list.list.length - 1] += ' ' + line.trim();
    } else {
      list = null;
      if (paragraph) paragraph.text += ' ' + line.trim();
      else blocks.push((paragraph = { text: line.trim() }));
    }
  }
  return { blocks, links };
}

/** 生成属性前的 /// 注释行（含 summary、remarks、seealso） */
function docComment(lines, indent) {
  const { blocks, links } = parseDoc(lines);
  const [first, ...rest] = blocks;
  if (!first?.text) throw new Error('JSDoc 第一段不是文字，无法作为 summary');
  const out = [`<summary>${inline(first.text)}</summary>`];
  if (rest.length > 0) {
    out.push('<remarks>');
    for (const block of rest) {
      if (block.list) {
        out.push('<list type="bullet">');
        for (const item of block.list) out.push(`<item><description>${inline(item)}</description></item>`);
        out.push('</list>');
      } else {
        out.push(`<para>${inline(block.text)}</para>`);
      }
    }
    out.push('</remarks>');
  }
  for (const href of links) out.push(`<seealso href="${escapeXml(href).replaceAll('"', '&quot;')}"/>`);
  return out.map((l) => `${indent}/// ${l}`);
}

// ---------- 分类与生成 ----------

/**
 * 决定 C# 类型与调用。
 * 颜色 token → CssColor；长度 token → CssLength；其余 → CssValue 并带单位规则。
 * 长度属性若按单位规则数字不补 px（如 strokeWidth），改用 CssValue，否则数字会被错补 px。
 */
function classify(name, groups, units) {
  const unit = units.unitless.has(name) ? 'None' : units.milliseconds.has(name) ? 'Milliseconds' : 'Pixels';
  const hasColor = groups.has('Colors');
  const hasLength = [...groups].some((g) => LENGTH_TOKEN_GROUPS.has(g));
  if (hasColor && hasLength) return { kind: 'value', unit, note: '同时接受颜色与长度 token' };
  if (hasColor) return { kind: 'color' };
  if (hasLength && unit !== 'Pixels') return { kind: 'value', unit, note: `长度 token 但数字不补 px（${unit}）` };
  if (hasLength) return { kind: 'length' };
  return { kind: 'value', unit };
}

function generate() {
  const units = parseUnits(readSource('units.ts'));
  const entries = parseDocumented(readSource('documented.ts'));
  const warnings = [];
  const skipped = [];
  const notes = [];
  const counts = { color: 0, length: 0, value: 0 };
  const seen = new Map();
  const members = [];

  for (const entry of entries) {
    if (NOT_A_PROPERTY.test(entry.doc[0])) {
      skipped.push(`${entry.name}（${entry.doc[0]}）`);
      continue;
    }
    const csName = csharpName(entry.name);
    const css = cssName(entry.name);
    if (RESERVED_MEMBERS.has(csName)) warnings.push(`${entry.name} 与 Style 已有成员 ${csName} 重名`);
    if (seen.has(csName)) warnings.push(`${entry.name} 与 ${seen.get(csName)} 生成了相同的 C# 名 ${csName}`);
    seen.set(csName, entry.name);

    // 说明里写的 CSS 名与按命名规则推出的不一致时提示（说明里第一处括号里的连字符名）
    const documented = entry.doc[0].match(/[（(](-?[a-z][a-z-]*)[）)]/)?.[1];
    if (documented && documented !== css) warnings.push(`${entry.name}：说明里写 ${documented}，推出 ${css}`);

    const cls = classify(entry.name, tokenGroups(entry.declaration), units);
    if (cls.note) notes.push(`${entry.name}：${cls.note}`);
    counts[cls.kind]++;
    const [type, call] = {
      color: ['CssColor', `Set("${css}", value)`],
      length: ['CssLength', `Set("${css}", value)`],
      value: ['CssValue', `Set("${css}", value, NumberUnit.${cls.unit})`],
    }[cls.kind];
    members.push({
      csName,
      text: [...docComment(entry.doc, '    '), `    public ${type}? ${csName} { init => ${call}; }`].join('\n'),
    });
  }

  members.sort((a, b) => (a.csName < b.csName ? -1 : a.csName > b.csName ? 1 : 0));
  const content = [
    '// <auto-generated>',
    '// 由 tools/css-gen/generate.mjs 生成，请勿手改；数据来源见 tools/css-gen/NOTICE.md。',
    '// </auto-generated>',
    '#nullable enable',
    '',
    'namespace Rune.Styles;',
    '',
    'public sealed partial class Style',
    '{',
    members.map((m) => m.text).join('\n\n'),
    '}',
    '',
  ].join('\n');

  return { content, total: members.length, counts, skipped, warnings, notes };
}

// ---------- 入口 ----------

const result = generate();
for (const w of result.warnings) console.warn(`警告：${w}`);

if (values.check) {
  const current = existsSync(outputPath) ? readFileSync(outputPath, 'utf8') : null;
  if (current !== result.content) {
    console.error('Style.Properties.g.cs 与数据源不一致，请运行 node tools/css-gen/generate.mjs');
    process.exit(1);
  }
  console.log('Style.Properties.g.cs 与数据源一致');
} else {
  writeFileSync(outputPath, result.content);
  const { total, counts, skipped, notes } = result;
  console.log(`已生成 ${total} 个属性：CssColor ${counts.color}、CssLength ${counts.length}、CssValue ${counts.value}`);
  console.log(`跳过 ${skipped.length} 个非 CSS 属性：\n  ${skipped.join('\n  ')}`);
  if (notes.length > 0) console.log(`分类说明：\n  ${notes.join('\n  ')}`);
}
