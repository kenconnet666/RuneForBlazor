// 从 vue-tsx-pro 的语言包（packages/ui/src/config/lang）生成 Rune.UI 的语言包：
//
//   src/Rune.UI/Configuration/Lang.g.cs   Lang 记录（每条文案一个属性，带 lang.ts 里的中文说明）与 ZhCn、EnUs 两份内置语言
//
// 读法：lang.ts 的 Lang 接口给出字段的顺序、说明与类型（函数的参数类型）；zh-CN.ts、en-US.ts 由 Node 直接导入
// （Node 24 自带去掉 TS 类型），取到每条文案的值。需要插值的文案在 vue-tsx-pro 里是箭头函数，这里把函数源码
// 翻译成 C# 的 lambda：模板字符串、三元、== null、真值判断、join、toLocaleString、按下标取字、对象字面量取值。
// 翻译不了的写法报错并列出字段，扩展翻译器或把字段放进 SKIPPED（写明原因）。
//
// 依赖：node 内置模块。用法：node tools/lang-gen/generate.mjs [--source <vue-tsx-pro 根目录>] [--check]

import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const outputPath = resolve(repoRoot, 'src/Rune.UI/Configuration/Lang.g.cs');

const { values: options } = parseArgs({
  options: {
    source: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    check: { type: 'boolean', default: false },
  },
});
const langDir = resolve(options.source, 'packages/ui/src/config/lang');

/** 不移植的字段：所在模块不移植，或由别的机制负责 */
const SKIPPED = {
  errorCodes: '接口错误码的文案随数据请求模块（不移植）',
  formIssues: '表单校验文案由 zod 生成；Blazor 用 DataAnnotations 等自己的校验文案',
  messages: '页面级组件（主题编辑器、文档站）自带的文案表，做到这些组件时再定',
  diagram: '流程图模块不移植',
  docsKit: '文档站组件的文案，做文档站时再定',
  patterns: '页面级组合的文案，做到时再定',
  themeCustomizer: '主题编辑器的文案，做到时再定',
};

/** 内置的语言：文件名、导出名、C# 属性名、说明 */
const LOCALES = [
  { file: 'zh-CN.ts', name: 'ZhCn', summary: '简体中文（默认）' },
  { file: 'en-US.ts', name: 'EnUs', summary: '美国英语' },
];

// ───────── lang.ts：字段的顺序、说明与类型 ─────────

/** 取出 Lang 接口里的字段：说明（JSDoc）、名字、是否可选、类型文本 */
function parseLangInterface(source) {
  const start = source.indexOf('export interface Lang {');
  if (start < 0) throw new Error('lang.ts 里找不到 export interface Lang');
  const body = source.slice(source.indexOf('{', start) + 1);
  const members = [];
  let doc = '';
  let i = 0;
  while (i < body.length) {
    const rest = body.slice(i);
    if (/^\s*\}/.test(rest) && depthAt(body, i) === 0) break;
    const comment = rest.match(/^\s*\/\*\*([\s\S]*?)\*\//);
    if (comment) {
      doc = comment[1];
      i += comment[0].length;
      continue;
    }
    const lineComment = rest.match(/^\s*\/\/[^\n]*/);
    if (lineComment) {
      i += lineComment[0].length;
      continue;
    }
    const member = rest.match(/^\s*readonly\s+([A-Za-z_$][\w$]*)(\??):\s*/);
    if (!member) {
      const next = rest.search(/\n/);
      if (next < 0) break;
      i += next + 1;
      continue;
    }
    i += member[0].length;
    const typeStart = i;
    let depth = 0;
    for (; i < body.length; i++) {
      const c = body[i];
      if ('([{<'.includes(c)) depth++;
      else if (')]}>'.includes(c) && !(c === '>' && body[i - 1] === '=')) depth--;
      else if (c === ';' && depth === 0) break;
    }
    members.push({ name: member[1], optional: member[2] === '?', type: body.slice(typeStart, i).trim(), doc: cleanDoc(doc) });
    doc = '';
    i++;
  }
  return members;
}

function depthAt(text, index) {
  let depth = 0;
  for (let i = 0; i < index; i++) {
    if (text[i] === '{') depth++;
    else if (text[i] === '}') depth--;
  }
  return depth;
}

