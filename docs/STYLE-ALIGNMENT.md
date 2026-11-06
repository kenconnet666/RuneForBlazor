# 跟进 vue-tsx-pro 的样式体系改造（2026-11-06 起）

vue-tsx-pro 在做一轮样式体系的联合改造（架构、整理、API、美学），计划已批准、按阶段执行：
`vue-tsx-pro/.claude/reports/style-overhaul-plan.md`（随执行更新）。那边每完成一个阶段会通知一次，这里按 Blazor 的技术栈跟进。
本文是对照评估：Rune 现在怎么做、哪些照搬、哪些换做法、先做什么。每个阶段跟进完在对应小节记下改了什么。

总原则（两边一致）：维护好基础设施与基础组件，上层组件一律依赖它们构建；上层需要的能力先在基础层补齐再用，不在上层写平行实现。

## 1. 现状对照

| vue 计划 | Rune 现在 | 跟进方式 |
| -------- | --------- | -------- |
| 阶段 0 护栏：性能原型、截图基线、计算样式快照 | 有 `tools/parity`（按部件配对、比两边的计算样式）、BenchmarkDotNet 基准（`tests/Rune.Benchmarks`，2026-11-06 新建）；没有自身的前后快照 | parity 加一个只截 Rune 的快照模式：记下全部示例每个元素的计算样式（含 `:hover`、`:focus-visible`），阶段 1–3 前后逐项比对 |
| 阶段 1 引擎：按合并来源分段输出、`@layer vtp.s{k}` | `Attrs.Merge` 把默认层、配置定制、使用方、受管几层合成**一个类**（`StyleCompiler`，内容哈希命名，按作用域登记，`RuneStyles` 只追加输出） | **照搬规则，换实现位置**：`Attrs.Merge` 按层分段编译，段 k 的规则进 `@layer rune.s{k}`，后段带上它改到的属性族在前面合并后的完整结果（语义与现在逐字一致）；`RuneStyles` 在第一块前写层顺序声明，SSR 的 head 同。属性族（简写与长属性）表由 `tools/css-gen` 从同一份数据生成。Rune 没有绑定、热更新改写、小程序这些模块，改动比 vue 小 |
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

（vue 那边每完成一个阶段，在这里记下 Rune 跟进了什么。）
