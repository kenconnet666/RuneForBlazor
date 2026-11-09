# 路线图

状态标记：`[ ]` 未开始、`[~]` 进行中、`[x]` 完成、`[!]` 待决（原因写在该项下面）。

本文只列目标、已定的决定和还没做完的事。已完成的变化见 [CHANGELOG.md](../CHANGELOG.md)，写法与取舍见 [DESIGN.md](DESIGN.md)，
跟进 vue-tsx-pro 的取舍见 [STYLE-ALIGNMENT.md](STYLE-ALIGNMENT.md)；开发流程、命令与发版步骤在项目 skill `.claude/skills/rune-dev`。
完成了的阶段（树与长列表、浮层与导航、表格、日期、图表）的清单见 git `46db83f:docs/ROADMAP.md`。

## 目标

**vue-tsx-pro 组件库的 Blazor 版，成熟可用；对齐 vue-tsx-pro 0.21.0，0.12.0 起发布到 nuget.org（`RuneForBlazor.*`）。**

文档站上线由用户决定；vue-tsx-pro 有的都尽量做，包括原来列为“不做”的扩展（阶段 7）。

- **外观与交互对齐 vue-tsx-pro**：样式配方、键盘、焦点、aria、动效以它为准，`tools/parity` 的报告与规格比对（`SpecParityTests`）没有未解释的差异。
- **写法是地道的 Blazor**：API 的形状按 Blazor 与 .NET 的习惯（见“Blazor 化的约定”），不照搬 Vue 的组合式写法；静态 SSR、Server、WebAssembly、Auto 四种渲染模式都能用。
- **能接 .NET 后端**：表格与筛选栏的查询请求是一份普通的 C# 类型，EasyQuerySharp 的 `FilterBy` / `SortBy` 能直接消费（全栈示例见阶段 8）。

## 已定的决定

| #   | 事项 | 决定 |
| --- | ---- | ---- |
| 1 | 外观 | 以 vue-tsx-pro 为准：token、主题由脚本生成；组件的配方按 vue-tsx-pro 对应组件**逐条移植**（尺寸、内边距、圆角、颜色 token、状态、过渡），不凭印象重画；做不到的写进 DESIGN |
| 2 | 交互与无障碍 | 以 vue-tsx-pro 为准（键盘、焦点、aria、读屏播报、动效）；实现按 DESIGN 第 8 节：原生元素优先，JS 初始化器做事件委托，组件只写 data 属性 |
| 3 | 依赖 | 运行时只依赖 ASP.NET Core；测试用 bUnit、xUnit、`Microsoft.Playwright`；生成器脚本只用 node 内置模块与 `npm pack`。阶段 7 的扩展包可以随包带上 vue 用的同一个 JS 库（three.js、CodeMirror 等，经 `tools/vendor-gen` 固定版本、许可写进 NOTICE，只在用到时加载）；其余依赖进“待决” |
| 4 | 包 | `RuneForBlazor`（运行时与生成器）、`.UI`（组件，含表格）、`.UI.Dates`、`.UI.Area`（省市区）、`.Charts`、`.Charts.Maps`、`.Icons`；程序集与命名空间是 `Rune.*`（nuget.org 上的 `Rune` 已被占用） |
| 5 | 命名 | 组件名、参数名照 vue-tsx-pro（PascalCase），参数与事件的**形状**按“Blazor 化的约定”；与 Razor / HTML 冲突时改名并记在 DESIGN（TextSpan、TextLink、NativeSelect） |
| 6 | 语言 | 组件里的文字进语言包（`UIConfig.Lang`），内置 zh-CN（默认）与 en-US，由 lang-gen 从 vue-tsx-pro 生成；拼音匹配做成可选的扩展（阶段 7，接 `UIConfig.TextMatcher`） |
| 7 | 演示 | 文档页的路由、示例的场景与顺序照 vue-tsx-pro 文档站，方便逐页对照；渲染模式测试台 Rune.Demo 保留，与文档站共用 Rune.Docs.Content 的页面 |
| 8 | 范围（2026-11-06 修订） | code-editor、diagram、three、xlsx、演练场、拼音、storage、mobile、流程的前端都做（阶段 7）。mini（Vue 渲染器跑进小程序）与 kit（Nitro 上的全栈框架）是 Vue / Node 专用的：kit 的角色由 Blazor Web App 本身与阶段 8 的全栈示例承担，mini 不做。完整的流程引擎暂不做 |
| 9 | 地图数据 | 图表的 Map 用公有领域或许可明确的来源（如 Natural Earth）导出，来源与许可写进 NOTICE |
| 10 | 部署 | 文档站导出静态文件，部署到 Cloudflare Workers 的静态资源（https://rune-for-blazor.zerodep.dpdns.org，同 vue-tsx-pro 的做法）；`docs.yml` 只手动触发，令牌 `CLOUDFLARE_API_TOKEN` 在只允许 master 的 `docs` 环境里 |
| 11 | 发布与版本（2026-11-09 修订） | 推 `v*` tag 由 `release.yml` 经 nuget.org 的可信发布推送，步骤见 rune-dev skill。版本号自己走（0.12.0 起），不跟 vue-tsx-pro 的版本号；对齐的 vue 版本写在 CHANGELOG 每一节 |
| 12 | 全栈示例（2026-11-06） | 做阶段 8。EasyQuerySharp 只进示例，组件库不引用；`TableQuery` 到 `FilterBy` / `SortBy` 的接线先写在示例里、不单独出包；CI 按固定的提交检出 EasyQuerySharp（同 `VUE_TSX_PRO_REF`） |

