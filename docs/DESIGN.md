# 设计与取舍

记录 Rune 的核心设计、为什么这样做，以及探测（2026-11-04，一次性的 Server / WebAssembly 原型与压力测试）得到的结论。
改动这些部分前先读对应一节。

## 1. 总原则

- **贴着 Blazor 的心智走**：组件整体重新渲染、`EventCallback`、`@bind-X` 协议、`RenderFragment` 都照原样用；只补 Blazor 缺的三样——跨组件状态的自动通知、受控/非受控的双向绑定、减少无谓的子组件渲染。
- **不改写源码**：C# 只有源代码生成器（只能新增代码）。语法糖只在生成器能做到、且能讲清楚的地方加（`[State]`）。
- **样式与属性一起合并**：样式只是元素属性的一部分，一次 `Attrs.Merge` 合出 class、aria、事件。

## 2. 响应式（`src/Rune/Reactivity`）

| 类型 | 作用 |
| --- | --- |
| `State<T>` | 可读写的值；渲染时读取会登记依赖，写入不同的值通知订阅者 |
| `Derived<T>` | 惰性、缓存的派生值；只在计算代价高时用，便宜的派生写普通属性 |
| `[State]` | 生成器把 `partial` 属性实现成懒创建的 `State<T>`，另给一个 `XxxState` 交出信号本身 |

- **不做深层代理**：对象、集合整体替换（record 的 `with`、不可变集合）。
- **线程安全**：Server 下后台任务会在别的线程写共享状态。早期实现“先读值后订阅”，在压力测试里几乎每次都丢通知、约 10% 的派生值永久过期。
  现在每个信号带版本号：读取时记版本，订阅新依赖之后再退订旧依赖（没有空窗），最后核对版本，变过就补一次（`Subscriptions`）。`Derived` 用两把锁（重算锁、状态锁）加 epoch 防止重算期间的通知被吞。
- **依赖收集不能跨 await**：收集帧放在线程上；渲染与派生计算都是同步的，跨线程结束帧直接报错。
- **只收集自己渲染时读到的**：交给别的组件的子内容（`<CascadingValue>` 里的内容）是那个组件渲染时执行的；框架组件不收集，读到的信号谁也不订阅。
  要在本组件的渲染里读（放在最外层、或先读到局部变量）。CheckboxGroup 把要级联的状态嵌在挡住 Field 的 CascadingValue 里，非受控时勾选了组不重新渲染（bUnit 发现）。
- **共享状态放 scoped 服务**：静态的 `[State]` 报 RUNE001（Server 下会被所有用户共享）。

## 3. 组件基类（`src/Rune/Components`）

- `RuneComponent` 自己实现 `IComponent`，在 `BuildRenderTree` 前后开关依赖收集与样式登记；生命周期照 .NET 10 的 `ComponentBase` 复刻（Razor 只要求基类有 `protected virtual void BuildRenderTree(RenderTreeBuilder)`）。
- 内容里（`ChildContent`）读到的状态归执行它的子组件：状态变了只重新渲染子组件。
- **参数没变就跳过渲染**（`ParameterChangeDetector`）：Blazor 自己只认基本类型。这里另外认回调（`Action` 等委托，目标与方法相同就相等）、`EventCallback`、结构体、record、信号、`Style` / `Sx`。
  内容片段（`RenderFragment`）不认：它的输出取决于父级渲染时的状态，普通字段变了不会通知，按委托相等跳过会显示旧内容（演示项目里计数按钮的文字不更新，实测发现后改回）。
  其他引用类型（`List`、普通类）可能被原地修改，一律当作变了——即使引用相同（测试覆盖了这个坑）。
- **热重载时不能跳过**：热重载会把同样的参数再传一遍，必须检查 `IsRenderingOnMetadataUpdate`，否则新代码不生效。
- `Bindable<T>`：`Xxx` + `XxxChanged` 协议。给了 `XxxChanged` 是受控（父级决定是否采纳）；只给值是初始值、父级传来新值时跟着变；都不给是组件自己的状态。
  受控时改值请求发出后、参数变化之前，`Value` 先用请求的值：组件自己处理完事件要渲染，外层的 CascadingValue 被父级重新渲染时
  又会带着它**上一次的直接参数**通知它，都会用旧值渲染、把旧值写回输入框（Server 模式下快速输入吞字，验证码的 E2E 加日志查到）。
  这一批渲染结束时参数还没变（父级没采纳，或回调的接收者不是组件），回到父级的值并再渲染一次。

## 4. 属性合并（`src/Rune/Attrs`）

`Attrs.Merge(默认层, 使用方, 受管层)`：

- 样式（`Sx`）按层叠加：只有一层带样式时（最常见）合成一个类；几层各有样式时按层分段，每段一个类、各进一层级联层（见第 5 节的“分段与级联层”）。
  class、style 依次拼接；其他属性后面的层覆盖前面的。`MergeProps`、`Layer` 交下去的 `Sx` 保留着分段。
- 三种合并的去处：`Merge` 合并到**元素**上（合成类）；`MergeProps` 展开到**组件**上（样式成了 Sx 参数）；`Layer` 交给子组件的
  **Props 参数**、由子组件再合并到它的元素上（样式留在 Attrs.Sx）。把 MergeProps 的结果交给 Props 参数时样式会变成一个叫 Sx 的属性、整个丢掉
  （列表框改成 ScrollArea 的视口时 E2E 发现虚拟滚动失效）。
- **事件**：两层都执行，按层的顺序。不能简单包一层 lambda——Blazor 事件后重新渲染的是处理器所属的组件（委托的 Target），包出来的 lambda 不属于任何组件。合并时让每个处理器经它自己的接收者执行（`EventHandlerMerger`），参数类型取自原处理器（Blazor 按它解析事件数据）。
- vue-tsx-pro 的“使用方调用 preventDefault 就跳过组件自己的处理”搬不过来：Server 模式下处理器在服务端执行，浏览器的默认行为早已发生。需要接管行为时用受控参数（给 `XxxChanged`）。
- 性能：探测时每次合并都展开声明、拼规范文本，1000 个元素约 3.5 ms、5 MB 一次渲染；按样式实例缓存后约 0.24 ms、226 KB。只有默认层样式时返回缓存的同一个对象，不分配。

## 5. 样式（`src/Rune/Styles`）

目录按职责分：`Values/`（CssValues、CssVar 这类值类型）、`Definition/`（Style、Sx、条件、关键帧、Css.Define、Theme）、`Engine/`（编译、优先级、注册表、`RuneStyles` 输出组件）、`Generated/`（css-gen 生成的属性表与关键字），命名空间都是 `Rune.Styles`。

- **写法**：`new Style { Display = Display.Flex, Padding = Space.Md, [When.Hover] = new() { … }, [Media.Up(768)] = new() { … } }`。515 个 CSS 属性与 318 个属性的关键字常量由 `tools/css-gen` 从 vue-tsx-pro 的数据生成，带中文说明；颜色属性是 `CssColor`、长度属性是 `CssLength`，token 写错类型编译不过。数字按属性补单位（px / 不补 / ms，同 StyleX）。
  值里没有数字的属性（display、cursor、font……，按 StyleX 的类型表判断）是 `CssKeyword`：只收字符串与关键字，`Display = 5` 编译不过（否则输出 `5px`）。
  单位有类型化的写法：`CssLength.Em(0.875)`、`Rem`、`Percent`、`Vw`、`Dvh`、`Ch`、`Auto`、`None`，角度 `CssValue.Deg(180)`；颜色关键字是 `CssColors`
  （transparent、currentcolor、强制颜色模式的系统色），全局关键字是 `CssWide`。尺寸约定同 vue-tsx-pro：组件内部用 em（随字号整体缩放），
  边框、焦点环、分隔线用 px 但必须经 `BorderWidths` token（主题统一改粗），最小点击区域 `Sizes.MinTarget` 24px。
  关键字类与属性同名（`Display`、`Width`）：组件里如果有同名的参数，`Width.Auto` 会解析成参数，样式类（静态类）里不受影响。
- **一直在变的值**（进度、宽度）用 `CssVar<T>`：样式引用变量、类不变，值经 `Attrs.Vars` 写在元素的 style 上；写进样式会让每个值多一条规则。
- **条件**：`When.*` 的状态别名同时认原生、ARIA、data 属性（同 vue-tsx-pro 的 CONDITIONS），状态变化只改属性、类名不变。
  外层状态决定里面部件的样式用 `When.Inside(状态, 标记)`：标记（`Css.Marker`，同 vue-tsx-pro 的 defineMarker）是挂在被观察元素上的固定类，
  只认带标记的祖先；不带范围的写法会被外面别的组件的同名状态触发（选中的表格行里的下拉对勾全亮），所以没有提供。只看直接父元素时用 `When.Parent`。
  另有容器查询 `Containers.MinWidth / MaxWidth / Query`（与 Rune.UI 的 Container 组件错开命名）、`Media.Supports`；回退值 `Css.Fallback("100vh", "100dvh")` 编译成同一属性的几条声明。
- **合成**：一段样式合成一个类，类名按内容哈希（服务端、浏览器、所有请求同名）。块的顺序与块内声明顺序沿用 StyleX 的优先级表：无条件块先写，简写先于长属性（探测时没排序，基础块写在 @media 后面把它盖掉了）。
- **分段与级联层**（2026-11-06，同 vue-tsx-pro 样式改造的阶段 1）：一个元素的样式按合并来源分段——部件的默认样式、配置的按部件定制
  （`Part.Of` 里单独一段）、使用方的 `Sx`、受管层各一段（`Sx.Layers`），每段一个类。规则都在级联层里：每块样式表开头声明
  `@layer rune.vars, rune.s0 … rune.s7`，主题变量在 `rune.vars`，第 k 段在 `rune.s{k}`（超过 8 段的并进最后一段），后面的段不论特异性都盖过前面的段；
  宿主不分层的 CSS 盖过整个组件库（重置样式要放进更低的层，演示与文档站的全局 CSS 只写自己的外壳）。
  语义与合成一个类逐字相同：第 k 段输出它改到的属性所在的属性族（`StyleCompiler.FamilyOf`，按名字第一段分，inset / top、gap / row-gap、place / align / justify、
  line-height 归 font、white-space 归 text 是例外）在前 k + 1 段合并后的完整取值，含它没改的条件值，所以每个属性族最高的那一层里总有这一族完整的合并结果；
  简写与长属性分在两段时靠属性族一起带。Rune 的合并只按“条件 + 属性”覆盖、没有去掉属性的写法，低层的条件取值不会漏上来，不需要 vue-tsx-pro 那样补 `revert`。
  好处：组合数随段相加而不是相乘（组件的档位 × 定制 × 使用方的样式），组件自己的那段在实例之间共用。
  验证：`SegmentTests` 随机生成样式与分段，对每种状态、每个属性族比对“按层选值”与“合成一个类”（500 轮；把属性族改成一个属性一族时第 0 轮就失败）；
  `tools/style-snapshot` 前后各拍一份全部演示页的计算样式（含强制的 :hover、:focus-visible、:active）比对。
  与 vue-tsx-pro 的不同：vue 每层 Provider 的定制各一段，这里定制合成一段（`ComponentStyles.Then` 已经按嵌套顺序合好，语义相同）；
  vue 只在使用方写了 sx 时才把受管层分出去，这里 `Attrs.Layer` / `MergeProps` 也用于组件内部的组合（第二个参数不一定是使用方），一律按层分段，语义相同、类多几个。
