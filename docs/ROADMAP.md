# 路线图

目标：把 vue-tsx-pro 的组件库（`packages/ui`，约 220 个组件、文档站 138 页）移植成 Blazor 版，**外观与交互尽量与 vue-tsx-pro 一致**，
同时保留 Blazor 的写法（`@bind-Value`、EventCallback、RenderFragment、EditContext）。设计取舍见 [DESIGN.md](DESIGN.md)。

阶段按依赖排：先补对齐工具与基础设施，再按“输入 → 小组件 → 布局展示 → 浮层导航 → 表格 → 日期 → 图表 → 页面组合”推进，最后做文档站与发布准备。
每个阶段内一项一项做完整（达到完成标准）再做下一项。

## 自主执行的规矩（用户不在时照此推进）

- **小步快跑、直接推 master**：每完成一项（一个组件或一组紧密相关的组件），本地跑相关测试与生成器检查，提交并推送到 master，不开 PR、不推 tag、不发 NuGet。
  整理与功能分开提交。推送前看上一次 CI，红了先修；长命令放后台，等待时准备下一项。
- **一般不派子代理**：组件开发、审查、浏览器验证都自己做；只有需要长时间后台盯着的事（等 CI、跑长测试）放后台。
- **遇到下面的情况，这一项停下，写进本文末尾的“待决”（日期、项、卡在哪、需要决定什么），转去做下一个不依赖它的项**：
  要加“已定的决定”第 3 条以外的第三方依赖；要凭据、账号、发布；同一个 CI 失败修了两次仍不明原因；
  vue-tsx-pro 的做法在 Blazor 里做不到、又有多种合理的替代方向；需要改公共 API 且影响已完成组件的用法（小的改名、补参数不算，直接改并写进提交说明）。
- **不写过程流水账**：结论进文档（决定进本文或 DESIGN，坑进 DESIGN 的对应节），过程留在提交说明里。
- **会话上下文**：每完成一个阶段在本文勾选并写一行结论；中途接手时先读本文、DESIGN、最近的提交说明。

## 已定的决定

| #   | 事项 | 决定 |
| --- | ---- | ---- |
| 1 | 外观 | 以 vue-tsx-pro 为准：token、主题由脚本生成（已完成）；组件的配方按 vue-tsx-pro 对应组件的样式**逐条移植**（尺寸、内边距、圆角、颜色 token、状态、过渡），不凭印象重画；做不到的写进 DESIGN |
| 2 | 交互与无障碍 | 以 vue-tsx-pro 为准（键盘、焦点、aria、读屏播报、动效）；Blazor 下按 DESIGN 第 8 节的做法实现（原生元素优先，JS 初始化器做事件委托，组件只写 data 属性） |
| 3 | 依赖 | 运行时只依赖 ASP.NET Core；测试可以用 bUnit、xUnit、`Microsoft.Playwright`（端到端）；生成器脚本只用 node 内置模块与 `npm pack`。其余依赖进“待决” |
| 4 | 包 | `Rune`（运行时）、`Rune.UI`（组件，含表格）、`Rune.Icons`（Lucide 全集）；日期做成 `Rune.UI.Dates`、图表做成 `Rune.Charts`，WebAssembly 下可以懒加载 |
| 5 | 命名 | 组件名、参数名照 vue-tsx-pro（PascalCase）；与 Razor / HTML 冲突时改名并记在 DESIGN（已有：TextSpan、TextLink、NativeSelect）；`Select` 留给可搜索、多选的自绘下拉 |
| 6 | 语言 | 组件里的文字（“关闭”“请选择”“共 N 条”）进语言包（`UIConfig.Lang`），内置 zh-CN（默认）与 en-US，由脚本从 vue-tsx-pro 的语言包生成 |
| 7 | 演示 | 演示页的名字与 vue-tsx-pro 文档站的路由一致（`/components/button` ↔ `/{模式}/button`），示例的场景、顺序照文档站，方便逐页对照 |
| 8 | 不做 | vue-tsx-pro 的 code-editor、diagram、maps、three（三维与特效背景）、xlsx 模块；mobile、mini、kit、flow、fullstack 包。需要时另议（待决） |
| 9 | 文档站 | 组件基本齐了（阶段 11 之后）再用本库自己的组件搭，不提前做 |
| 10 | 发布 | 只把打包、SourceLink、README 准备好，发不发、发到哪由用户决定 |

## 与 vue-tsx-pro 对照的做法

vue-tsx-pro 的组件与本库一样在每个内部元素上写 `data-scope` / `data-part`（实测文档站的按钮页有 77 个），所以可以按部件逐个对比计算后的样式：

