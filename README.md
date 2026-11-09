# Rune for Blazor

基于 .NET 10 Blazor 的组件库底座：容易理解的响应式状态、可以逐层覆盖的属性合并，以及与属性一起管理的强类型运行时样式。
设计思路来自姊妹项目 [vue-tsx-pro](https://github.com/kenconnet666/vue-tsx-pro)，按 Blazor 与 C# 的特点重新设计，取舍见 [docs/DESIGN.md](https://github.com/kenconnet666/RuneForBlazor/blob/master/docs/DESIGN.md)，
接下来的计划见 [docs/ROADMAP.md](https://github.com/kenconnet666/RuneForBlazor/blob/master/docs/ROADMAP.md)。

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

## 安装

```bash
dotnet add package RuneForBlazor.UI
```

| 包 | 内容 |
| --- | --- |
| `RuneForBlazor` | 运行时：信号与 `[State]` 生成器、组件基类、属性合并、样式引擎（RuneForBlazor.UI 已依赖它） |
| `RuneForBlazor.UI` | 组件库（`builder.Services.AddRuneUI()`） |
| `RuneForBlazor.UI.Dates` | 日期组件 |
| `RuneForBlazor.Charts`、`RuneForBlazor.Charts.Maps` | 图表；地图的地理数据 |
| `RuneForBlazor.Icons` | Lucide 图标全集 |
| `RuneForBlazor.UI.Area` | 省市区数据 |

包名带 RuneForBlazor 前缀（nuget.org 上的 Rune 是别人的包），程序集与命名空间仍是 `Rune`、`Rune.UI`……。

## 结构

| 路径 | 说明 |
| --- | --- |
| `src/Rune` | 运行时：信号（`Reactivity/`）、组件基类与双向绑定（`Components/`）、属性合并（`Attrs/`）、样式引擎（`Styles/`：值类型、样式定义、编译引擎、生成的属性表各一个子目录） |
| `src/Rune.Generators` | 源代码生成器：`[State]` 属性的实现与 RUNE 诊断（netstandard2.0） |
| `src/Rune.UI` | 组件库，目录同 vue-tsx-pro 的 `packages/ui/src`：组件按分类放在顶层（`Base/`、`Input/`、`Overlay/`……，每个组件一个文件夹，分类共用的代码放在分类这一层）、配置层与 `RuneProvider`、token 与内置主题、语言包（`Config/`）、配方与共用样式、数据与 DOM 工具（`Utils/`）、JS 初始化器（`wwwroot/`） |
| `src/Rune.Icons` | Lucide 图标全集（`Lucide.Plus`……，首次访问时才创建，WebAssembly 发布时裁剪未用到的） |
| `apps/Rune.Docs.Content` | 文档内容：指南页、组件页（`Components/{分类}/{组件}/`，一个示例一个文件，源码嵌进程序集显示在示例卡片里）、站点部件（示例卡片、API 表、目录） |
| `apps/Rune.Docs`、`apps/Rune.Docs.Client` | 文档站：AppShell 外壳、搜索、本页目录；整站 WebAssembly（预渲染 HTML，可以导出成静态文件） |
| `apps/Rune.Demo`、`apps/Rune.Demo.Client` | 渲染模式测试台：文档内容的每一页都能以静态 SSR、Server、WebAssembly、Auto 打开（`/server/collapsible`），端到端测试与对照工具用它 |
| `tests/Rune.Tests` | 运行时、组件（bUnit）、生成器、并发的测试 |
| `tests/Rune.E2E` | 端到端测试：启动演示项目，用 Playwright 的 Chromium 在静态 SSR、Server、WebAssembly 下验证键盘、焦点与 JS 初始化器的行为（第一次运行时下载 Chromium） |
| `tools/css-gen` | 从 vue-tsx-pro 的 CSS 数据生成 `Style` 的属性表与关键字常量（带中文说明） |
| `tools/theme-gen` | 从 vue-tsx-pro 的主题生成 16 套内置主题（`Themes.g.cs`）；CI 校验 |
| `tools/icon-gen` | 下载 Lucide 官方 SVG（`lucide-static`，版本固定在脚本里）生成图标数据：全集、组件库内置的一小组与许可文件；CI 校验 |
| `tools/lang-gen` | 从 vue-tsx-pro 的语言包生成 `Lang`（简体中文、美国英语，插值文案翻译成 C# 的 lambda）；CI 校验 |
| `tools/docs-export` | 把文档站导出成静态文件（发布、预渲染全部页面、按端点清单复制带指纹的静态资源），`--serve` 按 Cloudflare 的规则本地预览；部署见 `.github/workflows/docs.yml`（手动触发） |
| `tools/parity` | 与 vue-tsx-pro 文档站逐个部件对比计算后的样式（亮、暗主题、悬停聚焦按下），本地跑，见其 README |

依赖方向：`Rune.UI → Rune`，`apps → Rune.UI + Rune`（文档站与测试台都引用 `Rune.Docs.Content`）；生成器以分析器方式引用。包版本统一在 `Directory.Packages.props`。

## 使用

```csharp
// Program.cs（服务端与 WebAssembly 项目各一次，配置要一致）
builder.Services.AddRuneUI(o => o.Components = new() { new ComponentConfig<CardDefaults, CardPart> { Defaults = new() { Variant = CardVariant.Outline } } });
```

```razor
@* App.razor 的 head：服务端渲染时用到的样式规则（含主题） *@
<RuneStyles />

@* 布局：套上应用级主题；局部可以再嵌套 RuneProvider 修改主题、组件的定制（Components） *@
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
dotnet watch --project apps/Rune.Docs    # 文档站（http://localhost:5310），保存后热重载
dotnet watch --project apps/Rune.Demo    # 渲染模式测试台
node tools/docs-export/export.mjs         # 导出文档站到 artifacts/docs-site（--serve 预览）
node tools/css-gen/generate.mjs           # 重新生成 Style 属性表
node tools/css-gen/generate.mjs --check   # 只检查是否与数据源一致
```
