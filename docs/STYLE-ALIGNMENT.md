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
| 阶段 2：配置合一 `components: { Button: { defaults, styles, variants } }` | 两处：`UIConfig.Defaults`（每个组件一个 `XxxDefaults` record，类型安全）与 `UIConfig.Styles`（`ComponentStyles`，按部件枚举追加样式）；没有按档位的 variants | 合成 `UIConfig.Components`：`new ButtonConfig { Defaults = …, Styles = { [ButtonPart.Root] = … }, Variants = … }`。C# 的类型由部件枚举、档位枚举约束，不需要 vue 那样生成登记表；旧的 `Defaults` / `Styles` 标 `[Obsolete]` 留一个版本 |
| 阶段 2：组件侧一个钩子 `useComponent(recipe, props)` | 各组件自己 `XxxStyles.Of(appearance, Config.Styles)` + `Config.Defaults.Get<XxxDefaults>()` | 在 `UIComponent` 上加一个取配方的方法（默认值、档位、部件层一次算好，按实例记忆），组件改用它 |
| 阶段 2：size、radius、tone 进配方档位 | 多数配方的键已含这些档位（`IconStyles.Of(size, tone, …)`），定制排在组件样式之后；个别组件在配方之外再叠 `ScaleStyles.Radius` | 逐个核对，叠在配方外的移进配方 |
| 阶段 2：每个内部节点都是部件、`data-scope` 用实际组件名 | 约 90 个组件走 `Recipe.Of`（有 `data-scope` / `data-part`、可定制）；阶段 2 后半新做的组件（Image、Lightbox、Sortable、Stat、Watermark、Splitter……）用的是内部静态 `Attrs`，没有部件标记、定制不到 | 这些组件补成配方（vue 那边同样在补）；`data-scope` 跟着 vue 的改名走（parity 按它配对） |
| 阶段 2：外观词表 | `Tone`（6 个状态色）与 `TextTone`（含 muted、inherit）已经分开，正是 vue 的最终取舍；`SparklineVariant`（图类型）、Dialog / Drawer 的 `DialogSize`（宽度）、各组件自己的方向枚举属于要改的 | 按 vue §4.4 的表改名，旧名 `[Obsolete]` 留一个版本 |
| 阶段 2：入口不再导出内部样式表 | 组件的 `XxxStyles` 多是 public（配方、部件枚举要给定制用），里面的单个样式多是 private / internal | 只留部件枚举与 `Of`（配方）public，其余 internal；有了 `Components` 入口后再收 |
| 阶段 3 整理：共用片段、条件别名、裸值换 token | `SharedStyles`（ButtonReset、FocusRing、InteractionColors……）、`ItemStyles`、`ShimmerStyles` 已有；`When.*` 条件别名已统一；分析器 RUNE006 / RUNE007 管着类型化取值与 px | 按 vue 的片段清单补齐（pressable、disabled、disclosure 箭头、scrim / onScrim token……），裸值（Lightbox、Upload 的遮罩色）换 token |
| 阶段 4 美学：token 与主题 | 主题由 `tools/theme-gen` 从 vue 的主题生成（`Themes.g.cs`） | token 取值、去掉海洋、加高对比暗色：vue 改完后重新生成即可；控件高度（xs 24 … xl 40）、中文字重、波纹默认关（`UIConfig.Ripple`）跟着改 |
| 阶段 5 文档 | `docs/DESIGN.md`、文档站的样式与主题页 | 同步规范与迁移说明 |

## 2. 改造前先做的性能准备（2026-11-06）

分段输出会让每个元素从一个类变成几个类、合并多一层工作，所以先把现在的合并路径压下来、把基准建起来，改完能对比：
合并路径（`Attrs.Merge` / `MergeProps` / `Layer`）同样的几层直接复用结果，部件层的合并从约 300 ns 降到 7 ns、不分配；
分段之后缓存的键从“几层”变成“几层 + 段”，命中的思路不变。数字、改法与待议项见 [PERFORMANCE.md](PERFORMANCE.md)，
基准在 `tests/Rune.Benchmarks`（阶段 1 做完用它与 PERFORMANCE.md 的数字对比）。

## 3. 各阶段的跟进记录

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
