# 文档站（与 vue-tsx-pro 文档站逐像素对照）

目标：Rune 的文档站与 vue-tsx-pro 的文档站在同一地址、同一视口、同一主题下打开，**外壳一致、页面骨架一致、示例画布逐像素一致**；
有意的差别逐条记在下面。外壳、页面骨架、示例画布的逐页对照已经做完一轮（2026-11-07 到 11-09，过程见 git `46db83f:docs/DOCS-SITE.md`）；
页面已按 vue 阶段 25 的 P2.1 合并、挪动，旧地址经 `DocRedirects` 永久跳转（服务端 301，静态导出写 `_redirects`）。

## 怎么对照

工具的用法见 `tools/parity/README.md`；本地先开 vue 文档站（5174）与 bench / docs（`.claude/launch.json`，固定了“现在”）。

1. **外壳**（`pages.mjs`）：同一地址两边打开，比较顶栏、侧栏、正文、本页目录、标题的盒子（允许 0.5px）与第一屏画面；宽 1440、390，亮、暗。
2. **页面骨架**（`columns.mjs`）：每页示例的分列与顺序，缺的示例列出来；`--apply` 照 vue 重排。
3. **示例画布**（`parity.mjs`）：按 `(示例, data-scope, data-part, 第几个)` 配对比较计算后的样式，`--shots` 截图、`shots.py` 汇总像素差异，`--states` 比悬停、聚焦、按下。

完成标准：每页在 1440 / 390、亮 / 暗下外壳盒子一致；每个示例画布不同的像素 < 1%，超过的写明原因。
`--states` 的差异里，两边同序号的元素状态本来就不同（校验失败的输入框、开着的开关）时三种状态会一起报，这类不是交互样式的问题；只在某一种状态下出现的才值得查。

截图基线（CI 的“截图基线”任务）防回归：每个组件页（静态 SSR）的每个示例效果区一张，只有亮色；重新截的方法见 rune-dev skill。

## 已知的有意差别

- 内容：Blazor 的写法与参数名（`Size="Md"`、`@bind-Value`）、包名、代码示例的语言（Razor / C# 的行数不同）、指南里只适用于一边的段落。
- 导入行写装哪个包（`dotnet add package RuneForBlazor.UI`，按分类生成），vue 写 `import { … } from '…'`。
- 不做：顶栏的 ORM 链接、属性演练场按钮（阶段 7）、示例卡片工具栏的“编辑”、Playground 代码演练场（vue 在浏览器里编译 TSX，Blazor 的对应做法代价大，最后再评估）。
- Data 指南写 Blazor 自己的话题（HttpClient、请求的状态、TableQueryState、出错与取消）。
- API：TextField 等输入框的 Sx 落在外框上（DESIGN 的约定），vue 的 sx 落在 input 上；DateTimeText 没有 Intl 的 `options`；DateRangePicker 的值是日期区间而不是期间文字；
  NumberField 的步进按钮按下时焦点一律留在原处。
- vue 的问题（已报）：DataTable 勾选列的负外边距落在 input 上没有生效，vue 的表头与带勾选框的行高出约 4px，本库按它注释里的意图。

## 还缺的页面与示例

- 阶段 6：13 个页面组合组件的页面（ActionBar、AuthLayout、CommentThread、ConditionBuilder、DataImport、FilterBar、FormDialog、KanbanBoard、NotificationCenter、
  PeoplePicker、QueryTable、SlideCaptcha、StepsForm）；ThemeCustomizer 之后的主题定制页与 theme-switcher 的 thumbnail、customizer 示例。
- 阶段 7：3D 与画布（three 一组、Diagram）、图表的统计看板；全栈、移动端两组随阶段 8、7。
- 逐页示例调整（vue P2.2 / P2.3），见 STYLE-ALIGNMENT 的待跟进。

## 上线前

- [ ] 程序集懒加载：Charts、Charts.Maps（地图数据大）、Dates 只在用到的页面下载（`BlazorWebAssemblyLazyLoad` + `Router.OnNavigateAsync`）；
  换页等下载时接上顶栏的 LoadingBar（与 vue 换页时懒加载页面代码的表现一致）。
- [ ] WebAssembly 裁剪与首屏体积预算（记进 PERFORMANCE.md）；预渲染全部地址（docs-export 已有），检查每页的 title、description。
- [ ] SEO：sitemap.xml、canonical、Open Graph；不加第三方统计。
- [ ] 部署：docs.yml 配好 `CLOUDFLARE_API_TOKEN` 后正式部署；域名与版本（只放最新版，或按大版本分目录）待定。
