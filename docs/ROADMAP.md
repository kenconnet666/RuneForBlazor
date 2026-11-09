# 路线图

状态标记：`[ ]` 未开始、`[~]` 进行中、`[x]` 完成、`[!]` 待决（原因写在该项下面）。

本文只列还没做完的事。已完成部分的写法与取舍见 [DESIGN.md](DESIGN.md)；2026-11-06 第一次重新规划之前的版本见 git `341f814:docs/ROADMAP.md`，跟进 vue 0.11 之后第二次重新规划（2026-11-06）之前的版本见 git `8ae5056:docs/ROADMAP.md`（旧阶段与新阶段的对应见文末）。

## 目标

**vue-tsx-pro 组件库的 Blazor 版，成熟可用；锁定对齐 vue-tsx-pro 0.12（开发中的 `0.12.0-dev`），Rune 的版本号同步为 `0.12.0-dev`。**

除了发布（NuGet、文档站上线由用户决定），vue-tsx-pro 有的都尽量做，包括原来列为“不做”的扩展（阶段 7）。

- **外观与交互对齐 vue-tsx-pro**（`packages/ui`，约 220 个组件）：样式配方、键盘、焦点、aria、动效以它为准，`tools/parity` 的报告没有未解释的差异。
- **写法是地道的 Blazor**：API 的形状按 Blazor 与 .NET 的习惯（见“Blazor 化的约定”），不照搬 Vue 的组合式写法；静态 SSR、Server、WebAssembly、Auto 四种渲染模式都能用。
- **能接 .NET 后端**：表格与筛选栏的查询请求是一份普通的 C# 类型，EasyQuerySharp 的 `FilterBy` / `SortBy` 能直接消费（全栈示例见阶段 8）。
- **按可发布的标准做**：公共 API 冻结、文档站齐全、能打包；发不发、发到哪由用户决定。

## 现状（2026-11-06，跟进 vue 0.11 之后）

- **已完成**：底座（信号、组件基类、属性合并、强类型 CSS、分段输出与级联层、分析器 RUNE001–008、主题与配置、`Components` 定制入口、语言包、生成器）、
  文档站（Rune.Docs，可导出静态站点，字体切换）、渲染模式测试台（Rune.Demo）、对照工具 `tools/parity`、样式快照 `tools/style-snapshot`、端到端测试项目、基准测试；
  组件约 170 个：基础、表单与输入（含 Upload / FileDrop、Mentions、Cascader、Transfer）、浮层（含 Lightbox、CommandPalette、Toast）、导航（Tabs、NavList、Pagination 一组、Toolbar、TableOfContents）、
  布局与展示（含 AsideLayout、Splitter、Affix、Stat、Ellipsis、Marquee、Watermark、Sparkline、Gauge、Sortable、Image）、反馈、静态 Table、AppShell、ThemeSwitcher、docs-kit。
- **还差约 90 个组件**（阶段 1–6）：树 3、浮层与导航 5、表格 10、日期 15、图表 17、页面组合与主题定制约 20。
- **样式**：跟进到 vue-tsx-pro 0.11（feat/style-overhaul 8f699409），对照见 STYLE-ALIGNMENT.md。
- **测试**：bUnit 520 个、端到端 170 个，CI 全绿。

## 已定的决定

