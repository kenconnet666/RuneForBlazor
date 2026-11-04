# Rune for Blazor

基于 .NET 10 Blazor 的组件库底座：容易理解的响应式状态、可以逐层覆盖的属性合并，以及与属性一起管理的强类型运行时样式。
设计思路来自姊妹项目 [vue-tsx-pro](../../IdeaProjects/vue-tsx-pro)，按 Blazor 与 C# 的特点重新设计，取舍见 [docs/DESIGN.md](docs/DESIGN.md)，
接下来的计划见 [docs/ROADMAP.md](docs/ROADMAP.md)。

```razor
@* 组件：状态、双向绑定、部件样式与使用方覆盖 *@
<Collapsible @bind-Open="filtersOpen" Variant="CollapsibleVariant.Outline"
             PropsTrigger="@(new() { Sx = new Style { [When.Expanded] = new() { Color = Tones.Accent.Text } } })">
    <Title>筛选条件</Title>
    <ChildContent>…</ChildContent>
</Collapsible>
```

```csharp
// 共享状态：普通类里的 [State]，读写像普通属性，组件只在读过的值变化时重新渲染
public sealed partial class CartStore
{
    [State] public partial int Count { get; set; }
}
```

## 结构

| 路径 | 说明 |
| --- | --- |
| `src/Rune` | 运行时：信号（`Reactivity/`）、组件基类与双向绑定（`Components/`）、属性合并（`Attrs/`）、样式引擎（`Styles/`：值类型、样式定义、编译引擎、生成的属性表各一个子目录） |
| `src/Rune.Generators` | 源代码生成器：`[State]` 属性的实现与 RUNE 诊断（netstandard2.0） |
| `src/Rune.UI` | 组件库：tokens、色调与内置主题（`Theme/`）、配置层与 `RuneProvider`（`Configuration/`）、配方与部件约定（`Parts/`）、组件（`Components/{分类}/{组件}/`，分类同 vue-tsx-pro 的文档分组，分类共用的代码放在分类这一层）、JS 初始化器（`wwwroot/`） |
| `src/Rune.Icons` | Lucide 图标全集（`Lucide.Plus`……，首次访问时才创建，WebAssembly 发布时裁剪未用到的） |
| `apps/Rune.Demo`、`apps/Rune.Demo.Client` | 演示项目：开发组件时实际运行、观察。每个演示都能以静态 SSR、Server、WebAssembly、Auto 打开（`/server/collapsible`），新增演示只要在 `Demos/{分类}/` 放一个带 `[ComponentDemo]` 的组件。文档站等组件库成熟后用自己的组件来做 |
| `tests/Rune.Tests` | 运行时、组件（bUnit）、生成器、并发的测试 |
| `tools/css-gen` | 从 vue-tsx-pro 的 CSS 数据生成 `Style` 的属性表与关键字常量（带中文说明） |
| `tools/theme-gen` | 从 vue-tsx-pro 的主题生成 16 套内置主题（`Themes.g.cs`）；CI 校验 |
| `tools/icon-gen` | 下载 Lucide 官方 SVG（`lucide-static`，版本固定在脚本里）生成图标数据：全集、组件库内置的一小组与许可文件；CI 校验 |
| `tools/lang-gen` | 从 vue-tsx-pro 的语言包生成 `Lang`（简体中文、美国英语，插值文案翻译成 C# 的 lambda）；CI 校验 |
| `tools/parity` | 与 vue-tsx-pro 文档站逐个部件对比计算后的样式（亮、暗主题、悬停聚焦按下），本地跑，见其 README |

依赖方向：`Rune.UI → Rune`，`apps → Rune.UI + Rune`；生成器以分析器方式引用。包版本统一在 `Directory.Packages.props`。

## 使用

```csharp
// Program.cs（服务端与 WebAssembly 项目各一次，配置要一致）
builder.Services.AddRuneUI(o => o.Defaults = new() { new CollapsibleDefaults { Variant = CollapsibleVariant.Outline } });
```

```razor
@* App.razor 的 head：服务端渲染时用到的样式规则（含主题） *@
<RuneStyles />

@* 布局：套上应用级主题；局部可以再嵌套 RuneProvider 修改主题、默认值、样式定制 *@
<RuneProvider>@Body</RuneProvider>
```

- 组件继承 `RuneComponent`（组件库里继承 `UIComponent`，能读配置）；`[State]` 写在 `.razor.cs` 里。
- 每个交互根（带 `@rendermode` 的页面或组件）里放一个 `<RuneStyles />`；流式渲染的页面在末尾也放一个。
- 用消息提示时在交互区域里（通常是布局）放一个 `<Toaster />`，注入 `ToastService` 调用 `Show`。
- 样式写成静态属性 `Css.Define(static () => new Style { … })`，不要在渲染里新建 `Style`（分析器 RUNE004、RUNE005 会提示）；关键字用常量（`Display.Flex`），一直在变的值用 `CssVar<T>`。

## 命令

需要 .NET SDK 10.0.400（见 `global.json`）与 Node 24（只有 css-gen 需要）。

```bash
dotnet build RuneForBlazor.slnx
dotnet test --project tests/Rune.Tests/Rune.Tests.csproj
dotnet watch --project apps/Rune.Demo    # 演示项目，保存后热重载
node tools/css-gen/generate.mjs           # 重新生成 Style 属性表
node tools/css-gen/generate.mjs --check   # 只检查是否与数据源一致
```
