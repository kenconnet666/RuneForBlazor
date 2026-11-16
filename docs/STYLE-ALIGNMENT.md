# 跟进 vue-tsx-pro

当前对齐 **vue-tsx-pro 0.21.0 之后的 `95ceb0d8`**（CI 的 `VUE_TSX_PRO_REF`；vue 还没为这批审查修复发新版，规格 JSON 仍是 0.21.0 的，发版后重新取）。跟进的步骤见 rune-dev skill 的“跟进 vue-tsx-pro”；
本文只记两样：**与 vue 有意不同的地方**（以后对照时不用再查一遍），和**还没跟的改动**。逐批跟进的过程在提交说明里，
2026-11-09 之前的逐批记录见 git `46db83f:docs/STYLE-ALIGNMENT.md`。

总原则（两边一致）：维护好基础设施与基础组件，上层组件一律依赖它们构建；上层需要的能力先在基础层补齐再用，不在上层写平行实现。

## 与 vue 不同的地方

### 样式引擎（分段输出与级联层，同 vue STYLE-ENGINE.md §11）

`Sx.Layers` 记段的边界，`Attrs.Merge` / `MergeProps` / `Layer` 的默认层、定制、使用方、受管层各一段，第 k 段进 `@layer rune.s{k}`（超过 8 段并进最后一段），
主题在 `rune.vars`，全局规则（`GlobalRule`，如部件一律 border-box）在最前面的 `rune.base`。验证：`SegmentTests`（随机 500 轮）与 `tools/style-snapshot`。

| 项 | vue | Rune | 理由 |
| -- | --- | ---- | ---- |
| `revert` | 后段里只剩条件取值、或被去掉的属性，补一条无条件的 `revert` | 不补 | Rune 的合并只按“条件 + 属性”覆盖，普通值不会整体替换条件值，也没有去掉属性的写法 |
| 定制的段数 | 每层扩展（`createUi`、每层 Provider）各一段 | 合成一段 | `ComponentStyles.Then` 已经按嵌套顺序合好；语义相同 |
| 受管层 | 使用方写了 sx 时才分出去 | 一律按层分段 | `Attrs.Layer` / `MergeProps` 也用于组件内部的组合；语义相同，类多几个 |
| 宿主 CSS | 不分层的盖过整个组件库 | 同 | 演示项目删掉了 Blazor 模板的表单校验规则（分层后会稳定地盖过组件） |

### API 与配置

- 组件定制一个入口 `Components`（`ComponentConfig<XxxDefaults, XxxPart>` 的 `Defaults`、`Styles`、`Variants`），添加时拆回 `ComponentDefaults` 与 `ComponentStyles`；
  名字对不上在 C# 里编译不过，真正新增的是按档位取值的定制。`UIConfig.Defaults` / `Styles` 只读，给组件与配方取值。
- 改名、破坏性改动不留旧名（vue 留一个版本）：0.12.0 之前没有外部使用方；之后的破坏性改动写进 CHANGELOG。
- `Tone` 就是 vue 的 `StatusTone`（6 个状态色），文字色调叫 `TextTone`，不改名。
- `Can` 用 Blazor 的 `AuthorizeView`，ErrorBoundary 用 Blazor 自带的，不另做组件。
- InfiniteList 的 params 换成 `ParamsChangedAsync()`（组件自己知道搜索词何时变）；Select、Combobox、Mentions 的分页另设 `LoadPage`（一个参数不能既收数组又收分页结果）；失败文案一律取语言包。
- Grid / GridList 的 `Balanced`：最小宽度只认 px、rem、em。
- Tree 没有 `PropsRow`：Virtualize 没有行包装（规格比对基线里唯一的部件属性差异）。
- 按项给的部件属性：vue 的 `propsCell` 可以是对象或函数，Rune 分成两个参数——`PropsCell`（共用）与 `PropsCellFor`（按项，叠在共用的之后）；
  NavBar、QuarterCalendar、DataTable 都是这个约定。
