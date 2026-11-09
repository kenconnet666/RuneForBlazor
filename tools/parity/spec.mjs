#!/usr/bin/env node
// 取 vue-tsx-pro 的组件规格（文档站发布的 /spec/components.json：每个组件的属性、种类、默认值），精简后存成
// tests/Rune.Tests/Parity/vue-spec.json，给 SpecParityTests 比对 Rune 组件的参数。跟进 vue 时与 VUE_TSX_PRO_REF 一起更新。
//
// 用法：node tools/parity/spec.mjs [--file <components.json>]
//   --file  用本地的规格文件；不写时从 https://vue-tsx-pro.zerodep.dpdns.org/spec/components.json 下载
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { parseArgs } from 'node:util';

const SPEC_URL = 'https://vue-tsx-pro.zerodep.dpdns.org/spec/components.json';
const repoRoot = resolve(import.meta.dirname, '../..');
const output = resolve(repoRoot, 'tests/Rune.Tests/Parity/vue-spec.json');
const { values } = parseArgs({ options: { file: { type: 'string' } } });

const spec = values.file ? JSON.parse(readFileSync(values.file, 'utf8')) : await (await fetch(SPEC_URL)).json();
const components = Object.fromEntries(
  spec.components
    .map((component) => [
      component.name,
      component.api.props.map((prop) => ({ name: prop.name, kind: prop.kind, ...(prop.default ? { default: prop.default } : {}) })),
    ])
    .sort(([a], [b]) => a.localeCompare(b)),
);
writeFileSync(output, `${JSON.stringify({ version: spec.version, components }, null, 1)}\n`);
console.log(`已写入 ${output}：vue-tsx-pro ${spec.version}，${Object.keys(components).length} 个组件`);
