# 色阶数据的来源

`generate.mjs` 用 `npm pack` 下载 npm 包 [`@radix-ui/colors`](https://www.npmjs.com/package/@radix-ui/colors)（版本见脚本里的 `RADIX_VERSION`，当前 3.0.0，来自 [Radix Colors](https://www.radix-ui.com/colors)），
读取其中各色阶的亮色、暗色 12 级，生成 `src/Rune.UI/Config/Theme/Customize/RadixColors.g.cs`（主题编辑器的品牌色、中性色），
并把包里的 LICENSE 复制为 `src/Rune.UI/LICENSE-radix-colors.txt`（随 Rune.UI 包分发）。

| 内容 | 来源 | 许可 |
| ---- | ---- | ---- |
| 色阶的颜色值 | [Radix Colors](https://github.com/radix-ui/colors)（WorkOS） | MIT |

更新：改 `RADIX_VERSION`，运行 `node tools/palette-gen/generate.mjs`；`--check` 校验生成文件是否与该版本一致（CI 会跑），`--source` 可指定已解压的包目录（离线时）。
内置主题（Themes.g.cs，由 theme-gen 从 vue-tsx-pro 生成）的配色同样取自这些色阶（indigo 与 slate）。