- 属性合并里“不表态”的值：vue 是 `undefined`（`null` 表示去掉），Rune 是 `null`（要去掉用 `Attrs.Without`）。
- 事件的合并没有“使用方 preventDefault 就跳过组件自己的处理”（Server 模式下处理器在服务端执行）：Pagination 页码的 `onClick` 拦换页这类用受控参数。
- NumberField 的百分比看 `Format` 是不是百分比格式（`P0`、带 `%` 的自定义格式），对应 vue 的 `format.style: 'percent'`。
- vue 的类型收紧（组件管理的属性从部件属性的类型里去掉，`check:managed`）在 Rune 没有对应：`Attrs` 不按部件分类型，受管属性靠合并顺序保证。
- 没有对应物、不用跟的：`useForm` 的异步单字段校验（Rune 用 EditForm）、表单重置时清半截输入（Blazor 没有重置事件，换模型即可）、
  `mergeJsxProps` 的同名事件、Splitter 收起时的两轨模板（Rune 收起的一栏是 0 宽 + inert，不是 hidden）、FloatButton 的 `badge={false}`（`Badge` 是 `int?`）。

### 交互的实现位置

- 单选、复选的 card 外观选中按 `:has(input:checked)` 用 CSS 变（vue 按 JS 的勾选状态加类）。
- Cascader 当前项常亮的焦点环用 CSS 条件（`[data-highlighted]:where(:focus *)`），不经状态往返。
- FileDrop / Upload 的 `Directory`：IBrowserFile 只有文件名，相对路径由 JS 在选到时交给 FileDrop（`PathOf`）。
- Dialog 的 `Draggable` 拖动在 JS（overlays.js 的 grabDialog）；FloatButton 速拨的展开状态在 C#，Esc 与点到组外由 JS 发 rune-command。
- 3D（Rune.Three）：场景在 JS 模块里，`Setup` 是模块的地址（vue 是内联的 setup 函数）；ModelViewer 的 `object` 换成 `ObjectModule`（规格比对基线里的
  ModelViewer.Object）；场景的上下文多了 `three`、`options`、`host`、`dark`、`send`。Globe 在减少动态效果时也能拖动（vue 停在一帧、不响应拖动），与 ModelViewer 一致。
  文档站 Confetti 的“庆祝”示例从视口中间放（vue 从点的按钮处放：C# 拿不到按钮在视口里的位置）。
- ImageCropper 导出经隐藏的 InputFile 回到 C#，不受 SignalR 32 KB 限制；SignaturePad 画在 JS 里，静态 SSR 也能签。
- Listbox、Tree 的虚拟行用 Virtualize，行在文档流里（vue 叠在同一个 grid 格子里）。

### 0.23.0–0.28.0 跟进时与 vue 不同的写法

- SearchField 继承 TextField（参数全部沿用），回车用输入框的 change 接（不用 keydown：那样每次按键都要交给 C#），所以失去焦点时还在等停手的那次也会立即触发。
- Embed 的地址由 JS 在接上 load 之后写到 iframe 上（`data-rune-embed`），加载完没有写在外层的 `data-state`、样式跟着它，没有往返；
  vue 是挂载后才渲染 iframe。Embed、Video 自己给 iframe / video 写了宽高 100%：外层的 `data-scope` 是 embed / video，AspectRatio 那条“里面的元素铺满”的规则选不中它
  （vue 0.26.0 里 iframe 因此是 300 × 150、没有铺满，已开 issue kenconnet666/vue-tsx-pro#7，等它定了再对齐）。
- Video 的自动播放由 JS 判断（`data-rune-autoplay`）：标记里不写 autoplay，减少动态效果时不播、显示控制条。
- Abbr 的点按显示、紧凑导航分组的触屏点开都在悬停层的 JS 里（`data-rune-tooltip-press`、`data-rune-hover-toggle`），状态不经过 C#。
- 内容参数：Blockquote 的 `Source`、Figure 的 `Caption` 是文字，要放链接时用 `SourceContent` / `CaptionContent`；PageHeader 的标题是文字，`Meta`、`Description`、`Actions` 是内容片段。
- AspectRatio、Embed、Video 的 `Ratio` 是 CSS 的写法（`"16 / 9"`），不收数字。
- RuneProvider 的 lang、dir：语言是配置出来的才写（AddRuneUI 写了 `Lang` / `Languages`，或某一层写了 `Lang`），最外层也一样；vue 是只看这一层有没有配置。
- AnimatedNumber 交给浏览器的 `signDisplay` 默认是 `negative`（取整后是 0 的负数不带负号），与 C# 这边的格式化一致。
- 表格里“没写色调的行”与“已选的行”拆成两个样式：高对比下只有已选、正在看的行画系统的高亮色。
- 浮层定位（`js/overlay/floating.js`）与 vue 的 `utils/dom/floating.ts` 逐行对应；`computePlacement` 的 9 组用例在 E2E 的 FloatingPlacementTests 里（没有 JS 单元测试工具，
  在浏览器里导入模块跑）。Rune 定位好之前用透明加不接指针（vue 是 visibility: hidden，见 DESIGN.md 的浮层一节），新手引导的卡片也走同一个 followElement。
