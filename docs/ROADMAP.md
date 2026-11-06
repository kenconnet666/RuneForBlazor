# 路线图

状态标记：`[ ]` 未开始、`[~]` 进行中、`[x]` 完成、`[!]` 待决（原因写在该项下面）。

本文只列还没做完的事。已完成部分的写法与取舍见 [DESIGN.md](DESIGN.md)；2026-11-06 重新规划之前的版本见 git `341f814:docs/ROADMAP.md`（旧阶段与新阶段的对应见文末）。

## 目标

**1.0：vue-tsx-pro 组件库的 Blazor 版，成熟可用。**

- **外观与交互对齐 vue-tsx-pro**（`packages/ui`，约 220 个组件）：样式配方、键盘、焦点、aria、动效以它为准，`tools/parity` 的报告没有未解释的差异。
- **写法是地道的 Blazor**：API 的形状按 Blazor 与 .NET 的习惯（见“Blazor 化的约定”），不照搬 Vue 的组合式写法；静态 SSR、Server、WebAssembly、Auto 四种渲染模式都能用。
- **能接 .NET 后端**：表格与筛选栏的查询请求是一份普通的 C# 类型，EasyQuerySharp 的 `FilterBy` / `SortBy` 能直接消费（全栈示例见阶段 8）。
- **按可发布的标准做**：公共 API 冻结、文档站齐全、能打包；发不发、发到哪由用户决定。

## 现状（2026-11-06）

- **已完成**：底座（信号、组件基类、属性合并、强类型 CSS、分析器 RUNE001–008、主题与配置、语言包、生成器）、文档站（Rune.Docs，可导出静态站点）、渲染模式测试台（Rune.Demo）、对照工具 `tools/parity`、端到端测试项目；
  组件：基础（按钮家族、文字、图标、标记、代码）、表单与输入（Field / Form、文本数字颜色、勾选单选开关、Select / Combobox / Autocomplete / TagsInput、Slider、Rating、OtpField、Cascader、Transfer、Mentions）、
  浮层（Popover、Menu / ContextMenu、Tooltip、HoverCard、Dialog / Drawer / AlertDialog / DialogService、Popconfirm、CommandPalette、Toast）、导航（Tabs、NavList、Pagination 一组、Toolbar、TableOfContents、SkipLink）、
  布局与展示（Flex、Grid、Container、ScrollArea、Separator、ResizeHandle、PageScrollbar、Card、Accordion、Collapse / Collapsible、Avatar、Descriptions、ItemList、VirtualList 第一版）、
  反馈（Alert、Empty、Progress、Skeleton、LoadingBar、LoadingOverlay）、静态 Table、AppShell、ThemeSwitcher、docs-kit。
- **还差约 90 个组件**：基础 3、反馈 2、输入 5、布局 6、展示 15、浮层 2、导航 4、表格 9、日期 15、图表 16、页面组合与主题定制 14。
- **测试**：bUnit 340 个、端到端 78 个；文档站 54 个组件页，部署已准备好、没发布。
- **CI**：master 最近 5 次都失败在端到端任务（跑到约 5 分钟时 runner 收到关停信号，或打开页面超时）；构建、单元测试、生成器检查是绿的。
- **清单与代码不一致**：旧路线图阶段 6 写在“待做”里的 CloseButton、CopyButton、IconLinkButton、Mark、Code、Blockquote 代码里已经有了，没勾；Mentions 还没有单元测试与端到端用例。

## 已定的决定

