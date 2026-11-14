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

### 0.23.0–0.26.0 跟进时与 vue 不同的写法

- SearchField 继承 TextField（参数全部沿用），回车用输入框的 change 接（不用 keydown：那样每次按键都要交给 C#），所以失去焦点时还在等停手的那次也会立即触发。
- Embed 的地址由 JS 在接上 load 之后写到 iframe 上（`data-rune-embed`），加载完没有写在外层的 `data-state`、样式跟着它，没有往返；
  vue 是挂载后才渲染 iframe。Embed、Video 自己给 iframe / video 写了宽高 100%：外层的 `data-scope` 是 embed / video，AspectRatio 那条“里面的元素铺满”的规则选不中它。
- Video 的自动播放由 JS 判断（`data-rune-autoplay`）：标记里不写 autoplay，减少动态效果时不播、显示控制条。
- Abbr 的点按显示、紧凑导航分组的触屏点开都在悬停层的 JS 里（`data-rune-tooltip-press`、`data-rune-hover-toggle`），状态不经过 C#。
- 内容参数：Blockquote 的 `Source`、Figure 的 `Caption` 是文字，要放链接时用 `SourceContent` / `CaptionContent`；PageHeader 的标题是文字，`Meta`、`Description`、`Actions` 是内容片段。
- AspectRatio、Embed、Video 的 `Ratio` 是 CSS 的写法（`"16 / 9"`），不收数字。
- RuneProvider 的 lang、dir：语言是配置出来的才写（AddRuneUI 写了 `Lang` / `Languages`，或某一层写了 `Lang`），最外层也一样；vue 是只看这一层有没有配置。
- AnimatedNumber 交给浏览器的 `signDisplay` 默认是 `negative`（取整后是 0 的负数不带负号），与 C# 这边的格式化一致。
- 表格里“没写色调的行”与“已选的行”拆成两个样式：高对比下只有已选、正在看的行画系统的高亮色。

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
   - FormDialog 的 select 支持 multiple（返回数组）、选项分组、多选默认值给数组；PeoplePicker 的触发框 role=combobox
   - 语言包新键 formRowLabel / formRowActions / formTabs / formValueTrue / formValueFalse / formValueEmpty（随 SchemaForm）
   - SchemaForm 一族（visibleWhen / requiredWhen / readonlyWhen、access、明细表与合计、formSources、页签、SchemaView、focusControl 发 beforematch）：先出方案
   - KanbanBoard 把手的悬停；NotificationList / NotificationCenter、CommentThread 的 RelativeTime
5. 不跟：vue 的流程引擎（与组件无关）；CSP nonce 等阶段计划时再定。
6. vue 0.23.0–0.26.0（`6e6f831d..c42b2f39`）已跟（2026-11-15，CI 的 `VUE_TSX_PRO_REF` 在 `c42b2f39`）。没有对应、留到以后的：
   - 随组件一起做（Rune 还没有）：StepsForm、KanbanBoard、CodeEditor、Diagram、DataImport、SchemaForm、SlideCaptcha、ActionBar、QueryTable、
     ConditionBuilder、NotificationList / NotificationCenter、ThemeCustomizer、ErrorBoundary、表格导出 xlsx 的那几条。
   - 交互模式是 Server 时拿不到浏览器的时区：时刻按 `Lang.TimeZone`，没配置时是服务器的时区（WebAssembly 里是浏览器的）。
     预渲染的页面里时刻、日历的“今天”同样按服务器当时的；交互接管是重新渲染一遍，不会像 vue 水合那样留着旧的属性（0.25.0 修的那个问题 Rune 没有），
     但永远不交互的静态页会停在渲染那一刻。要准的话配置 `Lang.TimeZone`，或者传 `Now` / `Today`。
