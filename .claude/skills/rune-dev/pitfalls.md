# 踩过的坑

模块里的约定见 `docs/DESIGN.md` 的对应节；这里是写组件、写测试时反复遇到的细节。

## JS 与渲染模式

- 键盘尽量放 JS（`RuneForBlazor.UI.lib.module.js` 的事件委托 + `js/*.js` 模块），C# 只在需要状态时经 `rune-command` 收命令，避免 Server 模式每次按键往返。
- Server 模式下 JS 要等 C# 渲染的元素时用 requestAnimationFrame 轮询（cascader.js 的 whenRendered），不要假设同步出现。
- JS 改 C# 渲染的内容时只改属性与文字节点本身（`nodeValue`），不换节点：Blazor 只在自己的输出变了时写 DOM（date-segments.js）。
- 受控浮层（data-open）的开关回响：只在 C# 的状态会变时排队；收到回响只消费、不照着开关。C# 自己关上也会触发原生 toggle，那次没有回响可等，排进去会让队列错位、浮层来回振荡。
- 偶发的端到端失败先用 Playwright 脚本在并发负载下复现（4 个上下文并行、重复 24 次），记下属性与事件的先后再改，不要只改测试。
- 包 ID 改了，静态资源路径、初始化器文件名、跨包的 import 都要跟着改（见 SKILL.md 的“发版”）。
- 文档站的图表、地图数据、省市区程序集按页面懒加载（`apps/Rune.Docs.Client/LazyAssemblies.cs`）：图表页以外不能用到 Rune.Charts / Rune.Charts.Maps 的类型，
  Cascader 页以外不能用到 Rune.UI.Area 的（`LazyAssemblyTests` 检查编译出的代码）。新页面要用它们时先改 LazyAssemblies。
- 文档页的示例源码、API 表经 `DocsData` 取（服务端 Rune.Docs.Server 生成，浏览器下载 `/_docs/…`）：内容项目里不要再嵌资源、不要在浏览器里反射 Lucide 全集。
- 新加的自定义事件参数类型（`[EventHandler]`）要在 `AddRuneUI` 上加 `[DynamicDependency]`：Blazor 按反射从 JSON 反序列化它，WebAssembly 裁剪后没有构造函数，
  事件全部失效（DeserializeNoConstructor）。端到端测试跑的是没裁剪的 Demo 测不出来，要用 `node tools/docs-export/export.mjs --preview`（5320）看裁剪后的效果。

## Razor 与组件

- 给子组件的样式经 `Attrs.MergeProps`（作为 Sx 参数交下去）；把带 Sx 的 Attrs 直接 `@attributes` 给组件，Sx 会变成同层的普通类、被组件自己的样式盖住（分析器 RUNE010 会报）。原生元素才用 `Attrs.Merge`。
- 组件转发参数给子组件时要显式声明：未声明的属性以字符串落到子组件，类型不对会抛异常。
- 按名字传参数（`AddComponentParameter(…, "Sx", …)`）不经过隐式转换：Style 要先包成 Sx。
- 要父级重新渲染的回调用 EventCallback（不用 Func）；需要返回结果时写在参数对象上（`CellChange.Reject`）。
- RenderFragment 与 string 放进同一个三元表达式会渲染出类型名：用 if / else。
- Razor 标记里的字面量 @ 写成 `@@`；带引号的 C# 表达式不要直接写在属性值里，挪到 @code 的属性上。
- Razor 生成的类里用不了 Regex 的源生成器（生成器之间看不到彼此的输出）：用静态的 `Regex` 实例。
- 列是表格的子组件（同 QuickGrid），套在别的组件里的列收集不到；列来自数据时用 `@foreach`。
- Memo 缓存派生值时，键里不能有每次新建的列表，否则每次渲染都重算。
- 小时制、地区惯例不要从 CultureInfo 的时间格式推：WebAssembly 的精简 ICU 没有地区数据（`LangFormat.TwelveHour` 按 CLDR timeData）。
- 示例里的“现在”用 `DemoClock`，组件用 `UIComponent.TimeSource / LocalNow / LocalToday`，不直接读 `DateTime.Now`。
- 分析器：RUNE006（用类型化常量）、RUNE007（Rune.UI 里不写 px）、RUNE008（lambda 不捕获循环变量）、RUNE010（带样式的 Attrs 不直接展开给组件）。

## 测试

- bUnit：`InvokeAsync(() => x = SomeTaskReturningCall())` 会等那个 Task，要写成语句体 `{ x = ...; }`。
- 期望值别心算：总数、合计按示例的数据算清楚再写。
- 端到端点不到的元素先 `ScrollIntoViewIfNeededAsync`：文档页合并后示例可能在视口外。

## 工具

- Git Bash 里多行 heredoc、带反斜杠的 awk / 正则容易被改坏：用 Write 工具写脚本文件，或用 `index()` 这类不需要转义的写法。
- `set -o pipefail` 下 `grep -q` 提前退出会让上游收到 SIGPIPE（退出码 141）：改成 `grep … >/dev/null`。