| #   | 事项 | 决定 |
| --- | ---- | ---- |
| 1 | 外观 | 以 vue-tsx-pro 为准：token、主题由脚本生成；组件的配方按 vue-tsx-pro 对应组件**逐条移植**（尺寸、内边距、圆角、颜色 token、状态、过渡），不凭印象重画；做不到的写进 DESIGN |
| 2 | 交互与无障碍 | 以 vue-tsx-pro 为准（键盘、焦点、aria、读屏播报、动效）；实现按 DESIGN 第 8 节：原生元素优先，JS 初始化器做事件委托，组件只写 data 属性 |
| 3 | 依赖 | 运行时只依赖 ASP.NET Core；测试用 bUnit、xUnit、`Microsoft.Playwright`；生成器脚本只用 node 内置模块与 `npm pack`。其余依赖进“待决” |
| 4 | 包 | `Rune`（运行时）、`Rune.UI`（组件，含表格）、`Rune.Icons`（Lucide 全集）、`Rune.UI.Dates`、`Rune.Charts`（后两个 WebAssembly 下可以懒加载） |
| 5 | 命名 | 组件名、参数名照 vue-tsx-pro（PascalCase），参数与事件的**形状**按“Blazor 化的约定”；与 Razor / HTML 冲突时改名并记在 DESIGN（TextSpan、TextLink、NativeSelect） |
| 6 | 语言 | 组件里的文字进语言包（`UIConfig.Lang`），内置 zh-CN（默认）与 en-US，由 lang-gen 从 vue-tsx-pro 生成；拼音匹配不做（`UIConfig.TextMatcher` 留着接口） |
| 7 | 演示 | 文档页的路由、示例的场景与顺序照 vue-tsx-pro 文档站，方便逐页对照；渲染模式测试台 Rune.Demo 保留，与文档站共用 Rune.Docs.Content 的页面 |
| 8 | 不做 | vue-tsx-pro 的 code-editor、diagram、three、xlsx、演练场；mobile、mini、kit、flow 包。需要时另议 |
| 9 | 地图数据 | 图表的 Map 用公有领域或许可明确的来源（如 Natural Earth）导出，来源与许可写进 NOTICE |
| 10 | 部署 | 文档站导出静态文件，部署到 Cloudflare Workers 的静态资源；工作流只手动触发，令牌放仓库 secret `CLOUDFLARE_API_TOKEN`，由用户配置后自己运行 |
| 11 | 发布 | NuGet 暂不发布，阶段 9 只做本地打包与安装试用 |
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
| `Can`（权限） | 包一层 `AuthorizeView`，按策略、角色显示或隐藏 |
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
  vue-tsx-pro 的做法在 Blazor 里做不到、又有多种合理的替代方向；要改公共 API 且影响已完成组件的用法（小的改名、补参数不算，R.3 已列出的不算，直接改并写进提交说明）。
- **不写过程流水账**：结论进文档（决定进本文或 DESIGN，坑进 DESIGN 的对应节），过程留在提交说明里。每完成一个阶段在本文勾选并写一行结论。
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

R → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9。阶段 9 的质量项贯穿全程，在最后收尾。

- 某一项进入待决时，跳到下一个不依赖它的项；日期（5）、图表（6）不依赖 2–4，前面卡住时可以先做。
- 已有页面里等其他组件的示例缺口，记在提供那个组件的阶段里，做到时补上。

---

## R 整理与纠正（不加新组件）

- [x] R.1 CI 恢复全绿：根因是每个用例开的浏览器上下文从来不关，全部页面（WebAssembly 运行时、Server 电路）留到结束，CI 的机器跑到一半被关停。
  测试类改继承 `E2ETest`，测试结束时关掉自己开的上下文；本地全部用例从约 5 分钟降到 3 分钟以内；任务加了 30 分钟时限。补了 Mentions 的 bUnit 与端到端用例。
