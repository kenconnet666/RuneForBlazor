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
- [ ] 首页：“从这里开始”的卡片随指南页的数量变（vue 5 张）；组件目录的分类顺序、计数文字核对。
- [ ] 404 页、`/frame/…` 整页示例页的外壳核对。
- [ ] 暗色主题跑一遍 `pages.mjs --themes=dark` 与全部示例的 `parity.mjs`，修掉只在暗色出现的差异。

### P2 内容补齐（跟着各阶段做，每做一个组件就补它的页面并跑对照）

- [ ] 已有页缺的 8 个示例（见上表）。
- [ ] 阶段 6 的 13 个页面组合组件及其页面；ThemeCustomizer 之后补主题定制页（指南 Theming）与 theme-switcher 的两个示例。
- [ ] 阶段 7：属性演练场（PropsPlaygroundButton，参数表从 API 文档生成）、3D 与画布、统计看板页。
- [ ] 指南：Data、Playground 的对应页（见待决）；全栈、移动端两组随阶段 8、7。
- [ ] 每页的键盘一节（KeyboardReference）补齐；搜索能搜到参数名与示例标题（阶段 9 的同一项）。

### P3 示例画布逐像素

- [ ] 按 `artifacts/parity/shots.md` 从差异大的往下修：code（text、basic、block、diff）、date-time（formats −39px、countdowns −167px）、
  data-table（import、orders、columns、ledger）、field-compare、form（layout、array、signup、admission、wizard）、timeline、scroll-area。
- [ ] `parity.mjs --states`（悬停、聚焦、按下）跑全部页面，亮、暗。

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
