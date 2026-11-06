# 跟进 vue-tsx-pro 的样式体系改造（2026-11-06 起）

vue-tsx-pro 在做一轮样式体系的联合改造（架构、整理、API、美学），计划已批准、按阶段执行：
`vue-tsx-pro/.claude/reports/style-overhaul-plan.md`（随执行更新）。那边每完成一个阶段会通知一次，这里按 Blazor 的技术栈跟进。
本文是对照评估：Rune 现在怎么做、哪些照搬、哪些换做法、先做什么。每个阶段跟进完在对应小节记下改了什么。

总原则（两边一致）：维护好基础设施与基础组件，上层组件一律依赖它们构建；上层需要的能力先在基础层补齐再用，不在上层写平行实现。

## 1. 现状对照

| vue 计划 | Rune 现在 | 跟进方式 |
| -------- | --------- | -------- |
| 阶段 0 护栏：性能原型、截图基线、计算样式快照 | 有 `tools/parity`（按部件配对、比两边的计算样式）、BenchmarkDotNet 基准（`tests/Rune.Benchmarks`，2026-11-06 新建）；没有自身的前后快照 | parity 加一个只截 Rune 的快照模式：记下全部示例每个元素的计算样式（含 `:hover`、`:focus-visible`），阶段 1–3 前后逐项比对 |
| 阶段 1 引擎：按合并来源分段输出、`@layer vtp.s{k}` | **已跟进**（见第 3 节）。之前 `Attrs.Merge` 把默认层、配置定制、使用方、受管几层合成一个类 | **照搬规则，换实现位置**：`Attrs.Merge` 按层分段编译，段 k 的规则进 `@layer rune.s{k}`，后段带上它改到的属性族在前面合并后的完整结果（语义与现在逐字一致）；`RuneStyles` 在第一块前写层顺序声明，SSR 的 head 同。属性族（简写与长属性）表由 `tools/css-gen` 从同一份数据生成。Rune 没有绑定、热更新改写、小程序这些模块，改动比 vue 小 |
| 阶段 2：配置合一 `components: { Button: { defaults, styles, variants } }` | **已跟进**（见第 3 节）。之前两处：`UIConfig.Defaults`（每个组件一个 `XxxDefaults` record，类型安全）与 `UIConfig.Styles`（`ComponentStyles`，按部件枚举追加样式）；没有按档位的 variants | 合成 `Components` 一个入口、补上按档位取值的定制；旧的 `Defaults` / `Styles` 直接换掉 |
| 阶段 2：组件侧一个钩子 `useComponent(recipe, props)` | 各组件自己 `XxxStyles.Of(appearance, Config.Styles)` + `Config.Defaults.Get<XxxDefaults>()` | 在 `UIComponent` 上加一个取配方的方法（默认值、档位、部件层一次算好，按实例记忆），组件改用它 |
| 阶段 2：size、radius、tone 进配方档位 | 多数配方的键已含这些档位（`IconStyles.Of(size, tone, …)`），定制排在组件样式之后；个别组件在配方之外再叠 `ScaleStyles.Radius` | 逐个核对，叠在配方外的移进配方 |
| 阶段 2：每个内部节点都是部件、`data-scope` 用实际组件名 | 约 90 个组件走 `Recipe.Of`（有 `data-scope` / `data-part`、可定制）；阶段 2 后半新做的组件（Image、Lightbox、Sortable、Stat、Watermark、Splitter……）用的是内部静态 `Attrs`，没有部件标记、定制不到 | 这些组件补成配方（vue 那边同样在补）；`data-scope` 跟着 vue 的改名走（parity 按它配对） |
| 阶段 2：外观词表 | `Tone`（6 个状态色）与 `TextTone`（含 muted、inherit）已经分开，正是 vue 的最终取舍 | **已跟进**：Skeleton `Variant` → `Shape`、Sparkline `Variant` → `Type`；Dialog / Drawer 的 `Size`（宽度）照 vue 的决定不改 |
| 阶段 2：入口不再导出内部样式表 | 组件的 `XxxStyles` 多是 public（配方、部件枚举要给定制用），里面的单个样式多是 private / internal | **已做**（2026-11-06）：配方类只留 `Of`（部件层）与 CSS 变量钩子 public，没有配方的样式表整类 internal（25 个）；共用件（`Utils/Style`、`ScaleStyles`、`FloatingStyles`、`OverlayTransitions`）照常公开；测试与基准经 `InternalsVisibleTo` |
| 阶段 3 整理：共用片段、条件别名、裸值换 token | `SharedStyles`（ButtonReset、FocusRing、InteractionColors……）、`ItemStyles`、`ShimmerStyles` 已有；`When.*` 条件别名已统一；分析器 RUNE006 / RUNE007 管着类型化取值与 px | 按 vue 的片段清单补齐（pressable、disabled、disclosure 箭头、scrim / onScrim token……），裸值（Lightbox、Upload 的遮罩色）换 token |
| 阶段 4 美学：token 与主题 | 主题由 `tools/theme-gen` 从 vue 的主题生成（`Themes.g.cs`） | **跟进了已完成的部分**（见第 3 节）：重新生成主题、跟随系统的增强对比度、elevated 卡片的边框、控件高度（xs 24 … xl 40）、中文字重、着色页面、表单标签与字段行距、小浮层的进出场；标题阶梯等 vue 做完再跟。波纹 vue 最后保持默认开启，Rune 本来就是 |
| 阶段 5 文档 | `docs/DESIGN.md`、文档站的样式与主题页 | 同步规范与迁移说明 |