- [~] R.2 清单、文档与代码对齐：
  - [x] CloseButton、CopyButton、IconLinkButton、Mark、Code、Blockquote 已在代码里（有文档页或示例、有测试）；Alert 已同 vue（说明与内容分开、经 dismiss 淡出）；
    Switch 还缺 Loading、CheckedContent / UncheckedContent 与对应的部件属性，进阶段 1；
  - [x] 文档站 54 页全部跑了对照（亮色）。修掉的系统性问题：部件名同 vue 保留 camelCase（`headerCell`，之前转成了连字符，多词部件从没配上对）；
    对照工具跳过代码高亮的词元与看不见的元素；Avatar 图片加载后不再显示后备内容；CodeBlock 展开按钮建在按钮重置样式上；分页的页码按钮保留 Button 的标记；
  - [x] 部件标记与 vue 对齐：Tabs 的标签项单独成 Tab 配方（tab/root、tab/close）；分页的页码按钮、标签栏的按钮保留 Button 的标记（`Part.Unmarked`）；
    表尾的行标题保留 table/rowHeader；Card 加了 skeleton 部件。新增 vue 的 `ButtonBase`（无外观的按钮，原来的按钮基类改名 `ButtonCore`），Menu 的示例与 ButtonBase 页照 vue 写；
    Field 不写 Required 时看 `[Required]`（同 vue 的 form.Field 按 schema 标必填）；对照工具不比关键帧名、跳过看不见的元素；
  - [ ] 剩下的差异（menu、tabs 已经 0 差异）：transfer 的 members、progress、card 的 effects、dialog 的进出场（这边用独立的 scale 属性，vue 用 transform，视觉一致）；
    等别的组件的：form-section / form 的 admission（AsideLayout）、skeleton（Stat）、checkbox 的 switch（Switch 补齐）；
    内容不同造成的宽度差（code、toast、loading-bar、button-base 的文字）与 rating（每颗星多一层填色，DESIGN 里写过）不处理；
  - [x] HANDOFF 的路径与“下一步”。
- [~] R.3 Blazor 化的 API 纠正（按“Blazor 化的约定”）：
  - [x] 可聚焦的组件公开 `Element` 与 `FocusAsync()`（输入类、按钮家族；NumberRangeField、Mentions 转给里面的；一组控件是 null）；
  - [x] Field 不写 Label 时取 `[Display(Name)]` / `[DisplayName]`，错误汇总同名；解析不了的输入核对过：NumberField、ColorField 失焦时恢复原值（同 vue），不留无效文字，不需要转换错误；
  - [x] 静态 SSR 的表单：每个控件一个 `Name`（原来有的用参数、有的用属性，统一了），服务端渲染的 EditForm 里按字段生成（同 InputBase），勾选框提交 true；Form 转交 `Enhance`；
    测试台 `/static/form-post` 的端到端用例验证了回发、服务端校验与 `[SupplyParameterFromForm]` 绑回（RuneComponent 上的级联值供应同一套机制）；
  - [x] `RuneComponent` 已有 `DispatchExceptionAsync`、`Assets`、`RendererInfo`、`AssignedRenderMode`；
  - [ ] 预渲染不重复取数（`[PersistentState]`）：现在没有预渲染时取数的组件（Select 的远程选项在打开时才请求），做法随阶段 4 的表格一起定；
  - [x] `DialogService.Open<TComponent>(options, parameters)`，正文经级联的 `DialogHandle` 用 `Close(结果)` 交回，`await handle.ResultAsync<T>()`；
  - [x] 泛型推断核对过：只有 RadioGroup → Radio 需要 `[CascadingTypeParameter]`（已有），其他泛型组件靠数据传项；必填参数的 `[EditorRequired]` 已齐；
  - [x] `RuneUIOptions.Languages`：按请求 / 电路的 `CurrentUICulture` 选语言包；
  - [x] 三个库 `IsTrimmable`，裁剪分析 0 警告（能不用反射的改掉了，剩下的写明理由）。
- [ ] R.4 代码走查（单独提交）：过长或职责混杂的文件（`Rune.UI.lib.module.js` 490 行、`lists.js`、`overlays.js`、`Cascader.razor.cs`、`ToastService.cs`）、重复的样式片段与死代码；
  入口顶部的 data 属性总表与实际用到的核对一遍。

完成标准：上面各项做完；文档与代码一致；CI 连续绿。

## 阶段 1：基础、反馈、输入收尾

- [ ] 基础：List、Can（包 `AuthorizeView`）、ClientOnly
- [ ] 反馈：Banner（关闭同 Tag、Alert 的 `data-rune-dismiss`）、Result
- [ ] FileDrop / Upload（拖放、粘贴、进度、列表、图片墙；vue：`input/FileDrop.tsx`、`input/Upload.tsx`）：选文件走 Blazor 的 `InputFile`（`IBrowserFile`），
  上传由使用方的函数完成并报告进度、支持取消；图片墙的预览等阶段 2 的 Lightbox、排序等 Sortable，做到时补
