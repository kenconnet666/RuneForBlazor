# 第三方浏览器脚本的来源

`generate.mjs` 用 `npm pack` 下载下面的 npm 包（版本见脚本里的常量），取其浏览器构建放进 `src/Rune.UI/wwwroot/vendor`，
随 Rune.UI 包分发（`_content/Rune.UI/vendor/…`），JS 初始化器第一次用到时才 `import()`。

| 文件 | 来源 | 版本 | 许可 |
| ---- | ---- | ---- | ---- |
| `floating-ui.core.mjs`、`floating-ui.dom.mjs` | [Floating UI](https://github.com/floating-ui/floating-ui)（`@floating-ui/core`、`@floating-ui/dom` 的 `dist/*.browser.min.mjs`） | 1.8.0 | MIT |

dom 的构建 import `"@floating-ui/core"`，生成时改指同目录的 `./floating-ui.core.mjs`，内容不做别的改动。
许可全文见 `src/Rune.UI/wwwroot/vendor/LICENSE-floating-ui.txt`。

版本与 vue-tsx-pro 的依赖保持一致（两边浮层的位置算法相同）。升级：改 `FLOATING_UI_VERSION`，运行 `node tools/vendor-gen/generate.mjs`；`--check` 校验（CI 会跑）。