1. **先读后写**：做一个组件前，读 vue-tsx-pro 里的源码（`packages/ui/src/<分类>/<组件>.tsx` 与它用到的 `utils/style/*`）、文档页的示例与 `COMPONENTS.md` 的相关节，
   把配方（部件 × 档位 × 状态）逐条移植成 C#，参数照搬名字与默认值。
2. **演示对齐**：演示页照文档站的示例写（同样的场景、同样的顺序、示例上写同样的 `data-testid`）。
3. **自动对比**（阶段 4 做工具 `tools/parity`）：本地开着 vue-tsx-pro 文档站（`pnpm dev:docs`，端口 5174，`--vue` 指定仓库）与本库演示项目（5300），
   脚本用 Playwright（取自 vue-tsx-pro 的依赖）打开同一个示例，按 `(示例, data-scope, data-part, 第几个)` 配对元素，比较计算后的
   尺寸（宽高、内外边距、圆角、边框宽度）、字体（字号、字重、行高、字体族）、颜色（文字、背景、边框，解析成 rgb）、阴影、过渡与动画，
   亮、暗两套主题各一遍，输出差异报告（容差 0.5px）；并存两边的截图供人看。悬停、聚焦、按下、打开等状态由脚本触发后再比。
4. **差异处理**：报告里的差异逐条改到一致；确实不该一致的（Blazor 的结构限制、原生元素差异）在演示页注释与 DESIGN 里写原因。
   这一步是每个组件的完成标准之一。
5. **CI**：对比依赖本地的 vue-tsx-pro 文档站，不进 CI；进 CI 的是本库自己的端到端测试与截图基线（阶段 4、14）。

## 每个组件的完成标准

1. **样式对齐**：按上一节移植配方，`tools/parity` 的报告没有未解释的差异（亮、暗主题）；
2. **API**：参数、默认值、部件与 vue-tsx-pro 对应；部件标记、`PropsXxx` 部件属性、组件默认值（有外观档位时登记 `XxxDefaults`）、中文文档注释；
3. **行为测试**（bUnit）：受控、只给值、都不给三种用法与主要分支，aria 属性；
4. **浏览器**：演示页照文档站示例写，静态 SSR、Server、WebAssembly、Auto 四种模式都打开看过、点过；有自己键盘或焦点逻辑的组件有端到端用例（阶段 4 起）；
5. **无障碍**：键盘与读屏符合 WAI-ARIA 的做法（以 vue-tsx-pro 的实现为准）；
6. **性能**：过一遍懒加载与按需实例化（下节）；
7. **文档**：DESIGN 记下新的取舍与坑；本文勾选。

## 工作方式

- **在真实浏览器里做组件**：开着演示项目（`dotnet watch --project apps/Rune.Demo`），对照着 vue-tsx-pro 文档站做。
- **懒加载与按需实例化**（每个组件都要过一遍）：
  - 对象按需创建：样式用 `Css.Define`，`[State]`、`Bindable`、部件 id 等用 `field ??=`；组件管理的属性用 `Memo` 或预先建好；
  - 内容按需渲染：收起的内容、没打开的浮层不渲染，大列表用虚拟滚动；
  - 渲染路径不分配：参数没变时复用同一个对象，不在渲染里新建 `Style`、捕获 this 的委托、集合；
  - 程序集按需加载：日期、图表是独立程序集，WebAssembly 下可以 `BlazorWebAssemblyLazyLoad`。
- **验证**：本地跑与改动相关的测试与生成器检查（`--check`），全量交给 CI；推送前看一眼上一次 CI。
- **生成器**：css-gen、theme-gen 读 vue-tsx-pro（CI 固定在 `VUE_TSX_PRO_REF`），icon-gen 读 lucide-static；vue-tsx-pro 更新了主题或样式数据时重新生成并更新 `VUE_TSX_PRO_REF`。

---

## 已完成

- **阶段 0 底座**：演示项目（按组件 × 渲染模式）、主题与配置层（`AddRuneUI`、`RuneProvider`、组件默认值、按部件定制）、强类型 CSS（css-gen）、
  `CssVar`、分析器 RUNE001–005、NuGet 打包、样式登记表按渲染器查找。
- **阶段 1 基础组件**：Button / IconButton / LinkButton、Icon / Spinner、Lucide 全集（`Rune.Icons`）、配方 API、TextSpan / Paragraph / H1–H6 / TextLink / Kbd / VisuallyHidden、
  Badge / Tag、Flex / Grid / GridItem、Card、Alert。