- [ ] Switch 补齐：Loading、CheckedContent / UncheckedContent（轨道里的文字）与对应的部件属性（同 vue-tsx-pro 的 Switch）

## 阶段 2：布局与展示

- [ ] 布局：Affix、BackTop、AsideLayout（FormSection 的 AdmissionExample 换用）、Prose、Splitter（JS 初始化器补尺寸变化：ResizeObserver）、ViewportFill
- [ ] 展示：Ellipsis、Stat、AnimatedNumber、ValueChange、ProportionBar、Timeline（操作人、折叠、字段差异）、Checklist、Marquee、Watermark、FieldCompare；
  Sparkline、Gauge 先做成独立的小 SVG，阶段 6 的底座做好后改用
- [ ] Sortable：拖动排序的通用能力（单容器与多容器、落点插入线、键盘），Tree、Upload、Tabs、KanbanBoard 共用
- [ ] Tree（勾选与勾选策略、懒加载、搜索过滤、拖动移动、虚拟滚动），紧接着 TreeSelect、TreeTransfer（Transfer 的树形）
- [ ] Image 与 Lightbox（缩放、旋转、拖动），接着补 Upload 的图片墙预览
- [ ] VirtualList 补齐：随页面滚动、横向、骨架行、跳到第几项、可见范围变化的回调
- 示例缺口：Skeleton 的 layouts 换用 Stat；Drawer 的 detail 示例换用 Timeline

## 阶段 3：浮层与导航

- [ ] Tour
- [ ] Breadcrumbs、Steps（Form 的分步示例换用）、NavMenu、NavBar（横向导航，放不下收进“更多”；AppShell 混合布局示例换用）

## 阶段 4：表格

- [ ] 查询的契约（先写 DESIGN 一节）：`TableQuery`（页码、每页条数、多列排序、列筛选、筛选栏的条件树）是纯数据类型、不依赖任何 ORM，
  能与查询字符串互转（刷新、分享链接保持状态）。对照 vue-tsx-pro 的 `use-table-query.ts`、easy-query TS 的 `parseTableRequest` 与 EasyQuerySharp 的 `FilterBy` / `SortBy`，
  让 EasyQuerySharp 一侧不经转换就能消费
- [ ] DataTable / DataTableView：列用子组件声明（`PropertyColumn` 按表达式取值，标题取 `[Display]`，排序键取属性路径；`TemplateColumn` 自定义内容），也可以给列定义列表；
  数据给 `Items`（内存里排序、筛选、分页）或 `ItemsProvider`（服务端）；排序、表头筛选、选择、展开、固定列、列宽拖动、网格方向键与单元格编辑（行校验）、行拖动、虚拟滚动、骨架行、密度
- [ ] ColumnHeaderActions、ColumnResizer、ColumnSettings、SelectionBar、ColumnTable、TreeDataTable、PivotTable、CSV 导出（vue 的 `csv.ts`；xlsx 不做）
- 示例缺口：Skeleton 的 table 示例、Table 的 column-table 示例、Drawer 的 imperative 示例、Badge 的表格状态示例换用 DataTable

## 阶段 5：日期（`Rune.UI.Dates`）

- [ ] 取值与格式：`DateOnly` / `TimeOnly` / `DateTime` / `DateTimeOffset`，格式、周起始、月份名取 `CultureInfo`，“现在”取 `TimeProvider`；
  对照 vue 的 `DateTime.tsx`、`date-locale.ts`、`date-segments.ts`，行为差异写进 DESIGN
- [ ] DateField、TimeField、SegmentedField、DateSegmentGroup
- [ ] Calendar、RangeCalendar、QuarterCalendar、CalendarBoard / CalendarView（整页日历的二维方向键：JS 初始化器补网格焦点）
- [ ] DatePicker（日、周、月、季、年，快捷项、“今天”、禁用的时间段）、DateRangePicker、TimePicker、RelativeTime、Countdown
- [ ] WebAssembly 下程序集懒加载（`BlazorWebAssemblyLazyLoad`）验证

