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
6. vue 0.23.0（`6e6f831d..b7b09011`）与 0.24.0（`b7b09011..6e7b07b7`），2026-11-14 收到通知，还没跟（细节以 vue 的 CHANGELOG.md 两节为准）：
   - ModelViewer：环境贴图等模型加载完才生成（Rune 已经是这样）；没有 WebGL 时新增状态 fallback（OnStatusChange 收到、根上 data-status="fallback"、
     不转圈、不标 aria-busy）——Rune 现在会一直停在 loading，要跟。
   - 0.23.0 行为：Provider / ConfigScope 只在这一层配置了语言时才写 lang、dir；取整后是 0 的负数不带负号，fileSize 遇到非数返回占位符、999,999 B 显示 1 MB；
     主题、语言包、组件默认值里值是 undefined 的键不盖掉外层，主题里不认识的 token 报错；表头关键字筛选停手 300ms 生效、回车与清空立即生效；
     Steps navigable 时出错的步骤可点回；LoadingOverlay 一开始加载就拦点击；没有箭头的 Popover 内容过高在面板里滚；嵌套 ContextMenu 只弹里层；
     模态快速关了再开不被迟到的 close 关掉；图表（柱子夹在数值轴范围里、折线与面积触屏点得动下钻、全负数的对齐轴、svg 固定 direction: ltr、
     雷达轴按数据最大值、zoomRange 夹到数据长度、MapChart 全空时显示 empty）；强制颜色模式下日历选中与区间用 Highlight、今天加下划线，表格选中与正在看的行画一圈 Highlight。
     StepsForm、KanbanBoard、CodeEditor 的几条随对应组件（Rune 还没有）。
   - 0.23.0 新属性与文案：Avatar 的 decorative；Listbox 的 readonly（Transfer、TreeTransfer 在只读的 Field / Form 里不能移动）；ControlGroup 把 Field 的不可用、只读交给组里的控件；
     Pagination、PaginationBar 的 disabled；themeSettingsCode 第四个参数 base；语言包新键 floatButtonMore、copyFailed，页面组合文案 dataImportFailed；英文 paginationTotal 1 条时是 “1 item”。
   - 0.24.0 行为：usePersistentState、useMediaQuery 水合时先按服务端的值渲染、挂载后换成真实的（Blazor 的预渲染 + 交互接管有同样的问题时照此：
     先渲染默认值，交互就绪后再读存的值、媒体查询）；sameValue 对 Map 逐项比、Set 比成员；tableToXlsx 的日期按 timeZone（新选项）的墙上时间写；
     TagsInput 允许自己输入时打字不自动高亮第一条建议；触屏：NavList 紧凑分组点图标弹出子项、ContextMenu 按住 700ms 打开、Splitter 与 ResizeHandle 在粗指针下扩大命中区；
     只读 / 不可用的表单里 Autocomplete 不可用时隐藏 input 不提交；NavBar 收进下拉与“更多”的项也带徽标。ConditionBuilder、CodeEditor、NotificationList、Diagram 的几条随对应组件。
   - 0.24.0 新属性：Switch 的 description / propsDescription；ColumnTable 的 empty（没有行时表体里一行占满，默认“暂无数据”）。
7. vue 0.25.0（`6e7b07b7..fffc0c7f`），2026-11-14 收到通知，还没跟（细节以 vue 的 CHANGELOG.md 的 0.25.0 一节为准）：服务端渲染后的时区与日历的“今天”。
   - 时区（语言没配置 timeZone 时）：服务端固定按 UTC 渲染时刻，不取服务器所在的时区；水合时先按 UTC，挂载后换成浏览器的时区，文字与悬停提示都改过来。
     涉及 useFormat 的 date / relative（DateTime、RelativeTime、表格的日期列）、Countdown、日历的“今天”、TimelineChart。语言配置了 timeZone 的不变。
   - 日历的“今天”（Calendar、RangeCalendar、QuarterCalendar、CalendarBoard）：服务端渲染时把渲染的时刻记在根元素上（data-now），水合时先按它算今天，
     挂载后换成真实的今天；没有值、没指定默认日期时显示的月份（季度）也跟到真实的今天。静态生成的页面隔几天再打开，今天的标记不再停在构建那天。
   - 新增：useTimeZone()、isServerRendering()；useDateLocale() 的返回值多了 rootAttrs()。
   - 对 Rune 要核对的：预渲染（服务器）与交互接管（浏览器）之间有同样三样只有浏览器知道的东西——时区、今天、存在浏览器里的状态（同上一条的 0.24.0）。
     预渲染时用确定的值（时区 UTC、渲染那一刻的今天），交互就绪后换成浏览器的；不在预渲染时取服务器本地时区。文档站是静态导出的，“今天停在构建那天”
     在 Rune 的日历页上多半也有。核对办法：浏览器时区设成和服务器不同、把系统日期往后拨几天再打开预渲染的页面，看相对时间的 title、日历的今天标记。
