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
- [ ] 配方 API：Collapsible、Button、Badge 三个组件做完后再抽通用的配方类型（第三次出现才抽象）→ 阶段 1 里做

## 阶段 1：基础组件

- [ ] 按钮家族（Button、IconButton、LinkButton）、Icon（Lucide 数据生成 C#）
- [ ] Text、Heading、Link、Kbd、VisuallyHidden
- [ ] Badge、Tag、Spinner
- [ ] 布局：Stack、Flex、Grid
- [ ] Card、Alert

## 阶段 2：浮层与反馈

- [ ] JS 互操作底座（从阶段 0 挪来：用到时再做，不凭空设计）：焦点管理、点击外部、锁定滚动、进出场（Presence）；JS 模块按需导入
- [ ] Popover、Tooltip、Menu / Dropdown（浮层第一次打开才渲染内容）
- [ ] Dialog（原生 `<dialog>`）、Drawer
- [ ] Toast、Popconfirm

## 阶段 3：输入与表单

- [ ] TextField、Textarea、NumberField
- [ ] Checkbox、Radio、Switch
- [ ] Select、Combobox（依赖阶段 2 的浮层）
- [ ] Field 与表单集成

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

1. 浮层定位：原生 `popover` + CSS anchor positioning（倾向，加一小段 JS 兜底），还是移植 Floating UI 这类 JS 定位库
2. 表单：扩展 Blazor 的 `EditContext`（倾向，兼容 `EditForm`、DataAnnotations、FluentValidation），还是另做一套
3. 首批组件范围：按上面的阶段顺序，还是从 vue-tsx-pro 的约 168 个组件里另挑
4. CD：NuGet 发到 nuget.org 还是私有源；文档站（含 Server 模式）部署到哪里