- **缓存**：结果挂在第一个样式实例上，按后面跟着的实例区分；实例被回收时缓存跟着回收。所以样式要用 `Css.Define(static () => …)` 定义成静态属性，渲染时用同一批实例。
- **为什么不是 `static readonly`**：静态字段只初始化一次，热重载改了样式值不生效。写在 `Define` 的 lambda 里，热重载改的是方法体；`StyleHotReload`（`MetadataUpdateHandler`）随后清掉缓存，状态不丢。实测 Server 与 WebAssembly 都在 2 秒左右生效。
- **输出**（`RuneStyles` 组件，每批新规则一个 `<style>`、开头声明层的顺序，只追加）：
  1. App.razor 的 head：服务端渲染与增强导航（换页时 head 被同步成新页面的那组）；
  2. 每个交互根里：交互模式下新用到的规则；
  3. 流式渲染的页面末尾：流式更新只发送标了 `[StreamRendering]` 的组件，head 里的那份收不到（探测时样式丢失，源码确认）。
- **找登记表**：合并样式是静态方法，要靠“当前作用域”找到登记表。最初只在 RuneComponent 渲染时设置，结果非 Rune 组件执行的内容片段
  （CascadingValue、Virtualize、EditForm 的 ChildContent）里一用样式就报错。现在按渲染器找：Server 与服务端渲染的每个电路、每次请求
  都在自己的同步上下文上渲染，登记表绑定到这个上下文（创建时、RuneComponent / RuneStyles 第一次渲染时绑定）；WebAssembly 单线程、只有一个作用域。

## 6. 组件约定（`src/Rune.UI`）

- **目录**（同 vue-tsx-pro 的 `packages/ui/src`）：组件按分类放在顶层（`Base`、`Layout`、`Display`、`Feedback`、`Overlay`、`Navigation`、`Input`、`Form`、`Table`、`Patterns`、`DocsKit`），
  一个组件和它的变体、样式文件放一个文件夹（`Base/Button/` 里有 Button、IconButton、LinkButton、ButtonStyles；一组紧密相关的组件同样一个文件夹，`Input/Choice/` 是勾选框、单选、开关），
  同一分类里几个组件共用的代码直接放在分类这一层（`Input/FormControl`、`InputStyles`，`Overlay/Floating`、`OverlayTransitions`，`Layout/LayoutStyles`）。
  公开面（同 vue-tsx-pro 0.11）：外观定制只走 `Components` 入口与部件属性；样式类公开的只有配方的 `Of`（部件层，自建组件可以借用）、CSS 变量钩子与共用件，组件内部的单个样式与没有配方的样式表是 internal。
  `Config/` 是配置层与组件基类（UIConfig、RuneProvider、UIComponent），下分 `Theme/`（token、色调、内置主题）与 `Lang/`（语言包）；
  `Utils/` 是组件共用的工具：`Style/`（配方与部件、共用样式）、`Data/`（列表状态、颜色解析、文字匹配）、`Dom/`（JS 初始化器发出的事件、UniqueId）。
  文件夹不对应命名空间，类型都在 `Rune.UI`。文档页（`apps/Rune.Docs.Content/Components/{分类}/`）、测试（`tests/Rune.Tests/{Runtime,UI,TestComponents}/`）同样按分类收纳。

- **配方**：`Recipe.Of(组件名, 档位组合, 定制, (档位, 部件) => 样式)`，每个部件一个 switch 分支；按档位组合缓存，统一加上按部件定制与部件标记，得到 `PartLayers<TPart>`。
- **色调**：每个色调一组颜色 token（`Tones.Danger.Soft`……），按色调变化的样式写成以它为参数的函数，每种外观只写一遍，样式里仍直接是主题 token（vue-tsx-pro 试过的“色调变量”多一层 CSS 变量、读样式时看不到实际 token，这里没有那一层）。
- **部件是本库组件时**（按钮里的图标、加载指示）用 `Attrs.MergeProps`：样式作为子组件的 `Sx` 参数交下去，子组件排在自己的样式之后（分段的层更高）。
  不要把两个类直接拼在一个元素上：不在层里时各自的规则谁先插入谁赢，结果不可靠。
- **组件管理的属性**按状态预先建好（按钮只有四种组合），或用 `Memo<TKey, T>` 按输入记住（字段，不是 readonly），渲染时不分配；
  创建时要用组件本身（事件处理器）时用带状态的重载，lambda 仍写成 static。
- **包一层的组件要声明带类型的参数**（CloseButton 包 IconButton、StatusBadge 包 Badge）：没声明的参数作为普通属性收进 Rest，
  `Radius="Radius.Full"` 会变成字符串 "Radius.Full"，转给里面的组件时类型转换失败（运行时才报错）。只有 Sx、data-*、事件这类本来就是属性的才走 Rest。
- **Razor 的空白**：元素与元素、表达式与元素之间的换行缩进会渲染成一个空格（`@ChildContent` 换行再写 `<Icon />`）。行内元素里多出一个空格的宽度
  （外链图标与读屏提示之间、标签文字与移除按钮之间），这些地方写在同一行；flex 容器里的纯空白文本不占位置，不用管。
- **组件命名避开 Razor 的特殊标记**：`<text>`（代码块里的标记，不区分大小写）、HTML 空元素（`link`、`input`、`img`……写不了内容）。所以是 `TextSpan`、`TextLink`。
  与 CSS 关键字类同名的组件（`Flex`、`Grid`、`Container`）：css-gen 扫描 Rune.Styles 与 Rune.UI 的类型名，重名的关键字类加 `Keywords` 后缀让路（新增组件后重新生成，CI 的 --check 会发现）。
- **文字的色调是 `TextTone`**（同 vue-tsx-pro 文字的 tone，定义在 `Theme/Tones.cs`）：TextSpan / Paragraph / 标题、TextLink、Icon 的 Tone 都是它，比组件的 `Tone` 多了 Muted（次要说明）与 Inherit；
  组件自己的 `Tone`（按钮、标记、提示条）要交给图标时用 `tone.AsText()`。
- **元素不同就分组件**（H1–H6、TextSpan / Paragraph / Strong / Em / Small）：共用基类与配方，子类只给标签与默认值，基类在 C# 里用 `RenderTreeBuilder` 输出元素。

## 7. 主题与配置（`src/Rune/Styles/Definition/Theme.cs`、`src/Rune.UI/Config`）

- **主题是 C# 对象**：token 是样式里写的 `var(--rune-…)`，主题给它们赋值（`new Theme("brand", basedOn: Themes.Light) { [Tones.Accent.Solid] = "…" }`），
  `ThemeStyles.ClassFor` 生成只设变量的类。换主题只换容器上的类，组件不用重新计算样式。
- **token 与内置主题同 vue-tsx-pro**：分组、命名、取值都对齐（颜色与阴影取自 Radix，间距与圆角用 em 随字号缩放，动效参考 Material 3）。
  16 套内置主题由 `tools/theme-gen` 从 vue-tsx-pro 的主题文件生成（`Themes.g.cs`），CI 校验两边一致；vue-tsx-pro 加了 token 而这边没有时脚本报错。
  各组 token 的默认值（vue 的 tokens.ts）进 `Themes.Base`，每套主题只覆盖写了的（同 vue 的 defineVars 默认值在 `:root`、主题类覆盖）。
  单行控件的高度是 `Sizes.Control`：字号的 2.25 倍取整到 4px（整数像素的边框在 1 倍屏上不发虚），字号档位（`ScaleStyles.FontSize`）连同这一档的高度一起给，
  Xs 用 2 倍，五档 24 / 28 / 32 / 36 / 40px；组件只要用字号档位。
  中性色调不另设 token，直接用反色表面、填充、文字、边框（同 vue-tsx-pro 的 tone-styles）。本库独有的只有层级（提示、消息提示不在顶层）。
- **主题自己说是亮是暗**（`IsDark`）：容器上的 `color-scheme` 按主题写，固定用赛博朋克这类暗色主题时原生控件、滚动条也是暗色。
- **条件取值**：`[Media.ReducedMotion] = new("…") { [Durations.Md] = "0ms" }`。用户要求减少动态效果时全部时长为 0，
  用时长 token 写的过渡自动关掉，组件不再各自加“减少动效”的样式（原来的 `SharedStyles.ReducedMotion` 已删）；循环的装饰动画（加载指示）自己处理。
- **作用范围**：`RuneProvider` 用 `display: contents` 的包裹元素挂主题类，并设文字颜色、字体、字号、行高（同 vue-tsx-pro 的 Provider），不影响布局；
  变量与这些属性沿 DOM 继承，所以静态外壳里的主题对里面的交互区域同样生效。
- **配置**：`UIConfig`（主题、亮暗、组件的定制、波纹开关）。应用级的来自 `AddRuneUI`（DI，服务端与 WebAssembly 各注册一次），
  `RuneProvider` 在一片区域里叠加修改。级联值不能跨渲染模式边界，交互区域里需要局部修改时在里面再放一个 Provider。
- **语言包**：`Lang`（同 vue-tsx-pro 的 Lang，由 `tools/lang-gen` 生成）放在 `UIConfig.Lang`，`AddRuneUI` 设应用级的、`RuneProvider` 的 `Lang` 在一片区域里换；
  组件里的文案参数（CloseLabel、ClearLabel……）不写时取语言包。Provider 的包裹元素写上 `lang` 与 `dir`。改几条用 `Lang.ZhCn with { … }`。
  `RuneUIOptions.Languages` 写了几套时按界面文化选（同 ASP.NET Core 的本地化：RequestLocalization 设好 CurrentUICulture）：UIConfig 改成 scoped，
  每个请求 / 电路第一次用到时按当时的文化取（`Lang.Match`：代码完全相同 → 语言相同 → 父文化），同一作用域里是同一个对象。
  vue-tsx-pro 的 toLocaleString 翻译成 `LangFormat.Number`：取不到文化数据时（WebAssembly 按需加载）用不变文化，不抛异常。
  数字、金额的格式（`NumberFormat`，同 vue 的 NumericFormat）按语言包的语言、用 decimal 算；WebAssembly 默认只按浏览器语言加载一片 ICU 数据，
  语言包的语言与浏览器不同时（英文浏览器看中文界面）取不到那个文化，所以 WebAssembly 应用要写 `BlazorWebAssemblyLoadAllGlobalizationData`（文档站、测试台都写了）。
