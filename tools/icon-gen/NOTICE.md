# 图标数据的来源

`generate.mjs` 读取 npm 包 [`@lucide/icons`](https://www.npmjs.com/package/@lucide/icons)（当前 1.59.0，来自 [Lucide](https://lucide.dev)）的 `dist/esm/icons/*.mjs`，生成 `src/Rune.Icons/Lucide.g.cs`（全集）与 `src/Rune.UI/Icons/BuiltInIcons.g.cs`（组件库内置的小集合）。

| 内容 | 来源 | 许可 |
| ---- | ---- | ---- |
| 图标名与 SVG 元素数据 | [Lucide](https://github.com/lucide-icons/lucide) | ISC |
| 其中派生自 Feather 的图标 | [Feather](https://github.com/feathericons/feather)（Cole Bemis） | MIT |

许可全文见 `src/Rune.Icons/LICENSE-lucide.txt`（即 `@lucide/icons` 包内的 LICENSE，含 ISC 与 Feather 的 MIT 声明），随 Rune.Icons 包分发。`BuiltInIcons.g.cs` 同样是 Lucide 数据，随 Rune.UI 分发时请一并保留此声明。

更新图标：升级 vue-tsx-pro 里的 `@lucide/icons`（或用 `--source` 指定其他目录），运行 `node tools/icon-gen/generate.mjs`；`--check` 可校验生成文件是否过期。数据源在 node_modules 里，CI 拿不到，目前只在本地使用。