- Video 的 `Controls` 是枚举 `VideoControls`（Native、Custom、None），对应 vue 的 `true` / `'custom'` / `false`。自绘控制条：操作 video 在 JS（`js/display/video.js`，
  按钮的 `data-rune-video-action`、滑块的 rune-slide、菜单项的 `data-rune-video-caption` / `-speed`），video 的状态经 rune-command（video-state）回到 C# 渲染；
  接管时 JS 先按 video 现在的状态发一次（vue 0.28.0 修的“预渲染页面上时长是 0:00”Rune 一开始就没有）。要 C# 跟着状态渲染，
  所以静态渲染与交互接管前的预渲染用浏览器自己的控制条。收起只在 JS（控制条的 `data-state`、外层的 `data-idle`），键盘聚焦在里面时 JS 不收起（vue 用 `:has(:focus-visible)` 的样式）；
  菜单里的按键全交给菜单（vue 在菜单里按 M、F、K 也会执行）。
- NativeSelect 的值是任意类型、按位置对应选项（之前就是这样），分组与不可用用 `ItemGroup`、`ItemDisabled` 给（vue 是选项对象的 group、disabled）。

### 阶段 6（页面组合）与 vue 不同的写法

- FormDialog：表单用 `Model` / `EditContext`（同 Form），没有 vue 的 controller；提交是 `OnSubmit`（`FormDialogSubmitEventArgs`），后端拒绝时设 `KeepOpen`、把说明交给 `Error`
  （vue 是 onSubmit 抛出带业务错误码的 HttpError、读 controller.formError）。确认按钮是表单的提交按钮：点它时 Dialog 的确认等着表单的 submit 事件交回结果，
  在字段里按 Enter 提交成功后组件自己关上（vue 要调用方在 onSubmit 里写 open = false）。Dialog 的参数逐个声明再交下去：带类型的参数不能经未声明的属性转交（Razor 当成字符串）。
- useFormDialog 对应 `DialogService.PromptAsync(FormDialogOptions)`：结果是 `FormDialogValues`（`values["名字"]`、多选 `values.All("名字")`），
  `OnSubmit` 返回 false 时对话框留着（同 `ConfirmOptions.OnConfirm`；vue 是 reject）；日期字段用浏览器自带的日期输入（Rune.UI 不依赖 Rune.UI.Dates）。
- StepsForm：表单同 Form 用 `Model` / `EditContext`、`OnValidSubmit`，一步的 `Fields` 是模型上的属性名（vue 是字段路径，含子路径前缀；Blazor 的嵌套字段要拿到所在对象，
  反射取属性在裁剪后不可靠，所以只认顶层属性）。“下一步”经 Form 新增的 `BeforeSubmit`（返回 false 时不校验、不提交）把提交换成下一步，
  只校验这一步：对这几个字段 NotifyFieldChanged，校验器只校验它们。Form 的错误汇总在分步表单里关掉，没通过时焦点到这一步第一个出错的控件。
- FilterBar：Blazor 的内容片段拆不成一个个节点，常用条件与 More 放在同一个栅格里（More 外面一层 display: contents），JS（`js/layout/filter-bar.js`，
  算法同 vue 的 measure / layoutFields）把列数写成 `--rune-filter-template`、收起时把放不下的藏起来；展开后下面几排的列宽与第一排相同，但只排在条件区里，
  不伸到按钮区下面（vue 的第二排起铺满整宽），所以没有 `PropsMore`。量出来之前 More 收着，常用条件按最小宽度自动排。
  高级筛选同 vue（ConditionFields、@bind-Condition、收起后的摘要），条件的写法见下一条。