- **组件的定制一个入口**（`AddRuneUI` 与 `RuneProvider` 的 `Components`，同 vue-tsx-pro 的 `components: { Button: { defaults, styles, variants } }`）：
  每个组件一项 `ComponentConfig<TDefaults, TPart>`（没有默认值的组件用 `ComponentConfig<TPart>`），写默认值、按部件追加的样式、某个档位取值时追加的样式；
  组件、部件、档位都是类型，写错编译不过。添加时拆进两张表（同 vue 的 splitComponents）：`ComponentDefaults` 给组件取默认值，`ComponentStyles` 给配方取样式；
  Provider 嵌套时内层叠在外层上面（默认值按项合并、内层优先，样式排在外层之后）。
- **默认值**：每个组件一个 record（`CardDefaults`），属性可空，`Over` 定义叠加规则；组件上写的参数 > 最近的 Provider > 应用级 > 组件自己的默认值。
- **按部件、按档位定制样式**：部件用组件的部件枚举（`CardPart.Header`），档位用档位的枚举值（`ButtonVariant.Outline`）。配方的键是档位记录时，
  记录实现 `IAppearance` 交出它的枚举取值，元组、单个枚举的键直接读；有按档位的定制时才去读。追加在组件样式之后、使用方的 Sx 之前，按部件的在前、
  按档位的在后，同在定制那一段（`rune.s1`）；`Part.Of` 统一处理，组件不用为此写代码。
- **不让下面的组件白白重新渲染**：Provider 的参数没变时给出同一个配置对象；配置是 record，级联参数与普通参数一样比较，没变就跳过。
- **共用的交互样式**：`SharedStyles.InteractionColors`（悬停、按下时颜色与阴影的过渡）、焦点环、视觉隐藏、色调文字色、波纹宿主；浮层进出场是 `OverlayTransitions`。

## 8. 浮层（`src/Rune.UI/Overlay`、`wwwroot/Rune.UI.lib.module.js` 与 `wwwroot/js/`）

- **ScrollArea 是滚动的基础组件**（同 vue-tsx-pro）：原生滚动、浮层滚动条。组件只渲染结构，滑块尺寸位置、显隐、拖动、点轨道翻页、滚轮转交、
  内容超出时视口可聚焦都在 JS（`js/layout/scroll-area.js`），静态 SSR 一样。会滚动的部分都用它：列表框自己滚动时列表元素就是视口（PropsViewport），
  选项浮层与菜单是 FloatingStyles.ScrollPanel 里放 ScrollArea，对话框、抽屉的正文是 ScrollArea（Body + BodyContent）。
- **Collapse 是展开收起的基础组件**（同 vue-tsx-pro）：grid-template-rows 0fr ↔ 1fr 做高度过渡；收起时组件渲染 inert，过渡结束后 JS（`js/layout/collapse.js`）加
  hidden="until-found"，页内查找命中时 beforematch 转成 rune-toggle 让 C# 展开。Collapsible 的内容区是 Collapse（收起后内容仍在页面里，Lazy 时第一次展开才渲染）。
  注意：带内容片段参数的组件会随外层 CascadingValue 的通知重新渲染（ParameterChangeDetector 把 RenderFragment 一律当作变了）。
- **Avatar**：图片盖在后备内容（首字母）上，加载状态由 JS 按 load / error 写在图片自己的 data-state 上（换图时图片按地址重建、状态重置），
  加载失败的图片藏起来、露出首字母；没有往返，静态 SSR 一样。AvatarGroup 的“超过 Max 折叠成 +N”在 Blazor 里没法截取子内容，改为 Items 加每项的模板。
  状态色分两层（同 vue 的 statusToneBorderStyles / statusToneStyles）：`SharedStyles.StatusBordered` 留出边框位置，`StatusColors` 只给颜色；
  标记、角标、提示条两层都用，头像只用颜色层（没有边框）。
- **Tabs**：Tabs 每次渲染新建 TabsState 级联下去（当前标签变了 Tab、TabPanel 跟着重渲染），标签与面板登记在同一个 TabsRegistry 里（不写值时选第一个，
  只当导航的标签不写 aria-controls）。漫游焦点、下划线指示条复用 roving.js、indicator.js；溢出时的滚动按钮、当前标签滚进来、Delete / 中键关闭、
  拖动与 Alt + ←→ 排序在 `js/navigation/tabs.js`。排序只发 rune-reorder，由使用方重排，JS 不挪 Blazor 管的元素。WebAssembly 下 C# 在 dispatchEvent / click
  里同步就渲染完了，“等渲染后再做”的逻辑（关闭后交焦点、排序后留焦点）要先同步查一次再挂观察器，否则观察器等不到变化、留到以后误触发。
- **CommandPalette**：Dialog（Lazy=false：打开时输入框已经在，autofocus 才生效）里放 combobox 输入框与 ListboxView，键盘复用 lists.js：
  列表在打开的对话框里也算“显示着”（原来只认弹出层），宿主写 `data-rune-list="text first"` 时聚焦、结果变了高亮第一项；输入时先清掉旧高亮
  （C# 渲染出新结果前按 Enter 不选中上一批的项）。快捷键在 `js/core/hotkeys.js`（`data-rune-hotkey` 写在对话框上，按下时直接打开或关上；
  显示快捷键的 Kbd 在 macOS 上换成 ⌘）。
- **本页目录、加载条、跳到正文**：TableOfContents 由 C# 按 Items 输出链接（预渲染的 HTML 里就有目录），滚动高亮在 `js/navigation/toc.js` 里写 aria-current="location"，
  指示条复用 indicator.js；链接按当前页解析（NavLinks.Resolve）。LoadingBar 的控制器只记状态（Status、Run），延迟显示、先快后慢地涨、走满淡出都在
  `js/navigation/loading-bar.js`，Server 模式不用每 300ms 往返；`Navigation` 时跟着 Blazor 的 enhancednavigationstart / end 走。SkipLink 复用 data-rune-focus
  （加了 data-focus-block="start"）。
- **AppShell**：抽屉、全屏这类开关尽量放在 JS 里，静态 SSR 也能用：顶栏的菜单按钮写 `data-rune-open` 指向抽屉（DialogBase 加了 `Id` 参数），
  抽屉写 `data-rune-close-on-link`（点了链接关上）与 `data-rune-close-when="(min-width: 64rem)"`（变宽关上）；全屏的状态在 C#，外壳上的
  `data-rune-fullscreen` 变了时 `js/core/fullscreen.js` 请求或退出浏览器全屏，用户自己离开浏览器全屏、或没进去时按 Esc，发 rune-command（fullscreen-exit）。
  收起、全屏要交互模式，静态渲染时不显示这两个按钮。顶栏高度经 `LayoutVars.StickyOffset`（`--rune-sticky-offset`）交给标题的 scroll-margin 与粘性侧栏。
  注意：样式合并按“条件 + 属性”覆盖，后写的无条件 `display: none` 盖不住先写的断点里的 display，要隐藏写在断点里显示的元素时断点里也写 none。
  整页示例：`[DocFrame]` 组件渲染在 `/frame/{页面}/{名字}`（测试台是 `/{模式}/frame/…`，FrameLayout 不套站点外壳），`Example` 写 `Frame="高度"` 用 iframe 打开。
- **主题切换全在浏览器里**：可选的主题写在 `RuneUIOptions.ThemeChoices`（服务注册的配置三种渲染模式都拿得到；交互模式的根接不到布局级联下来的值）。
  最外层的 RuneProvider 输出每套主题按 `:root[data-theme="名字"] .主题类` 取用的变量（排在跟随系统的两段之后，选择器更具体）；
  head 里的 ThemeBootScript 输出配置（`script#rune-theme-config`：存储键、每套的底色、文字色、配色方案）与一小段脚本，页面显示前按 localStorage
  写好根元素的 data-theme 与颜色。ThemeSwitcher 的菜单项写 `data-rune-theme-choice`，`js/core/theme.js` 处理点击（记下、写根元素、从按钮处圆形展开）、
  同步选中标记与按钮图标（复制选中项的图标），并跟着系统明暗与对比度、其他标签页、增强导航换页重新应用。
  跟随系统时先按 `prefers-contrast: more` 选一对（`ThemeChoices.SystemContrast`，内置的是高对比亮、暗，同 vue 的 builtinSystemThemes），再按明暗选一套；
  脚本没运行时主题类里同样的媒体查询兜底。C# 不知道选了哪套，所以单选项的选中标记按
  aria-checked 用 CSS 显示（MenuRadioItem 的 Indicator），JS 改 aria-checked 就够。代价是每页都带全部可选主题的变量（16 套约 230KB，未压缩），
  静态导出时再考虑抽成共享样式表。
- **文档内容是一个 Razor 类库（apps/Rune.Docs.Content）**：组件页 `Components/<分类>/<组件>/<组件>Page.razor`（`@page` + `[DocMeta]`，示例按 vue-tsx-pro 的两列写定），
  一个示例一个文件（`Demo/<名字>Example.razor`），源码作为嵌入资源打进程序集，示例卡片按类型取出原文（ExampleSource），WebAssembly 里也能读。
  API 表运行时生成（ComponentApiReader）：反射读 [Parameter]，新建实例取默认值，说明取嵌进来的 Rune.UI.xml（所以三个库在项目文件里开 GenerateDocumentationFile）。
  文档站（Rune.Docs）与渲染模式测试台（Rune.Demo，`/{模式}/{页面}`，端到端测试与对照工具用它）渲染同一批页面。
  注意：收起的源码也在 DOM 里（页内搜索能找到），端到端测试按文字找元素时要只看可见的（`Filter(new() { Visible = true })`）或限定在示例里。
- **文档站静态导出**（tools/docs-export）：整站是 WebAssembly 的交互根，服务端只做预渲染，所以可以导出成静态文件。静态资源按发布结果的
  端点清单复制（MapStaticAssets 的地址带指纹，磁盘上是原名；压缩版本不要，托管方自己压缩），import map 里服务端运行时生成的模块
  （`_framework/resource-collection.*.js`）从运行中的服务端取下来；页面从首页按站内链接与 iframe 爬完，存成 `路径.html`。
  部署到 Cloudflare Workers 的静态资源（apps/Rune.Docs/wrangler.jsonc，`not_found_handling: 404-page`）。
