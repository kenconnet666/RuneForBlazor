# 第三方浏览器脚本的来源

`generate.mjs` 用 `npm pack` 下载下面的 npm 包（版本见脚本里的常量），取其浏览器构建放进 `src/Rune.UI/wwwroot/vendor`，
随 Rune.UI 包分发（`_content/RuneForBlazor.UI/vendor/…`），JS 初始化器第一次用到时才 `import()`。

| 文件 | 来源 | 版本 | 许可 |
| ---- | ---- | ---- | ---- |
| `floating-ui.core.mjs`、`floating-ui.dom.mjs` | [Floating UI](https://github.com/floating-ui/floating-ui)（`@floating-ui/core`、`@floating-ui/dom` 的 `dist/*.browser.min.mjs`） | 1.8.0 | MIT |
| `shiki/`（shiki.mjs、langs/*.mjs、chunks/*.mjs） | [Shiki](https://github.com/shikijs/shiki)（`shiki/core`、`shiki/engine/javascript`、`@shikijs/langs` 的语法，含其依赖） | 4.5.0 | MIT（各包许可汇总在 `LICENSE-shiki.txt`） |

dom 的构建 import `"@floating-ui/core"`，生成时改指同目录的 `./floating-ui.core.mjs`，内容不做别的改动。
许可全文见 `src/Rune.UI/wwwroot/vendor/LICENSE-floating-ui.txt`。

版本与 vue-tsx-pro 的依赖保持一致（两边浮层的位置算法相同）。升级：改 `FLOATING_UI_VERSION`，运行 `node tools/vendor-gen/generate.mjs`；`--check` 校验（CI 会跑）。

Shiki 由多个模块组成，没有现成的单文件浏览器构建：`tools/vendor-gen/shiki` 里的 package.json 与 package-lock.json 锁定版本（只给生成用，
不是 Rune 的依赖），生成时 `npm ci` 到临时目录，用 esbuild 打包：核心一个模块，每种语言一个模块，razor 引用的 html、css、javascript 等
共用语法拆进 chunks。esbuild 的工作目录设成临时目录，块名的哈希才每次相同（`--check` 依赖这一点）。打包的语言见脚本里的 `SHIKI_LANGUAGES`，
与 `wwwroot/js/highlight.js` 的 `LANGUAGES` 保持一致；版本与 vue-tsx-pro 一致。
