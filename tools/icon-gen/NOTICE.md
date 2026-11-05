# 图标数据的来源

`generate.mjs` 用 `npm pack` 下载 npm 包 [`lucide-static`](https://www.npmjs.com/package/lucide-static)（版本见脚本里的 `LUCIDE_VERSION`，当前 1.69.0，来自 [Lucide](https://lucide.dev)），
读取其中的官方 SVG 文件（`icons/*.svg`）与关键词（`tags.json`），生成 `src/Rune.Icons/Lucide.g.cs`（全集）与 `src/Rune.UI/Components/Base/Icon/BuiltInIcons.g.cs`（组件库内置的小集合），
并把包里的 LICENSE 复制为 `src/Rune.Icons/LICENSE-lucide.txt`。

| 内容 | 来源 | 许可 |
| ---- | ---- | ---- |
| 图标名、SVG 元素、关键词 | [Lucide](https://github.com/lucide-icons/lucide) | ISC |
| 其中派生自 Feather 的图标 | [Feather](https://github.com/feathericons/feather)（Cole Bemis） | MIT |

许可全文见 `src/Rune.Icons/LICENSE-lucide.txt`（含 ISC 与 Feather 的 MIT 声明），随 Rune.Icons 包分发。`BuiltInIcons.g.cs` 同样是 Lucide 数据，随 Rune.UI 分发时请一并保留此声明。

更新图标：改 `LUCIDE_VERSION`，运行 `node tools/icon-gen/generate.mjs`；`--check` 校验生成文件是否与该版本一致（CI 会跑），`--source` 可指定已解压的包目录（离线时）。
Lucide 改过名的图标（如 circle-alert → circle-exclamation-point）旧名生成为 [Obsolete] 的转发属性。