- **docs-kit 放在 Rune.UI 里**（不像 vue-tsx-pro 分子路径导出）：CodePreview 的源码由使用方给出，收起时是纯文本，指针移到、焦点进入工具栏或展开时才
  交给 CodeBlock 高亮。PropsTable、TextTable 显式转交 Table 的外观参数（未声明的属性按字符串传，类型对不上）。ColorSwatch 的 token 取值由 JS 读
  （`data-rune-token-value`，插入时与换主题后重读），C# 不知道浏览器里当前是哪套主题。
- **代码高亮在浏览器里做**（同 vue-tsx-pro 的 createShikiHighlighter）：CodeBlock 输出纯文本的 `code[data-rune-highlight]`，JS 初始化器（`js/display/highlight.js`）
  第一次遇到时加载 vendor 的 Shiki（JavaScript 正则引擎、CSS 变量主题，颜色指向 `--rune-code-colors-*`）上色，三种渲染模式一样，没有往返。
  JS 改过 code 的子节点，所以代码、语言、高亮开关、行设置变了时 code 用 @key 整个重建，不让 Blazor 在上面做差异更新。
  选过 C# 的 TextMateSharp（3.0 起纯托管、能跑 WebAssembly），但只有预览版，且 WebAssembly 接管时要另外保留服务端的结果，暂不用。
- **ColorField 与 ColorValue**：vue 用 culori 解析任意 CSS 颜色；这里 `ColorValue`（Rune.UI/Data）解析 hex、rgb、hsl、hwb、oklab、oklch、148 个颜色名
  （从 culori 的 named.js 生成）与 transparent，单元测试的期望值由 culori 算出。文字框边输边记草稿（oninput，实时标无效），change（失焦、回车）时
  规范成 #hex 写回或恢复；色块里的颜色经 CSS 变量写在元素上，没有值时露出棋盘格（SwatchStyles，之后的 ColorSwatch 共用）。
- **Descriptions**：项是子组件 DescriptionsItem（Value 文字 / 子内容任意内容），经级联值拿外观与部件属性。vue 的 columns="auto" 用 ResizeObserver 量宽度算栏数，
  这里写 MinItemWidth（同 Grid）后改用容器查询：每多一栏一条 `@container (min-width: calc(k * 宽度))`，跨栏同理封顶；静态 SSR 也对、不需要往返。
- **ResizeHandle**：拖动、方向键、双击都在 JS（`js/layout/resize.js`）当场改父元素的宽（横向是高），并去掉按档位给的最大宽度；一次操作结束才发 rune-resize
  交给 C#（拖动中 C# 不渲染，就不会有晚到的渲染把宽度拉回）。Drawer 左右两侧默认可拖宽（`@bind-Width`，DialogBase 的 Edge / RootInlineStyle 钩子）。
- **JS 的组织**：入口 `Rune.UI.lib.module.js` 只注册自定义事件；主体 `js/core/runtime.js`（全局事件委托、DOM 变化监听）在入口执行时就开始在后台加载，
  按职责分到 `wwwroot/js/` 的模块：overlays、floating、lists、tooltips、overflow、controls、effects、layout、shared，以及 roving、indicator、slider、move、rating、otp、scroll-area 等。
  模块用相对路径互相 import（浏览器原生 ES 模块，不打包）。入口不静态导入任何模块：Blazor 启动前要等初始化器连同它的静态导入加载完，
  几十个模块挡在启动的关键路径上（本机 HTTP/1.1 约 0.7 秒）。但事件委托的**顺序**要保持排在 Blazor 之前（输入时先清旧高亮、
  C# 渲染出新结果再高亮第一项；WebAssembly 下 C# 在事件里同步渲染，真实事件的两个监听器之间会跑微任务）：入口启动时同步在 document 上
  为每种事件占位（`SLOTS`），主体 `install(blazor, on)` 经 `on` 把处理函数挂进去；新的事件类型要先在 `SLOTS` 里占位。
  入口顶部的注释是全部 data 属性约定的总表，新增约定写在那里。
- **DOM 变化的处理**（`js/core/runtime.js` 的 onMutations）：Blazor 先插父元素、再逐个插子元素，一棵新子树的每个节点各有一条记录；
  只从这一批里最外层的新节点往下找要处理的元素，列表、标签行等按元素去重后每批只处理一次（Select 文档页经增强导航进入时，
  逐条处理要做约 2800 次 querySelectorAll、10 万次 matches）。

- **尽量用浏览器原生**：弹出层是 `popover="auto"`（顶层、点外面与 Esc 关闭、同时只开一个、`popovertarget` 打开），对话框与抽屉是模态 `<dialog>`
  （焦点圈在里面、Esc 关闭、关闭后焦点还给触发元素），进出场用 `@starting-style` 加 display / overlay 的离散过渡（`When.Starting`），关闭动画不需要 JS 等待。
- **组件不调用 JS**：只在元素上写属性，JS 初始化器（Blazor 按 `{程序集名}.lib.module.js` 自动加载）用全局事件委托处理：
  `data-rune-overlay`（开关时发出冒泡的 `rune-toggle`，C# 里是 `@onrunetoggle`）、`data-open`（受控时 JS 同步原生状态）、
  `data-rune-open` / `data-rune-close`（点击打开、关闭）、`data-rune-light-dismiss`、菜单键盘、提示的 Esc 与 aria-describedby、模态时锁定滚动、
  锚定浮层的定位。好处：Server / WebAssembly / 静态 SSR 行为一致，没有 `IJSRuntime` 的异步与往返；
  静态 SSR 里弹出框、菜单、用 Trigger 打开的对话框都能用。
- **原生 toggle / close 事件不冒泡**，Blazor 内置的 `@ontoggle` 也不带新状态，所以转发成 `rune-toggle`（`OverlayToggleEventArgs.Open`）。
  Chromium 在渲染更新时才派发这两个事件：页面不渲染（被隐藏的预览窗格）时收不到，测试时注意。
- **锚定浮层用 Floating UI 定位**（与 vue-tsx-pro 同一个库、同一个版本，`tools/vendor-gen` 放进 `wwwroot/vendor`，第一次打开浮层时才 `import()`）：
  浮层带 `data-rune-floating="bottom-start"`（`Placement` 12 种），打开时 offset、flip、shift，可选 arrow 与 size（`data-fit-height`、之后下拉框的 `data-match-width`），
  `autoUpdate` 跟随；最终方向写到 `data-side` / `data-align`，缩放原点朝向触发元素。最初用的是 CSS 锚点定位（`position-area` 加翻面），
  做不到贴着视口平移（shift），也不知道最终翻到了哪边（箭头、缩放原点没法跟着），Firefox 还要 JS 兜底，与 vue-tsx-pro 对齐时换掉。
  定位好之前浮层透明、不接指针（`opacity: 0`、`pointer-events: none`），焦点在定位之后才移进去（菜单的第一项、对话框类浮层的 autofocus 或第一个可聚焦元素）。
  不用 `visibility: hidden`：原生 popover 打开时就按 autofocus 聚焦了里面的搜索框，元素一藏就失焦，定位期间打的字落到页面上丢了（TreeSelect 的 E2E 发现）；
  第一次打开才渲染内容的浮层打开时还是空的，JS 等内容渲染出来（最多 1 秒）再定位，不先按空盒子定位闪一下。
- **触发元素由使用方写**：`Trigger` 是 `RenderFragment<Attrs>`，组件把 `popovertarget`、id、aria-controls 等交给它（`<Button @attributes="t">`），按钮仍是使用方的组件。
- **内容第一次打开才渲染**（交互模式，Popover 的 Lazy）。菜单项、气泡确认一开始就渲染。静态渲染没有交互，一开始就渲染。
  浮层的名称默认取触发元素的文字（aria-labelledby 指向触发元素的 id，同 vue-tsx-pro）。
- **气泡确认的异步确认**：`OnConfirm` 执行期间确认按钮加载中、取消不可用，完成后组件把受控的 Open 设为 false 关上；
  vue-tsx-pro 里返回 false 保持打开，Blazor 的 EventCallback 没有返回值，改成参数 `ConfirmEventArgs.KeepOpen`（同 .NET 的 CancelEventArgs）。
  取消就是关闭（取消按钮只带 `data-rune-close`），关闭时没确认过就调用 OnCancel，所以 Esc、点外面与取消按钮一样。
- **进出场统一**：`OverlayTransitions`（同 vue-tsx-pro 的 transitions）：打开状态 `:is(:popover-open, :modal)` 里用 `@starting-style`，
  进场 Normal 时长 + enter 缓动，离场 Fast + exit 缓动；Scale（弹出框、菜单、对话框）、Fade（提示）、Backdrop（遮罩）；
  抽屉的滑入在 DialogStyles 里（进场 Lg、离场 Md，打开时 translate 是 none，不然它会成为里面 fixed 元素的包含块）。
- **对话框与抽屉同 vue-tsx-pro 的 ModalCore**：标题（必填，也是名称）、说明（aria-describedby）、标题行的关闭按钮（Closable）、
  点遮罩关闭（Dismissible）、正文（只滚正文）、页脚；写了 OnConfirm 时页脚另有“取消 / 确认”，确认中关不掉。
  BeforeClose（关之前问一次，如有未保存的改动）与确认中都要拦住浏览器自己的关闭：对话框带 `data-rune-guard-close` 时，
  JS 初始化器拦下 Esc 的 cancel 事件、点遮罩与 data-rune-close 的点击，改发 `rune-close-request`（带原因），C# 决定后把受控的 Open 设为 false。
  没有 BeforeClose 时不带这个属性，关闭仍由浏览器直接完成，静态 SSR 也能关。宽度档位：Dialog 的 `DialogSize`（Xs–Xl 同 Container，另有 Full），
  Drawer 的 `ContainerSize`，默认值可以经 `DialogDefaults`、`DrawerDefaults` 统一改。
- **锁定页面滚动**（同 vue-tsx-pro 的 lockScroll）：模态打开时 html `overflow: hidden`，在结束边补上与滚动条等宽的内边距，页面不左右跳。
  最初用 `scrollbar-gutter: stable`，槽留在视口里，贴着结束边的抽屉与视口边缘隔着一条，对照时发现、换掉。
- **按钮的不可用与加载中**（同 vue-tsx-pro 的 useButtonBehavior）：`Disabled` 是原生 disabled；`FocusableWhenDisabled` 改用 aria-disabled，仍能聚焦，
  外面包 Tooltip 说明原因。加载中只写 aria-busy，外观不变灰。这两种状态组件去掉使用方的点击处理，并带 `data-rune-inert`，
  JS 初始化器在捕获阶段拦下点击的默认动作（提交按钮不提交表单）。加载指示推迟 200ms 才出现（同 useDelayedLoading，CSS 过渡延迟实现，
  没有往返），很快完成的操作按钮不闪一下；vue-tsx-pro 还保证指示至少显示 500ms，CSS 做不到，这里没有。
