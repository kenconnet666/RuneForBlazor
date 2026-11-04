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
- **共享状态放 scoped 服务**：静态的 `[State]` 报 RUNE001（Server 下会被所有用户共享）。

## 3. 组件基类（`src/Rune/Components`）

- `RuneComponent` 自己实现 `IComponent`，在 `BuildRenderTree` 前后开关依赖收集与样式登记；生命周期照 .NET 10 的 `ComponentBase` 复刻（Razor 只要求基类有 `protected virtual void BuildRenderTree(RenderTreeBuilder)`）。
- 内容里（`ChildContent`）读到的状态归执行它的子组件：状态变了只重新渲染子组件。
- **参数没变就跳过渲染**（`ParameterChangeDetector`）：Blazor 自己只认基本类型。这里另外认回调（`Action` 等委托，目标与方法相同就相等）、`EventCallback`、结构体、record、信号、`Style` / `Sx`。
  内容片段（`RenderFragment`）不认：它的输出取决于父级渲染时的状态，普通字段变了不会通知，按委托相等跳过会显示旧内容（演示项目里计数按钮的文字不更新，实测发现后改回）。
  其他引用类型（`List`、普通类）可能被原地修改，一律当作变了——即使引用相同（测试覆盖了这个坑）。
- **热重载时不能跳过**：热重载会把同样的参数再传一遍，必须检查 `IsRenderingOnMetadataUpdate`，否则新代码不生效。
- `Bindable<T>`：`Xxx` + `XxxChanged` 协议。给了 `XxxChanged` 是受控（父级决定是否采纳）；只给值是初始值、父级传来新值时跟着变；都不给是组件自己的状态。

## 4. 属性合并（`src/Rune/Attrs`）

`Attrs.Merge(默认层, 使用方, 受管层)`：

- 样式（`Sx`）按层叠加成一个类；class、style 依次拼接；其他属性后面的层覆盖前面的。
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
- **合成**：一个元素一个类，类名按内容哈希（服务端、浏览器、所有请求同名）。块的顺序与块内声明顺序沿用 StyleX 的优先级表：无条件块先写，简写先于长属性（探测时没排序，基础块写在 @media 后面把它盖掉了）。
- **缓存**：结果挂在第一个样式实例上，按后面跟着的实例区分；实例被回收时缓存跟着回收。所以样式要用 `Css.Define(static () => …)` 定义成静态属性，渲染时用同一批实例。
- **为什么不是 `static readonly`**：静态字段只初始化一次，热重载改了样式值不生效。写在 `Define` 的 lambda 里，热重载改的是方法体；`StyleHotReload`（`MetadataUpdateHandler`）随后清掉缓存，状态不丢。实测 Server 与 WebAssembly 都在 2 秒左右生效。
- **输出**（`RuneStyles` 组件，每条规则一个 `<style>`，只追加）：
  1. App.razor 的 head：服务端渲染与增强导航（换页时 head 被同步成新页面的那组）；
  2. 每个交互根里：交互模式下新用到的规则；
  3. 流式渲染的页面末尾：流式更新只发送标了 `[StreamRendering]` 的组件，head 里的那份收不到（探测时样式丢失，源码确认）。
- **找登记表**：合并样式是静态方法，要靠“当前作用域”找到登记表。最初只在 RuneComponent 渲染时设置，结果非 Rune 组件执行的内容片段
  （CascadingValue、Virtualize、EditForm 的 ChildContent）里一用样式就报错。现在按渲染器找：Server 与服务端渲染的每个电路、每次请求
  都在自己的同步上下文上渲染，登记表绑定到这个上下文（创建时、RuneComponent / RuneStyles 第一次渲染时绑定）；WebAssembly 单线程、只有一个作用域。

## 6. 组件约定（`src/Rune.UI`）

- **目录**：`Components/{分类}/{组件}/`，分类同 vue-tsx-pro 的文档分组（Base、Layout、Display、Feedback、Overlay、Input、Form……）；
  一个组件和它的变体、样式文件放一个文件夹（`Base/Button/` 里有 Button、IconButton、LinkButton、ButtonStyles），
  同一分类里几个组件共用的代码放进分类下的 `Shared/`（`Input/Shared/` 的 FormControl、InputStyles，`Overlay/Shared/` 的 Floating、OverlayTransitions、ConfirmEventArgs，
  `Layout/Shared/LayoutStyles`），所有组件共用的基础放 `Components/Shared/`（UIComponent、UniqueId）；
  关系紧的一组再收一层（`Input/Choice/` 里是勾选框、开关、单选与它们共用的 ChoiceStyles）。文件夹不对应命名空间，类型都在 `Rune.UI`。
  演示（`apps/Rune.Demo.Client/Demos/{分类}/`）、测试（`tests/Rune.Tests/{Runtime,UI,TestComponents}/`）同样按分类收纳。