## 2. 改造前先做的性能准备（2026-11-06）

分段输出会让每个元素从一个类变成几个类、合并多一层工作，所以先把现在的合并路径压下来、把基准建起来，改完能对比：
合并路径（`Attrs.Merge` / `MergeProps` / `Layer`）同样的几层直接复用结果，部件层的合并从约 300 ns 降到 7 ns、不分配；
分段之后缓存的键从“几层”变成“几层 + 段”，命中的思路不变。数字、改法与待议项见 [PERFORMANCE.md](PERFORMANCE.md)，
基准在 `tests/Rune.Benchmarks`（阶段 1 做完用它与 PERFORMANCE.md 的数字对比）。

## 3. 各阶段的跟进记录

### 阶段 2：架构与 API（2026-11-06 跟进了一部分）

已跟进（外观不变，计算样式快照比对过）：

- 词表：`SkeletonVariant` / `Skeleton.Variant` → `SkeletonShape` / `Shape`，`SparklineVariant` / `Sparkline.Variant` → `SparklineType` / `Type`。
  Rune 还没发布过，没有外部使用方，**直接改名、不留旧名**（vue 留一个版本，因为它已经发布）。
- 共用档位进配方：Rune 的配方本来就把字号、圆角、色调放在配方里（按部件的定制排在后面，盖得住）；
  例外两处改了：Accordion 的字号（原来在受管层，会盖过定制）、Progress 轨道的圆角（同 vue，字号仍按布局放在外层或轨道上）。
- 按钮家族的部件：Rune 的 `ButtonPart` 本来就有 Content、Icon、IconEnd、Spinner，都带 `data-part`、可定制，不用改。
- `Tone` 不改名：Rune 的 `Tone` 就是 vue 的 `StatusTone`（6 个状态色），文字色调早就叫 `TextTone`；改成 `StatusTone` 只换说法、全库都要改。

组件定制合成一个入口（改公共 API 的形状，用户拍板：合成新入口）。vue 的动机是两处配置、名字对不上静默失效（Tab / Tabs）、没有按档位的定制；
C# 里名字对不上编译不过，所以真正新增的是“按档位取值的定制”：