- **骨架、加载遮罩的防闪烁用 `DelayedLoading`**（同 useDelayedLoading：200ms 后才显示，显示了至少 500ms）：骨架要整块换掉内容，只能由渲染决定，
  所以在 C# 里计时，到点后 `InvokeAsync(StateHasChanged)`（Server 模式多一次推送）；第一次参数就在加载时立即显示，静态 SSR 只有这一次。
  按钮的加载指示仍用上面的 CSS 延迟（不换内容，没有往返）。加载遮罩一直在页面上，按 DelayedLoading 切换 `hidden`，淡入用 `@starting-style`、
  淡出靠 `display` 的 `allow-discrete`，不需要离场计时器；外层的 aria-busy 按真实的 Loading 立即生效。
- **Spinner 同 vue 的结构**：外层 span，有 Label 时是 role="status"，说明放在 VisuallyHidden 里，旋转的图标对读屏隐藏（role="status" 写在 svg 上读屏支持差）。扫光（`ShimmerStyles.Wave`）画在 ::after 上用 transform 平移，各行的延迟写在行内样式上，光带继承。
- **可移除的标签、可关闭的提示条**（同 vue-tsx-pro 的 useDismissible）：移除按钮带 `data-rune-dismiss`，标签带 `data-rune-dismissible`。JS 初始化器先加
  `data-dismissing` 播放离场过渡，结束后发出 `rune-dismiss`（`@onrunedismiss`），组件再调用 OnRemove；标签被移除时焦点交给同组的下一个
  （没有就上一个）可聚焦元素，等 2 秒还在（使用方没移除）就去掉 `data-dismissing` 重新显示。没有往返也不用 C# 计时。
- **复制按钮**：给 Value 时按钮带 `data-rune-copy`，JS 初始化器在点击当下写入剪贴板（Safari 只认点击当下的写入，静态 SSR 也能用）；
  给 GetValue 时 C# 取到值后换一个递增的 `data-rune-copy-request`，JS 看到就写入。成功后 JS 在按钮上写 `data-state="copied"`
  （图标用 `When.Inside` 按它切换，过一会儿复原）、经共用的播报区念“已复制”，再发出 `rune-copied`（`@onrunecopied`）。
- **图标描线**：Icon 的 Draw 写成 `data-rune-draw`，JS 初始化器在它变成 true（含第一次渲染）时用 Web Animations 把每一笔从 0 画到 1。
- **波纹**（同 vue-tsx-pro）：按钮、菜单项带 `data-rune-ripple`，JS 初始化器在按下时从指针位置（键盘 Enter / 空格时从中心）扩散一圈 currentColor 的半透明圆，
  松开后淡出；纯客户端，Server 模式没有往返。配置 `UIConfig.Ripple`（默认开）、Provider、组件的 `Ripple` 参数逐层覆盖；像链接的按钮没有波纹。
- **提示**（同 vue-tsx-pro 的 Tooltip 与 hover-intent）：`popover="manual"`（不会关掉正打开的菜单、弹出框），触发元素带 `data-rune-tooltip`，
  JS 初始化器按悬停意图开关：停留 `data-delay`（默认 500ms）后显示，刚关掉一个（300ms 内）时直接显示，离开 100ms 后隐藏（可以移到提示上），
  键盘聚焦立即显示，按下、失焦、Esc 隐藏，同一时间只显示一个；Server 模式没有往返。最初是纯 CSS（外层悬停时显示），
  做不到组内跳过延迟、移到提示上不消失、同时只显示一个，内容也只能是文字，对齐时换掉。
- **NavList**：当前页按 NavigationManager 判断（`NavLinks.IsCurrent`，Match 同 Blazor 的 NavLink），换页时各项跟着变；只有锚点的地址
  （`#orders`）由 `NavLinks.Resolve` 接在当前页后面——Blazor 按 &lt;base href&gt; 解析会跳到站点根目录。组内各项经级联的 NavGroupState 报告
  自己是不是当前页，分组据此自动展开（静态 SSR 也是：报告时分组 StateHasChanged，输出前处理完）。分组用 Collapse，紧凑分组用 HoverCard（Lazy=false：
  子项要一直在、才能报告）。ItemStyles 补了 Hoverable、Current。
- **悬停卡片与提示共用悬停层**（`js/overlay/hover-layers.js`，同 vue 的 useHoverLayer）：触发元素分别带 `data-rune-tooltip` / `data-rune-hovercard`，
  面板带 `data-rune-hover-panel="tooltip|card"`（两组各自“同时只开一个”）。卡片停留 700ms 打开、离开 300ms 关闭，焦点移进卡片不关，
  离开触发元素与卡片才关；开关经 rune-toggle 告诉 C#（第一次打开才渲染内容），但不是 data-rune-overlay（打开时焦点不移进去）。
  卡片在交互渲染后才输出：预渲染的 HTML 里触发元素在 `<p>` 里时 `<div>` 会把段落截断（同 vue 的 useMounted）。
- **命令式对话框**：`DialogService.Open(options)` 的正文是 `RenderFragment<DialogHandle>`，`Open<TComponent>(options, parameters)` 的正文是一个组件
  （同 DynamicComponent 的参数字典）；DialogHost 把句柄级联给正文，组件里 `[CascadingParameter] DialogHandle` 拿到它，`Close(结果)` 关上并交回，调用处 `await handle.ResultAsync<T>()`。
- **消息提示**：`ToastService`（scoped）把列表放在 `State` 里，`Toaster` 读到它就随变化重新渲染，后台线程调用也安全；每条是一个 Alert。
  Toaster 要放在交互区域里（同一个电路 / WebAssembly 实例才拿得到同一个服务）。

## 9. 表单（`src/Rune.UI/Input`、`src/Rune.UI/Form`）

- **接入 Blazor 自己的表单**：不另做一套。控件继承 `FormControl<TValue>`：`@bind-Value` 给出的 `ValueExpression` 换成 `FieldIdentifier`，
  改值后 `NotifyFieldChanged`（DataAnnotations、FluentValidation 等照常工作），这个字段有错误时控件标 `aria-invalid`。单独使用时就是普通的受控 / 非受控控件。
- **Field**：标签、说明、错误信息，经级联的 `FieldContext` 把控件 id、aria-describedby、必填 / 不可用 / 只读交给控件（控件上显式写的优先）。
  控件把绑定的字段报给 Field（`FieldRegistration` 是信号，报上来后 Field 自动重新渲染），Field 显示表单里这个字段的错误。
  Field 不写 Label 时取这个字段上的 `[Display(Name)]` / `[DisplayName]`（同 Blazor 的 DisplayName 组件；框架的组件要 For 表达式，Field 拿到的是 FieldIdentifier，所以按属性自己读）。
- **聚焦**：控件与按钮公开 `Element` 与 `FocusAsync()`（同 InputBase.Element）：输入框、触发按钮、勾选框、第一个滑块；包了一层的（NumberRangeField、Mentions）转给里面的；
  一组控件没有单一主元素，是 null。用的是 Blazor 自带的 `ElementReference.FocusAsync`，静态渲染时什么也不做。
- **必填用 aria-required**：原生 `required` 会在提交前拦下表单、弹浏览器自己的气泡，EditForm 根本收不到提交（实测发现）。校验统一交给表单。
- **使用方的属性落在 input 上**（placeholder、name、autocomplete……），Sx 落在外框上：外框是看得见的盒子，input 是语义上的控件。
- **输入框共用一个外壳配方**（`InputStyles`，scope 为 input，同 vue-tsx-pro 的 inputRecipe）：部件 Root / Input / Icon / Affix / Button，
  样式 Surface / Soft，只读；焦点环是外壳上的 outline（focus-within）；校验未通过、不可用按里面输入元素的 aria-invalid、:disabled 用 :has 变化。
  TextField、Textarea、NumberField 直接用它，NativeSelect 用它的片段。默认值是 `InputDefaults`（同 defaults.Input）。
- **输入框的客户端行为都在 JS 初始化器里**（没有往返）：点外壳空白处聚焦输入元素（`data-rune-control`）、清空后焦点回到输入框（`data-rune-refocus`）、
  显示密码切换 type 与 aria-pressed（`data-rune-reveal`）、输入时更新字数计数（`data-rune-count`）、多行文本框按 scrollHeight 增高
  （`data-rune-autosize`，值变了属性跟着变，使用方改值也会重新量；没用 field-sizing：Firefox 不支持）。
- **勾选框、开关是原生 input** 去掉默认外观重画：键盘、表单提交、读屏都是原生的；半选只能用 JS 设置，JS 初始化器按 `data-indeterminate` 同步。
  三者共用一个配方（scope 为 choice，同 vue-tsx-pro）：选中、不可用、校验未通过都按原生 input 的状态用 CSS 变化（:checked、:disabled、aria-invalid，
  在校验未通过的组里也标红），选中的颜色来自外层的 `--rune-tone-fill` / `--rune-tone-on-fill`（状态色用文字色，保证与表面的对比）。
  半选的填充只给复选框：一组单选都没选时浏览器把每个 radio 都算作 :indeterminate。用户点击勾上时 JS 初始化器把对勾一笔画出（`data-rune-check-draw`）。
- 文本框默认 change 时更新（失去焦点、回车），`Immediate` 才每次输入都更新：Server 模式下每次按键一次往返。
- **随输入改值的 input 经 `ValueElement` 渲染**（TextField、Slider、Combobox、TagsInput）：Blazor 只对 `@bind` 生成的处理器登记 `SetUpdatesAttributeName("value")`，
  登记了，派发事件时先把渲染树里的 value 换成浏览器里的当前值；我们用 Attrs 展开 value 与 oninput、写不了 @bind，不登记时处理上一个按键后渲染出的
  旧值与旧树比出差异、写回浏览器，盖掉之后打的字（Server 模式下快速输入“Blazor,”只加进了“B”，E2E 偶发，脚本快速派发 input 复现）。
- **NumberField 的值类型不加约束**（同 Blazor 的 InputNumber）：Razor 给泛型组件生成 `@bind` 代码时处理不了 `TValue?`（可空的值类型参数），
  会生成引用 `global::TValue` 的代码编译失败。所以 TValue 本身可以是 `int?`，Min / Max / Step 写成 decimal，计算都在 decimal 里（没有浮点误差），
  再按值的类型转回去。方向键在 JS 初始化器里处理（按输入框里现在的文字算，没有往返），改完发 change，C# 再夹到范围、对齐步长。
