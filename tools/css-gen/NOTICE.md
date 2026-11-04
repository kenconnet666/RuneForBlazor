# CSS 属性表的数据来源

`generate.mjs` 读取姊妹项目 vue-tsx-pro 已生成的 `packages/tsx/src/styles/documented.ts`（各 CSS 属性的中文说明与值类型）和 `units.ts`（数字单位规则），生成 `src/Rune/Styles/Style.Properties.g.cs`。vue-tsx-pro 的 `scripts/css-docs/NOTICE.md` 记录了这些数据的上游，沿用如下：

| 内容                                      | 来源                                                                                                                           | 许可       |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | ---------- |
| 属性集合、伪元素、值类型、数字单位规则    | [StyleX](https://github.com/facebook/stylex) 0.19.1 的类型声明                                                                 | MIT        |
| 初始值、是否继承、标准状态、MDN 链接      | [mdn-data](https://github.com/mdn/data)                                                                                        | CC0-1.0    |
| 浏览器支持（Baseline）                    | [web-features](https://github.com/web-platform-dx/web-features)                                                                | Apache-2.0 |
| 中文说明、常用值、适用场景（`docs.json`） | vue-tsx-pro 维护；起点是 [zerodep-js](https://github.com/kenconnet666/zerodep-js) 的 `scripts/css-author-docs.json`（同一作者，MIT） | MIT        |

约定：

- 中文说明用自己的话写，不复制 MDN 正文（MDN 正文为 CC-BY-SA），只通过 `<seealso>` 链接到 MDN。
- 属性数据变化时，先在 vue-tsx-pro 里运行 `pnpm css-docs`，再在本仓库运行 `node tools/css-gen/generate.mjs`；`--check` 可用于 CI 校验生成文件是否过期。