| #   | 事项 | 决定 |
| --- | ---- | ---- |
| 1 | 外观 | 以 vue-tsx-pro 为准：token、主题由脚本生成；组件的配方按 vue-tsx-pro 对应组件**逐条移植**（尺寸、内边距、圆角、颜色 token、状态、过渡），不凭印象重画；做不到的写进 DESIGN |
| 2 | 交互与无障碍 | 以 vue-tsx-pro 为准（键盘、焦点、aria、读屏播报、动效）；实现按 DESIGN 第 8 节：原生元素优先，JS 初始化器做事件委托，组件只写 data 属性 |
| 3 | 依赖 | 运行时只依赖 ASP.NET Core；测试用 bUnit、xUnit、`Microsoft.Playwright`；生成器脚本只用 node 内置模块与 `npm pack`。阶段 7 的扩展包可以随包带上 vue 用的同一个 JS 库（three.js、CodeMirror 等，经 `tools/vendor-gen` 固定版本、许可写进 NOTICE，只在用到时加载）；其余依赖进“待决” |
| 4 | 包 | `Rune`（运行时）、`Rune.UI`（组件，含表格）、`Rune.Icons`（Lucide 全集）、`Rune.UI.Dates`、`Rune.Charts`（后两个 WebAssembly 下可以懒加载） |
| 5 | 命名 | 组件名、参数名照 vue-tsx-pro（PascalCase），参数与事件的**形状**按“Blazor 化的约定”；与 Razor / HTML 冲突时改名并记在 DESIGN（TextSpan、TextLink、NativeSelect） |
| 6 | 语言 | 组件里的文字进语言包（`UIConfig.Lang`），内置 zh-CN（默认）与 en-US，由 lang-gen 从 vue-tsx-pro 生成；拼音匹配做成可选的扩展（阶段 7，接 `UIConfig.TextMatcher`） |
| 7 | 演示 | 文档页的路由、示例的场景与顺序照 vue-tsx-pro 文档站，方便逐页对照；渲染模式测试台 Rune.Demo 保留，与文档站共用 Rune.Docs.Content 的页面 |
| 8 | 范围（2026-11-06 修订） | 只有发布暂不做。code-editor、diagram、three、xlsx、演练场、拼音、storage、mobile、流程的前端都做（阶段 7）。mini（Vue 渲染器跑进微信、支付宝小程序）与 kit（Nitro 上的全栈框架）是 Vue / Node 专用的：kit 的角色由 Blazor Web App 本身与阶段 8 的全栈示例承担，mini 没有 Blazor 的对应物，不做 |
| 9 | 地图数据 | 图表的 Map 用公有领域或许可明确的来源（如 Natural Earth）导出，来源与许可写进 NOTICE |
| 10 | 部署 | 文档站导出静态文件，部署到 Cloudflare Workers 的静态资源；工作流只手动触发，令牌放仓库 secret `CLOUDFLARE_API_TOKEN`，由用户配置后自己运行 |
| 11 | 发布与版本 | NuGet 暂不发布，阶段 9 只做本地打包与安装试用；版本号跟 vue-tsx-pro 走（现在 `0.12.0-dev`），vue 发版时 Rune 同步改 |
| 12 | 全栈示例（2026-11-06） | 做阶段 8。EasyQuerySharp 只进示例，组件库不引用；`TableQuery` 到 `FilterBy` / `SortBy` 的接线先写在示例里、不单独出包；CI 按固定的提交检出 EasyQuerySharp（同 `VUE_TSX_PRO_REF`） |

## Blazor 化的约定（2026-11-06）

以 vue-tsx-pro 为准的是**外观、交互、无障碍和名字**；**API 的形状**按 Blazor 与 .NET。R.3 按这张表改已有组件，之后的组件直接照做。