- **FormValue**：不是原生输入元素的控件（Select、Combobox、TagsInput、NumberField、Slider）把值交给表单的隐藏 input，同 vue-tsx-pro：
  数字按不带分组、不用科学计数法的十进制，日期时间按 ISO；没有 name、值为 null 或不可用时不渲染。
- **提交的名字**：每个控件一个 `Name` 参数（在 FormControl 上；写成 `name="…"` 也落到它上面，Razor 按参数名匹配不区分大小写），原生元素与隐藏的 FormValue 都用它。
  没写时，`EditContext.ShouldUseFieldIdentifiers` 为真（EditForm 在服务端渲染时设的）就按 `@bind-Value` 的字段生成（`() => Model.Lines[1].Sku` → `Model.Lines[1].Sku`，
  同 Blazor 的 InputBase.NameAttributeValue；框架的 HtmlFieldPrefix 不公开，嵌套的 Editor<T> 前缀不认）。静态 SSR 下 `[SupplyParameterFromForm]` 按它绑回模型（测试台 `/static/form-post`）。
  勾选框、开关勾上时提交 `true`（同 InputCheckbox）；NativeSelect 提交的是选项序号，绑不回模型的值，只用使用方写的 Name。
- **解析不了的输入**：NumberField、ColorField 失焦时恢复原值（同 vue-tsx-pro），不会留下无效的文字，所以不需要 InputBase 那种“无法转换”的校验错误。
- **数字框**：写了 name 时提交的是隐藏 input 里的十进制文本（不带千分位与货币符号）；加减按钮按下时不抢输入框的焦点、按住连续加减（JS 初始化器的 `data-rune-repeat`），Clearable 清空成 null。
- **单选、原生下拉用原生元素**：键盘、读屏、手机上的系统选择器都是原生的。NativeSelect 按位置对应值（任何类型）。
- **Slider 不用原生 range**（同 vue-tsx-pro）：原生的做不了两个滑块、刻度，外观也只能靠浏览器各自的伪元素。滑块是 role="slider" 的 div，
  按下、拖动（`js/core/move.js`，指针捕获）、方向键都在 JS 里算并当场画出来（外层的位置变量、aria-valuenow 与取值范围、刻度的 data-active、隐藏 input），
  再经 `rune-slide`（带序号）交给 C#；静态 SSR 下没有 C#，JS 画的就是最终状态，表单照样提交。Server 模式下拖动中 C# 的渲染晚到、会写回旧值：
  C# 把处理到的序号写回 `data-ack`、认定的值写在 `data-values`（JS 从不改它），没追上时 JS 立刻改回本地的值（MutationObserver 在绘制前回调），
  追上之后按 `data-values` 画（父级没采纳改动时 Blazor 的旧树没变、不会重写被 JS 改过的属性）。单值绑 Value，范围绑 Range（`NumberRange`），同 Select 的 Value / Values。
  C# 收到后再夹一次范围、对齐步长，舍入与 JS 的 Math.round 一致（正好一半往大取）。
- **Form 包一层 EditForm**：默认带 DataAnnotationsValidator；提交回调执行期间级联的 `FormState.Submitting` 让提交按钮显示加载中；
  提交未通过时顶部列出错误，Field 渲染后把“字段 → 标签、控件 id”登记给 Form，汇总写成“标签：错误”并能跳到控件。
  跳转用 `data-rune-focus` 在捕获阶段拦下：`#id` 链接会被 Blazor 按 `<base href>` 解析成别的页面（实测跳回了首页）。
- **列表（Listbox，之后的 Select、Combobox 共用）**：数据层是 `ListState`（选项、分组排序、切换选择，同 vue-tsx-pro 的 list-state），
  高亮与键盘在 JS 初始化器的 `data-rune-list`：焦点留在宿主（列表本身，或 aria-controls 指向列表的触发按钮、输入框）上，方向键、Home / End、
  Page Up / Down、类型搜索只改选项的 `data-highlighted` 与宿主的 `aria-activedescendant`，没有往返；Enter / 空格点击高亮项，选中才回到 C#。
  高亮按序号记在宿主的 `data-rune-active` 上（选项的 id 是 `{列表 id}-{序号}`），渲染出来的那项另写 `data-highlighted` 给样式用；
  这些属性都不在 Blazor 的渲染树里，重新渲染不会抹掉，列表重新渲染（DOM 有新增节点）时 JS 再把 data-highlighted 放回去。
  选项超过 100 个且在交互模式下用 Blazor 的 `Virtualize`（静态 SSR 没法按滚动加载，全部渲染）：看不见的选项不在 DOM 里，
  C# 把全部选项的文字与不可用写成 `data-rune-options`（手写 JSON，不经反射序列化），第一个已选项的序号写成 `data-rune-selected`；
  键盘移到没渲染的项时先按比例估算着滚过去，Virtualize 渲染出来后再精确滚进可见范围。
  类型搜索与菜单共用一个实现（连续输入拼词、停顿 500 毫秒清空、同一个字母轮换）。
  vue-tsx-pro 的 Listbox 用 css.create，没有部件标记：对照工具另按 role 配对。
  渲染拆成 `ListboxView`（只画列表与选项）：Listbox 在它外面加值与焦点，弹出的选项列表 `ListPopover`（Select，之后的 Combobox、TagsInput）在它外面加浮层与头部。
- **VirtualList**：虚拟滚动器在 JS（js/table/virtual.js，移植 vue-tsx-pro 的 virtualizer.ts）：按估计大小排位置表，渲染后量真实大小、按 data-key 缓存，
  可视区上方的项变了大小时调整滚动位置；位置（transform）与列表的总大小由 JS 写，滚动时算出要渲染的那一段经命令交给 C#（range），C# 只渲染这一段。
  不用 Blazor 的 Virtualize：它没有横向、随页面滚动、跳到第几项与可见范围的回调。ScrollToIndex / ScrollToOffset 由 C# 写 data-rune-virtual-scroll（带序列号）。
  横向时视口 `contain: inline-size`：几万像素的总宽不算进它的固有宽度，按内容排版的祖先（表格、auto 的网格列）不会被撑开；
  项按内容的高度（视口里隔着 ScrollArea 的内容层，百分比高度算不出来）。静态 SSR、预渲染时在普通流里输出开头几项。
- **Tree**：数据逻辑在 `TreeData`（同 vue-tsx-pro 的 utils/data/tree.ts：平铺、索引、勾选联动与半选、取法、过滤、组装、移动，都不改原数据），
  TreeSelect、TreeTransfer 与之后的树形表格共用。树就是一个列表宿主（`role="tree"` + `data-rune-list`）：↑↓、Home / End、类型搜索、高亮与虚拟滚动
  照 lists.js 走（treeitem 按 `data-index` 编号，aria-posinset 是同级里的位置）；tree.js 只管 → ←、Enter、空格、Alt + 方向键，
  展开收起等要改状态的经 rune-command 交给 C#。看不见的行不在 DOM 里，C# 把各行的父行、能否展开、是否展开写在 `data-rune-tree-rows` 上。
  不可用的节点能经过（同 vue），所以选项表里不标不可用，选中、勾选时由 C# 拦下。勾选框只是外观：`inert` 的原生 checkbox，
  `:checked` / `:indeterminate` 的样式照用勾选框的配方，点击落到外层。`LoadChildren` 是 EventCallback：完成后使用方重新渲染，新数据才交得回来
  （普通的 Func 不会让父组件刷新）。TreeSelect 的外壳、标签、远程搜索同 Select（`RemoteItems`），弹层里打开后才渲染 Tree，
  单选点中（或 Enter）时 JS 关弹层（同列表的 closeAfterPick）。拖动整个在 JS 里（落点写成行上的 `data-drop`，靠边滚动、停在收起的节点上展开），松开时才交给 C#：
  `CanDrop` 是 C# 函数，所以在松开时判断，拖动途中不像 vue 那样隐藏不能放的落点（问它就要往返，Server 模式下跟不上指针）。
- **Select**：外壳、前置图标、清空按钮、箭头是输入框的部件（`InputStyles`，`InputAppearance.Picker`），按 InputPart 定制时一起变，同 vue-tsx-pro；
  触发按钮是 `role="combobox"` 的 button，用原生 `popovertarget` 打开（静态 SSR 也能开），弹层 `data-anchor` 贴着外壳、`data-match-width` 同宽。
  开关、高亮、焦点都在 JS：关闭时 ↓ / Enter / 空格打开、↑ 打开到最后一项、单选按字母直接选中；打开后高亮已选项；单选列表点选后关闭；
  弹层关闭时焦点在里面（搜索框）就还给触发按钮；`aria-expanded` 与外壳的 `data-state` 在开关时由 JS 写（C# 收到 rune-toggle 后写同样的值）。
  搜索框也是列表的宿主（`data-rune-list="text"`，Home / End 留给输入），过滤在 C#；列表的 `data-rune-query` 变了时 JS 高亮第一个匹配项并播报 `data-rune-results`。
  多选时标签排在触发按钮前面（按钮里不能有按钮），触发按钮让出宽度、只留读屏用的文字，点外壳空白处与点它一样；Backspace 经 `data-rune-backspace`
  发 `rune-command`（不用每次按键往返）。按宽度收起标签由 JS 量（`data-rune-overflow`，同 vue-tsx-pro 的 useOverflowItems），“+N”的文字由 C# 按语言包给全（`data-rune-more`）。
  远程选项 `RemoteItems`：打开时请求、输入停下 300 ms 再请求、只采用最后一次。过滤用 `UIConfig.Matches`（按语言忽略大小写、重音与全半角，可另配 `TextMatcher`）。
- **Combobox / Autocomplete**：焦点一直在输入框里。输入框不能做 popovertarget，用 `data-rune-popover` 指出弹层：输入时 JS 打开，↑↓ 打开并发 `show-all`
  命令（C# 清掉过滤词、显示全部），弹层关着时 Esc 发 `clear`；展开按钮用原生 popovertarget。过滤词（query）与输入框的文字分开：选中后文字是选项的文字、
  过滤词清空。失焦时由 C# 提交（文字不是选项就恢复）。Autocomplete 是子类（值就是文字），弹层带 `data-rune-hide-empty`：输入过且有建议或正在加载才显示。
  中文输入法：组合期间列表宿主输入框的 input 在捕获阶段拦下、组合结束补发一次，不拿拼音字母去过滤（Select 的搜索框同样受益）。
  Select、Combobox 的选项数据（本地或远程、过滤、已选项文字的记忆、播报与空状态文字）在 `OptionSource`。
