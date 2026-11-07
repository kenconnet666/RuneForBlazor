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
- [ ] 对照模式：地址带 `?parity` 时关掉排队渲染（RenderQueue）与进场动画，图表直接画完、不等进入视口；两边都要有同样的开关
  （vue 的懒画、空闲时渲染同样会让截图不稳定，需要时按“报告 vue 的问题”的流程提给 vue）。
- [ ] 固定“现在”：文档站注册可以由地址或环境变量指定的 `TimeProvider`（UIComponent.TimeSource 已经统一取它），
  相对时间、倒计时、日历的“今天”、DeadlineBadge 在两边截图里一样。
- [ ] 字体：两边都等 `document.fonts.ready`，字体切换的默认值一致（已经一致，进 CI 后要在容器里装同样的字体）。
- [ ] 截图基线进 CI（阶段 9 的同一项）：固定的 Linux 容器、Chromium，亮、暗；先放本库自己的基线（防回退），与 vue 的对照仍在本地跑。

### P1 外壳与页面骨架补齐

- [ ] 导入行：vue 在简介下有一行 import。本库对应的是“装哪个包、用哪个命名空间”，例如
  `<PackageReference Include="Rune.Charts" />` 加 `@using Rune.UI`，或只写 `@using`。定下写法后 DocPage 统一生成（按页面的 ApiReference 类型所在的程序集推出包名），
  这样标题下的竖向节奏与 vue 相同。
- [ ] 顶栏补 ORM 的链接（EasyQuerySharp，位置、样式同 vue 的 easy-query-ts），窄屏的“链接”一组同步。
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