- **配方**：`Recipe.Of(组件名, 档位组合, 定制, (档位, 部件) => 样式)`，每个部件一个 switch 分支；按档位组合缓存，统一加上按部件定制与部件标记，得到 `PartLayers<TPart>`。
- **色调**：每个色调一组颜色 token（`Tones.Danger.Soft`……），按色调变化的样式写成以它为参数的函数，每种外观只写一遍，样式里仍直接是主题 token（vue-tsx-pro 试过的“色调变量”多一层 CSS 变量、读样式时看不到实际 token，这里没有那一层）。
- **部件是本库组件时**（按钮里的图标、加载指示）用 `Attrs.MergeProps`：样式作为子组件的 `Sx` 参数交下去，子组件排在自己的样式之后只合成一个类；两个类各自的规则谁先插入谁赢，结果不可靠。
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

## 7. 主题与配置（`src/Rune/Styles/Definition/Theme.cs`、`src/Rune.UI/Theme`、`src/Rune.UI/Configuration`）

- **主题是 C# 对象**：token 是样式里写的 `var(--rune-…)`，主题给它们赋值（`new Theme("brand", basedOn: Themes.Light) { [Tones.Accent.Solid] = "…" }`），
  `ThemeStyles.ClassFor` 生成只设变量的类。换主题只换容器上的类，组件不用重新计算样式。
- **token 与内置主题同 vue-tsx-pro**：分组、命名、取值都对齐（颜色与阴影取自 Radix，间距与圆角用 em 随字号缩放，动效参考 Material 3）。
  16 套内置主题由 `tools/theme-gen` 从 vue-tsx-pro 的主题文件生成（`Themes.g.cs`），CI 校验两边一致；vue-tsx-pro 加了 token 而这边没有时脚本报错。
  中性色调不另设 token，直接用反色表面、填充、文字、边框（同 vue-tsx-pro 的 tone-styles）。本库独有的只有层级（提示、消息提示不在顶层）。
- **主题自己说是亮是暗**（`IsDark`）：容器上的 `color-scheme` 按主题写，固定用赛博朋克这类暗色主题时原生控件、滚动条也是暗色。
- **条件取值**：`[Media.ReducedMotion] = new("…") { [Durations.Md] = "0ms" }`。用户要求减少动态效果时全部时长为 0，
  用时长 token 写的过渡自动关掉，组件不再各自加“减少动效”的样式（原来的 `SharedStyles.ReducedMotion` 已删）；循环的装饰动画（加载指示）自己处理。
- **作用范围**：`RuneProvider` 用 `display: contents` 的包裹元素挂主题类，并设文字颜色、字体、字号、行高（同 vue-tsx-pro 的 Provider），不影响布局；
  变量与这些属性沿 DOM 继承，所以静态外壳里的主题对里面的交互区域同样生效。
- **配置**：`UIConfig`（主题、亮暗、组件默认值、按部件的样式定制、波纹开关）。应用级的来自 `AddRuneUI`（DI，服务端与 WebAssembly 各注册一次），
  `RuneProvider` 在一片区域里叠加修改。级联值不能跨渲染模式边界，交互区域里需要局部修改时在里面再放一个 Provider。
- **语言包**：`Lang`（同 vue-tsx-pro 的 Lang，由 `tools/lang-gen` 生成）放在 `UIConfig.Lang`，`AddRuneUI` 设应用级的、`RuneProvider` 的 `Lang` 在一片区域里换；
  组件里的文案参数（CloseLabel、ClearLabel……）不写时取语言包。Provider 的包裹元素写上 `lang` 与 `dir`。改几条用 `Lang.ZhCn with { … }`。
  vue-tsx-pro 的 toLocaleString 翻译成 `LangFormat.Number`：取不到文化数据时（WebAssembly 按需加载）用不变文化，不抛异常。
- **默认值**：每个组件一个 record（`CollapsibleDefaults`），属性可空，`Over` 定义叠加规则；组件上写的参数 > 最近的 Provider > 应用级 > 组件自己的默认值。
- **按部件定制样式**：用组件的部件枚举（`CollapsiblePart.Trigger`）指定，写错编译不过；追加在组件样式之后、使用方的 Sx 之前，`Part.Of` 统一处理，组件不用为此写代码。
- **不让下面的组件白白重新渲染**：Provider 的参数没变时给出同一个配置对象；配置是 record，级联参数与普通参数一样比较，没变就跳过。
- **共用的交互样式**：`SharedStyles.InteractionColors`（悬停、按下时颜色与阴影的过渡）、焦点环、视觉隐藏、色调文字色、波纹宿主；浮层进出场是 `OverlayTransitions`。