- **TagsInput**：分隔符在 C# 里按输入拆（输入框本来就逐字交给 C#）；Enter（列表没用掉时）、退格（输入框空时）、粘贴多段（JS 按 `data-rune-delimiters` 判断）
  由 JS 转成 `rune-command`（add、remove-last、paste 带文字）。输入时 JS 立刻清掉旧的高亮、因输入打开的弹层也不高亮已选项：
  过滤结果渲染出来之前按 Enter，不能选中上一批结果里的项（Server 模式下打字快时真的会发生，多选时还会把已选的取消掉）。
- **输入组件的外观参数在 `InputControl<TValue>`**：Variant、Size、Radius 与所在的相连组（`AttachedGroup`）合成 `InputAppearance`，各输入框不再各写一遍。
- **一组控件**（RadioGroup、CheckboxGroup、ToggleGroup）：根元素的属性由 `FormControl.GroupAttrs` 给（id、role、Field 的标签与说明、方向与状态；
  使用方写了 aria-label 就不再 aria-labelledby，后者会盖过前者）。radiogroup 才支持 aria-required / aria-invalid / aria-readonly，group 只写 data-*。
  组挡住外面 Field 的级联（Field 的 id、说明属于组），把不可用、只读、校验未通过经 `InheritedState` 交给里面的控件。
  CheckboxGroup、RadioGroup 的 Items 同 Select（ListboxOption、字符串，或任意数据加 ToOption）。
- **Rating**：可编辑时每个分数是一个视觉上隐藏的原生 radio（半星时每颗两个），键盘、表单、读屏都是原生的。已选的填充不由 C# 渲染，
  按勾上的 radio 用 CSS 算（这颗之后有勾上的 → 满；勾在自己里面 → 整颗或半颗，三条互斥，不打特异性的仗），静态 SSR 下点了也会变；
  悬停预览由 JS 在每颗星上写行内的 `--rune-rating-fill`（行内盖过类规则，离开时去掉），都没有往返。只读时整排是 role="img"，填充由 C# 写。
- **ToggleGroup**：项是按钮家族（ToggleItem 继承按钮家族的基类 ButtonCore），按下时 aria-pressed。整组一个 Tab 位置、组内方向键移动是 JS 的漫游焦点
  （`data-rune-roving`，ToggleGroup、Toolbar、之后的 Tabs 共用；tabindex 只由 JS 写，组件不渲染）。
  Toolbar 带 `first` 标记：Tab 进来落在第一项而不是按下的那项（工具栏里按下的状态各管各的，不代表“当前位置”）。ToggleGroup 放进 Toolbar 时经级联的 ToolbarContext
  得知自己在工具栏里，不再自建漫游，由工具栏统一管理整排的方向键。单选时选中项的底色是一个滑块
  （`data-rune-indicator` 指出跟随哪一项，JS 量好位置写成 CSS 变量，样式里过渡；减少动态效果时直接到位）。
- **ControlGroup 与相连**：组经级联的 `AttachedGroup` 告诉里面的输入框外壳与按钮“我在相连的组里”，它们在自己的配方里加上 `AttachedStyles`：
  去掉相邻一侧的圆角、负外边距叠住边框、聚焦的浮到上面。首尾按 `:nth-child(1 of :not([popover], [hidden], input[type=hidden], template))` 判断，
  跳过选择框的弹层这类不占位置的兄弟元素。ButtonGroup 用同一套，另外级联 `ButtonGroupAppearance`：组上写的外观是组里按钮的默认值
  （按钮自己写的 > 组上写的 > 配置的默认值 > 组件自己的），组里的图标按钮默认色调同 Button，拆分按钮两半同色。
- **交给子组件的样式要经 `Attrs.MergeProps` 作为 Sx 交下去**：先 `Attrs.Merge` 合成类再交给子组件，元素上会有两个类，谁赢看规则插入的先后
  （Combobox 的展开按钮的箭头过渡就被按钮自己的过渡盖掉过）。
- **外框的状态只看自己的输入元素**：`:has(> :is(input, select, textarea, [role=combobox]):disabled)`。最初写的 `:has(:disabled)` 会被数字框到了上限时不可用的步进按钮、
  下拉里不可用的占位选项匹配，整个外框变灰（实测发现）。

## 10. 生成器与分析器（`src/Rune.Generators`）

- 后备字段懒创建：热重载新增的实例字段在已有对象上是 null（字段初始化器不会再跑）。普通字段写法在 Server 上会让整个电路崩溃；.NET 10 的 WebAssembly 也接受新增实例字段（官方文档说不支持，已过时）。
- `XxxState` 的访问级别取 get 与 set 里更严格的那个，`private set` 不会被它绕过。
- `.razor` 的 `@code` 里写 `[State]`：生成器看不到 Razor 生成的代码，编译器只报 CS9248。生成器扫描 Razor SDK 交来的 `.razor` 附加文件，报 RUNE003 并指向 `.razor` 里的行（分析器在编译出错时不运行，走不通）。
- 分析器：`Style` 类型的静态只读字段、带初始化器的静态属性（RUNE004，热重载不生效）；组件渲染路径上的 `new Style`（RUNE005，每次都要重新算类名）。
  `Css.Define` 的 lambda 里、静态成员、构造函数不报。
- 样式值（`StyleValueAnalyzer`）：RUNE006 字面量有现成写法（关键字类、`CssWide`、`CssColors`、`CssLength.Auto`、带单位的 `CssLength.Em(…)`），
  替换文本放在诊断属性里，`src/Rune.CodeFixes` 的代码修复照它替换（IDE 灯泡、`dotnet format analyzers --diagnostics RUNE006`；.razor 里改不了，手改）；
  使用方默认是建议，本仓库经 `rune.globalconfig` 提到警告。RUNE007 长度写成 px（非零数字或带 px 的字符串，边框、描边、焦点环属性除外），
  默认关闭、Rune.UI 经 `components.globalconfig` 打开：使用方的页面写 px 没问题，组件内部要随字号缩放。只看字面量，`CssWide.Inherit` 这类常量不报。
  代码修复要引用 Workspaces，单独一个程序集（生成器的程序集引用它会在命令行构建时加载失败，RS1038）。
- 新增诊断要登记在 `AnalyzerReleases.Unshipped.md`。
- 生成器与代码修复随 Rune 包发布（`analyzers/dotnet/cs`），仓库里的项目各自以分析器方式引用它们。

## 11. 构建与发布

- **端到端测试**（`tests/Rune.E2E`）：测试程序启动构建好的 Rune.Demo（另一个进程、真实的 Kestrel、Development 环境），用 Playwright 的 Chromium 打开 `/{模式}/{演示}`。
  先等 `html[data-rune-ready]`（JS 初始化器装好了），交互模式再等演示宿主的 `data-demo-interactive=true`（之前的点击不会触发 C# 的处理）。CI 只在 Linux 上跑。

- **可裁剪**：三个库都是 `IsTrimmable`（同时打开裁剪分析器，警告即错误）。不用反射能做到的改掉了（组件默认值的叠加走非泛型接口、主题配置的 JSON 用 Utf8JsonWriter 手写）；
  剩下的反射（组件参数、表单模型的属性、record 判断、事件处理器的合并）放在具名方法上，写明为什么裁剪后仍然安全。
- 库专属的设置（文档注释检查、打包信息）在 `Directory.Build.targets`：props 在项目文件之前导入，那时项目里的 `IsLibrary` 还没有值。版本号例外，写在 props（SDK 更早算版本）。
- CI 在 Linux 上打包三个库，检查生成器在 Rune 包里、Lucide 许可证在 Rune.Icons 包里，并把包作为产物留存；发布到哪里（CD）待定。

## 12. 已知限制与下一步

- 组件库样式预生成静态 CSS（减少内联 `<style>`）是优化，不改 API。
- 主题对象由 `Css.Define` 定义，热重载改主题值后，应用级配置（DI 单例）仍拿着旧主题，要重启；Provider 上直接传的主题会更新。

## 13. 表格的查询契约（`src/Rune.UI/Table/Query`）

同 vue-tsx-pro 的 `use-table-query.ts`、`columns.ts` 的排序部分。服务端分页的列表（DataTable 的 `ItemsProvider`、阶段 6 的 QueryTable）用它；
组件库不引用任何 ORM，线上的格式与 EasyQuerySharp 的 `TableRequest.Parse` 一一对应，后端一行就能解析。

- **类型**（纯数据，不依赖 Rune 的其他部分）：
  - `ColumnSort(Key, SortDirection)`，排序是几列（前面的优先）；`ColumnSort.Format` / `Parse` 是线上的写法 `-amount,name`（减号是倒序，同 JSON:API）。
  - `TablePage<TRow>(Rows, Total)`：一页结果。
  - `TableQuery<TFilters>`：页码（从 1 开始）、每页条数、排序、表头筛选（列 key → 选中的值；关键字筛选是一个值）、
    已知的总数（只翻页、换排序或条数时是上一次的总数，服务端可以不再数行数；条件变了、“查询”、刷新时为 null）、已应用的筛选条件 `TFilters`。
    vue 的第二个类型参数 Params（当前页签这类筛选条件之外的值）在这里并进 `TFilters`：C# 的 record 本来就能多放几个属性，少一个类型参数。
- **线上的格式**（`TableQuery.ToParameters(keywordColumns)`，同 vue 的 `tableParams`）：`page`、`pageSize`；有排序时 `sort`；已知总数时 `total`；
  表头筛选里多选的列写成 `filter.列=值`（可重复），关键字的列写成 `search.列=关键字`；`TFilters` 的公开属性按名字（首字母小写，同 JSON）平铺，
  空值（null、空字符串、空集合）不写，集合写成重复的参数，区间（`Start` / `End` 两个属性的值）写成两个同名参数（空的一端是空字符串）。
  值的文字按不变区域：数字不用科学计数法、日期是 ISO 8601、布尔是 true / false、枚举是名字的首字母小写。
  `page`、`pageSize`、`sort`、`total` 与 `filter.`、`search.` 开头的名字留给这里，筛选条件的属性不要用。
- **状态**（`TableQueryState<TRow, TFilters>`，同 vue 的 `useTableQuery`，不是组件，同 `RemoteItems` 的写法）：页码、条数、排序、表头筛选可读写，
  改了就请求（改条数、排序、表头筛选回到第一页）；`SearchAsync(filters)` 应用新的筛选条件、回到第一页、条件没变也重新请求并重新数总数；
  `RefreshAsync()` 按当前条件重新请求；只采用最后一次的结果，被顶掉的请求取消；删掉数据后当前页空了退到最后一页。
  给 `NavigationManager` 时页码、条数、排序、筛选条件同步到地址的查询参数（刷新、分享链接、后退回来还是这些条件），写法同线上的格式；
  页码、条数、排序由状态读回，筛选条件由页面用 `[SupplyParameterFromQuery]` 读出来作为初始的 `Filters`（vue 用 useQueryStates 的编解码器，
  Blazor 自带按名字、按类型从地址取值，不另造一套）。