## 阶段 6：图表（`Rune.Charts`，C# 输出 SVG）

- [ ] 底座：比例尺、坐标轴刻度、图例、提示、响应式尺寸、图表色 token（已有 `ChartColors`）、标注、缩放、导出图片与数据
- [ ] Line、Area、Bar（堆叠、100% 堆叠）、Combo（双轴）、Scatter、Donut、Radar、Funnel、Treemap、Waterfall、Sankey、Chord、Graph、Timeline、热力图；Sparkline、Gauge 改用这套底座
- [ ] Map（数据来源见已定的决定第 9 条）
- [ ] WebAssembly 下程序集懒加载验证

## 阶段 7：页面组合与主题定制

- [ ] 输入：ConditionBuilder（条件树与阶段 4 的 `TableQuery` 共用一份类型）、PeoplePicker / PeoplePickerPanel（用户、部门、角色）
- [ ] FilterBar（基本筛选 + 接 ConditionBuilder 的高级筛选）、ActionBar、QueryTable（FilterBar + DataTable + 工具栏：密度、列设置、刷新、全屏；查询状态进地址栏）
- [ ] FormDialog（对话框里的 EditForm：确认时校验、提交中拦住关闭）、StepsForm、DataImport（CSV）
- [ ] AuthLayout、NotificationCenter、CommentThread（接 Mentions）、KanbanBoard（Sortable 多容器）、DeadlineBadge、SlideCaptcha
- [ ] ThemeCustomizer、ThemeThumbnail，接着做文档站的主题定制页；ThemeSwitcher 的局部主题；Provider 页

## 阶段 8：.NET 全栈示例（已定的决定第 12 条）

- [ ] `apps/Rune.Sample`（Blazor Web App，Auto 渲染模式）+ EasyQuerySharp + SQLite，走完一个后台模块：
  QueryTable 服务端分页、排序、筛选（`TableQuery` → `FilterBy` / `SortBy`）、FormDialog 新建与编辑（聚合根保存）、权限（`[Authorize]`、`Can`）、预渲染 + `[PersistentState]`；
  端到端测试在 Server、WebAssembly 下各跑一遍。示例里重复出现的接线到第三次时再收成包。

## 阶段 9：质量与发布准备（贯穿全程，这里收尾）

- [ ] 端到端覆盖全部有键盘、焦点、拖动逻辑的组件；输入法用例（组字中的回车不提交、不选中：Combobox、TagsInput、Mentions、Textarea）；axe-core 无障碍检查跑全部示例（亮、暗主题）
- [ ] 截图基线进 CI（固定的 Linux 容器，Chromium，亮、暗主题），变化时人工确认
- [ ] 文档站：键盘与无障碍一节（KeyboardReference）；搜索能搜到参数名与示例标题
- [ ] 性能：大列表、表格的渲染与内存基准；WebAssembly 裁剪（R.3 打开分析器之后）与懒加载程序集；组件库样式预生成静态 CSS（可选，DESIGN 第 12 节）
- [ ] API 冻结前的审计：全部公共 API 对一遍“Blazor 化的约定”与 vue-tsx-pro 的名字，出清单逐条改
- [ ] 打包：五个包的说明、图标与许可、版本号、SourceLink、发布说明；`dotnet new` 模板（可选）；本地打包与安装试用

---

## 待决

（执行中遇到的停顿点记在这里：日期、项号、卡在哪、需要用户决定什么。）

## 旧阶段的去处

旧 4（对照工具与基础设施）→ R.2 与各阶段；旧 5 → 1（Upload）、2（TreeSelect、TreeTransfer）、7（PeoplePicker、ConditionBuilder）；旧 6 → 1；旧 7 → 2；
旧 8 → 2（Lightbox）、3；旧 9 → 4；旧 10 → 5；旧 11 → 6；旧 12 → 7；旧 13（文档站）已基本完成，剩下的进 7（主题定制页）、9（键盘说明）；旧 14 → 9。