function cleanDoc(raw) {
  return raw
    .split('\n')
    .map((line) => line.replace(/^\s*\*\s?/, '').trim())
    .filter(Boolean)
    .join(' ')
    .replace(/`([^`]*)`/g, '$1');
}

// ───────── 各语言的值：Node 直接导入去掉 zod 依赖后的文件 ─────────

async function loadLocale(file) {
  const text = readFileSync(join(langDir, file), 'utf8')
    .replace(/^import\s+zodMessages\s+from\s+[^\n]+\n/m, '')
    .replace(/zodMessages\(\)\.localeError/g, 'undefined');
  const dir = mkdtempSync(join(tmpdir(), 'rune-lang-'));
  try {
    const path = join(dir, file);
    writeFileSync(path, text);
    const module = await import(pathToFileURL(path).href);
    const value = Object.values(module).find((v) => v && typeof v === 'object');
    if (!value) throw new Error(`${file} 没有导出语言对象`);
    return value;
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

// ───────── 类型：TS → C# ─────────

const pascal = (name) => name[0].toUpperCase() + name.slice(1);

/** 函数类型的参数：(count: number, max?: number) => string */
function parseFunctionType(type) {
  const match = type.match(/^\(([\s\S]*)\)\s*=>\s*string$/);
  if (!match) return null;
  const params = splitTopLevel(match[1]).filter((part) => part.trim()).map((part) => {
    const p = part.trim().match(/^([A-Za-z_$][\w$]*)(\??):\s*([\s\S]+)$/);
    if (!p) throw new Error(`看不懂参数 ${part}`);
    return { name: p[1], optional: p[2] === '?', type: p[3].trim() };
  });
  return params;
}

function splitTopLevel(text) {
  const parts = [];
  let depth = 0;
  let current = '';
  for (const c of text) {
    if ('([{<'.includes(c)) depth++;
    else if (')]}>'.includes(c)) depth--;
    if (c === ',' && depth === 0) {
      parts.push(current);
      current = '';
    } else current += c;
  }
  parts.push(current);
  return parts;
}

/** 参数类型 → C# 类型与它在翻译器里的类别（决定真值判断怎么写） */
function csParam(param) {
  const t = param.type.replace(/\s+/g, ' ');
  let cs;
  let kind;
  if (t === 'number') [cs, kind] = ['double', 'number'];
  else if (t === 'string' || /^keyof /.test(t) || /^'/.test(t) || /^[A-Z]\w*$/.test(t)) [cs, kind] = ['string', 'string'];
  else if (t === 'boolean') [cs, kind] = ['bool', 'bool'];
  else if (/^(readonly )?string\[\]$/.test(t)) [cs, kind] = ['IReadOnlyList<string>', 'list'];
  else if (/^number \| null$/.test(t)) [cs, kind] = ['double?', 'number?'];
  else if (/^string \| null$/.test(t) || /^string \| undefined$/.test(t)) [cs, kind] = ['string?', 'string'];
  else throw new Error(`参数类型 ${t} 还不认识`);
  if (param.optional && !cs.endsWith('?')) {
    cs += '?';
    if (kind === 'number') kind = 'number?';
  }
  return { cs, kind };
}

/** 字段类型 → C# 属性类型；null 表示不认识 */
function csMemberType(member) {
  const t = member.type.replace(/\s+/g, ' ');
  const nullable = member.optional ? '?' : '';
  if (t === 'string') return { kind: 'string', cs: `string${nullable}` };
  if (t === 'number') return { kind: 'int', cs: `int${nullable}` };
  if (/^keyof \w+$/.test(t)) return { kind: 'string', cs: `string${nullable}` };
  if (/^readonly \[string(, string)*\]$/.test(t)) return { kind: 'list', cs: `IReadOnlyList<string>${nullable}` };
  if (/^Readonly<(Partial<)?Record<[^>]+, string>>?>$/.test(t) || /^Readonly<\{[^}]*\}>$/.test(t))
    return { kind: 'dictionary', cs: `IReadOnlyDictionary<string, string>${nullable}` };
  // 两层的表：键 → 一组文案（结果页的标题与说明、每种字段类型可用的运算符）
  if (/^Readonly< ?(Partial<)?Record<.+, (\{ ?(readonly \w+: string;? ?)+\}|Readonly<(Partial<)?Record<[^>]+, string>>?>)>>? ?>$/.test(t))
    return { kind: 'nested', cs: `IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>${nullable}` };
  const params = parseFunctionType(t);
  if (params) {
    const cs = params.map(csParam);
    return { kind: 'function', params: params.map((p, i) => ({ ...p, ...cs[i] })), cs: `Func<${[...cs.map((c) => c.cs), 'string'].join(', ')}>${nullable}` };
  }
  return null;
}

// ───────── 函数源码 → C# lambda ─────────

function tokenize(source) {
  const tokens = [];
  let i = 0;
  const punct = ['===', '!==', '=>', '==', '!=', '<=', '>=', '&&', '||', '??', '(', ')', '[', ']', '{', '}', '.', ',', '?', ':', '+', '-', '*', '/', '<', '>', '!'];
  while (i < source.length) {
    const c = source[i];
    if (/\s/.test(c)) {
      i++;
      continue;
    }
    if (c === '`') {
      const parts = [];
      let text = '';
      i++;
      while (source[i] !== '`') {
        if (source[i] === '\\') {
          text += source[i + 1];
          i += 2;
        } else if (source[i] === '$' && source[i + 1] === '{') {
          parts.push({ text });
          text = '';
          let depth = 1;
          let j = i + 2;
          for (; depth > 0; j++) {
            if (source[j] === '{') depth++;
            else if (source[j] === '}') depth--;
            else if (source[j] === '`') j = skipTemplate(source, j);
          }
          parts.push({ expr: source.slice(i + 2, j - 1) });
          i = j;
        } else text += source[i++];
      }
      parts.push({ text });
      tokens.push({ type: 'template', parts });
      i++;
      continue;
    }
    if (c === "'" || c === '"') {
      let j = i + 1;
      let value = '';
      while (source[j] !== c) {
        if (source[j] === '\\') {
          value += source[j + 1];
          j += 2;
        } else value += source[j++];
      }
      tokens.push({ type: 'string', value });
      i = j + 1;
      continue;
    }
    const number = source.slice(i).match(/^\d+(\.\d+)?/);
    if (number) {
      tokens.push({ type: 'number', value: number[0] });
      i += number[0].length;
      continue;
    }
    const ident = source.slice(i).match(/^[A-Za-z_$][\w$]*/);
    if (ident) {
      tokens.push({ type: 'ident', value: ident[0] });
      i += ident[0].length;
      continue;
    }
    const p = punct.find((op) => source.startsWith(op, i));
    if (!p) throw new Error(`看不懂的字符 ${c}`);
    tokens.push({ type: 'punct', value: p });
    i += p.length;
  }
  return tokens;
}