```csharp
builder.Services.AddRuneUI(o => o.Components = new()
{
    new ComponentConfig<ButtonDefaults, ButtonPart>
    {
        Defaults = new() { Variant = ButtonVariant.Outline, Radius = Radius.Full },
        Styles = { [ButtonPart.Root] = Wide },
        Variants = { [ButtonVariant.Outline] = { [ButtonPart.Root] = Dashed } },
    },
});
```

- 入口在添加时拆回现有的两层机制（同 vue 的 `splitComponents`）：`Defaults` 进 `ComponentDefaults`，`Styles` 进 `ComponentStyles`，
  `Variants` 进 `ComponentStyles` 里新的“档位取值 → 部件 → 样式”表；合并、分段（定制在自己的一段）都不变。`AddRuneUI` 与 `RuneProvider` 都是 `Components`。
- 基础层先补：`Recipe.Of` 读出组件当时的档位取值。档位记录（`ButtonAppearance` 这类，20 个）实现 `IAppearance` 交出枚举取值，元组、单个枚举的键直接读；
  只在配置里有按档位的定制时才读，`Part.Of` 把取到的定制排在按部件的定制之后。
- 与 vue 不同：旧的 `Defaults` / `Styles` 直接换掉、不留旧名（没发布过）；`UIConfig.Defaults` / `Styles` 留作只读的合成结果，给组件与配方取值。
- 验证：bUnit 测试（按档位的定制只作用于取这个值的按钮，排在按部件的定制之后、同在 `rune.s1`）；Provider 页加“按档位取值定制样式”示例；
  计算样式快照与改动前比对。

还没跟进：

- 部件补齐：阶段 2 后半新做的组件（Image、Lightbox、Sortable、Stat、Timeline、Watermark、Splitter、Checklist、Gauge、Sparkline、ProportionBar、Marquee、
  Ellipsis、Upload 的图片墙、Affix / BackTop、AsideLayout）用的是内部的静态 `Attrs`，没有配方与部件标记、定制不到，要逐个改成配方（vue 也在逐批补）。

### 阶段 3：整理（2026-11-06 跟进了一部分）

- 展开箭头收进共用件 `DisclosureStyles`（同 vue 的 disclosureStyles）：`Icon`（旋转的过渡，Sm）、`Flip`（向下的箭头展开朝上：Accordion、CodeBlock）、
  `Turn`（朝行尾的箭头展开朝下，从右往左时反着转：NavList）。去掉 `CodeBlockStyles.ExpandIconOpen`、`NavListStyles.ChevronOpen`。
  有意的变化：FormSection 收起时从右往左排版下箭头朝左（原来朝右）；NavList、FormSection 的箭头过渡从 Md 改成 Sm（同 vue）。
- 条件别名：Rune 的样式本来就统一写 `When.*`，没有要改的。
- 状态色调的共用色表：Badge、Alert、Avatar、Banner、Indicator 本来就共用 `SharedStyles.StatusColors`。
- 弹出面板：Popover、HoverCard、Menu 本来就共用 `FloatingStyles.Anchored` + `Surface`。
- 焦点环的 `virtual`（当前项常亮）：Rune 的 Cascader 用 CSS 条件（`[data-highlighted]:where(:focus *)`）画，不经状态往返；只有它一处用，
  按“第三次才抽象”的约定先不抽共用件。`inset` 早就有（`SharedStyles.FocusRingInset`）。
- 还没做：OtpField 的格子改用输入框的外观（会改外观、要加 Variant / Radius 参数），放到下一批。
- 遮罩的颜色 token（vue 9d5af3f3）：`Colors.Scrim`（图片上的深色遮罩）、`ScrimStrong`（看图背景）、`OnScrim`（遮罩上的文字）；
  Upload 的进度遮罩、Lightbox 的背景与按钮改用它们（取值不变）。不同：Upload 图片墙右上角的小按钮仍用半透明的表面色底（vue 改成 `OnScrim` 的白底，
  但按钮的文字是主题的文字色，深色主题里白底浅字看不清；Rune 原来就用表面色，深浅主题都看得清）。