## 8. 浮层（`src/Rune.UI/Components/Overlay`、`wwwroot/Rune.UI.lib.module.js`）

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
  定位好之前浮层 `visibility: hidden`，所以焦点在定位之后才移进去（菜单的第一项、对话框类浮层的 autofocus 或第一个可聚焦元素）；
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
- **骨架的防闪烁用 `DelayedLoading`**（同 useDelayedLoading：200ms 后才显示，显示了至少 500ms）：骨架要整块换掉内容，只能由渲染决定，
  所以在 C# 里计时，到点后 `InvokeAsync(StateHasChanged)`（Server 模式多一次推送）；第一次参数就在加载时立即显示，静态 SSR 只有这一次。
  按钮的加载指示仍用上面的 CSS 延迟（不换内容，没有往返）。扫光（`ShimmerStyles.Wave`）画在 ::after 上用 transform 平移，各行的延迟写在行内样式上，光带继承。
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
- **消息提示**：`ToastService`（scoped）把列表放在 `State` 里，`Toaster` 读到它就随变化重新渲染，后台线程调用也安全；每条是一个 Alert。
  Toaster 要放在交互区域里（同一个电路 / WebAssembly 实例才拿得到同一个服务）。

## 9. 表单（`src/Rune.UI/Components/Input`、`src/Rune.UI/Components/Form`）

- **接入 Blazor 自己的表单**：不另做一套。控件继承 `FormControl<TValue>`：`@bind-Value` 给出的 `ValueExpression` 换成 `FieldIdentifier`，
  改值后 `NotifyFieldChanged`（DataAnnotations、FluentValidation 等照常工作），这个字段有错误时控件标 `aria-invalid`。单独使用时就是普通的受控 / 非受控控件。
- **Field**：标签、说明、错误信息，经级联的 `FieldContext` 把控件 id、aria-describedby、必填 / 不可用 / 只读交给控件（控件上显式写的优先）。
  控件把绑定的字段报给 Field（`FieldRegistration` 是信号，报上来后 Field 自动重新渲染），Field 显示表单里这个字段的错误。
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
- **NumberField 的值类型不加约束**（同 Blazor 的 InputNumber）：Razor 给泛型组件生成 `@bind` 代码时处理不了 `TValue?`（可空的值类型参数），
  会生成引用 `global::TValue` 的代码编译失败。所以 TValue 本身可以是 `int?`，Min / Max / Step 写成 decimal，计算都在 decimal 里（没有浮点误差），
  再按值的类型转回去。方向键在 JS 初始化器里处理（按输入框里现在的文字算，没有往返），改完发 change，C# 再夹到范围、对齐步长。
- **数字框**：写了 name 时提交的是隐藏 input 里的十进制文本（不带千分位与货币符号）；加减按钮按下时不抢输入框的焦点、按住连续加减（JS 初始化器的 `data-rune-repeat`），Clearable 清空成 null。
- **单选、下拉、滑块用原生元素**：键盘、读屏、手机上的系统选择器都是原生的。下拉按位置对应值（任何类型），可搜索、多选的 Select 之后基于 Popover 做。
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
- **Select**：外壳、前置图标、清空按钮、箭头是输入框的部件（`InputStyles`，`InputAppearance.Picker`），按 InputPart 定制时一起变，同 vue-tsx-pro；
  触发按钮是 `role="combobox"` 的 button，用原生 `popovertarget` 打开（静态 SSR 也能开），弹层 `data-anchor` 贴着外壳、`data-match-width` 同宽。
  开关、高亮、焦点都在 JS：关闭时 ↓ / Enter / 空格打开、↑ 打开到最后一项、单选按字母直接选中；打开后高亮已选项；单选列表点选后关闭；
  弹层关闭时焦点在里面（搜索框）就还给触发按钮；`aria-expanded` 与外壳的 `data-state` 在开关时由 JS 写（C# 收到 rune-toggle 后写同样的值）。
  搜索框也是列表的宿主（`data-rune-list="text"`，Home / End 留给输入），过滤在 C#；列表的 `data-rune-query` 变了时 JS 高亮第一个匹配项并播报 `data-rune-results`。
  多选时标签排在触发按钮前面（按钮里不能有按钮），触发按钮让出宽度、只留读屏用的文字，点外壳空白处与点它一样；Backspace 经 `data-rune-backspace`
  发 `rune-command`（不用每次按键往返）。按宽度收起标签由 JS 量（`data-rune-overflow`，同 vue-tsx-pro 的 useOverflowItems），“+N”的文字由 C# 按语言包给全（`data-rune-more`）。
  远程选项 `RemoteItems`：打开时请求、输入停下 300 ms 再请求、只采用最后一次。过滤用 `UIConfig.Matches`（按语言忽略大小写、重音与全半角，可另配 `TextMatcher`）。
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

- 库专属的设置（文档注释检查、打包信息）在 `Directory.Build.targets`：props 在项目文件之前导入，那时项目里的 `IsLibrary` 还没有值。版本号例外，写在 props（SDK 更早算版本）。
- CI 在 Linux 上打包三个库，检查生成器在 Rune 包里、Lucide 许可证在 Rune.Icons 包里，并把包作为产物留存；发布到哪里（CD）待定。

## 12. 已知限制与下一步

- 组件库样式预生成静态 CSS（减少内联 `<style>`）是优化，不改 API。
- 主题对象由 `Css.Define` 定义，热重载改主题值后，应用级配置（DI 单例）仍拿着旧主题，要重启；Provider 上直接传的主题会更新。