function skipTemplate(source, start) {
  let j = start + 1;
  while (source[j] !== '`') j += source[j] === '\\' ? 2 : 1;
  return j;
}

const CS_KEYWORDS = new Set([
  'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked', 'class', 'const', 'continue',
  'decimal', 'default', 'delegate', 'do', 'double', 'else', 'enum', 'event', 'explicit', 'extern', 'false', 'finally',
  'fixed', 'float', 'for', 'foreach', 'goto', 'if', 'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock',
  'long', 'namespace', 'new', 'null', 'object', 'operator', 'out', 'override', 'params', 'private', 'protected',
  'public', 'readonly', 'ref', 'return', 'sbyte', 'sealed', 'short', 'sizeof', 'stackalloc', 'static', 'string',
  'struct', 'switch', 'this', 'throw', 'true', 'try', 'typeof', 'uint', 'ulong', 'unchecked', 'unsafe', 'ushort',
  'using', 'virtual', 'void', 'volatile', 'while',
  // 上下文关键字：语言写在属性访问器里（field ??=），field 在那里是后备字段
  'field', 'value',
]);

/** 参数名是 C# 关键字时加 @（transferCount 的 checked） */
const csIdentifier = (name) => (CS_KEYWORDS.has(name) ? `@${name}` : name);

