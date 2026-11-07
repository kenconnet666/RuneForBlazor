# 正式文档站规划（与 vue-tsx-pro 文档站逐像素对照）

目标：Rune 的文档站与 vue-tsx-pro 0.12 的文档站在同一地址、同一视口、同一主题下打开，**外壳一致、页面骨架一致、示例画布逐像素一致**；
有意的差别（Blazor 的写法、参数名、包名）逐条记下原因。之后才谈正式上线（域名、部署、版本）。

## 一、现状（2026-11-07 量的）

### 已经一致的

- 外壳：顶栏 48px，侧栏 x=11、宽 234，正文 960（首页 1184，首页没有右侧栏），本页目录 224；窄屏（390）下侧栏、目录收起，正文满宽。
  这次修掉的：首页的右侧栏（Aside 插槽总是传进去，AppShell 留了一栏，组件目录挤成两列）；Tour、ValueChange、NavMenu、
  指南 Theming 四页 vue 是宽版、本库是窄版（标题宽 918 对 768）。修完后两边都有的 116 页（1440）外壳盒子全部一致，只有首页标题因文字不同而宽度不同。
- 页面骨架：标题、简介、示例分列（`tools/parity/columns.mjs`：0 页分列不同）、API、键盘一节、上一篇 / 下一篇、搜索（Ctrl + K，搜页面）、主题切换、字体切换。
- 示例画布：`parity.mjs --shots` 的截图比较，大部分示例 0–2%（`artifacts/parity/shots.md` 按差异排序，前面几项见第四节）。
- 静态导出与部署链路：`tools/docs-export` 导出整站静态文件，`.github/workflows/docs.yml` 手动触发上传 Cloudflare Workers 静态资源（`apps/Rune.Docs/wrangler.jsonc`）。

### 还缺的

| 类别 | vue 有、本库没有 | 归属 |
| --- | --- | --- |
| 组件页（24） | 页面组合 13：ActionBar、AuthLayout、CommentThread、ConditionBuilder、DataImport、FilterBar、FormDialog、KanbanBoard、NotificationCenter、PeoplePicker、QueryTable、SlideCaptcha、StepsForm | 阶段 6 |
| | 3D 9：ThreeCanvas、ModelViewer、Globe、ParticleField、ShaderBackground、MeshGradient、AuroraBackground、WaveGrid、Confetti；画布 1：Diagram | 阶段 7 |
| | 图表的统计看板（Dashboard，要 Globe） | 阶段 7 之后 |
| 已有页缺的示例（8） | provider：locales、rtl、services；drawer：imperative；theme-switcher：thumbnail、customizer；data-table：tree、single | 阶段 3、6 |
| 指南 | Data 请求与数据、Playground 代码演练场 | 待决（第五节） |
| 全栈、移动端两组 | Framework、Flow；Mobile 概览、分组页、Capabilities | 阶段 8（.NET 全栈示例）、阶段 7（Rune.Mobile） |
| 外壳 | 顶栏的属性演练场按钮（PropsPlaygroundButton）、ORM 的链接（vue 链 easy-query-ts，本库对应 EasyQuerySharp） | 阶段 7、本规划 P1 |
| 页面骨架 | 简介下的导入行（vue：`import { Button, … } from '…'`）；示例卡片工具栏的“编辑”（带进演练场） | P1、待决 |
| 换页 | 顶部加载条（vue 换页时懒加载页面的代码；本库 WebAssembly 换页是同步的，要等程序集懒加载才有东西可等） | P4 |

## 二、对照分三层

1. **外壳**（`tools/parity/pages.mjs`，本次新增）：同一地址在两边打开，比较顶栏、侧栏、正文、本页目录、页面标题的位置与大小（允许 0.5px），
   再比第一屏的画面（不同像素的比例）。页面取两边侧栏都有的地址加首页；宽 1440、390，亮、暗主题。
2. **页面骨架**（`columns.mjs`）：每页示例的分列与顺序；缺的示例列出来。
3. **示例画布**（`parity.mjs`）：按部件配对比较计算后的样式，`--shots` 截每个示例；`shots.py` 汇总像素差异。

“像素级一致”的完成标准：每页在 1440 / 390、亮 / 暗下外壳盒子一致；每个示例画布不同的像素 < 1%，超过的写明原因
（只因参数名、包名不同而文字变宽的，算有意的差别）。页面总高度的差异要能由已知原因解释。

## 三、先做哪些（按依赖排序）

### P0 对照的前提：确定性（先做，后面每一项都靠它）

- [x] 整页对照工具 `tools/parity/pages.mjs`。
- [x] 排队渲染与懒画：不用另加开关。vue 带锚点打开时示例立即全部渲染（parity.mjs 打开 `#parity`），本库首次加载不排队；
  图表进入视口才画，两个工具都先滚过去再截；进场动画用 `reducedMotion` 或截图前的等待。
