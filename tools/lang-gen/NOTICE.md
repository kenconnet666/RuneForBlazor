# 语言包的来源

`generate.mjs` 读取 [vue-tsx-pro](https://github.com/kenconnet666/vue-tsx-pro) 的 `packages/ui/src/config/lang`（版本见 CI 的 `VUE_TSX_PRO_REF`），
生成 `src/Rune.UI/Config/Lang/Lang.g.cs`：`Lang` 记录（每条文案一个属性，说明取自 lang.ts）与内置的简体中文、美国英语。

- 字段的顺序、说明与类型来自 `lang.ts` 的 `Lang` 接口；每种语言的值由 Node 直接导入 `zh-CN.ts`、`en-US.ts` 取得（Node 24 自带去掉 TS 类型；
  导入前去掉 zod 的语言包，表单校验文案不移植）。
- 需要插值的文案在 vue-tsx-pro 里是箭头函数，脚本把源码翻译成 C# 的 lambda（模板字符串、三元、`== null`、真值判断、`join`、
  `toLocaleString`、按下标取字、对象字面量取值）。翻译不了的写法报错并列出字段：扩展翻译器，或把字段加进 `SKIPPED` 并写明原因。
- 不移植的字段（所在模块不移植、或由别的机制负责）列在脚本的 `SKIPPED` 里。

| 内容 | 来源 | 许可 |
| ---- | ---- | ---- |
| 文案与字段的分组、命名 | vue-tsx-pro（同一作者） | 与本仓库相同 |

更新：vue-tsx-pro 改了语言包后运行 `node tools/lang-gen/generate.mjs`（`--source` 指定 vue-tsx-pro 根目录）；`--check` 校验生成文件与数据源一致（CI 会跑）。