/** 把一个箭头函数（源码）翻译成 C# lambda */
function translateFunction(fn, params) {
  const source = fn.toString();
  const arrow = source.match(/^\(?\s*([\w$,\s]*?)\s*\)?\s*=>\s*([\s\S]*)$/);
  if (!arrow) throw new Error('不是箭头函数');
  const names = arrow[1].split(',').map((n) => n.trim()).filter(Boolean);
  if (names.length !== params.length) throw new Error(`参数个数与 lang.ts 不一致：${names.join(', ')}`);
  if (arrow[2].trim().startsWith('{')) throw new Error('函数体是语句块，翻译不了');
  const scope = Object.fromEntries(names.map((n, i) => [n, { name: csIdentifier(params[i].name), kind: params[i].kind }]));
  const expr = parseExpression(tokenize(arrow[2]), scope);
  const csParams = names.length === 1 ? scope[names[0]].name : `(${names.map((n) => scope[n].name).join(', ')})`;
  return `${csParams} => ${asString(expr)}`;
}

/** 表达式解析（Pratt）：结果是 { code, kind }，kind 是 string / number / number? / bool / list / char */
function parseExpression(tokens, scope) {
  let pos = 0;
  const peek = () => tokens[pos];
  const next = () => tokens[pos++];
  const isPunct = (value) => peek()?.type === 'punct' && peek().value === value;
  const expect = (value) => {
    if (!isPunct(value)) throw new Error(`应当是 ${value}`);
    pos++;
  };

  const ternary = () => {
    const condition = logicalOr();
    if (!isPunct('?')) return condition;
    pos++;
    const yes = ternary();
    expect(':');
    const no = ternary();
    const kind = yes.kind === 'string' || no.kind === 'string' ? 'string' : yes.kind;
    return { code: `(${truthy(condition)} ? ${coerce(yes, kind)} : ${coerce(no, kind)})`, kind };
  };
  const logicalOr = () => {
    let left = logicalAnd();
    while (isPunct('||')) {
      pos++;
      const right = logicalAnd();
      left = { code: `(${truthy(left)} || ${truthy(right)})`, kind: 'bool' };
    }
    return left;
  };
  const logicalAnd = () => {
    let left = equality();
    while (isPunct('&&')) {
      pos++;
      const right = equality();
      left = { code: `(${truthy(left)} && ${truthy(right)})`, kind: 'bool' };
    }
    return left;
  };
  const equality = () => {
    let left = additive();
    while (['==', '===', '!=', '!=='].some(isPunct)) {
      const op = next().value.startsWith('!') ? '!=' : '==';
      const right = additive();
      left = { code: `(${left.code} ${op} ${right.code})`, kind: 'bool' };
    }
    return left;
  };
  const additive = () => {
    let left = multiplicative();
    while (isPunct('+') || isPunct('-')) {
      const op = next().value;
      const right = multiplicative();
      const kind = left.kind === 'string' || right.kind === 'string' ? 'string' : 'number';
      left = { code: `(${left.code} ${op} ${right.code})`, kind };
    }
    return left;
  };
  const multiplicative = () => {
    let left = unary();
    while (isPunct('*') || isPunct('/')) {
      const op = next().value;
      const right = unary();
      left = { code: `(${left.code} ${op} ${right.code})`, kind: 'number' };
    }
    return left;
  };
  const unary = () => {
    if (isPunct('!')) {
      pos++;
      return { code: `!${truthy(unary())}`, kind: 'bool' };
    }
    return postfix(primary());
  };
  const postfix = (target) => {
    for (;;) {
      if (isPunct('.')) {
        pos++;
        const member = next().value;
        if (member === 'length') target = { code: `${target.code}.Length`, kind: 'number' };
        else if (member === 'join') {
          expect('(');
          const separator = ternary();
          expect(')');
          target = { code: `string.Join(${separator.code}, ${target.code})`, kind: 'string' };
        } else if (member === 'toLocaleString') {
          expect('(');
          const culture = isPunct(')') ? null : ternary();
          expect(')');
          target = culture
            ? { code: `LangFormat.Number(${target.code}, ${culture.code})`, kind: 'string' }
            : { code: `${target.code}.ToString("#,0.###", CultureInfo.CurrentCulture)`, kind: 'string' };
        } else throw new Error(`不认识的成员 .${member}`);
      } else if (isPunct('[')) {
        pos++;
        const index = ternary();
        expect(']');
        if (target.kind === 'object') {
          const arms = target.entries.map(([key, value]) => `${JSON.stringify(key)} => ${value.code}`);
          target = { code: `(${index.code} switch { ${arms.join(', ')}, _ => "" })`, kind: 'string' };
        } else target = { code: `${target.code}[(int)(${index.code})]`, kind: 'char' };
      } else return target;
    }
  };
  const primary = () => {
    const token = next();
    if (!token) throw new Error('表达式不完整');
    if (token.type === 'string') return { code: JSON.stringify(token.value), kind: 'string' };
    if (token.type === 'number') return { code: token.value, kind: 'number' };
    if (token.type === 'template') {
      const pieces = token.parts
        .map((part) => ('text' in part ? (part.text ? JSON.stringify(part.text) : null) : asString(parseExpression(tokenize(part.expr), scope))))
        .filter(Boolean);
      return { code: pieces.length === 0 ? '""' : pieces.length === 1 ? pieces[0] : `string.Concat(${pieces.join(', ')})`, kind: 'string' };
    }
    if (token.type === 'ident') {
      if (token.value === 'null' || token.value === 'undefined') return { code: 'null', kind: 'null' };
      if (token.value === 'true' || token.value === 'false') return { code: token.value, kind: 'bool' };
      if (token.value === 'String') {
        expect('(');
        const inner = ternary();
        expect(')');
        return { code: asString(inner), kind: 'string' };
      }
      const variable = scope[token.value];
      if (!variable) throw new Error(`不认识的名字 ${token.value}`);
      return { code: variable.name, kind: variable.kind };
    }
    if (token.value === '(') {
      const inner = ternary();
      expect(')');
      return inner;
    }
    if (token.value === '{') {
      const entries = [];
      while (!isPunct('}')) {
        const key = next();
        expect(':');
        entries.push([key.value, ternary()]);
        if (isPunct(',')) pos++;
      }
      pos++;
      return { code: '', kind: 'object', entries };
    }
    throw new Error(`看不懂 ${token.value}`);
  };

  const result = ternary();
  if (pos < tokens.length) throw new Error(`后面还有没读完的：${tokens.slice(pos).map((t) => t.value ?? t.type).join(' ')}`);
  return result;
}

