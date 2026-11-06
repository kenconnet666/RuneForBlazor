# 内置主题的来源

`generate.mjs` 读取 [vue-tsx-pro](https://github.com/kenconnet666/vue-tsx-pro) 的 `packages/ui/src/config/theme`（版本见 CI 的 `VUE_TSX_PRO_REF`），
生成 `src/Rune.UI/Config/Theme/Themes.g.cs`：16 套内置主题，以及所有主题共用的 Base（各组 token 的默认值：字号、间距、尺寸、动效等组只取这里，主题没写的 token 也取这里）。

| 内容 | 来源 | 许可 |
| ---- | ---- | ---- |
| 主题的取值、token 的分组与命名 | vue-tsx-pro（同一作者） | 与本仓库相同 |
| 亮色、暗色主题的颜色与阴影，其他主题中性色的色阶约定 | [Radix Colors](https://github.com/radix-ui/colors) / [Radix Themes](https://github.com/radix-ui/themes) | MIT |

更新：改 vue-tsx-pro 的主题后运行 `node tools/theme-gen/generate.mjs`（`--source` 指定 vue-tsx-pro 根目录）；
`--check` 校验生成文件与数据源一致（CI 会跑）。vue-tsx-pro 加了新 token 而这边没有对应时脚本报错，在 `src/Rune.UI/Config/Theme/Tokens.cs` 里补上。
本库独有的 token（不在顶层里的浮动元素的层级）写在脚本的 `RUNE_ONLY` 里。
