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

- **写法**：`new Style { Display = Display.Flex, Padding = Space.Md, [When.Hover] = new() { … }, [Media.Up(768)] = new() { … } }`。515 个 CSS 属性与 318 个属性的关键字常量由 `tools/css-gen` 从 vue-tsx-pro 的数据生成，带中文说明；颜色属性是 `CssColor`、长度属性是 `CssLength`，token 写错类型编译不过。数字按属性补单位（px / 不补 / ms，同 StyleX）。
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

- **配方**：`Recipe.Of(组件名, 档位组合, 定制, (档位, 部件) => 样式)`，每个部件一个 switch 分支；按档位组合缓存，统一加上按部件定制与部件标记，得到 `PartLayers<TPart>`。
- **色调**：每个色调一组颜色 token（`Tones.Danger.Soft`……），按色调变化的样式写成以它为参数的函数，每种外观只写一遍，样式里仍直接是主题 token（vue-tsx-pro 试过的“色调变量”多一层 CSS 变量、读样式时看不到实际 token，这里没有那一层）。
- **部件是本库组件时**（按钮里的图标、加载指示）用 `Attrs.MergeProps`：样式作为子组件的 `Sx` 参数交下去，子组件排在自己的样式之后只合成一个类；两个类各自的规则谁先插入谁赢，结果不可靠。
- **组件管理的属性**按状态预先建好或按输入记住（按钮只有四种组合），渲染时不分配。

## 7. 主题与配置（`src/Rune/Styles/Theme.cs`、`src/Rune.UI/Configuration`）

- **主题是 C# 对象**：token 是样式里写的 `var(--rune-…)`，主题给它们赋值（`new Theme("dark", basedOn: Light) { [Colors.Text] = "…" }`），
  `ThemeStyles.ClassFor` 生成只设变量的类。换主题只换容器上的类，组件不用重新计算样式。主题与 token 只有一份定义（取代手写的 CSS），测试检查每个 token 在内置主题里都有值。
- **作用范围**：`RuneProvider` 用 `display: contents` 的包裹元素挂主题类，不影响布局；变量沿 DOM 继承，所以静态外壳里的主题对里面的交互区域同样生效。
- **配置**：`UIConfig`（主题、亮暗、组件默认值、按部件的样式定制）。应用级的来自 `AddRuneUI`（DI，服务端与 WebAssembly 各注册一次），
  `RuneProvider` 在一片区域里叠加修改。级联值不能跨渲染模式边界，交互区域里需要局部修改时在里面再放一个 Provider。
- **默认值**：每个组件一个 record（`CollapsibleDefaults`），属性可空，`Over` 定义叠加规则；组件上写的参数 > 最近的 Provider > 应用级 > 组件自己的默认值。
- **按部件定制样式**：用组件的部件枚举（`CollapsiblePart.Trigger`）指定，写错编译不过；追加在组件样式之后、使用方的 Sx 之前，`Part.Of` 统一处理，组件不用为此写代码。
- **不让下面的组件白白重新渲染**：Provider 的参数没变时给出同一个配置对象；配置是 record，级联参数与普通参数一样比较，没变就跳过。

## 8. 生成器与分析器（`src/Rune.Generators`）

- 后备字段懒创建：热重载新增的实例字段在已有对象上是 null（字段初始化器不会再跑）。普通字段写法在 Server 上会让整个电路崩溃；.NET 10 的 WebAssembly 也接受新增实例字段（官方文档说不支持，已过时）。
- `XxxState` 的访问级别取 get 与 set 里更严格的那个，`private set` 不会被它绕过。
- `.razor` 的 `@code` 里写 `[State]`：生成器看不到 Razor 生成的代码，编译器只报 CS9248。生成器扫描 Razor SDK 交来的 `.razor` 附加文件，报 RUNE003 并指向 `.razor` 里的行（分析器在编译出错时不运行，走不通）。
- 分析器：`Style` 类型的静态只读字段、带初始化器的静态属性（RUNE004，热重载不生效）；组件渲染路径上的 `new Style`（RUNE005，每次都要重新算类名）。
  `Css.Define` 的 lambda 里、静态成员、构造函数不报。
- 新增诊断要登记在 `AnalyzerReleases.Unshipped.md`。
- 生成器随 Rune 包发布（`analyzers/dotnet/cs`），仓库里的项目各自以分析器方式引用它。

## 9. 构建与发布

- 库专属的设置（文档注释检查、打包信息）在 `Directory.Build.targets`：props 在项目文件之前导入，那时项目里的 `IsLibrary` 还没有值。版本号例外，写在 props（SDK 更早算版本）。
- CI 在 Linux 上打包两个库、检查生成器在 Rune 包里，并把包作为产物留存；发布到哪里（CD）待定。

## 10. 已知限制与下一步

- 组件库样式预生成静态 CSS（减少内联 `<style>`）是优化，不改 API。
- 主题对象由 `Css.Define` 定义，热重载改主题值后，应用级配置（DI 单例）仍拿着旧主题，要重启；Provider 上直接传的主题会更新。