## Blazor 化的约定（2026-11-06）

以 vue-tsx-pro 为准的是**外观、交互、无障碍和名字**；**API 的形状**按 Blazor 与 .NET。

| vue-tsx-pro | 本库 |
| --- | --- |
| `v-model`、`_bind:open` | `@bind-Value`、`@bind-Open`（`Xxx` + `XxxChanged`，输入控件另有 `ValueExpression`）；`@bind-Value:after` 照常可用 |
| 事件回调返回 `false` 拦下 | 事件参数上的 `KeepOpen` / `Cancel`（同 `CancelEventArgs`） |
| 插槽 | `RenderFragment` / `RenderFragment<T>`，名字是 PascalCase 名词（`Title`、`Footer`、`ItemTemplate`） |
| `items` / `columns` 数组 | 能声明的用子组件（`<TabPanel>`、`<DescriptionsItem>`、表格的 `<PropertyColumn>`）；数据驱动的用 `Items` + `ItemTemplate` |
| 取远程数据的函数 | 委托：选项用 `OptionsLoader<T>`；表格、长列表用 `ItemsProvider`（同 Virtualize / QuickGrid：起始位置、条数、排序、`CancellationToken`） |
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

- **部件属性 `PropsXxx`**：类型是 `Attrs`（带 Sx、事件按接收者合并），不是 `AdditionalAttributes` 那样的字典；改名牵动全部组件，收益小。
- **组件不调用 `IJSRuntime`**：JS 初始化器 + data 属性，四种渲染模式行为一致、Server 模式没有往返（DESIGN 第 8 节）。`FocusAsync` 用 Blazor 自带的 `ElementReference.FocusAsync`，不算例外。
- **样式是运行时生成的强类型 CSS**，不用 CSS 隔离（`.razor.css`）：主题、档位、按部件定制都要在 C# 里合并（DESIGN 第 5 节）。

## 执行顺序

6 → 7 → 8 → 9；阶段 9 的质量项贯穿全程，跟进 vue-tsx-pro 不单列阶段（见 STYLE-ALIGNMENT 的“待跟进”）。某一项进入待决时，跳到下一个不依赖它的项。
已有页面里等其他组件的示例缺口，记在提供那个组件的阶段里，做到时补上。

---

## 阶段 6：页面组合与主题定制

- [ ] 输入：ConditionBuilder（条件树与 `TableQuery` 共用一份类型）、PeoplePicker / PeoplePickerPanel（用户、部门、角色）
- [ ] FilterBar（基本筛选 + 接 ConditionBuilder 的高级筛选）、ActionBar、QueryTable（FilterBar + DataTable + 工具栏：密度、列设置、刷新、全屏；查询状态进地址栏）；
  照 vue 的新版布局（STYLE-ALIGNMENT 的待跟进）