| vue-tsx-pro | 本库 |
| --- | --- |
| `v-model`、`_bind:open` | `@bind-Value`、`@bind-Open`（`Xxx` + `XxxChanged`，输入控件另有 `ValueExpression`）；`@bind-Value:after` 照常可用 |
| 事件回调返回 `false` 拦下 | 事件参数上的 `KeepOpen` / `Cancel`（同 `CancelEventArgs`） |
| 插槽 | `RenderFragment` / `RenderFragment<T>`，名字是 PascalCase 名词（`Title`、`Footer`、`ItemTemplate`） |
| `items` / `columns` 数组 | 能声明的用子组件（`<TabPanel>`、`<DescriptionsItem>`、表格的 `<PropertyColumn>`）；数据驱动的用 `Items` + `ItemTemplate` |
| 取远程数据的函数 | 委托：选项用 `OptionsLoader<T>`（已有）；表格、长列表用 `ItemsProvider`（同 Virtualize / QuickGrid：起始位置、条数、排序、`CancellationToken`） |
| zod schema + `useForm` | `EditForm` / `EditContext` + DataAnnotations（或任意校验器）；字段名取 `[Display(Name)]`；提交的 name 在服务端渲染的表单里按字段生成（同 `InputBase`），静态 SSR 用 `[SupplyParameterFromForm]` 收；解析不了的输入恢复原值（同 vue），不留无效文字 |
| `useToast()` / `useDialog()` 等组合式函数 | scoped 服务（`ToastService`、`DialogService`）+ 布局里放宿主（`Toaster`、`DialogHost`） |
| `el.focus()` | 可聚焦的组件公开 `Element`（`ElementReference?`）与 `FocusAsync()`，同 `InputBase.Element` |
| `createUi` / `Provider` | `AddRuneUI`（DI，服务端与 WebAssembly 各一次）+ `RuneProvider`（级联值） |
| `Can`（权限）、`useCan` | 直接用 Blazor 的 `AuthorizeView`（`Policy` 写权限点、`Roles`、`NotAuthorized`）与 `IAuthorizationService`，不另做组件；文档站 Provider 页的“权限”一节演示这种写法 |
| `ClientOnly` / `useMounted` | `RendererInfo.IsInteractive`；`ClientOnly` 只是它的组件写法 |
| “现在”（今天、相对时间） | 注入 `TimeProvider`：预渲染与交互渲染用同一个时刻，测试可以固定 |
| 日期模型 `DateTime` | `DateOnly`、`TimeOnly`、`DateTime`、`DateTimeOffset`；格式、周起始、月份名取 `CultureInfo`，不另造日期类型 |
| SSR 水合时带过去的数据 | `[PersistentState]`（.NET 10）：预渲染时取到的数据带给交互渲染，不重复请求 |
| 列表的查询状态进地址栏 | `[SupplyParameterFromQuery]` + `NavigationManager.GetUriWithQueryParameters` |
| kit 的 `load`、action、权限 | Blazor Web App 自带（`@rendermode`、`[PersistentState]`、Minimal API、`[Authorize]`），不移植 kit |

与 Blazor 常见写法不同、但保留不改的：

- **部件属性 `PropsXxx`**（277 个参数）：类型是 `Attrs`（带 Sx、事件按接收者合并），不是 `AdditionalAttributes` 那样的字典；改名牵动全部组件，收益小。
- **组件不调用 `IJSRuntime`**：JS 初始化器 + data 属性，四种渲染模式行为一致、Server 模式没有往返（DESIGN 第 8 节）。`FocusAsync` 用 Blazor 自带的 `ElementReference.FocusAsync`，不算例外。
- **样式是运行时生成的强类型 CSS**，不用 CSS 隔离（`.razor.css`）：主题、档位、按部件定制都要在 C# 里合并（DESIGN 第 5 节）。

## 自主执行的规矩（用户不在时照此推进）

- **小步快跑、直接推 master**：每完成一项（一个组件或一组紧密相关的组件），本地跑相关测试与生成器检查，提交并推送；不开 PR、不推 tag、不发 NuGet。整理与功能分开提交。
- **master 不留红**：推送前看上一次 CI，红了先修；长命令放后台，等待时准备下一项。
- **一般不派子代理**：组件开发、审查、浏览器验证都自己做；只有要长时间盯着的事（等 CI、跑长测试）放后台。
- **遇到下面的情况，这一项停下，写进“待决”**（日期、项、卡在哪、需要决定什么），转去做下一个不依赖它的项：
  要加“已定的决定”第 3 条以外的依赖；要凭据、账号、发布；同一个 CI 失败修了两次仍不明原因；
  vue-tsx-pro 的做法在 Blazor 里做不到、又有多种合理的替代方向；要改公共 API 且影响已完成组件的用法（小的改名、补参数不算，直接改并写进提交说明）。
