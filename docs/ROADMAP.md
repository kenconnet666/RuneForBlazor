# 路线图

先把底座铺好，再成批做组件；每批做完整（达到下面的完成标准）再进下一批。设计取舍见 [DESIGN.md](DESIGN.md)。

## 工作方式

- **在真实浏览器里做组件**：开着演示项目（`dotnet watch --project apps/Rune.Demo`）边做边看。每个组件一个演示（`Demos/` 里带 `[ComponentDemo]` 的组件），
  静态 SSR、Server、WebAssembly、Auto 四种模式都要打开看过、点过；bUnit 测试管行为，演示项目管“看起来、用起来对不对”。
- **文档站暂不做**：等组件库适当成熟后，用组件库自己的组件搭文档站（同 vue-tsx-pro 的做法），顺便检验组件的完整度。
- **懒加载与按需实例化**（每个组件都要过一遍）：
  - 对象按需创建：样式用 `Css.Define`（第一次读取时创建、之后同一个实例），`[State]`、`Bindable`、部件 id 等用 `field ??=`；
  - 内容按需渲染：收起的内容、没打开的浮层不渲染（Collapsible 的内容区已是如此），大列表用 `Virtualize`；
  - 渲染路径不分配：参数没变时复用同一个对象（`Rest`、只有默认层样式的合并结果），不在渲染里新建 `Style`、委托、集合；
  - 程序集按需加载：图表、日期这类大块做成独立程序集，WebAssembly 下用 Blazor 的懒加载程序集（`BlazorWebAssemblyLazyLoad`）在用到时才下载。
- **验证**：本地跑与改动相关的测试，全量交给 CI；推送前看一眼上一次 CI。

## 每个组件的完成标准

1. 配方样式（部件 × 档位）、部件标记、`PropsXxx` 部件属性、中文文档注释；
2. bUnit 测试：行为、aria 属性、键盘；
3. 演示项目里有贴近真实场景的演示，四种渲染模式都实测过；
4. 键盘与读屏符合 WAI-ARIA 的做法；
5. 过一遍上面的懒加载与按需实例化。

## 阶段 0：底座补齐

- [x] 演示项目：按组件 × 渲染模式打开（`/{模式}/{演示}`），目录自动发现，切换演示时保持模式
- [x] 渲染路径的分配：`Rest` 参数没变时复用，`Attrs.From` 不复制属性字典
- [x] 主题：主题是 C# 对象（`Themes.Light` / `Dark`，基于另一个主题只写不同的 token），生成只设变量的类；亮暗跟随系统或固定；
      token 补齐字号、行高、阴影、缓动、层级、尺寸；测试检查内置主题给每个 token 都定义了值（取代原来手写的 rune-ui.css）
- [x] 配置层：应用级 `AddRuneUI(o => …)` 与局部的 `<RuneProvider>`（主题、亮暗、组件默认值、按部件的样式定制，嵌套时按项叠加）；
      组件继承 `UIComponent` 读取。全局配置走 DI（服务端与 WebAssembly 各注册一次），局部覆盖走级联值；参数没变时给出同一个配置对象
- [x] 强类型 CSS：css-gen 生成 318 个属性的关键字常量（`Display.Flex`、`Position.Sticky`）；`CssVar<T>` 与 `Attrs.Vars` 承载连续变化的值
- [x] 共享样式片段：焦点环（外侧、内侧）、视觉隐藏、减少动效
- [x] 分析器：`Style` 类型的静态只读字段 / 静态属性初始化器（RUNE004）、组件渲染路径上的 `new Style`（RUNE005）
- [x] NuGet 打包：生成器放进 Rune 包的 `analyzers/dotnet/cs`、SourceLink、README、MIT；CI 打包并检查包的内容、留存产物
- [x] 样式登记表按渲染器查找：非 Rune 组件执行的内容片段（CascadingValue、Virtualize、EditForm 的 ChildContent）里也能用样式
- [x] 顺带修正：库专属的构建设置挪到 `Directory.Build.targets`（原来写在 props 里，条件永远不成立：文档注释检查、打包都没生效）
- [x] 配方 API：见阶段 1

## 阶段 1：基础组件

- [x] 按钮家族（Button、IconButton、LinkButton）：样式 × 色调写成以色调 token 为参数的函数；加载中可聚焦不可点；图标按钮必填名称
- [x] Icon、Spinner：Lucide 图标生成 C#（`Rune.Icons`，每个图标首次访问时才创建），组件库内置 16 个；关键帧动画进样式引擎
- [x] 配方 API 定型（`Recipe.Of` + `PartLayers<TPart>`，Collapsible、Button、Badge 三处用到）；部件是本库组件时样式作为 Sx 交下去（`Attrs.MergeProps`）
- [x] 文字：TextSpan、Paragraph、H1–H6（一个元素一个组件，共用一个配方）、TextLink（外链新窗口与读屏提示）、Kbd、VisuallyHidden。
      不叫 Text / Link：Razor 在代码块里把 `<text>` 当作自己的标记，`<link>` 是 HTML 空元素，两者都写不了内容