- [x] 固定“现在”：服务端读配置 `FixedNow`（`.claude/launch.json` 给 bench、docs 传 `--FixedNow=2026-10-21T02:00:00Z`），
  注册固定的 TimeProvider，组件经 UIComponent.TimeSource、示例经 DemoClock 读它；浏览器里两个工具用 Playwright 的 clock 固定同一时刻（`tools/parity/clock.mjs`）。
  date-time/countdowns 从高度差 −167px 降到 0.3%。
- [x] 字体：两个工具截图前都等 `document.fonts.ready`。
- [x] 截图基线进 CI（阶段 9 的同一项）：`tests/Rune.E2E/Visual/VisualTests.cs`，每个组件页（静态 SSR）的每个示例效果区一张，
  在浏览器里与 `Visual/Baselines` 比较；CI 的 `visual` 任务跑在 ubuntu-24.04，“现在”固定。手动运行 CI 勾选 `update-visual-baselines` 时重新截并上传构件，
  看过后提交。先只有亮色（387 张，约 7MB）；暗色等 P1 的暗色检查做完再加。

### P1 外壳与页面骨架补齐

- [x] 导入行：组件都在 Rune.UI 命名空间里，对应的是装哪个包：`dotnet add package Rune.UI`（日期 Rune.UI.Dates、图表 Rune.Charts、地图 Rune.Charts.Maps），
  DocPage 按分类生成。标题下的竖向节奏与 vue 相同后，button 页第一屏的差异从 7.8% 降到 2.9%。
- 暂不做（2026-11-07 用户决定）：顶栏的 ORM 链接、属性演练场按钮、示例卡片工具栏的“编辑”。整页对照里这三处的差异是已知的。
- [x] 首页：右侧栏去掉后组件目录同 vue 排三列；指南卡片的图标同 vue；卡片数随指南页（本库 4 张，没有 Playground）。
- [x] 404 页改用 Result 的 404（同 vue）；整页示例（iframe）由 app-shell 页的示例对照覆盖（0 处差异）。
- [x] 暗色主题跑过 `pages.mjs --themes=light,dark`（116 页）：外壳在亮暗下都一致；暗色比亮色多出的差异来自示例本身（image、badge、code 等），归入 P3。

### P2 内容补齐（跟着各阶段做，每做一个组件就补它的页面并跑对照）

- [x] 已有页缺的示例：drawer（detail、imperative）、date-time（deadlines）、provider（整页按 vue 重写：安装代码、scoped、locales、rtl、services，
  services 用到新加的 `UIConfig.Highlighter`）、value-change（log），data-table 的单选 id 改成 single。剩下 theme-switcher 的 thumbnail、customizer（阶段 6）。
- [x] 示例排布：`columns.mjs` 改成比较整页排布（整行的示例、分列、宽列），修了 Descriptions、Image、NavMenu、PivotTable、Tabs 与 Tour、ValueChange、NavMenu、Theming 的宽版。
- [ ] 阶段 6 的 13 个页面组合组件及其页面；ThemeCustomizer 之后补主题定制页（指南 Theming）与 theme-switcher 的两个示例。
- [ ] 阶段 7：属性演练场（PropsPlaygroundButton，参数表从 API 文档生成）、3D 与画布、统计看板页。
- [x] 指南：Styling 按 vue 的四节加四个示例重写；Theming 加内置主题、跟随系统两个示例（主题编辑器、运行时切换、品牌色等阶段 6）；
  新增 Data（HttpClient、请求的状态、TableQueryState、出错与取消，两个连假接口的演示）。Playground 暂不做（用户决定）；全栈、移动端两组随阶段 8、7。
- [x] 键盘一节：已有页面只缺 DataTable，已补；其余缺的属阶段 6、7 的组件。搜索与 vue 一样只搜页面。

### P3 示例画布逐像素

- [x] 全站重跑（`all.mjs --shots --mode=server`，固定时间之后），按 `shots.md` 往下修。修掉的（组件层面）：
  - Timeline 的操作人头像、Stat 的骨架、Timeline 的加载指示、NavGroup 的内容：样式直接展开给子组件，Sx 变成同层的普通类，被组件自己的样式盖住
    （头像显示成 32px）；改走 `Attrs.MergeProps`。
  - DateRangePicker：开始那组分段也撑满，“–”跑到框中间；改成只有结束那组撑满（同 vue）。
  - Form：字段间距 Space.Lg → Space.Md（vue 的表单都套一层 Flex gap="md"）。
  - RuneProvider：应用配了可选主题时，最外层写了 Theme 也被忽略（测试台里 provider/scoped 的纸张主题不生效）。
  - DataTable 的勾选列：报给 vue 后照它的修法对齐（顶部对齐、只在下边抵掉多出的高度），两边行高一致。
  - 数字按语言的文化格式化（NumberField、Progress、Indicator、DataTable 的编辑）：原来用服务器的 CurrentCulture，en-US 的服务器上 C2 显示成 $。
  - 只到小时的分段在中文、日文后面带“时”“時”（同 Intl）。
  - 新的分析器 RUNE010：把带样式的 Attrs 直接展开给 Rune 组件时报错（要经 MergeProps），顺手修了 9 个包装组件。
