# Changelog

格式按 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号按 [语义化版本](https://semver.org/lang/zh-CN/)。
1.0 之前次版本号的升级可能带破坏性改动，会写在对应版本的“破坏性改动”里。发版见 `.github/workflows/release.yml`：
推送 `v` 开头的 tag（与 `Directory.Build.props` 的 `VersionPrefix` 一致、这里有对应的一节），CI 通过后推送到 nuget.org。

## [Unreleased]

对齐 vue-tsx-pro 0.26.0（`9afdc156..c42b2f39`，0.22.0–0.26.0）：它审查出的问题逐项在 Rune 里对照，同样有的一并修掉；新增的组件、参数照着补上。

### 破坏性改动

- **Select、Cascader、TreeSelect、Transfer、JsonViewer 与表头筛选里的搜索框**换成了 SearchField：现在有清空按钮；JsonViewer 的搜索改成边输边过滤。
  表头的关键字筛选停手 300ms 才生效（回车、清空立即生效），服务端筛选时不再每敲一个字发一次请求
- **RuneProvider** 只在语言是配置出来的时候才往包裹元素上写 `lang`、`dir`（AddRuneUI 写了 `Lang` / `Languages`，或某一层写了 `Lang`）；
  没配置时沿用宿主页面自己的，不再盖成默认的中文、从左到右
- **Steps** `Navigable` 时出错的步骤也能点回去；**TagsInput** 允许自己输入（默认）时打字不再自动高亮第一条建议：回车加的是打的字，
  要选建议按 ↓ 再回车（`AllowCustomValue="false"` 时照旧）
- **Blockquote** 的部件类型从 `InlinePart` 换成 `BlockquotePart`（多了出处那一行）；**Descriptions** 没有值的项显示占位符（`—`）
- **NumberField** 的百分比格式（`Format="P0"`、自定义格式里带 `%`）编辑时输入的是百分数：显示 `85%`，输入 `90` 得到 `0.9`
  （之前输入 `90` 被夹成 `100%`，方向键也按值本身加减）。带着百分号输入（`45 %`）也认。原来按小数输入的用法（输入 `0.9`）改成输入 `90`
- **TableHeaderCell** 在表体、表尾里默认 `scope="row"`（之前都是 `col`）；要列标题时写 `Scope="col"`
- **ImageCropper** 的图片默认带 `crossorigin="anonymous"`（别的域名的图不按跨域取的话 canvas 导不出来）：对方不允许跨域的图不再显示，
  要照旧显示（不导出）用 `PropsImage` 改掉

### 改进

- 属性合并（`Attrs.Merge` / `MergeProps` / `Layer`，所有组件的 Rest 与 `PropsXxx` 都经过它）：
  - 值是 `null` 的属性不表态：`title="@error"`、`aria-label="@label"` 的值是 `null` 时不再盖掉组件的默认值
  - `aria-describedby` 各层的 id 拼在一起、去掉重复的：控件在 Field 里时，自己写的提示与 Field 的说明、错误都念得到
  - 嵌套的部件属性（值是 `Attrs` 的 `PropsXxx`，如 `PropsPrevious` 里的 `PropsIcon`）与组件给这个子部件的默认合并，不再整个替换
- TextLink、LinkButton、IconLinkButton 的 `External`：`target`、`rel` 是默认值，可以改（`rel="noopener noreferrer nofollow"`）
- NumberRangeField 两个框的名称与占位在默认层：`PropsStart` / `PropsEnd` 的 `aria-label`、`placeholder` 生效
- Field：控件自己写了 `id` 时，标签的 `for` 与错误汇总的链接跟着指过去
- Rating 只读时名称里总有分数：自己的 `aria-label` 或 Field 的标签后面接上“4 星，满分 5 星”（之前在 Field 里只念标签，写了 `aria-label` 又被分数盖掉）
- Checklist 勾选按钮的可点区域不小于 24px（WCAG 2.5.8）
- Textarea 自适应高度：宽度变了也重新算（容器变窄多换了行；一开始在没显示的标签页、弹层里）
- PaginationBar 的 `Size` 也作用于每页条数的下拉（PageSizeSelect 新增 `Size`）
- 输入法组合中不改写输入框：验证码（OtpField）与分组显示的输入框（TextField 的 `Format`）等组合结束才过滤、重排
- 文档站与 vue 文档站核对了一遍：新增指南“Choosing 选组件”“Accessibility 无障碍”；10 页示例的排布、7 页在侧栏里的顺序、若干示例的 id 与标题、
  9 页的简介改成一致；二维码页的地址改成 `/components/qrcode`（同 vue，旧地址 `/components/qr-code` 永久跳转）
- 文档站的加载（数字见 PERFORMANCE.md 第 3 节）：Rune 的 JS 模块在页面头里预加载，页面一显示交互就能用，不再等 .NET 启动；
  带哈希的静态文件整个目录长期缓存，再次打开不逐个回源确认；地图数据只在 MapChart 页下载；导出时生成 sitemap.xml
- 几层各有样式的元素（部件的默认样式加上使用方的 `Sx` 或组件管理的样式）合并属性时，分段样式的编译结果按样式实例缓存，不再每次渲染都重算：
  WebAssembly 里元素多的页面第一次渲染快一到两成（DataTable 的文档页 8.5 秒 → 6.8 秒）
- 文档站打开第一页时 WebAssembly 分批接管：示例一节一节换成交互的，没轮到的先留着预渲染的内容，不再整页冻住几秒
  （DataTable 页最长的卡顿 7.4 秒 → 1.6 秒，外壳可以操作的时刻 7.8 秒 → 2.9 秒；全部换完因为多排几遍版慢约两成）

### 新增

- **新组件**：文字 `Sub`、`Sup`（上下标，不撑高所在的行）、`Del`、`Ins`（文档的增删）、`Strike`（不再有效的内容）；`Abbr`（缩写，全称在提示里，
  悬停、键盘聚焦、触屏点按都看得到）；`Figure`（带说明的插图）；`AspectRatio`（定比例的框）；`Embed`（iframe：定比例、延迟加载、加载完之前是骨架）；
  `Video`（原生控制条、封面、多种格式、字幕，减少动态效果时不自动播放）；`SearchField`（搜索框：`OnSearch` 只在停手、回车、清空时调用）；
  页面组合 `PageHeader`（面包屑、返回、标题与标记、说明、操作）
- **新参数**：Image 的 `Sources`（渲染成 picture）；Progress 的 `Bands`（量表，`role="meter"`，值落在哪一段用哪一段的色调）；Blockquote 的 `Source` /
  `SourceContent` / `PropsSource`；Switch 的 `Description` / `PropsDescription`；ColumnTable 的 `Empty`（没有行时表体里一行，默认“暂无数据”）；
  Avatar 的 `Decorative`（名字已经写在旁边时对读屏隐藏，UserButton 展开时自动带上）；Pagination、PaginationBar、PageSizeSelect、PageJump 的 `Disabled`
- 语言包新增 `CopyFailed`、`FloatButtonMore`，页面组合的文案新增 `PageHeaderBack`、`NotificationsNoMatch`、`DataImportFailed`：自己写了完整语言包的要补上
- ModelViewer 的状态多了 fallback（没有 WebGL、显示的是 Fallback 内容时：不转圈、不标忙碌）
- 触屏：ContextMenu 按住 700ms 打开（iOS 的 Safari 长按不发 contextmenu）；紧凑的 NavGroup 点图标弹出子项；Splitter、ResizeHandle 能按到的范围扩到 24px 以上
- 部件属性：NavBar 的 `PropsItem` / `PropsLink` / `PropsTrigger`（各有按项给的 `PropsItemFor` / `PropsLinkFor` / `PropsTriggerFor`）与 `PropsMore`；
  QuarterCalendar 的 `PropsHeader` / `PropsHeading` / `PropsGrid` / `PropsCell` / `PropsCellButton`（后两个有按季度给的 `PropsCellFor` / `PropsCellButtonFor`）；
  DataTable 的 `PropsFilterButton` / `PropsColumnMenuButton`；ImageCropper 的 `PropsImage`
- ColumnSettings 的 `Changed`：显示与顺序之外还有别的列设置改过（固定、列宽）时“重置”也可用
- TreeDataTable 的 `MultiSort` / `Sorts` / `SortsChanged`：同级之间按几列依次排（之前交给里面的 DataTable，表头能点、行不动）

### 修复

- Collapse（连带 Collapsible、Accordion、示例的源码区）的内容区不再被里面最宽的内容撑开
- 浮层：没有箭头的 Popover 内容过高时在面板里滚；LoadingOverlay 一开始加载就拦住点击（遮罩的外观仍然延迟出现）；模态、Lightbox 很快关了再开时
  不会被上一次迟到的关闭事件关掉；ContextMenu 不抢里面已经处理过的右键
- 输入：Transfer、TreeTransfer 在只读的 Field、Form 里不能移动；ControlGroup 把 Field 的不可用、只读交给组里的控件；Upload 图片墙里失败的非图片文件
  也显示原因，只收一个文件时同一批里的坏文件不再把好文件顶掉；Listbox 只读时带 `aria-readonly`
- 展示与反馈：头像的首字母按码点取（表情不被切成半个）；CopyButton 复制失败会播报；QRCode 加载中读屏念得到；FloatButtonGroup 的主按钮默认有名称；
  CodeBlock 的复制按钮不再让长行从缝里露出来；Carousel 用指针点了翻页按钮后自动播放照常，点“播放”一定恢复；Sortable 不可用的把手变淡；
  回到顶部用完去掉临时的 tabindex；代码高亮下载失败后下次再试；AppShell 进入、退出全屏后焦点跟到对应的按钮上
- 格式化：`999,999 B` 显示 `1 MB`；AnimatedNumber 取整后是 0 的负数不带负号
- 图表：柱子、瀑布图不再冲出数值轴的范围；折线、面积图在触屏上点得动下钻；对齐的双轴全是负数时上端不再被改成 1；漏斗图不再显示“导出图片”，
  导出的图片保留镂空；从右到左的语言里坐标不再镜像；雷达图的轴按数据的最大值（不再至少是 1）；缩放的区间超出数据时夹回去；MapChart 没有任何数据时显示 `Empty`
- 无障碍：Windows 高对比（强制颜色）下日历选中的日期、区间与表格选中、正在看的行看得出来
- Popconfirm：`OnConfirm` 还在进行时被 Esc、点外面关掉，不再触发 `OnCancel`；没确认成（`KeepOpen`）才触发
- 不可用的日期分段输入按 Enter 不再提交所在的表单
- 日历（Calendar、RangeCalendar）的 `Granularity` 变了（按日改成按月）视图跟着回到新的单位；QuarterCalendar 跟随外面改的值（快捷按钮、重置）翻到那一年
- TagsInput 只读、不可用时粘贴、输入分隔符都不加标签
- Upload 不可用时“重试”也不可用
- 导出 CSV：状态列的名称与没登记的状态值、数字与日期列里混进来的字符串同样防公式（之前只防文字列）
- Breadcrumbs 的 `MaxItems` 小于 3 时不再折出空菜单（3 项、`MaxItems="2"`）
- LoadingBar 走满、正在淡出时再开始：从头走，不再从 100% 退回 90%
- Marquee 的 `role` 由组件管理，不被未声明的属性改掉
- ModelViewer：摄影棚的环境光等模型加载成功后才生成（没有显卡、软件渲染时这一步要几秒并占住页面，一页几个查看器排着做，
  地址错了的那个迟迟报不出错）；加载失败的不再生成

## [0.17.1] - 2026-11-11

只修一个桌面适配包的问题，组件的样式与交互不变（仍对齐 vue-tsx-pro 0.21.0）。

### 修复

- RuneForBlazor.Desktop.Maui：运行一段时间后窗口顶上出现两条标题栏（系统的一条加页面自己画的一条）。MAUI 在换主题、改标题时会重建自己的标题栏，
  或换掉窗口的根视图，原来只在第一个根视图上挂一次布局事件、并缓存找到的元素，之后新建的那条没人收起。
  现在缓存的元素不在视觉树里就重新找，激活时与每秒一次核对根视图有没有换，换了就重新挂接并立刻检查

## [0.17.0] - 2026-11-10

组件的样式与交互仍对齐 vue-tsx-pro 0.21.0。新增 RuneForBlazor.Three 包（3D 与 WebGL 效果），桌面应用补上窗口底边与抓手光标。

### 新增

- 新包 RuneForBlazor.Three：3D 与 WebGL 效果，同 vue-tsx-pro 的 three 一组——ThreeCanvas（自己的 three.js 场景）、EffectSurface、ShaderBackground、
  MeshGradient、AuroraBackground、ParticleField、WaveGrid、Confetti、Globe、ModelViewer；随包带 three.js 0.186.1（用到时才下载）。
  场景与动画循环在 JS，C# 只交参数，Server 模式下每帧不往返；静态 SSR、WebAssembly、桌面（WebView2）都能用。
  ThreeCanvas 的场景写在 JS 模块里（`Setup` 是模块的地址），与内置效果用同一个约定，以后加新的 3D 组件是一个场景模块加一个 Razor 壳
- 文档站加“3D · three.js”一组（5 页），按页面懒加载 Rune.Three

### 改进

- AppShell 桌面模式：窗口底边留一条与页面同色的边（6px），滚动的正文、两侧栏、滚动条在它上面结束，不再贴着任务栏（同常见的桌面应用）
- RuneForBlazor.Desktop.Maui：窗口建好后先隐身（DWM 的 cloak：照常能激活、在任务栏），内容第一次画出来再现身，
  不再先黑约 0.3 秒（WinUI 在 MAUI 的内容放进去之前画的）；最多等 2 秒

### 修复

- 桌面应用里能拖动的地方（滑块、拖动排序、表格列、图片墙、裁剪框、看大图、地球、模型）没有抓手光标：WinUI 托管的 WebView2 不认 CSS 的 grab，
  退回成箭头。抓手光标改用 Rune.UI 的 `Cursors.Grab` / `Grabbing`（CSS 变量，浏览器里照旧），桌面适配包把它们换成系统的手形
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

[Unreleased]: https://github.com/kenconnet666/RuneForBlazor/compare/v0.17.1...HEAD
[0.17.1]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.17.1
[0.17.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.17.0
[0.16.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.16.0
[0.15.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.15.0
[0.14.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.14.0
[0.13.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.13.0
[0.12.1]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.12.1
[0.12.0]: https://github.com/kenconnet666/RuneForBlazor/releases/tag/v0.12.0