- Table 与 DataTable 共用部件样式（vue d450fe3a）：Rune 还没有 DataTable，只有一个表格配方，没有可合并的。

### 阶段 4：美学（2026-11-06 跟进 vue 已完成的部分，vue feat/style-overhaul 到 8f699409，PR #4 未合并）

vue 那边合并用快进、不 squash，提交号不变；CI 的 `VUE_TSX_PRO_REF` 指到当前跟进到的提交（样式属性表、主题、语言包同一版本）。

- 主题重新生成（先到 9de32da3）：去掉海洋、加高对比暗色 `Themes.HighContrastDark`（仍是 16 套，ThemeSwitcher 的图标用 eclipse）；
  两套高对比文字到 7:1；占位文字改用次要文字色（4.5:1）；亮色的成功、警告、信息文字改中间调。
- theme-gen 修了一处：vue 的遮罩 token 只写在 light.ts，其他主题取 defineVars 的默认值；生成器原来只把“主题之外的组”放进 `Themes.Base`，
  主题没写的颜色在 Rune 里就成了未定义的变量（`ThemeTests` 查出来）。现在各组的默认值都进 Base，主题只覆盖写了的，同 vue。
- 跟随系统时尊重增强对比度（同 vue 的 `SystemThemes.contrast` / builtinSystemThemes）：`ThemeChoices.SystemContrast`（亮、暗两套），
  `ThemeChoices.Builtin` 配好高对比的两套；开机脚本与 `js/theme.js` 先按 `prefers-contrast: more` 选一对再按明暗选，并监听对比度的变化；
  主题类（`ThemeStyles.ClassFor` 的 `contrast`）同样写进媒体查询，脚本没运行时也对。
- elevated 卡片加 `BorderMuted` 的边框（深色主题里阴影看不见）。
- ButtonBase 继承字重：Rune 的 `SharedStyles.ButtonReset` 本来就是 `font: inherit`（含字重），不用改。
- 删 Figma 设计资源：Rune 没有这块。
- 控件高度取整到 4px 网格（vue 4cfe5a3e）：`Sizes.Control` 从 `2.25em` 改成 `round(nearest, 2.25em, 4px)`（重新生成）；字号档位
  `ScaleStyles.FontSize`（同 vue 的 fontSizeStyles）连同这一档的控件高度一起给，Xs 用 2 倍：五档依次 24 / 28 / 32 / 36 / 40px。
- 界面文字的字重 600 → 500（vue 3df7f9ee，微软雅黑没有 600，落成粗体显得笨重）：按钮、Accordion 的触发、Descriptions / Timeline / Alert / Banner /
  Empty / Popconfirm / Transfer 的标题、FormSection 的标题、Listbox / Menu / NavList 的分组标签、表头。仍是 600 的同 vue：标题 H1–H6、数字
  （Gauge、ProportionBar、Progress、OtpField、Indicator、表尾合计）、Result 的页面级标题。vue 改了而 Rune 还没有的组件（日历、看板、评论、
  通知中心、泳道、图表、列设置、表单错误汇总）以后做时按 500。
- H6 默认字号 Sm（vue 3df7f9ee），与 H5 分开。
- Upload 图片墙小按钮的对比度（vue 4f7770f9）：vue 改成 Rune 原来的做法（半透明表面色），两边一致了。
- 亮色主题着色页面 + 白卡片（vue c340d084，重新生成）：亮色、森林、柠檬、樱花、喜庆、薰衣草、墨白的页面取原来的 surfaceMuted，
  surface / surfaceRaised 改白色，surfaceMuted 取页面与 fill 的中间，surfaceTranslucent 跟着页面；几套主题的图表系列色调深到 3:1。
  纸张、高对比、深色不动。开机脚本的底色取主题的 Background，自动跟着变。端到端测试里色块的期望值改成 `#ffffff`。