- [x] 修掉的（示例层面）：ColumnTable、List、DeadlineBadge、ScrollArea、ButtonBase、标题尺寸、标记圆点、说明里的行内代码、按钮尺寸与 vue 不同的示例；
  date-picker 的 DateTimeOffset 字段拆成页末的单独示例；扫描脚本见 `tools/parity/columns.mjs`（排布）。
- [ ] `parity.mjs --states`（悬停、聚焦、按下）跑全部页面，亮、暗。

**已知的有意差别**（不修，截图对照里会一直有）：

- 内容：Blazor 的写法与参数名（`Size="Md"`、`@bind-Value`）、包名、代码示例的语言（Razor / C# 的行数不同）、指南里只适用于一边的段落。
- API：TextField 等输入框的 Sx 落在外框上（DESIGN 的约定），vue 的 sx 落在 input 上，同样写 16rem 时 vue 的外框宽 44px；
  DateTimeText 没有 Intl 的 `options`（date-time/formats 的后两行）；DateRangePicker 的值是日期区间而不是期间文字（periods 示例的值显示不同）。
- vue 的问题（已报给 vue 的会话）：DataTable 勾选列的负外边距落在 input 上没有生效，vue 的表头与带勾选框的行高出约 4px，本库按它注释里的意图。
- DateRangePicker 带时间的区间已做（`DateRangePicker<TValue>`，periods 示例的“维护窗口”）。
### P4 正式上线前

- [ ] 程序集懒加载：Charts、Charts.Maps（地图数据大）、Dates 只在用到的页面下载（`BlazorWebAssemblyLazyLoad` + `Router.OnNavigateAsync`）；
  换页等下载时接上顶栏的 LoadingBar（与 vue 换页时懒加载页面代码的表现一致）。验证阶段 4、5 留下的“懒加载验证”两项。
- [ ] WebAssembly 裁剪与首屏体积预算（记进 PERFORMANCE.md）；预渲染全部地址（docs-export 已有），检查每页的 title、description。
- [ ] SEO：sitemap.xml、canonical、Open Graph；404 页；不加第三方统计。
- [ ] 部署：docs.yml 配好 `CLOUDFLARE_API_TOKEN` 后正式部署；域名与版本（只放最新版，或按大版本分目录）待定。

## 四、对照的基线数据

- 组件页：vue 136，本库 114（本次补了 ItemList、DeadlineBadge）。
- 整页对照（1440，亮色，116 页）：外壳盒子一致；第一屏不同的像素 4–18%，大头是 vue 简介下那行导入代码（高 57.5px，下面的内容整体下移），
  所以 P1 的导入行是收益最大的一项；其次是各页示例本身的差异（图表页 9–18%，多是进场动画与懒画的时机，P0 的对照模式解决）。
  报告：`artifacts/parity/_pages/report.md`。
- 示例画布：`artifacts/parity/summary.md`、`shots.md`。

## 五、待决（需要用户定）

1. **代码演练场**：vue 在浏览器里编译 TSX。Blazor 的对应做法有三种：
   在 WebAssembly 里跑 Razor 编译器（几十 MB，首次打开慢）；接一个服务端编译服务（要部署后端）；不做，示例卡片不显示“编辑”（与 vue 的工具栏差一个按钮）。
   倾向：先做属性演练场（只改参数，不编译代码，阶段 7 已列），代码演练场放到最后再评估。
2. **Data 指南页**：vue 讲请求客户端与 useRequest。本库没有对应的请求层，可以改成 Blazor 自己的话题（渲染模式与预渲染取数、
   `PersistentComponentState`、HttpClient 的用法），页名不同，侧栏位置对齐。
3. **导入行的写法**（P1 第一项）：包名 + 命名空间，还是只写命名空间。
4. **因参数名不同造成的文字差异**（`Size="Md"` 对 `size="md"`）：按有意的差别处理（倾向），还是示例文字改写成等宽。
5. ~~DateRangePicker 带时间的区间~~：已按推荐做成泛型 `DateRangePicker<TValue>`（2026-11-07 用户确认）。`DateRange` 改成 `DateRange<T>`，
   静态类 `DateRange` 放 `Of`、`Preset` 与 `DateRange<DateOnly>` 的 `Days`、`Contains`（C# 14 扩展成员）；日历与快捷区间按天，用 `DateRange<DateOnly>`。

已定（2026-11-07）：导入行写包名（`dotnet add package …`）；Data 指南写 Blazor 的话题；代码演练场、属性演练场按钮、示例的“编辑”、顶栏的 ORM 链接暂不做。