- **不写过程流水账**：结论进文档（决定进本文或 DESIGN，坑进 DESIGN 的对应节），过程留在提交说明里。每完成一个阶段在本文勾选并写一行结论。
- **跟进 vue-tsx-pro**：vue 那边的会话每发一批改动（或发版）会通知；当轮读它的改动，跟上并把 `VUE_TSX_PRO_REF` 指到那个提交，取舍记进 STYLE-ALIGNMENT。
  vue 的 ROADMAP 阶段 24（部件补齐、状态写法换条件别名、裸值换 token）它做到哪一步，Rune 同步到哪一步。
- **vue 有问题时通知 vue 的会话**：对照中发现 vue 的行为 bug、无障碍问题或样式不一致，把复现与建议发给维护 vue-tsx-pro 的会话，由它修好、发版，
  Rune 再指到新提交；不直接改 vue 的仓库。
- **中途接手**：先读 [HANDOFF.md](HANDOFF.md)、本文、DESIGN、最近的提交说明。

## 每个组件的完成标准

1. **样式对齐**：配方逐条移植，`tools/parity` 的报告没有未解释的差异（亮、暗主题）；
2. **API**：参数、默认值、部件与 vue-tsx-pro 对应，形状符合“Blazor 化的约定”；部件标记、`PropsXxx` 部件属性、组件默认值（有外观档位时登记 `XxxDefaults`）、必填参数 `[EditorRequired]`、中文文档注释；
3. **行为测试**（bUnit）：受控、只给值、都不给三种用法与主要分支，aria 属性；输入类另测在 `EditForm` 里的用法；
4. **浏览器**：静态 SSR、Server、WebAssembly、Auto 四种模式都打开看过、点过；有键盘、焦点、拖动逻辑的组件有端到端用例；
5. **无障碍**：键盘与读屏符合 WAI-ARIA 的做法（以 vue-tsx-pro 的实现为准）；
6. **性能**：对象按需创建（`Css.Define`、`field ??=`、`Memo`）、收起的内容与没打开的浮层不渲染、渲染路径不分配、大列表虚拟滚动；
7. **文档**：文档页照 vue-tsx-pro 文档站的示例写，有 API 表与键盘一节；DESIGN 记下新的取舍与坑；本文勾选。

## 工作方式

- **先读后写**：做一个组件前读 vue-tsx-pro 的源码（`packages/ui/src/<分类>/<组件>.tsx` 与它用到的 `utils/style/*`）、文档页的示例与 `COMPONENTS.md` 的相关节。
- **对照**：本地开着 vue-tsx-pro 文档站（端口 5174）与测试台（5300），`tools/parity` 按 `(示例, data-scope, data-part, 第几个)` 配对元素比较计算后的样式（亮暗、悬停聚焦按下打开），差异改到一致，确实不该一致的在文档页注释与 DESIGN 里写原因。对照不进 CI。
- **生成器**：css-gen、theme-gen、lang-gen 读 vue-tsx-pro（CI 固定在 `VUE_TSX_PRO_REF`），icon-gen 读 lucide-static；vue-tsx-pro 更新了主题、样式数据或语言包时重新生成并更新 `VUE_TSX_PRO_REF`。
- **验证**：本地跑与改动相关的测试与生成器检查（`--check`），全量交给 CI。

## 执行顺序

0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9。阶段 9 的质量项贯穿全程，在最后收尾；跟进 vue-tsx-pro 不单列阶段（见“自主执行的规矩”）。

- 树（1）在前：TreeSelect、TreeDataTable 都建在它上面；表格（3）在页面组合（6）之前：QueryTable、FilterBar 建在它上面。
- 某一项进入待决时，跳到下一个不依赖它的项；日期（4）、图表（5）不依赖 1–3，前面卡住时可以先做，也可以在后台工作树里并行。
- 已有页面里等其他组件的示例缺口，记在提供那个组件的阶段里，做到时补上。