- [ ] FormDialog（对话框里的 EditForm：确认时校验、提交中拦住关闭）、StepsForm、DataImport（CSV）；SchemaForm（按 C# 模型生成，先出方案）
- [ ] AuthLayout、NotificationCenter、CommentThread（接 Mentions）、KanbanBoard（Sortable 多容器）、SlideCaptcha
- [ ] ThemeCustomizer、ThemeThumbnail，接着做文档站的主题定制页；ThemeSwitcher 的局部主题与选主题网格

## 阶段 7：扩展

各自成包或成可选模块，WebAssembly 下能懒加载；做之前先读 vue 对应包的现状，照它的 API 与示例。

- [ ] 拼音匹配：`UIConfig.TextMatcher` 的拼音实现（全拼、首字母、多音字），Select、Combobox、CommandPalette、Transfer 的搜索都能用；字表的来源与许可写进 NOTICE
- [ ] 表格导出 xlsx（vue 的 xlsx：自己写 OOXML，不引第三方库；C# 用 `System.IO.Compression`）
- [ ] 文档站的属性演练场（vue 的 PropsPlaygroundButton：改参数实时看效果，参数表从 API 文档生成）
- [ ] 前端存储（vue 的 `@zerodep-dev/storage`）：IndexedDB / localStorage / 内存一个异步接口，上面一层 TTL 与过期后先用旧值再刷新的缓存
- [ ] CodeEditor（CodeMirror，同 vue 的 code-editor）
- [ ] Diagram、DiagramControls、DiagramLanes、DiagramMinimap、DiagramNode（同 vue 的 diagram）
- [ ] three：ThreeCanvas、ModelViewer、Globe、ParticleField、ShaderBackground、MeshGradient、AuroraBackground、WaveGrid、EffectSurface、Confetti（three.js）；
  之后补图表的“统计看板示例”页（vue 的 charts/Dashboard，用到 Globe）
- [ ] 流程的前端：流程设计与审批界面（vue ROADMAP 阶段 9、14 的前端部分），接阶段 8 的全栈示例
- [ ] 移动端组件库 `Rune.Mobile`（vue 的 mobile，约 50 个组件，H5）：触控手势、安全区、下拉刷新、底部弹层等；文档站加移动端一栏

## 阶段 8：.NET 全栈示例（已定的决定第 12 条）

- [ ] `apps/Rune.Sample`（Blazor Web App，Auto 渲染模式）+ EasyQuerySharp + SQLite，走完一个后台模块：
  QueryTable 服务端分页、排序、筛选（`TableQuery` → `FilterBy` / `SortBy`）、FormDialog 新建与编辑（聚合根保存）、权限（`[Authorize]`）、预渲染 + `[PersistentState]`
  （TableQueryState 在预渲染时取的那页交给交互后的渲染，不再请求一次）；端到端测试在 Server、WebAssembly 下各跑一遍。示例里重复出现的接线到第三次时再收成包。

## 阶段 9：质量（贯穿全程，这里收尾）

- [ ] 对照工具跑全部页面（亮、暗），改掉差异；之前留下的：transfer 的 members、progress、card 的 effects
- [ ] 代码走查（单独提交）：过长或职责混杂的文件（`js/core/runtime.js`、`lists.js`、`overlays.js`、`Cascader.razor.cs`、`ToastService.cs`）、重复的样式片段与死代码；入口顶部的 data 属性总表与实际用到的核对一遍
- [ ] 端到端覆盖全部有键盘、焦点、拖动逻辑的组件；输入法用例（组字中的回车不提交、不选中：Combobox、TagsInput、Mentions、Textarea）；axe-core 无障碍检查跑全部示例（亮、暗主题）
- [ ] 截图基线加暗色主题
- [ ] 文档站上线前的几项（程序集懒加载、裁剪与体积预算、SEO、部署）：见 [DOCS-SITE.md](DOCS-SITE.md)
- [ ] 性能：大列表、表格的渲染与内存基准；组件库样式预生成静态 CSS（可选，DESIGN 第 12 节）
- [ ] API 冻结前的审计：全部公共 API 对一遍“Blazor 化的约定”与 vue-tsx-pro 的名字，出清单逐条改
- [ ] `dotnet new` 模板（可选）

---

## 待决

（执行中遇到的停顿点记在这里：日期、项号、卡在哪、需要用户决定什么。）