8. vue 0.26.0（`fffc0c7f..c42b2f39`），2026-11-14 收到通知，还没跟（细节以 vue 的 CHANGELOG.md 的 0.26.0 一节为准）：新增 8 个组件、统一搜索框、Collapse 不被内容撑宽。
   - 新组件，文字（Text 家族，属性同 Text）：Sub、Sup（position: relative、font-size 0.75em、line-height 0、vertical-align baseline，sub 用 bottom -0.25em、sup 用 top -0.5em）；
     Del（划线 + 次要色）、Ins（浅绿底 successSoft、不用下划线、padding-inline 0.15em、圆角 xs）；Strike（s 元素，只是划线）。
   - 新组件 Abbr（abbr）：点状下划线、cursor: help、tabindex=0；全称放在 Tooltip 里（不写原生 title），悬停、聚焦、点按都打开，aria-describedby 指向提示。属性 title（必填）、propsTooltip。
   - 新组件 Figure（figure + figcaption）：caption、captionSide（bottom 默认 / top，用 column-reverse）、align（start / center / end）、propsCaption；去掉 figure 的默认外边距。
   - 新组件 AspectRatio（div）：ratio（数或 CSS 写法，默认 16 / 9）；里面的元素铺满，img、video 默认 object-fit: cover。
   - 新组件 Embed（iframe）：src、title（必填）、ratio（默认 16 / 9，null 不定比例）、onLoad、propsRoot、propsPlaceholder；默认 loading=lazy、
     referrerpolicy=strict-origin-when-cross-origin；加载完之前显示骨架、iframe 透明；data-state=loading / loaded。
   - 新组件 Video（video）：src 或 sources、poster、tracks、ratio（默认 16 / 9）、controls（默认 true）、autoplay（默认静音 + playsinline；系统要求减少动态效果时不自动播放并显示控制条）；
     preload 默认 metadata；画面 object-fit: contain、黑底。
   - 新组件 SearchField（input type=search）：TextField 的预设——搜索图标、clearable 默认开、名称与占位文字默认 selectSearch、enterkeyhint=search；
     onSearch（停手 delay 毫秒、回车、清空时调用，同一个词不重复，回车与清空立即）、delay（默认 300，0 是每次输入都触发）；父级从外面改值不触发 onSearch。
   - 新组件 PageHeader（patterns）：header 里依次是面包屑、标题行（返回按钮、hgroup［标题 + 标记一行、说明］、结束边的操作）、children。
     属性 title、description、headingLevel（1 默认 / 2 / 3，标题字号固定 xxl）、breadcrumbs、meta、actions、onBack、backHref（给了是链接）、各部件的 propsXxx。
   - Collapse（连带 Collapsible、Accordion、FormSection 的折叠、CodePreview 的源码区）：网格列是 minmax(0, 1fr)、内容层 min-width: 0，内容区不再被里面最宽的内容撑开
     （展开后长代码行把整块撑宽，手机上看不全也滚不了）——Rune 文档站的源码区要核对有没有同样的问题。
   - Select、Cascader、TreeSelect、Transfer、PeoplePicker、JsonViewer、表头筛选里的搜索框都换成 SearchField（有了清空按钮）；propsSearch 的类型跟着变。
     表头关键字筛选的“停手 300ms 生效”（第 6 条里的 0.23.0）就是 SearchField 的 onSearch。
   - 新属性：Blockquote 的 source（引用下面一行出处：footer 里“— ”加 cite，小一号，cite 不用斜体）、propsSource；Image 的 sources（渲染成 picture；每项 srcset、type、media、sizes）；
     Progress 的 bands（[{ to, tone }]，值落在哪一段用哪一段的色调、盖过 tone，超过最后一段算最后一段；有 bands 时 role="meter"）。
   - 文案：页面组合的文案新增 pageHeaderBack（“返回” / “Back”）；语言包主体没有新键，主题 token 没有变。
   - 文档站：新增页面 Figure 插图、视频与嵌入（display）、PageHeader 页头（patterns）；Text 页补行内组件，TextField 页加搜索框，页面模板里的页头换成 PageHeader。