---

## 阶段 0：vue-tsx-pro 0.11 收尾与整理（不加新组件）

vue 的样式体系改造（0.11，阶段 0–5）已跟进，取舍见 [STYLE-ALIGNMENT.md](STYLE-ALIGNMENT.md)；这里是剩下的与之前 R 阶段没做完的。

- [x] 0.1 公开面收口（同 vue 0.11 不再导出组件内部的样式表）：组件的样式类改 internal，外观定制只走 `Components` 入口与部件属性；
  使用方自建组件要用的共用件（`SharedStyles`、`ScaleStyles`、`ShimmerStyles` 等）、部件枚举与默认值类型照常公开；测试与基准经 `InternalsVisibleTo`
- [x] 0.2 OtpField 的格子用输入框的配方（有 variant、radius，同 vue）
- [ ] 0.3 对照工具跑全部页面（亮、暗），改掉改造后的差异；之前留下的：transfer 的 members、progress、card 的 effects
  （dialog 的进出场用独立的 scale 属性、内容不同造成的宽度差、rating 的填色层不处理，DESIGN 写过）
- [ ] 0.5 跟进 vue 0.12（它的 ROADMAP 阶段 24，按它的进度逐批跟）：部件补齐（内部节点补 `PropsXxx` 与 `data-part`，部件多却没有配方的改配方，
  登记进 `Components`）、状态写法（Rune 的 `When.Selected` 等与 vue 的条件别名对应，组合状态的先后照 vue 定）、裸值换 token、文档站 API 小节列配方的部件与档位
- [ ] 0.4 代码走查（单独提交）：过长或职责混杂的文件（`js/core/runtime.js`、`lists.js`、`overlays.js`、`Cascader.razor.cs`、`ToastService.cs`）、
  重复的样式片段与死代码；入口顶部的 data 属性总表与实际用到的核对一遍

## 阶段 1：树与长列表

- [x] Tree（勾选与勾选策略、懒加载、搜索过滤、拖动移动、虚拟滚动；从右到左时展开箭头朝下，同 vue 0.11）：`TreeData` 底座、键盘与拖动在 JS（lists.js + tree.js）
- [x] TreeSelect：外壳同 Select，弹层里是 Tree（单选、勾选与取法、搜索、远程搜索）
- [x] TreeTransfer：左边是勾选的 Tree，只移叶子；与 Transfer 共用一边（TransferPanel）与中间的按钮（TransferActions）
- [x] VirtualList 补齐：随页面滚动、横向、骨架行、跳到第几项、可见范围变化的回调（虚拟滚动器移植到 js/table/virtual.js，不再用 Blazor 的 Virtualize）；文档页

## 阶段 2：浮层与导航

- [x] Tour（模态 dialog 同 Dialog；镂空与卡片的跟随在 js/overlay/tour.js，卡片定位用 floating.js 新增的 followElement）
- [x] Breadcrumbs、Steps（同一页，同 vue；底座补了 FlexOrderedList）
- [x] Form 的分步示例换用 Steps
- [x] NavMenu、NavBar（菜单数据同一份 NavMenuItem；NavBar 放不下的收进“更多”，复用 js/layout/overflow.js）
- [x] AppShell 混合布局示例换用 NavBar

## 阶段 3：表格

- [x] 查询的契约（DESIGN §13；`TableQuery`、`TableQueryState`）：`TableQuery`（页码、每页条数、多列排序、列筛选、筛选栏的条件树）是纯数据类型、不依赖任何 ORM，
  能与查询字符串互转（刷新、分享链接保持状态）。对照 vue-tsx-pro 的 `use-table-query.ts`、easy-query TS 的 `parseTableRequest` 与 EasyQuerySharp 的 `FilterBy` / `SortBy`，
  让 EasyQuerySharp 一侧不经转换就能消费