- [x] Badge、Tag（可移除）
- [x] 色调 token（6 个色调 × 7 种用途）、字号与圆角档位
- [x] 修正：内容片段（RenderFragment）参数不再按委托相等跳过渲染（演示项目里计数按钮的文字不更新，实测发现）
- [x] 布局：Flex（不另做 Stack，竖排写 Direction）、Grid（固定列数 / 按最小宽度自动换列，宽度经 CSS 变量）、GridItem；
      CSS 关键字类 `Flex`、`Grid` 改名 `FlexKeywords`、`GridKeywords` 给组件让路（css-gen 的保留名）
- [x] Card（四种样式、标题 / 自定义页头、Extra、页脚）、Alert（样式 × 色调、按色调选图标、操作、关闭、进行中、读屏播报）
- [x] 整理：组件管理的属性统一用 `Memo<TKey, T>` 按输入复用（原先各组件手写缓存，IconButton、LinkButton 每次渲染都新建）；演示目录按指定顺序分组

## 阶段 2：浮层与反馈

- [x] JS 底座：一个 JS 初始化器做全局事件委托，组件只写 data 属性、不调用 IJSRuntime（见 DESIGN 第 8 节）。
      焦点管理、点外面关闭交给原生 popover / dialog，进出场交给 `@starting-style`，锁定滚动、菜单键盘、受控开关同步在 JS 里
- [x] 样式条件：`When.PopoverOpen`、`When.Empty`、`When.Parent(…)`、`When.Starting`（@starting-style）、`Pseudo.Backdrop`
- [x] Popover、Menu（键盘、首字母、选中后关闭）、Tooltip（纯 CSS）；Popover 内容第一次打开才渲染
- [x] Dialog、Drawer（原生模态 `<dialog>`，`@bind-Open` 或 Trigger，四个方向）
- [x] Toast（`ToastService` + `Toaster`，信号驱动）、Popconfirm

## 整理（阶段 2 之后）

- [x] 图标：脚本直接下载 Lucide 官方 SVG（`lucide-static` 1.68.0，2020 个图标；改名前的 289 个旧名生成 [Obsolete] 转发），关键词写进文档注释；CI 校验生成文件
- [x] 手写的 SVG 都换成 Lucide：Spinner 是旋转的 `loader-circle`（经 Icon 组件，名称存在时 role 让给 status）
- [x] 合并重复：色调文字色（四处）→ `SharedStyles.ToneText`；无档位的配方 `Recipe.Of(组件名, 定制, 部件 => 样式)`；Icon 的手写缓存 → `Memo`
- [x] 文件归位：TextLink 归到 Typography，内置图标归到 Icon，Dialog / Drawer、TextSpan / Paragraph 各自一个文件
- [x] 演示：`DemoSection` 取代每页重复的 section + h2，横排用 Flex 组件
- [x] 对齐 vue-tsx-pro：按钮与菜单项的波纹（JS 初始化器，可按配置 / 组件关闭）；浮层进出场统一为 `OverlayTransitions`（进场慢、离场快）

## 阶段 3：输入与表单

决定：扩展 Blazor 的 `EditContext`，不另做一套（兼容 `EditForm`、DataAnnotations、FluentValidation）。设计见 DESIGN 第 9 节。

- [x] 底座：`FormControl<TValue>`（@bind-Value、接入 EditContext、aria-invalid）、`Field` + `FieldContext`（标签、说明、错误、id 关联）、共用的输入外框样式
- [x] TextField（图标、后缀、Immediate）、Textarea（高度跟着内容）
- [x] Checkbox（半选）、Switch
- [x] NumberField（任何数字类型、decimal 计算、步进按钮与方向键、格式化、全角数字）
- [x] RadioGroup + Radio（原生 radio，横排 / 竖排，以 Field 的标签为名称）
- [x] NativeSelect（原生 select 重画，按位置对应任何类型的值）
- [x] Slider（原生 range + accent-color，可选 Immediate、显示值）
- [x] `Form`：包 EditForm，默认 DataAnnotations，提交中时提交按钮加载中，错误汇总并跳到字段
- [ ] Select / Combobox（可搜索、多选，基于 Popover 与列表键盘）、ToggleGroup、范围滑块
- [ ] FormSection、标签在起始边的横排布局

## 阶段 4：导航与外壳

- [ ] Tabs、NavList、Breadcrumbs、Pagination、AppShell

## 阶段 5：数据展示

- [ ] DataTable（`Virtualize` 虚拟滚动）、Tree、Descriptions、Timeline、Empty、Result

## 阶段 6：日期

- [ ] Calendar、DatePicker、DateRangePicker、TimePicker（独立程序集，懒加载）

## 阶段 7：扩展

- [ ] 图表（SVG，独立程序集，懒加载）、页面级组合（查询表格、分步表单）

## 阶段 8：质量与发布（贯穿全程）

- [ ] 浏览器端到端测试（Playwright，跑演示项目）进 CI
- [ ] 视觉回归
- [ ] 文档站：用组件库自己的组件搭建，API 表从 XML 文档注释生成
- [ ] 发 0.x 到 NuGet；API 稳定后 1.0

## 待决

1. ~~浮层定位~~：已定为原生 `popover` + CSS 锚点定位，JS 兜底（阶段 2）
2. ~~表单~~：已定为扩展 Blazor 的 `EditContext`（阶段 3）
3. 首批组件范围：按上面的阶段顺序，还是从 vue-tsx-pro 的约 168 个组件里另挑
4. CD：NuGet 发到 nuget.org 还是私有源；文档站（含 Server 模式）部署到哪里