- 预渲染时取的数交给交互后的渲染（`[PersistentState]`）在阶段 8 的全栈示例里定：状态对象由使用方持有，组件库只给可序列化的 `TablePage`。

## 14. 日期（`src/Rune.UI.Dates`，阶段 4）

同 vue-tsx-pro 的 `src/date`（DateField、Calendar、DatePicker……）与 `utils/data/date.ts`。单独成包：只用表格、表单的应用不带日期的代码，WebAssembly 下可以懒加载。

- **包**：`Rune.UI.Dates` 引用 `Rune.UI`，Rune.UI 用 `InternalsVisibleTo` 让它用内部的样式工具（Attrs 的层、InputStyles、ItemStyles……），
  不为了分包把这些变成公开 API。JS 放在它自己的 `wwwroot/js`，由 Rune.UI 的初始化器按选择器加载（同其他 `js/*.js`）。
- **值的类型**：日期是 `DateOnly`，时间是 `TimeOnly`，日期时间是 `DateTime`（不带时区）或 `DateTimeOffset`（带时区）。
  vue 用 @internationalized/date 的 CalendarDate / CalendarDateTime / ZonedDateTime 与 ISO 字符串，C# 直接用 BCL 的类型，不另造；
  组件的值类型是泛型（`DateField<TValue>`，同 NumberField），写回时保持传入的类型（vue 的 writeDateLike）。
- **地区设置**：月份名、星期名、日期的排法、上午 / 下午取 `CultureInfo`（按语言包的 `Code`）；一周的第一天取语言包的 `FirstDayOfWeek`，
  没有时取 `CultureInfo.DateTimeFormat.FirstDayOfWeek`；12 / 24 小时制取组件的 `HourCycle`、语言包的 `HourCycle`，都没有时按地区惯例（CLDR timeData 里惯用 12 小时制的地区，写在 `LangFormat`）：
  不看文化的时间格式，WebAssembly 的精简 ICU 没有地区的小时制数据，zh-CN 也给出“下午2:30”。时刻（DateTimeOffset、UTC 的 DateTime）按语言包的时区显示。
  日历系统只做公历（vue 的 calendar 选项：语言包的 `Calendar` 都是 gregory；别的历法等有需要再说）。
- **“现在”**：取 `TimeProvider`（DI 里有就用，没有时 `TimeProvider.System`），时区取语言包的 `TimeZone`，没有时是服务器 / 浏览器的本地时区；
  测试、截图、服务端渲染时给固定的 TimeProvider。RelativeTime、Countdown 的定时更新也走它（对齐到整秒、整分，同 vue 的 useNow）。
- **相对时间的措辞**：.NET 没有 Intl.RelativeTimeFormat，vue 的语言包也不带这些文字（靠 Intl）。`RelativeTimeText` 按 CLDR 手写 zh、en 两套
  （秒、分、时、天、周、月、年的“前 / 后”，以及“昨天、明天、上个月、去年……”这类 numeric: auto 的写法；别的语言用英文），
  不往生成的 Lang 里加必填成员；单位的取法同 vue 的 relativeUnit（45 秒以内用秒，45 分钟以内用分……）。
- **分段输入**（DateField、TimeField 的年、月、日、时、分……）：状态机在 JS（`date-segments.js`，移植 vue 的 date-segments.ts），
  每段是 role="spinbutton"，打字、↑↓、Page Up / Down、Backspace、段之间的焦点都不往返；所有段填齐（或删掉已填的一段）时把 ISO 文字交给 C#，
  C# 解析成 TValue 写回。段的顺序、分隔符、占位文字、段名由 C# 按文化算好渲染出来（JS 不再查 Intl，两边结果一致）。
  JS 只改 Blazor 渲染的属性与文字节点本身（不换节点）：Blazor 只在自己的输出变了时写 DOM，写回了（值变了、晚到的渲染）就按草稿重画；
  `data-value` 是自己刚交出去的值时草稿留着（删掉一段后其余段还在），不是时以它为准。DateField、TimeField 继承抽象的 `SegmentedField<TValue>`。
  JS 由 Rune.UI 的初始化器在第一次遇到 `[data-rune-date-segments]` 时动态 import（`../../Rune.UI.Dates/js/…`），不用日期组件的页面不下载。
- **日历网格**：状态（视图、焦点日期、页、区间的开始）在 C# 的 `CalendarState`（移植 calendar-state.ts）；`js/calendar.js` 按键算出要去的日期，
  在这一页里就当场移焦点，再交给 C#（rune-command focus），翻页、夹到可选范围由 C# 定，渲染后焦点跟到 `[data-focused]`。
  点格子是一条 activate 命令（几百个格子不各挂委托）。区间选到一半时的悬停预览在 JS 里画（`data-preview*`，C# 不写这几个属性），没有往返。
  格子的状态都写成 data-*，样式跟着属性走。月份、年份视图里聚焦某格时保留原来的日（vue 点击时会变成 1 日，键盘不会；这里两者一致）。
  一周的第一天同小时制，按 CLDR weekData（`LangFormat.FirstDayOfWeek`），不取 CultureInfo 的。
- **选择器**（DatePicker，之后的 DateRangePicker、TimePicker）：继承输入框（DatePicker 继承 DateField），外壳里加打开按钮（popovertarget）、外壳后面接弹层；
  弹层是 role="dialog" 的原生 popover（贴着外壳定位，开关经 rune-toggle 报给 C#，内容只在打开时渲染），打开后焦点进日历当前那一格
  （overlays.js 认 `[role=grid] [tabindex=0]`）；分段里 Alt + ↓ 点打开按钮。
    Intl 选项之外的日期写法用 `LangFormat.Date(value, lang, "M月d日 ddd")`（.NET 的格式，文化与时区随语言）。

## 15. 图表（`src/Rune.Charts`，阶段 5）

同 vue-tsx-pro 的 `src/charts`。单独成包（同第 14 节的日期）：只用表格、表单的应用不带图表的代码，WebAssembly 下可以懒加载。

- **底座**（`Foundation/`，纯函数，与 vue 逐字相同的输出）：比例尺与刻度 `ChartScale`、`LinearScale`（d3 的 ticks / nice / timeTicks）、
  双数值轴的对齐 `AxisTicks.Aligned`、路径 `ChartShapes`（折线、单调平滑、阶梯、面积、圆角柱、扇区、弦、桑基的带子）、堆叠 `ChartStack`、
  颜色 `ChartColor`（色板 `ChartColors.Series1–8`、语义色、任意 CSS 颜色）、文字宽度估算 `ChartText`、标签避让 `LabelLayout`。
  SVG 里的数字都经 `SvgFormat.Number`（Rune.UI 内部，Gauge、Sparkline 共用）：三位小数、JS 的 Math.round 与数字写法，服务端、浏览器、vue 版一致。
  单测的期望值抄 vue 的 `.test/*.test.ts`。
- **谁画**：C# 按比例尺算好整张 SVG（坐标轴、网格、图形、标注都在渲染树里）。绘图区的宽高要量：
  `charts.js` 用 ResizeObserver 量出绘图区的内容尺寸、IntersectionObserver 等它进入视口附近（同 vue 的 usePlotSeen，打印前全部画上），
  再以 rune-command `size`（"宽,高"，取整，变了才发）交给 C#，C# 按尺寸重排。量到之前绘图区只占高度、不画（同 vue：服务端渲染时不画图形）；
  给读屏的数据表、图例、单位不依赖尺寸，静态 SSR 也有。
- **悬停不往返**：十字线、悬停的浅底、各系列的圆点、提示框、散点图的最近点、环形图的高亮都在 JS 里。
  C# 在绘图区上写好 JS 要的数据（各行在类目轴上的位置、各系列圆点的坐标、每行提示框的标题与格式化好的数值，JSON 放在 data-* 上），
  并渲染出空的十字线、圆点、提示框骨架与 `<template>` 原型行；JS 只改属性（位置、hidden）与提示框容器里自己克隆的节点（Blazor 没有往那个容器里渲染子节点）。
  Server 模式下指针移动不产生往返，WebAssembly 下也不重渲染。
- **点击下钻**：JS 按悬停的行（点在柱上时按那根柱的 `data-index`）发 rune-command `activate`，C# 调 `OnItemClick`。
  图例的显示 / 隐藏、缩放滑块、导出菜单是普通的 Blazor 组件（按钮、Slider、Menu），会往返。
- **组件结构**：`ChartFrame`（外层 figure、单位与图例、加载与空状态、读屏数据表、导出菜单）公开，自定义图表也能用。
  所有图表继承 `ChartBase`（量尺寸的握手、ChartFrame 接线、共有的参数、提示框 JSON 与原型），只给出 `Draw()`（绘图区内容与悬停数据）、图例、数据表。
  直角坐标图表再共用 `XYChartBase<TItem>`（比例尺、坐标轴 `ChartAxis`、目标线、标注、区间、缩放、提示框数据，同 vue 的 XYChartKind 的各个开关）；
  `XYChart<TItem>` 加上按行的数据参数（Items、Category、Series、图例、下钻），Line、Area、Bar、Combo、Scatter 继承它；瀑布图的数据是各步，直接继承 XYChartBase。
  按项悬停的图（环形、雷达、矩形树图、桑基、弦图、网络图、时间条、地图）用 charts.js 的 item 模式：指针下的 data-index 写 data-active，
  其余写 data-dimmed（可以只在悬停某一种项时，data-dim-on），groups 给出一起高亮的项（节点连着的带子）。
  网络图、地图的平移缩放在 charts.js 里：`g[data-pan-zoom]` 整个缩放，`data-follow-x/y` 的元素（地点、名字）只跟着移动。
- **地理数据**单独成包 `Rune.Charts.Maps`（内嵌 TopoJSON，`ChartMaps.ChinaProvinces`、`WorldCountries`）：只用图表的应用不带它。
- **系列与值**：`ChartSeries<TItem>(Key, Label, Value)`，`Value` 是 `Func<TItem, decimal?>`（同 Sparkline、NumberFormat：金额不经过浮点，
  提示框、数据表按原值格式化）；画图时转成 double。类目是 `Func<TItem, object?>`，显示文字默认 `ToString()`，可另给 `CategoryText`；
  标注、区间的位置按类目的值相等（`Equals`）匹配。
- **样式**：同 vue 的 chartStyles / chartSvgStyles，颜色、字号走主题 tokens；系列颜色是 CSS 变量，换主题不用重画。
  进场动画（淡入、柱子长出来）用 `@starting-style`，“减少动态效果”时时长为 0。