- [x] DataTable：列用子组件声明（`PropertyColumn` 按表达式取值，标题取 `[Display]`，排序键取属性路径；`TemplateColumn` 自定义内容）；列来自数据时用 `@foreach` 生成列组件（PivotTable 就是这样）；
  数据给 `Items`，服务端分页由 `TableQueryState` 管（不另设 `ItemsProvider`，同 vue 的 useTableQuery）；排序、表头筛选、选择、展开、固定列、列宽拖动、网格方向键与单元格编辑（行校验）、行拖动、虚拟滚动、骨架行、密度
  - [x] 核心：列的子组件（PropertyColumn、StatusColumn、TemplateColumn）、单列与多列排序（本地 / ManualSort）、单选与多选、虚拟滚动（virtual.js 的表格模式）、
    骨架与空状态、固定列、合计行、行的色调、主从视图的当前行与点行
  - [x] 表头筛选与列菜单（ColumnFilterButton、ColumnMenuButton）、列宽调整（ColumnResizer）
  - [x] 分组表头（列的 Group）、详情行（DetailContent）、按行与列的单元格属性（PropsCellFor）
  - [x] 列设置记在浏览器里：LocalSetting（同 vue 的 usePersistentState；js/core/storage.js，跨标签页同步）
  - [x] 网格方向键与单元格编辑（列校验、行校验、接口拒绝）
  - [x] 行拖动（OnReorder，复用 js/table/sortable.js）
  - [x] 服务端数据：ManualSort、ManualFilter 直接绑 TableQueryState 的 Sort / ColumnFilters（有测试；示例随阶段 6 的 QueryTable，同 vue）
- [x] ColumnHeaderActions（ColumnFilterButton、ColumnMenuButton）、ColumnResizer、SelectionBar、ColumnSettings（列给 ListboxOption：列是表格的子组件，外面看不到）
- [x] ColumnTable（列与 DataTable 共用；MergeRepeated、CellSpan 合并单元格）——Table 的 column-table 示例
- [x] TreeDataTable（建在 DataTable 上：Leading、PartialKeys；同级排序走 TreeData.Flatten 的 order）
- [x] CSV 导出（DataTable.ToCsv；下载成文件随阶段 6 的 DataImport，xlsx 见阶段 7）
- [x] PivotTable（Pivot.Compute 与 ColumnTable；热力图）
- [x] FieldCompare（字段写法同表格的列；Diff.Fields / Diff.Rows），Timeline 的 change-log 示例换用它；DataTable 的数据修正示例（Diff.Rows、单击编辑）
- [→ 阶段 8] 预渲染不重复取数（`[PersistentState]`）：TableQueryState 在预渲染时取的那页交给交互后的渲染，不再请求一次（DESIGN §13：状态由页面持有，随全栈示例定）
- 示例缺口：Drawer 的 imperative 与 detail 示例（detail 用 DataTable、Timeline、DeadlineBadge；DeadlineBadge 随阶段 6）。
  （Skeleton 的 table、Table 的 column-table 已补；Badge 的表格状态示例 vue 也用静态 Table，已一致）

## 阶段 4：日期（`Rune.UI.Dates`）

- [x] 取值与格式：`DateOnly` / `TimeOnly` / `DateTime` / `DateTimeOffset`，格式、周起始、月份名取 `CultureInfo`，“现在”取 `TimeProvider`；
  对照 vue 的 `DateTime.tsx`、`date-locale.ts`、`date-segments.ts`，行为差异写进 DESIGN（DateTimeText、LangFormat.Date 的详略、时区与小时制）
