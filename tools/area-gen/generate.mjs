#!/usr/bin/env node
// 从 vue-tsx-pro 的省市区数据（packages/ui/src/area/data.ts，取自 @vant/area-data，MIT）生成 Rune.UI.Area 内嵌的 JSON。
//
// 用法：node tools/area-gen/generate.mjs [--source <vue-tsx-pro 根目录>] [--check]
//   --source  vue-tsx-pro 仓库根目录，默认 ../../IdeaProjects/vue-tsx-pro（相对本仓库根）
//   --check   只比较不写入；生成文件与现有内容不一致时以非零状态退出（CI 用）
//
// 输出：src/Rune.UI.Area/Data/area.json：{ "province": { 代码: 名称 }, "city": {…}, "county": {…} }，键按代码排序。
// data.ts 是一个对象字面量（键是不带引号的 6 位代码与 *_list），按 JSON 读：去掉注释与类型标注、补上键的引号、去掉尾逗号。
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { parseArgs } from 'node:util';

const repoRoot = resolve(import.meta.dirname, '../..');
const { values } = parseArgs({
  options: {
    source: { type: 'string', default: resolve(repoRoot, '../../IdeaProjects/vue-tsx-pro') },
    check: { type: 'boolean', default: false },
  },
});
const sourceFile = resolve(values.source, 'packages/ui/src/area/data.ts');
if (!existsSync(sourceFile)) {
  console.error(`找不到 ${sourceFile}；用 --source 指定 vue-tsx-pro 根目录`);
  process.exit(1);
}
const output = resolve(repoRoot, 'src/Rune.UI.Area/Data/area.json');

const text = readFileSync(sourceFile, 'utf8');
const literal = text.slice(text.indexOf('} = {') + 4).replace(/;\s*$/, '');
const json = literal
  .replace(/\/\/.*$/gm, '')
  .replace(/([{,]\s*)(\w+)\s*:/g, '$1"$2":')
  .replace(/'/g, '"')
  .replace(/,(\s*[}\]])/g, '$1');
const data = JSON.parse(json);

const sorted = (record) => Object.fromEntries(Object.entries(record).sort(([a], [b]) => a.localeCompare(b)));
const result = `${JSON.stringify({ province: sorted(data.province_list), city: sorted(data.city_list), county: sorted(data.county_list) })}\n`;

if (values.check) {
  const current = existsSync(output) ? readFileSync(output, 'utf8') : '';
  if (current !== result) {
    console.error(`${output} 与数据源不一致：运行 node tools/area-gen/generate.mjs 重新生成`);
    process.exit(1);
  }
  console.log('生成文件与数据源一致');
} else {
  writeFileSync(output, result);
  const counts = [data.province_list, data.city_list, data.county_list].map((list) => Object.keys(list).length);
  console.log(`已写入 ${output}：${counts[0]} 个省、${counts[1]} 个市、${counts[2]} 个区县`);
}