- QueryTable：列是 `Columns` 里的列组件（同 DataTable），列设置要的列由 QueryTable 经 DataTable 的列收集通知拿到；TItem、TFilters 要写出来（Razor 推断出的类型参数传不到列里）。
  草稿是 `Query.Filters` 的浅拷贝（MemberwiseClone，vue 是深拷贝 cloneValue；列表类的条件由控件整个换掉）。加载失败的提示统一是“加载失败，请重试”（vue 按错误码说明原因）。
  全屏由 fullscreen.js 让表格区域自己进浏览器全屏（`data-rune-fullscreen-element`）；浏览器不让时区域照样铺满窗口。
- 条件（vue 的 Condition）叫 `ConditionExpression`：`Rune.Styles.Condition` 已是样式条件。比较、全部、任一、否定是四个 record（ConditionRule、ConditionAll、ConditionAny、
  ConditionNot），值是 object（数字用 decimal，日期是 ISO 文字，一组值、范围是 IReadOnlyList&lt;object?&gt;）；求值与摘要在 `Conditions`（Matches、Describe、CountRules），
  求值按字段名取值由调用方给（C# 没有按名字取属性的便宜办法）。还没有与 vue 的 JSON 写法互转的转换器。
  ConditionBuilder 的日期是浏览器自带的日期输入（Rune.UI 不依赖 Rune.UI.Dates），要 DatePicker 时写字段的 Input。
- ActionBar 是否盖着下面的内容由 JS 看（`data-rune-action-bar`，affix.js），写 `data-stuck`。
- DataImport：没有 zod，vue 的 `schema` 拆成 `Validate`（字段名 → 原因，在 Required、数字检查之后）与 `ToRow`（通过校验的值 → 行）；值是 object，数字是 decimal。
  “下载模板”是链接 `TemplateHref`（下载接口，或 `Sheets.CsvDataUrl` 生成的 data URL），不是 vue 的 `onTemplate` 回调：C# 触发浏览器下载要 JS 互操作，链接在静态 SSR 里也能用。
  只自带 CSV 的读写（`Sheets.ReadCsvAsync`、`ToCsv`）；vue 的 xlsx 子路径（ExcelJS）没有对应，Excel 由调用方选库读成矩阵后交给 `Sheets.FromMatrix`（Parse 参数）。
- PeoplePicker：`PeopleEntry` 的 Id 是文字（vue 是文字或数字），类型是文字常量 `PeopleType.User / Department / Role`（扩展类型直接写名字，名称进语言包的 PeopleTypes，vue 是接口合并）；
  `PeopleSource` 是带委托的 record（Children 必填，Search、Roles 可选），取消用 CancellationToken；整棵在内存里时用 `PeopleSource.Local`（vue 的 localPeopleSource）。
  “最近”经 LocalSetting 存，键是 `rune.people.recent.{RecentKey}`（vue 是 `vtp.people.recent.…`）。触发按钮上的 ↓ 经 `data-rune-arrow-down` 交给 C# 打开对话框。
- NotificationList / NotificationCenter 在 Rune.UI.Dates 包里（要 RelativeTime；Rune.UI 不依赖日期包）。时间是 DateTimeOffset（vue 是 ISO 文字）；
  没有地址的一条是 button（vue 是 `a role="button"` 加按键处理：Blazor 不能按条件阻止默认行为）；`title={false}` 对应 `HideTitle`，按条的部件属性是 `PropsItemFor`。
  有地址的一条是普通链接，由 Blazor 的导航接管（vue 走路由适配 useRouterLink）。

### 外观与文档站

- 界面文字的字重是 500（标题、数字、Result 的页面级标题仍是 600）；以后做的组件照此。组件库只用系统字体栈。
- 文档站的字体切换：Rune 没有前端打包，字体 CSS 从 jsDelivr 按版本取（`apps/Rune.Docs/wwwroot/fonts.js`），在 app.css 里按 `data-docs-font` 覆盖最外层 Provider 的字体变量。
- 文档站有意的差别（参数名、包名、代码示例的语言等）见 [DOCS-SITE.md](DOCS-SITE.md)。