- **阶段 2 浮层与反馈**：JS 初始化器（事件委托、受控开关同步、菜单键盘、提示的 Esc、锁定滚动、锚点定位兜底）、Popover、Menu、Tooltip、Dialog、Drawer、Toast、Popconfirm。
- **整理与对齐**：图标改从 Lucide 官方 SVG 生成；token 与 16 套内置主题由 theme-gen 从 vue-tsx-pro 生成（em 间距与圆角、时长 xs–xl 与减少动效、enter / exit 缓动）；
  `SharedStyles.InteractionColors`；按钮与菜单项的波纹；浮层统一的进出场 `OverlayTransitions`；.editorconfig 与 Rider 设置。
- **阶段 3 输入与表单（前半）**：`FormControl<TValue>`（接入 EditContext）、Field（标签可在起始边）、Form（提交中、错误汇总并跳到字段）、
  TextField、Textarea、NumberField、Checkbox、Switch、RadioGroup / Radio、NativeSelect、Slider。

---

## 阶段 4：对照工具与基础设施（后面阶段要用的先铺好）

- [x] `tools/parity`：按上面“对照的做法”第 3 条实现；README 写用法；拿已完成的组件试一遍，修掉报告里的差异
      （按钮已对齐：亮暗主题、悬停聚焦按下一致；Badge、Kbd、Card 在各自页面对照时改）
- [ ] 演示页改名对齐文档站的路由（typography → text、layout → flex / grid 等），示例按文档站补齐；已完成组件逐个过一遍对照报告
      （已对齐：button、icon-button、icon、text、badge、alert、text-field（除了 Format）、number-field（NumberRangeField 待做）、checkbox（radio 已对齐，CheckboxGroup、items、开关的 loading 与轨道内容待做）、flex（除了还没做的 ToggleGroup、Avatar、Descriptions、输入框）；card 差 loading 与 effects 示例；缺的组件做到时补示例：Descriptions、Table、Select、ButtonGroup、Avatar、ToggleGroup；toast 的完整功能在阶段 6）
- [ ] 语言包：`UIConfig.Lang` 与 `RuneProvider` 的 `Lang`；`tools/lang-gen` 从 vue-tsx-pro 的语言包生成 zh-CN、en-US；已有组件里的文字（关闭、确定、取消、增加、减少……）改成取语言包
- [ ] 端到端测试项目 `tests/Rune.E2E`（Microsoft.Playwright，Chromium）：启动演示项目，覆盖已有的键盘与焦点逻辑（菜单、对话框、气泡确认、数字框方向键、表单错误汇总跳转）；进 CI
- [ ] 列表状态与键盘（同 vue-tsx-pro 的 `list-state.ts`）：高亮项、`aria-activedescendant`、方向键 / Home / End / PageUp / PageDown、首字母、多选；Listbox 组件；后面的 Select、Combobox、Tree、Tabs、Menu 共用
- [ ] 骨架与扫光：Skeleton、SkeletonText、`ShimmerStyles`（同 `utils/style/shimmer.ts`）；延迟显示加载态（同 `useDelayedLoading`）
- [ ] JS 初始化器按需补的能力（用到时再加，每项写进 DESIGN 第 8 节）：尺寸变化（ResizeObserver → data 属性或事件）、拖动（指针捕获）、复制到剪贴板、滚动到可见、宽度与触发元素一致的浮层
- [ ] 虚拟滚动：包一层 Blazor 的 `Virtualize`，与列表状态配合（VirtualList、DataTable、长下拉要用）

## 阶段 5：输入与选择（阶段 3 后半）

- [ ] Select（单选、多选、可搜索、清空、远程选项、已选标签“+N”）、Combobox、Autocomplete、ListPopover
- [ ] TagsInput、SelectedTags、ToggleGroup、ControlGroup、CheckboxGroup（Checkbox 的 items）
- [ ] Slider 的范围（两个滑块）、刻度、竖向；Rating；OtpField；NumberRangeField；ColorField；CharacterCount（Textarea 的字数统计与清空）
- [ ] Cascader、Transfer、Mentions、FileDrop / Upload（拖放、进度、列表）
- [ ] TreeSelect（依赖阶段 7 的 Tree）、PeoplePicker（阶段 12）、ConditionBuilder（阶段 12）
- [ ] 表单：FormSection、FormErrorSummary 独立成组件、FormValue（只读展示）、Form 的离开前确认

## 阶段 6：基础与反馈补齐

