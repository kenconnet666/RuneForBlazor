# Changelog

格式按 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号按 [语义化版本](https://semver.org/lang/zh-CN/)。
1.0 之前次版本号的升级可能带破坏性改动，会写在对应版本的“破坏性改动”里。发版见 `.github/workflows/release.yml`：
推送 `v` 开头的 tag（与 `Directory.Build.props` 的 `VersionPrefix` 一致、这里有对应的一节），CI 通过后推送到 nuget.org。

## [Unreleased]

### 新增

- 新包 RuneForBlazor.Three：3D 与 WebGL 效果，同 vue-tsx-pro 的 three 一组——ThreeCanvas（自己的 three.js 场景）、EffectSurface、ShaderBackground、
  MeshGradient、AuroraBackground、ParticleField、WaveGrid、Confetti、Globe、ModelViewer；随包带 three.js 0.186.1（用到时才下载）。
  场景与动画循环在 JS，C# 只交参数，Server 模式下每帧不往返；静态 SSR、WebAssembly、桌面（WebView2）都能用。
  ThreeCanvas 的场景写在 JS 模块里（`Setup` 是模块的地址），与内置效果用同一个约定，以后加新的 3D 组件是一个场景模块加一个 Razor 壳
- 文档站加“3D · three.js”一组（5 页），按页面懒加载 Rune.Three

### 改进

- RuneForBlazor.Desktop.Maui：窗口建好后先隐身（DWM 的 cloak：照常能激活、在任务栏），内容第一次画出来再现身，
  不再先黑约 0.3 秒（WinUI 在 MAUI 的内容放进去之前画的）；最多等 2 秒

### 修复

- RuneForBlazor.Desktop.Maui：拖动窗口边框调整大小时边缘闪烁、露出窗口后面的内容：每一步大小变化都通知了窗口状态变了，
  AppShell 跟着整页重新渲染、跟不上窗口；现在只在最大化、焦点、全屏真的变了时通知

## [0.16.0] - 2026-11-10

组件的样式与交互仍对齐 vue-tsx-pro 0.21.0。这一版主要打磨桌面应用的启动与关闭。

### 修复

- 没有预渲染的交互根（Blazor Hybrid、只在客户端渲染的应用）第一屏闪一帧没有样式的页面（图标铺满窗口）：
  `RuneStyles` 在渲染器的线程上收到新登记的样式时，直接排进正在进行的这一批渲染，样式与内容一起到达页面
  （之前经 `InvokeAsync`，MAUI 的调度器总是排到下一轮）

### 改进

- RuneForBlazor.Desktop.Maui：启动时 WebView 在页面画出来之前保持透明，不再露出 WebView2 自己的灰色底（#202020）；
  关闭窗口（标题栏的关闭、Alt+F4、任务栏、`IDesktopWindow.CloseAsync`）时先隐藏窗口再拆，不再闪一帧白（亮色系统）或灰
- 桌面样板的启动：窗口一打开就是主题的底色，中间是标志（先原生画、再由页面里的启动画面接着轻轻呼吸），
  Rune 的样式到了内容淡入；自检与 CI 的“桌面”任务检查第一屏内容出来时样式已经在了

## [0.15.0] - 2026-11-10

组件的样式与交互仍对齐 vue-tsx-pro 0.21.0。新增 RuneForBlazor.Desktop.Maui 包（.NET MAUI 桌面应用，目前只支持 Windows）。

### 新增

- Blazor Hybrid（.NET MAUI、WPF、WinForms 的 BlazorWebView）可用：`ThemeBootScript` 放在根组件（Routes）的开头，
  交互渲染时只输出主题配置，Rune 的脚本看到配置后给根元素设好主题与底色（没有 Razor 的 head，插入的脚本不执行）。
  仓库里的 `apps/Rune.Desktop` 是 MAUI（Windows）的样板，窗口里是整个文档站（外壳、首页与全部页面同网站）
- 新包 RuneForBlazor.Desktop.Maui（.NET MAUI，目前只支持 Windows）：`builder.UseRuneDesktop()` 去掉系统标题栏，
  AppShell 的顶栏就是窗口的标题栏，保留 Windows 11 的窗口外框；WebView2 按桌面应用设置：页面请求全屏时窗口本身全屏，
  正式版关掉网页的右键菜单（输入框与选中文字的剪切、复制、粘贴照常）、浏览器快捷键与缩放，不显示链接网址；记住窗口的大小与位置
- AppShell 的桌面模式（注册了 `IDesktopWindow`，或给了 `Window`）：顶栏的空白处拖动窗口、双击最大化、拖到屏幕边缘贴靠，
  结束边是窗口按钮（`WindowControls`：最小化、最大化 / 还原、关闭）；顶栏矮一些（40px）、不透明，窗口失去焦点时变淡；
  整页滚动条从顶栏下面开始，侧栏的文字不被拖动选中。浏览器里打开时一切照旧。窗口按钮的名称在语言包的 `Desktop` 里

### 修复

- RadarChart 再次渲染（切换图例、改参数）时整个渲染器出错、之后页面不再更新：网格、轴、系列的 `@key` 在 svg 下的兄弟元素里重复了
- 随 RuneForBlazor.UI 分发的第三方脚本（浮层定位的 Floating UI、代码高亮的 Shiki）扩展名改为 `.js`：
  Blazor Hybrid 的静态文件不认 `.mjs`，按 `application/octet-stream` 返回，浏览器拒绝执行，浮层定位不了、代码不高亮

## [0.14.0] - 2026-11-09

组件的样式与交互仍对齐 vue-tsx-pro 0.21.0。

### 新增

- LoadingBar 的 `Startup`：预渲染出来就显示、在走（先由 CSS 动画、JS 到了接着走），交互模式（WebAssembly、Server）接管后消失。
  WebAssembly 站点下载、启动期间页面看得见却点不动，用它告诉用户还在启动

### 修复

- 能拖动的 Tree（写了 `OnMove`）点展开箭头、勾选框、行没反应：按下时就捕获了指针，松开时的 click 落在整棵树上；
  改为移动超过拖动的门槛才捕获，只是点一下的照常落在点到的元素上

## [0.13.0] - 2026-11-09

组件的样式与交互仍对齐 vue-tsx-pro 0.21.0。

### 新增

- `RuneUIOptions.ThemeStylesheet` 与 `ThemeStylesheetLink`：应用级主题（含可选主题）的变量放进按内容哈希命名的外部样式表，不再内联在每个页面里
  （十六套可选主题约 120 KB，放进样式表后浏览器只下载一次）；底层是 `ThemeStyles.Stylesheet`

### 改动

- Collapsible 的触发器、CodePreview 的“显示代码”在 JS 里当场切换（`data-rune-disclose`），再写回 C#：静态 SSR 与 WebAssembly 还在启动时也能点；
  写了 id 时 WebAssembly 接管重画后保持接管前的状态。Collapsible 的触发器不再带 `OnClick`
- CodePreview 的源码展开时才上色（JS 等内容区展开再加载 Shiki），去掉了指针移到工具栏时预先上色

## [0.12.1] - 2026-11-09

### 修复

- WebAssembly 发布（开启裁剪）后，组件库的自定义事件（`rune-command` 等）参数反序列化失败：虚拟滚动（VirtualList、DataTable、Listbox、Tree 的长列表）滚动后不再换行，
  键盘命令、粘贴、滑块、拖动排序等经 JS 交给 C# 的交互不生效。`AddRuneUI` 声明了这些参数类型的依赖，裁剪后仍保留
- Collapse 展开后宽度被里面最长的一行撑开：CodePreview 的源码看不全、没有横向滚动条

## [0.12.0] - 2026-11-09

第一次发布到 nuget.org。组件、样式与交互对齐 [vue-tsx-pro](https://github.com/kenconnet666/vue-tsx-pro) 0.21.0。

### 包

包名带 `RuneForBlazor` 前缀（nuget.org 上的 `Rune` 是别人的包），程序集与命名空间仍是 `Rune`、`Rune.UI`……

| 包 | 内容 |
| --- | --- |
| `RuneForBlazor` | 运行时：信号与 `[State]` 生成器、组件基类与双向绑定、属性合并、强类型的运行时样式引擎、RUNE 诊断与代码修复 |
| `RuneForBlazor.UI` | 组件库：基础、布局、展示、反馈、浮层、导航、输入、表单、表格、页面组合；16 套内置主题、简体中文与美国英语语言包、组件默认值与按部件定制 |
| `RuneForBlazor.UI.Dates` | 日期组件：DateField、TimeField、Calendar、DatePicker、DateRangePicker、TimePicker、CalendarBoard |
| `RuneForBlazor.Charts` | 图表：折线、面积、柱状、组合、散点、瀑布、环形、雷达、漏斗、矩形树图、桑基、和弦、关系、时间线 |
| `RuneForBlazor.Charts.Maps` | MapChart 的地理数据：中国省份与世界国家 |
| `RuneForBlazor.Icons` | Lucide 图标全集（WebAssembly 发布时裁剪未用到的） |
| `RuneForBlazor.UI.Area` | 全国省、市、区县数据（Cascader 用） |

### 能力

- 静态 SSR、Server、WebAssembly、Auto 四种渲染模式下行为一致；交互在 JS 初始化器里，C# 不用 IJSRuntime
- 每个部件带 `data-scope` / `data-part` 标记，都有 `PropsXxx` 部件属性，组合组件把里面每层的部件属性转发出去
- 组件规格与 vue-tsx-pro 的 `/spec/components.json` 逐项比对（`tests/Rune.Tests/Parity`），差异记在基线里

[Unreleased]: https://github.com/kenconnet666/RuneForBlazor/compare/v0.16.0...HEAD
[0.16.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.16.0
[0.15.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.15.0
[0.14.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.14.0
[0.13.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.13.0
[0.12.1]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.12.1
[0.12.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.12.0
