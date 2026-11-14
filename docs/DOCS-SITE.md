# 文档站（与 vue-tsx-pro 文档站逐像素对照）

目标：Rune 的文档站与 vue-tsx-pro 的文档站在同一地址、同一视口、同一主题下打开，**外壳一致、页面骨架一致、示例画布逐像素一致**；
有意的差别逐条记在下面。外壳、页面骨架、示例画布的逐页对照已经做完一轮（2026-11-07 到 11-09，过程见 git `46db83f:docs/DOCS-SITE.md`）；
页面已按 vue 阶段 25 的 P2.1 合并、挪动，旧地址经 `DocRedirects` 永久跳转（服务端 301，静态导出写 `_redirects`）。

## 怎么对照

工具的用法见 `tools/parity/README.md`；本地先开 vue 文档站（5174）与 bench / docs（`.claude/launch.json`，固定了“现在”）。

1. **外壳**（`pages.mjs`）：同一地址两边打开，比较顶栏、侧栏、正文、本页目录、标题的盒子（允许 0.5px）与第一屏画面；宽 1440、390，亮、暗。
2. **页面骨架**（`columns.mjs`，读源码）：每页示例的分列与顺序，缺的示例列出来；`--apply` 照 vue 重排。
   `titles.mjs` 列出页面的标题、侧栏里的顺序、示例的 id 与标题不同的地方（vue 改了示例的 id、挪了页面顺序时在这里看得出来）。
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

2026-11-13 按 vue `95ceb0d8` 用 `columns.mjs`、`titles.mjs` 核对过一遍：两边都有的 110 页，排布、侧栏顺序、示例的 id 一致；标题只剩下面“有意不同”的几条。
2026-11-15 跟到 vue 0.26.0（`c42b2f39`）：新增 figure（插图、视频与嵌入）、page-header 两页，text 的行内语义、text-field 的搜索框、image 的备选源、
progress 的量表照着补了。这一批没有再跑 `columns.mjs` / `titles.mjs`。
2026-11-14 跟到 vue 0.28.0（`c2be5550`）：figure 页加“自绘的控制条”与键盘一节，视频示例改用 vue 文档站同一份片段（`wwwroot/media`，不再在浏览器里现录）；
select 页加“原生下拉框”，选组件指南加原生下拉框、搜索框、图片与视频三行。
2026-11-15 跟到 vue 0.29.0（`0de173eb`）：combobox、cascader、affix 各加一个示例，tags-input 的状态示例补齐；0.27/0.28 留下的 autocomplete、mentions 示例
与 popover 的“自己算位置”也补上了。后者的拖动与计算在文档站自己的 `wwwroot/compute.js`（导入组件库的 `js/overlay/floating.js`），
初始化器按 `data-docs-compute` 在示例出现时加载它。

- 阶段 6：13 个页面组合组件的页面（ActionBar、AuthLayout、CommentThread、ConditionBuilder、DataImport、FilterBar、FormDialog、KanbanBoard、NotificationCenter、
  PeoplePicker、QueryTable、SlideCaptcha、StepsForm）与 PageTemplates；ThemeCustomizer 之后的主题定制页与 theme-switcher 的 thumbnail、customizer 示例；
  form 的 generated、conditional、sources、read-view 四个示例（随 SchemaForm）。
- 阶段 7：Diagram、Workflow、CodeEditor、图表的统计看板（Dashboard）；全栈、移动端两组随阶段 8、7（3D 一组已有，分类“3D · three.js”）。
- 不做的页面：ClientOnly、ErrorBoundary（Blazor 自带对应的做法）；指南的 Testing（vue 的 `/testing` 驱动没有对应物，bUnit 的写法以后另写）、Playground。
- signature-pad 的 export 示例（导出 SVG 的 handle）：本库的 SignaturePad 没有这个方法，那一格是自己的 pens 示例。
- 本库多出来的示例（vue 没有、有意留着）：app-shell 的 desktop、button 的 icon-states 与 icon-links、code 的 text、date-picker 的 offset、image 的 attachments、
  timeline 的 contract、upload 的 existing。
- 标题有意不同：提到参数名、API 的（`LangFormat` / `useFormat`、`decimal` / `Decimal`、`Id` / `id`、`OnPick` / `onPick`）与示例内容不同的
  （form 的 admission 与 sections、date-field 的 rules、range-calendar 的 display、time-picker 的 states、tabs 的 workspace、timeline 的 releases、date-range-picker 的 report）。
- 逐页示例调整（vue P2.2 / P2.3），见 STYLE-ALIGNMENT 的待跟进。

## 上线前

- [x] 程序集懒加载：Charts、Charts.Maps、Rune.UI.Area、Rune.Three 只在用到的页面下载，下载时顶栏显示 LoadingBar；Dates 有五个别的页面用到、只有 159 KB，照常启动时下载。
  首次加载的体积与改法见 PERFORMANCE.md 第 3 节。
- [ ] 每页的 title、description 检查一遍（预渲染全部地址 docs-export 已有）。
- [ ] SEO：sitemap.xml 已有（导出时按页面清单生成）；canonical、Open Graph 还没做；不加第三方统计。
- [ ] 部署：`docs.yml` 部署到 https://rune-for-blazor.zerodep.dpdns.org（自定义域名的 DNS 与证书由 wrangler 建）；版本（只放最新版，或按大版本分目录）待定。
