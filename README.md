# Rune for Blazor

基于 .NET 10 Blazor 的组件库底座：容易理解的响应式状态、可以逐层覆盖的属性合并，以及与属性一起管理的强类型运行时样式。
设计思路来自姊妹项目 [vue-tsx-pro](../../IdeaProjects/vue-tsx-pro)，按 Blazor 与 C# 的特点重新设计，取舍见 [docs/DESIGN.md](docs/DESIGN.md)，
接下来的计划见 [docs/ROADMAP.md](docs/ROADMAP.md)。

```razor
@* 组件：状态、双向绑定、部件样式与使用方覆盖 *@
<Collapsible @bind-Open="filtersOpen" Variant="CollapsibleVariant.Outline"
             PropsTrigger="@(new() { Sx = new Style { [When.Expanded] = new() { Color = Colors.Accent } } })">
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
| `src/Rune` | 运行时：信号（`Reactivity/`）、组件基类与双向绑定（`Components/`）、属性合并（`Attrs/`）、样式引擎（`Styles/`） |
| `src/Rune.Generators` | 源代码生成器：`[State]` 属性的实现与 RUNE 诊断（netstandard2.0） |
| `src/Rune.UI` | 组件库：主题 tokens（`Theme/`）、部件约定（`Parts/`）、组件（`Components/`），默认主题在 `wwwroot/rune-ui.css` |
| `apps/Rune.Demo`、`apps/Rune.Demo.Client` | 演示项目：开发组件时实际运行、观察。每个演示都能以静态 SSR、Server、WebAssembly、Auto 打开（`/server/collapsible`），新增演示只要在 `Demos/` 放一个带 `[ComponentDemo]` 的组件。文档站等组件库成熟后用自己的组件来做 |
| `tests/Rune.Tests` | 运行时、组件（bUnit）、生成器、并发的测试 |
| `tools/css-gen` | 从 vue-tsx-pro 的 CSS 数据生成 `Style` 的属性表（带中文说明） |

依赖方向：`Rune.UI → Rune`，`apps → Rune.UI + Rune`；生成器以分析器方式引用。包版本统一在 `Directory.Packages.props`。

## 使用

```csharp
// Program.cs（服务端与 WebAssembly 项目各一次）
builder.Services.AddRune();
```

```razor
@* App.razor 的 head：默认主题与服务端渲染的样式 *@
<link rel="stylesheet" href="@Assets["_content/Rune.UI/rune-ui.css"]" />
<RuneStyles />
```

- 组件继承 `RuneComponent`（`@inherits RuneComponent`）；`[State]` 写在 `.razor.cs` 里。
- 每个交互根（带 `@rendermode` 的页面或组件）里放一个 `<RuneStyles />`；流式渲染的页面在末尾也放一个。
- 样式写成静态属性 `Css.Define(static () => new Style { … })`，不要在渲染里新建 `Style`。

## 命令

需要 .NET SDK 10.0.400（见 `global.json`）与 Node 24（只有 css-gen 需要）。

```bash
dotnet build RuneForBlazor.slnx
dotnet test --project tests/Rune.Tests/Rune.Tests.csproj
dotnet watch --project apps/Rune.Demo    # 演示项目，保存后热重载
node tools/css-gen/generate.mjs           # 重新生成 Style 属性表
node tools/css-gen/generate.mjs --check   # 只检查是否与数据源一致
```