/** JS 的真值判断写成 C# 的 bool */
function truthy({ code, kind }) {
  switch (kind) {
    case 'bool':
      return code;
    case 'string':
      return `!string.IsNullOrEmpty(${code})`;
    case 'number':
      return `${code} != 0`;
    case 'number?':
      return `${code} is not null and not 0`;
    case 'list':
      return `${code}.Count > 0`;
    default:
      throw new Error(`${code} 的真值判断还不会写（${kind}）`);
  }
}

/** 三元两边类型对齐：都要字符串时把数字转成字符串 */
function coerce(expr, kind) {
  if (kind === 'string' && expr.kind !== 'string') return asString(expr);
  return expr.code;
}

/** 放进字符串里：数字按当前文化格式化（C# 的默认），字符串原样 */
function asString(expr) {
  if (expr.kind === 'string') return expr.code;
  if (expr.kind === 'char') return `${expr.code}.ToString()`;
  if (expr.kind === 'number' || expr.kind === 'number?') return `${expr.code}?.ToString(CultureInfo.CurrentCulture)`.replace(/(\w)\?\.ToString/, (m, c) => (expr.kind === 'number' ? `${c}.ToString` : m));
  if (expr.kind === 'null') return '""';
  throw new Error(`${expr.code} 放不进字符串（${expr.kind}）`);
}

// ───────── 值 → C# 字面量 ─────────

const dictionary = (value) =>
  `new Dictionary<string, string> { ${Object.entries(value).map(([k, v]) => `[${JSON.stringify(k)}] = ${JSON.stringify(v)}`).join(', ')} }`;