## 待跟进

跟进时以 vue 的组件规格 JSON（`/spec/components.json`）与 RELEASE.md 为准逐项核对；vue 的问题在 kenconnet666/vue-tsx-pro 开 issue（标签 `parity`）。

1. 文档站逐页示例调整（vue P2.2 / P2.3）：示例改用组件库组件、卡片网格落单等；Splitter、ScrollArea、Tabs 里手写的可点行改用 ItemList。
2. TabPanel 听 `beforematch` 切到自己（页内查找命中隐藏的页签时）；现在只有 Collapse 处理。
3. 紧凑密度 = 主题 scaling 0.9，不另设密度开关。
4. 随阶段 6 的组件一起做（Rune 还没有这些组件）：
   - FilterBar / QueryTable 的新版布局：第一排是标题、放得下的条件（常用在前、more 在后）与按钮（展开、重置、查询），不加竖线；放不下的收进“展开”，
     全放得下不出“展开”；展开的几排与第一排每格同宽，minFieldWidth 默认 14em；按钮区与 QueryTable 工具栏 minWidth 0、maxWidth 100%
   - FormDialog 的 select 支持 multiple（返回数组）、选项分组、多选默认值给数组
   - 语言包新键 formRowLabel / formRowActions / formTabs / formValueTrue / formValueFalse / formValueEmpty（随 SchemaForm）
   - SchemaForm 一族（visibleWhen / requiredWhen / readonlyWhen、access、明细表与合计、formSources、页签、SchemaView、focusControl 发 beforematch）：先出方案
   - KanbanBoard 把手的悬停；CommentThread 的 RelativeTime
5. 不跟：vue 的流程引擎（与组件无关）；CSP nonce 等阶段计划时再定。
6. vue 0.23.0–0.26.0（`6e6f831d..c42b2f39`）已跟（2026-11-15，CI 的 `VUE_TSX_PRO_REF` 在 `c42b2f39`）。没有对应、留到以后的：
   - 随组件一起做（Rune 还没有）：KanbanBoard、CodeEditor、Diagram、SchemaForm、SlideCaptcha、
     ThemeCustomizer、ErrorBoundary、表格导出 xlsx 的那几条（StepsForm、DataImport、ActionBar、QueryTable、ConditionBuilder、NotificationCenter 已在阶段 6 做了）。
   - 交互模式是 Server 时拿不到浏览器的时区：时刻按 `Lang.TimeZone`，没配置时是服务器的时区（WebAssembly 里是浏览器的）。
     预渲染的页面里时刻、日历的“今天”同样按服务器当时的；交互接管是重新渲染一遍，不会像 vue 水合那样留着旧的属性（0.25.0 修的那个问题 Rune 没有），
     但永远不交互的静态页会停在渲染那一刻。要准的话配置 `Lang.TimeZone`，或者传 `Now` / `Today`。
7. vue 0.27.0、0.28.0（`c42b2f39..c2be5550`）已跟（2026-11-14，CI 的 `VUE_TSX_PRO_REF` 在 `c2be5550`），与 0.23.0–0.26.0 一起进 Rune 0.18.0。没有对应、留到以后的：
   - StepsForm 的只读回看（Rune 还没有 StepsForm）。
   - Autocomplete 去掉 `empty`：Rune 的 Autocomplete 继承 Combobox，`Empty` 参数还在（没有建议时本来就不弹出，写了也显示不出来）。
   - 文档站 Popover 页的“自己算位置”、Autocomplete 与 Mentions 新加的示例已在 0.19.0 补上（见 DOCS-SITE.md）。
8. vue 0.29.0（`0de173eb`）已跟（2026-11-15，CI 的 `VUE_TSX_PRO_REF` 在 `0de173eb`），进 Rune 0.19.0：Video 不知道时长的视频（JS 的 lengthOf 同 vue）、
   Affix 的部件标记、四个文档站示例。部件属性去掉原生元素专用绑定（`_value`、`_checked`…）是 TS 类型层面的，Blazor 的参数本来就是各组件自己声明的，不用跟。