- [x] DateField、TimeField、SegmentedField（DateSegmentGroup 先内联在 SegmentedField 里，DateRangePicker 要两组时再拆出来）
- [x] Calendar、RangeCalendar（CalendarView 是抽象的 CalendarBase）
- [x] QuarterCalendar（DatePicker、DateRangePicker 选季度时弹出它）
- [x] CalendarBoard（整页日历）
- [x] DatePicker（日、周、月、季、年，快捷项、“今天”、禁用的时间段；季度暂在月份视图里选，QuarterCalendar 做好后换上）
- [x] TimePicker
- [x] DateRangePicker（内置与自定义快捷区间；2026-11-07 改成泛型 `DateRangePicker<TValue>`，值是 `DateRange<TValue>`，Hour / Minute / Second 是带时间的区间）
- [x] RelativeTime、Countdown
- [ ] WebAssembly 下程序集懒加载（`BlazorWebAssemblyLazyLoad`）验证

## 阶段 5：图表（`Rune.Charts`，C# 输出 SVG）

- [x] 底座：比例尺、坐标轴刻度、图例、提示（JS 里画，不往返）、响应式尺寸、图表色、标注与区间、缩放、导出图片与数据；布局算法（矩形树图、桑基、弦图、网络图）与地图投影、TopoJSON 简化
- [x] Line、Area、Bar（堆叠、100% 堆叠）、Combo（双轴）、Scatter、Waterfall、Donut、Radar、Funnel、Treemap、Sankey、Chord、Graph、Timeline（vue 没有热力图，不做）；
  文档站各页与 vue 截图对照（差异在进场动画的时机与 1px 以内）
- [x] Sparkline、Gauge 留在 Rune.UI（同 vue 的 display），与图表共用 SVG 数字的写法（`SvgFormat`）；改用图表底座会让 Rune.UI 反过来依赖 Rune.Charts
- [x] Map 与地理数据包 `Rune.Charts.Maps`（Natural Earth 经 Unovis，Apache-2.0，NOTICE 随包）
- [ ] 图表的“统计看板示例”页（vue 的 charts/Dashboard）：用到 Globe（three.js，阶段 7），等它做好再补
- [ ] WebAssembly 下程序集懒加载验证（与阶段 4 的同一项一起，见阶段 9 的文档站规划）

## 阶段 6：页面组合与主题定制

- [ ] 输入：ConditionBuilder（条件树与阶段 3 的 `TableQuery` 共用一份类型）、PeoplePicker / PeoplePickerPanel（用户、部门、角色）
- [ ] FilterBar（基本筛选 + 接 ConditionBuilder 的高级筛选）、ActionBar、QueryTable（FilterBar + DataTable + 工具栏：密度、列设置、刷新、全屏；查询状态进地址栏）
- [ ] FormDialog（对话框里的 EditForm：确认时校验、提交中拦住关闭）、StepsForm、DataImport（CSV）
- [ ] AuthLayout、NotificationCenter、CommentThread（接 Mentions）、KanbanBoard（Sortable 多容器）、DeadlineBadge、SlideCaptcha
- [ ] ThemeCustomizer、ThemeThumbnail，接着做文档站的主题定制页；ThemeSwitcher 的局部主题；Provider 页

## 阶段 7：扩展（原来列为“不做”的，2026-11-06 起做）

各自成包或成可选模块，WebAssembly 下能懒加载；做之前先读 vue 对应包的现状（有的还在开发），照它的 API 与示例。