function csValue(type, value, params) {
  if (value === undefined || value === null) return 'null';
  switch (type.kind) {
    case 'string':
      return JSON.stringify(value);
    case 'int':
      return String(value);
    case 'list':
      return `[${value.map((v) => JSON.stringify(v)).join(', ')}]`;
    case 'dictionary':
      return dictionary(value);
    case 'nested':
      return `new Dictionary<string, IReadOnlyDictionary<string, string>> { ${Object.entries(value).map(([k, v]) => `[${JSON.stringify(k)}] = ${dictionary(v)}`).join(', ')} }`;
    case 'function':
      return translateFunction(value, params);
    default:
      throw new Error(`不认识的类型 ${type.kind}`);
  }
}

// ───────── 生成 ─────────

const members = parseLangInterface(readFileSync(join(langDir, 'lang.ts'), 'utf8'));
const locales = await Promise.all(LOCALES.map(async (locale) => ({ ...locale, value: await loadLocale(locale.file) })));
const errors = [];
const properties = [];
for (const member of members) {
  if (member.name in SKIPPED) continue;
  const type = csMemberType(member);
  if (!type) {
    errors.push(`${member.name}：类型 ${member.type} 还不认识`);
    continue;
  }
  const values = {};
  for (const locale of locales) {
    try {
      values[locale.name] = csValue(type, locale.value[member.name], type.params);
    } catch (error) {
      errors.push(`${member.name}（${locale.file}）：${error.message}`);
    }
  }
  properties.push({ member, type, values });
}
for (const locale of locales) {
  const unknown = Object.keys(locale.value).filter((key) => !members.some((m) => m.name === key));
  if (unknown.length) errors.push(`${locale.file} 有 lang.ts 里没有的字段：${unknown.join('、')}`);
}
if (errors.length) {
  console.error(`语言包有 ${errors.length} 处翻译不了：\n  ${errors.join('\n  ')}`);
  process.exit(1);
}

const escapeXml = (text) => text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const lines = [
  '// <auto-generated>',
  '// 由 tools/lang-gen/generate.mjs 从 vue-tsx-pro 的 packages/ui/src/config/lang 生成，不要手改。',
  '// </auto-generated>',
  '#nullable enable',
  'using System.Globalization;',
  '',
  'namespace Rune.UI;',
  '',
  '/// <summary>',
  '/// 语言：地区信息与组件的文案平铺在一个记录里（同 vue-tsx-pro 的 Lang）。组件从 <see cref="UIConfig.Lang"/> 读，',
  '/// 需要插值的文案是函数（<c>lang.PaginationTotal(42)</c>）。内置简体中文（默认）与美国英语；',
  '/// 改几条用 <c>Lang.ZhCn with { Close = "关掉" }</c>，新增一种语言时把每个属性都填上（required）。',
  '/// </summary>',
  'public sealed partial record Lang',
  '{',
];
for (const { member, type } of properties) {
  if (member.doc) lines.push(`    /// <summary>${escapeXml(member.doc)}</summary>`);
  lines.push(`    public ${member.optional ? '' : 'required '}${type.cs} ${pascal(member.name)} { get; init; }`, '');
}
for (const locale of locales) {
  lines.push(`    /// <summary>${locale.summary}。</summary>`);
  lines.push(`    public static Lang ${locale.name} => field ??= new()`, '    {');
  for (const { member, values } of properties) {
    if (values[locale.name] === 'null') continue;
    lines.push(`        ${pascal(member.name)} = ${values[locale.name]},`);
  }
  lines.push('    };', '');
}
lines[lines.length - 1] = '}';
const output = lines.join('\n') + '\n';

if (options.check) {
  const current = readFileSync(outputPath, 'utf8');
  if (current.replace(/\r\n/g, '\n') !== output) {
    console.error('Lang.g.cs 与 vue-tsx-pro 的语言包不一致：运行 node tools/lang-gen/generate.mjs 重新生成');
    process.exit(1);
  }
  console.log('语言包与 vue-tsx-pro 一致');
} else {
  writeFileSync(outputPath, output);
  console.log(`已写入 ${outputPath}：${properties.length} 个字段，跳过 ${Object.keys(SKIPPED).length} 个`);
}