- [ ] 基础：ButtonGroup（相连的按钮）、CloseButton、CopyButton、IconLinkButton、Indicator、StatusDot、Mark、Code / CodeText / CodeBlock（不带高亮，带复制）、Blockquote、List、Can（权限）
- [ ] 反馈：Banner、Empty、Result、Progress（线形、环形）、LoadingBar、LoadingOverlay
- [ ] 补齐已有组件：Toast（标题与说明、操作、进度、位置、最多条数与堆叠、悬停暂停、important）、Alert（description 与额外内容分开、关闭时淡出）、
      Card（loading 骨架、装饰效果 tilt / spotlight / beam）、Switch（loading、开关内的文字）、Menu（勾选项、单选项、子菜单、分组标题、说明）、Drawer（可拖动宽度）

## 阶段 7：布局与展示

- [ ] 布局：Container、Separator、ScrollArea、Splitter / ResizeHandle、Affix、BackTop、AsideLayout、Prose、PageScrollbar、ViewportFill
- [ ] 展示：Accordion、Collapse、Avatar / AvatarGroup、Descriptions、Ellipsis、Image（预览接 Lightbox）、ItemList、Stat、AnimatedNumber、ValueChange、
      Timeline、Tree（勾选、懒加载、拖动排序）、VirtualList、Checklist、Marquee、ProportionBar、Gauge、Sortable、Watermark、FieldCompare

## 阶段 8：浮层与导航

- [ ] 浮层：AlertDialog、ContextMenu、HoverCard、CommandPalette、Lightbox、Tour
- [ ] 导航：Tabs（溢出、可关闭、拖动排序）、Breadcrumbs、Steps、NavList、NavMenu、NavBar、Pagination / Pager / PageSizeSelect / PageJump / PaginationBar、SkipLink、TableOfContents、Toolbar

## 阶段 9：表格

- [ ] Table（静态表格的样式）、DataTable / DataTableView（排序、筛选、选择、展开、固定列、列宽拖动、虚拟滚动、骨架行）、
      ColumnHeaderActions、ColumnResizer、ColumnSettings、SelectionBar、ColumnTable、TreeDataTable、PivotTable

## 阶段 10：日期（`Rune.UI.Dates`）

- [ ] 日期模型与格式（同 vue-tsx-pro 的 DateTime，按语言包的地区格式化）、DateField / TimeField / SegmentedField / DateSegmentGroup
- [ ] Calendar、RangeCalendar、QuarterCalendar、CalendarBoard / CalendarView、DatePicker、DateRangePicker、TimePicker、RelativeTime、Countdown

## 阶段 11：图表（`Rune.Charts`，SVG）

- [ ] 底座：坐标轴、比例尺、图例、提示、响应式尺寸、图表色 token（已有 `ChartColors`）
- [ ] Line、Area、Bar、Combo、Scatter、Donut、Radar、Funnel、Treemap、Waterfall、Sankey、Chord、Graph、Timeline；Sparkline、Gauge 改用这套底座；Map 进“待决”（需要地图数据）

## 阶段 12：页面级组合

- [ ] AppShell、AuthLayout、ActionBar、FilterBar、QueryTable、FormDialog、StepsForm、KanbanBoard、NotificationCenter、CommentThread、
      DataImport、DeadlineBadge、ThemeSwitcher（含主题偏好的保存）、SlideCaptcha、PeoplePicker、ConditionBuilder

## 阶段 13：文档站

- [ ] 用本库组件搭（同 vue-tsx-pro 的 docs-kit：CodePreview、PropsTable 从 XML 文档注释生成、DocArticle、ColorSwatch）；页面与 vue-tsx-pro 文档站一一对应；
      主题定制页（同 theme-customize）；演示项目的示例迁过去

## 阶段 14：质量与发布准备（贯穿全程，这里收尾）

- [ ] 端到端测试覆盖全部有键盘、焦点、拖动逻辑的组件；axe-core 无障碍检查跑全部示例（亮、暗主题）
- [ ] 截图基线进 CI（Chromium，亮、暗主题），变化时人工确认
- [ ] 性能：大列表、表格的渲染与内存基准；WebAssembly 的裁剪与懒加载程序集验证
- [ ] 打包：四个包的说明、图标与许可、版本号；`dotnet new` 模板（可选）；发布说明。是否发布、发到哪由用户决定

---

## 待决

（执行中遇到的停顿点记在这里：日期、项号、卡在哪、需要用户决定什么。）

- CD：NuGet 发到 nuget.org 还是私有源；文档站（含 Server 模式）部署到哪里。
- 地图图表（MapChart）要地图数据（GeoJSON），来源与许可待定。
- vue-tsx-pro 的可选模块（代码编辑器、流程图 diagram、地图、三维与特效背景、xlsx）是否移植。