- [ ] 拼音匹配：`UIConfig.TextMatcher` 的拼音实现（全拼、首字母、多音字），Select、Combobox、CommandPalette、Transfer 的搜索都能用；字表的来源与许可写进 NOTICE
- [ ] 表格导出 xlsx（vue 的 xlsx：自己写 OOXML，不引第三方库；C# 用 `System.IO.Compression`）
- [ ] 文档站的属性演练场（vue 的 PropsPlaygroundButton：改参数实时看效果，参数表从 API 文档生成）
- [ ] 前端存储（vue 的 `@zerodep-dev/storage`）：IndexedDB / localStorage / 内存一个异步接口，上面一层 TTL 与过期后先用旧值再刷新的缓存
- [ ] CodeEditor（CodeMirror，同 vue 的 code-editor）
- [ ] Diagram、DiagramControls、DiagramLanes、DiagramMinimap、DiagramNode（同 vue 的 diagram）
- [ ] three：ThreeCanvas、ModelViewer、Globe、ParticleField、ShaderBackground、MeshGradient、AuroraBackground、WaveGrid、EffectSurface、Confetti（three.js）
- [ ] 流程的前端：流程设计与审批界面（vue ROADMAP 阶段 9、14 的前端部分），接阶段 8 的全栈示例
- [ ] 移动端组件库 `Rune.Mobile`（vue 的 mobile，约 50 个组件，H5）：触控手势、安全区、下拉刷新、底部弹层等；文档站加移动端一栏

## 阶段 8：.NET 全栈示例（已定的决定第 12 条）

- [ ] `apps/Rune.Sample`（Blazor Web App，Auto 渲染模式）+ EasyQuerySharp + SQLite，走完一个后台模块：
  QueryTable 服务端分页、排序、筛选（`TableQuery` → `FilterBy` / `SortBy`）、FormDialog 新建与编辑（聚合根保存）、权限（`[Authorize]`、`Can`）、预渲染 + `[PersistentState]`；
  端到端测试在 Server、WebAssembly 下各跑一遍。示例里重复出现的接线到第三次时再收成包。

## 阶段 9：质量与发布准备（贯穿全程，这里收尾）

- [ ] 端到端覆盖全部有键盘、焦点、拖动逻辑的组件；输入法用例（组字中的回车不提交、不选中：Combobox、TagsInput、Mentions、Textarea）；axe-core 无障碍检查跑全部示例（亮、暗主题）
- [ ] 截图基线进 CI（固定的 Linux 容器，Chromium，亮、暗主题），变化时人工确认
- [ ] 文档站：键盘与无障碍一节（KeyboardReference）；搜索能搜到参数名与示例标题
- [ ] 正式文档站与 vue 文档站逐像素对照：计划、现状与先后顺序见 `docs/DOCS-SITE.md`（P0 对照模式与固定时间 → P1 外壳与导入行 → P2 内容 → P3 示例画布 → P4 上线）
- [ ] 性能：大列表、表格的渲染与内存基准；WebAssembly 裁剪（R.3 打开分析器之后）与懒加载程序集；组件库样式预生成静态 CSS（可选，DESIGN 第 12 节）
- [ ] API 冻结前的审计：全部公共 API 对一遍“Blazor 化的约定”与 vue-tsx-pro 的名字，出清单逐条改
- [ ] 打包：五个包的说明、图标与许可、版本号、SourceLink、发布说明；`dotnet new` 模板（可选）；本地打包与安装试用

---

## 待决

（执行中遇到的停顿点记在这里：日期、项号、卡在哪、需要用户决定什么。）

- 2026-11-07 文档站：代码演练场的做法、Data 指南页的替代话题、导入行的写法、参数名造成的文字差异怎么算，见 `docs/DOCS-SITE.md` 第五节。

## 旧阶段的去处

旧 4（对照工具与基础设施）→ R.2 与各阶段；旧 5 → 1（Upload）、2（TreeSelect、TreeTransfer）、7（PeoplePicker、ConditionBuilder）；旧 6 → 1；旧 7 → 2；
旧 8 → 2（Lightbox）、3；旧 9 → 4；旧 10 → 5；旧 11 → 6；旧 12 → 7；旧 13（文档站）已基本完成，剩下的进 7（主题定制页）、9（键盘说明）；旧 14 → 9。

第二次（跟进 vue 0.11 之后）：R 剩下的 → 0（R.3 的预渲染取数 → 3）；1 已完成；2 剩下的（Tree 一组、VirtualList）→ 1；3–7 依次 → 2–6；8 → 8；9 → 9；新增阶段 7（原来不做的扩展）。