- 组件库只用系统字体栈、不带 web 字体（vue 的决定）：Rune 本来就是。
- 文档站的字体切换（vue 758c20b9）：顶栏主题按钮旁的字体按钮，系统字体（默认）、更纱黑体等宽、霞鹜文楷等宽（npm 包 rune-ui-font），
  选中才加载。Rune 没有前端打包，字体 CSS 从 jsDelivr 按版本取（`apps/Rune.Docs/wwwroot/fonts.js`）；vue 换主题的 fonts，
  Rune 在 app.css 里按 `data-docs-font` 覆盖最外层 Provider 的字体变量（不分层，盖过主题），示例里自带主题的嵌套 Provider 不动。
- 示例画布改用页面底色（vue d82f6eff）：卡片、浮层的表面色在它上面才分得出层级。
- Tabs 改用逻辑属性（vue 9976b696）：分隔线、下划线用 block 方向；指示条横排、竖排各给一份位置，竖排不再叠着横排的 `left`
  （从右到左时结束边就是 left，物理属性会盖过 `inset-inline-end`）。Rune 没有 Steps。
- 表单标签升到 14px（vue 5b471484）：Field 的标签 `FontSizes.Md`、中等字重，说明文字仍小一档、弱化色；FormSection 的字段行距 Md → Lg。
- 小浮层的进出场更快（vue 5b471484）：新增 `OverlayTransitions.Pop`（进场 Sm + enter、离场 Xs），Popover、HoverCard、Menu、选项列表用它
  （vue 的 anchoredPanelSx；Rune 还没有日期面板、Cascader、TreeSelect），对话框仍用 `Scale`。`Durations` 各档的注释按实际用处改写。

### 阶段 1：分段输出与级联层（2026-11-06 跟进）

vue 的规则（STYLE-ENGINE.md §11）照搬，按 Rune 的结构落在这几处：

- 分段：`Sx.Layers(组…)`（同 `css.layers`，公开）记下各段的边界；`Part.Of` 把配置的按部件定制分成单独一段；
  `Attrs.Merge` / `MergeProps` / `Layer` 的默认层、使用方、受管层各一段，往下交的 `Sx` 保留分段。只有一段时一个类、类名与以前相同。
- 层：`StyleCompiler` 第 k 段的规则包在 `@layer rune.s{k}`，主题（`Theme.ClassFor`）在 `rune.vars`；`RuneStyles` 每块开头声明
  `@layer rune.vars, rune.s0 … rune.s7`；超过 8 段并进最后一段。
- 语义：第 k 段带上它改到的属性族在前 k + 1 段合并后的完整取值；属性族表（`FamilyOf`）照搬 vue 的 `familyOf`。
- 验证：`SegmentTests`（随机 500 轮，按属性族、按状态比对；对照的属性族表独立于实现，把实现改成一个属性一族时第 0 轮就失败）；
  `tools/style-snapshot` 全部演示页的计算样式前后比对。

与 vue 不同的取舍：

| 项 | vue | Rune | 理由 |
| -- | --- | ---- | ---- |
| `revert` | 后段里只剩条件取值、或被去掉的属性，补一条无条件的 `revert` | 不补 | Rune 的合并只按“条件 + 属性”覆盖，普通值不会整体替换条件值，也没有去掉属性的写法，低层的条件取值不会漏上来 |
| 定制的段数 | 每层扩展（`createUi`、每层 Provider）各一段 | 合成一段 | `ComponentStyles.Then` 已经按嵌套顺序合好；语义相同，只是嵌套定制多时类不再细分 |
| 受管层 | 使用方写了 sx 时才分出去 | 一律按层分段 | `Attrs.Layer` / `MergeProps` 也用于组件内部的组合（第二个参数不一定是使用方）；语义相同，类多几个 |
| 宿主 CSS | 不分层的盖过整个组件库 | 同 | 演示项目删掉了 Blazor 模板留下的表单校验规则（`.invalid { outline }` 这类，分层后会稳定地盖过组件），文档站的标题焦点规则收窄到 `h1` |
