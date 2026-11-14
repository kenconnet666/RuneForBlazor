# 第三方浏览器脚本的来源

`generate.mjs` 用下面的 npm 包（版本见锁文件与脚本里的常量）打包浏览器用的模块放进 `src/Rune.UI/wwwroot/vendor`，
随 Rune.UI 包分发（`_content/RuneForBlazor.UI/vendor/…`），JS 初始化器第一次用到时才 `import()`；three.js 放进 Rune.Three 的 `wwwroot/vendor`。

| 文件 | 来源 | 版本 | 许可 |
| ---- | ---- | ---- | ---- |
| `src/Rune.Three/wwwroot/vendor/three/`（three.js、addons.js、chunks/*.js） | [three.js](https://github.com/mrdoob/three.js)（`three` 的全部导出与 `three/addons` 的 GLTFLoader、RoomEnvironment） | 0.186.1 | MIT（`src/Rune.Three/wwwroot/vendor/LICENSE-three.txt`） |
| `shiki/`（shiki.js、langs/*.js、chunks/*.js） | [Shiki](https://github.com/shikijs/shiki)（`shiki/core`、`shiki/engine/javascript`、`@shikijs/langs` 的语法，含其依赖） | 4.5.0 | MIT（各包许可汇总在 `LICENSE-shiki.txt`） |

浮层定位不用第三方库（同 vue-tsx-pro 0.27.0），算法在 `src/Rune.UI/wwwroot/js/overlay/floating.js`。重新生成运行 `node tools/vendor-gen/generate.mjs`；`--check` 校验（CI 会跑）。

three.js 随 Rune.Three 包分发（`_content/RuneForBlazor.Three/vendor/three/…`）：`tools/vendor-gen/three` 的锁文件固定版本（与 vue-tsx-pro 一致），
esbuild 打包成 three.js（整个 three）与 addons.js 两个入口，共用的代码拆进 chunks；使用方自己的场景从同一个 three.js 导入，页面上只有一份 three。

Shiki 由多个模块组成，没有现成的单文件浏览器构建：`tools/vendor-gen/shiki` 里的 package.json 与 package-lock.json 锁定版本（只给生成用，
不是 Rune 的依赖），生成时 `npm ci` 到临时目录，用 esbuild 打包：核心一个模块，每种语言一个模块，razor 引用的 html、css、javascript 等
共用语法拆进 chunks。esbuild 的工作目录设成临时目录，块名的哈希才每次相同（`--check` 依赖这一点）。打包的语言见脚本里的 `SHIKI_LANGUAGES`，
与 `wwwroot/js/highlight.js` 的 `LANGUAGES` 保持一致；版本与 vue-tsx-pro 一致。
