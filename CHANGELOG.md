# Changelog

格式按 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号按 [语义化版本](https://semver.org/lang/zh-CN/)。
1.0 之前次版本号的升级可能带破坏性改动，会写在对应版本的“破坏性改动”里。发版见 `.github/workflows/release.yml`：
推送 `v` 开头的 tag（与 `Directory.Build.props` 的 `VersionPrefix` 一致、这里有对应的一节），CI 通过后推送到 nuget.org。

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

[0.14.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.14.0
[0.13.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.13.0
[0.12.1]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.12.1
[0.12.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.12.0
