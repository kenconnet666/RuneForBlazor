# 交接文档（2026-11-07）

换电脑继续时先读这一页，再看 `docs/ROADMAP.md`（进度的权威记录）与 `docs/DESIGN.md`（设计约定）。

## 目标与工作方式

- RuneForBlazor 是 vue-tsx-pro（`~\IdeaProjects\vue-tsx-pro`）的 Blazor（.NET 10）移植：样式、交互都要和 vue 一致，API 的形状按 Blazor 习惯（ROADMAP 的“Blazor 化的约定”）。
  EasyQuerySharp（`~\RiderProjects\EasyQuerySharp`）是表格查询契约与全栈示例的参考（ROADMAP 阶段 3、8）。
- 用户的要求：**无人值守持续执行完整路线图**（2026-11-06 跟进 vue 0.11 之后第二次重新规划：阶段 0 → 9；锁定对齐 vue 0.12.0-dev，除发布外都做）；直接提交推送 master，不开 PR。
  行为与样式尽量和 vue-tsx-pro 一致；vue 有问题时通知维护它的会话修好、发版，不直接改 vue 的仓库。
- 测试节奏：本地只跑改动相关的测试；完整测试交给 CI，不等它；**下次提交前先 `gh run list` 看 CI，失败就先修**。
- 回复与注释用中文；代码、标识符、commit message 用英文；提交末尾加 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。
- 密钥只在用户环境变量 / GitHub secrets 里，不写进任何文件。

## C# 的 LSP（Claude Code）

- 项目级插件：`.claude/settings.json` 登记仓库内的本地插件市场 `.claude/marketplace`，启用 `rune-csharp-lsp`；第一次打开项目时 Claude Code 会问是否信任它。
  桌面版有时只登记了市场、没装插件（LSP 工具报“没有处理 .cs 的语言服务器”）：在 claude 终端里 `/plugin install rune-csharp-lsp@rune-for-blazor`（项目级），
  或照 `~/.claude/plugins/installed_plugins.json` 里已有的项目级插件补一条、把插件的两个文件拷进 `~/.claude/plugins/cache/rune-for-blazor/rune-csharp-lsp/1.0.0/`；装好后重开会话。
  会话里加了别的 C# 仓库当附加目录（如 EasyQuerySharp）时，它的 LSP 插件也会加载，`.cs` 会被它抢走（报 `plugin:easy-query-sharp-lsp:csharp crashed`）：
  在本项目的 `.claude/settings.local.json`（不进仓库）里把它设成 `"easy-query-sharp-lsp@easy-query-sharp": false`，重开会话。
  之后会话里有 `LSP` 工具（定义、引用、悬停、文档与工作区符号），编辑 `.cs` 后推送诊断。查符号、调用链、改名前的影响面优先用它，文本搜索用 Grep。
- 语言服务器是 `dotnet-tools.json` 固定版本的 csharp-ls（Roslyn），加载 `RuneForBlazor.slnx`，约 20 秒加载完。`.razor` 不在它的支持范围（它的 Razor 支持只认 `.cshtml`），
  但组件经 Razor 源生成器成为类型，`.cs` 里对组件的引用照样能解析；`.razor` 里的语义查询仍用 Rider 的 MCP。
- 启动脚本经 `tools/dotnet.mjs` 按 DOTNET_ROOT → `~/.dotnet` → PATH 找装着 global.json 所要 SDK 的 dotnet（本机 PATH 上的 dotnet 没有 10.x）。
  `.claude/launch.json` 也经它启动：`docs`（文档站，5310）、`bench`（测试台，5300），在 Claude Code 的浏览器面板里打开。
  升级：`dotnet tool update csharp-ls`（改的是 dotnet-tools.json）。
- 语言服务器加载了源生成器就会锁住它的 DLL：启动脚本先把 Rune.Generators、Rune.CodeFixes 的输出拷到 `bin/ls/`，经环境变量 `RuneLanguageServer` 让它加载副本，
  正式构建照常写 `bin/`。生成器改了以后语言服务器里还是旧的，重开会话才更新。
- csharp-ls 只监视 `.cs / .cshtml / .csproj / .sln / .slnx`，新建、修改 `.razor` 它都不知道（组件生成的类型认不出、参数是旧的）；
  通知它“所属 `.csproj` 变了”时它会重新加载解决方案。所以启动脚本是一层很薄的代理：自己监视 `src/ apps/ tests/` 下的 `.razor`，
  变了（防抖 1.5 秒）就在客户端的消息之间插一条 `workspace/didChangeWatchedFiles`（所属项目的 `.csproj`，Changed），约半分钟后新组件就认得了，不用重开。
  实测过：只发 `.razor` 的创建通知、打开 `.razor` 文档都没用，`--features razor-support` 也只管 `.cshtml`。

## 常用命令

```bash
# 构建前先结束占用文件的进程（Windows）
taskkill //F //IM Rune.Demo.exe; taskkill //F //IM Rune.Docs.exe
dotnet build apps/Rune.Demo -c Debug

# 测试台（bench，E2E 与对照工具用它）：http://localhost:5300/{static|server|wasm}/{页面}
ASPNETCORE_ENVIRONMENT=Development dotnet run --project apps/Rune.Demo --no-build --no-launch-profile --urls http://localhost:5300

# vue 文档站（对照用）：在 vue-tsx-pro/apps/docs 下
npx vite --port 5174 --strictPort

# Rune 文档站
ASPNETCORE_ENVIRONMENT=Development dotnet run --project apps/Rune.Docs -c Release --no-launch-profile --urls http://localhost:5310

# 视觉对照（两边都开着时）：artifacts/parity/{页面}/
MSYS_NO_PATHCONV=1 node tools/parity/parity.mjs <页面> --themes=light --shots

# 测试
dotnet test tests/Rune.Tests --filter "FullyQualifiedName~XxxTests"
dotnet test tests/Rune.E2E --filter "DisplayName~关键词"
```

## 这一轮做完的（均已推送，除最后一条见下）

- Toast：ToastService 控制器 + Toaster（位置、叠放、进度、F8/Esc/滑动）。
- Card：Loading 示例、装饰效果 Tilt / Spotlight / Beam（`js/pointer.js` 写阻尼后的指针变量）。
- Badge：表格里的状态示例。
- Menu：MenuItemBase（说明、快捷键注册与平台显示）、MenuCheckboxItem、MenuGroup、MenuLinkItem、MenuSub（悬停、安全三角、←/→）、Size、CloseOnSelect、ContextMenu；菜单项改用 ItemStyles。
- Dialog：AlertDialog、DialogService（Open 对话框/抽屉、句柄 Update/Close/Closed、ConfirmAsync 带必填理由）+ DialogHost（布局里与 Toaster 并排）；浮层关闭原因 OverlayChangeReason（JS 随 rune-toggle 报 close/escape/outside）；ItemList 组件。
- Pagination / PaginationBar / PageSizeSelect / PageJump；内置图标加了 ellipsis。
- TextField 的 Format（手机号、身份证、银行卡分组，JS 保持光标）+ `[ChinaPhone]`、`[IdCard]` 校验。
- Tabs 的“全部标签页”菜单（ShowAllMenu）。
- Cascader（列内键盘用 data-rune-list，列之间在 `js/cascader.js`）。
- Transfer（双击移动：data-rune-option-dblclick）。
- Mentions + MentionText + MentionSegments（`js/mentions.js`、`js/caret.js`，弹层贴着 @ 的位置）。

## 最近的进度

- 旧路线图的 R、阶段 1 完成，阶段 2 做到 Image / Lightbox 与 Upload 的图片墙；中间暂停做了性能（`docs/PERFORMANCE.md`，基准 `tests/Rune.Benchmarks`）。
- vue-tsx-pro 的样式体系改造（0.11）已跟进（`docs/STYLE-ALIGNMENT.md`）：分段输出与级联层、`Components` 一个定制入口、主题与对比度、
  控件高度、界面字重、着色页面 + 白卡片、表单标签、小浮层的进出场、Tabs 的逻辑属性、文档站的字体切换与页面底色的画布。
- 阶段 0：公开面收口、OtpField 用输入框配方已做；全部页面的对照跑过一遍（报告在 artifacts/parity），差异多是两边示例结构不同造成的配对错位，逐页处理还没做。
- 阶段 1 完成：TreeData、Tree、TreeSelect、TreeTransfer、VirtualList 补齐。
- 阶段 2 完成：Tour、Breadcrumbs、Steps、NavMenu、NavBar（AppShell 混合布局、Form 分步示例已换用）。下一步阶段 3（表格）。
- 2026-11-06 用户恢复路线图并重新规划（阶段 0 → 9）：锁定对齐 vue-tsx-pro 0.12.0-dev（版本号同步），除了发布，原来不做的扩展也做（阶段 7）。
- 阶段 3（表格）基本完成（2026-11-07）：TableQuery / TableQueryState（DESIGN §13）；DataTable（列的子组件、排序、筛选、列菜单、列宽、分组表头、详情行、
  网格方向键与编辑、行拖动、虚拟滚动的表格模式、CSV）；SelectionBar、ColumnSettings、ColumnTable、TreeDataTable、PivotTable、FieldCompare 与 Diff。
  剩下：预渲染不重复取数、Drawer 的两个示例（等阶段 6 的 DeadlineBadge）。LocalSetting 是 usePersistentState 的对应物。
- 单元测试约 600 个、端到端 220 个全过（本地全量跑过一次）；CI 也跑端到端。

## 下一步

1. 阶段 3 的收尾（见上），然后阶段 4（日期）→ 9；阶段 0 的逐页对照与代码走查穿插做。
2. vue 那边的批次由“组件库样式合并性能对比”会话通知，照“自主执行的规矩”跟进；vue 有问题发复现给那个会话，不改 vue 的仓库。

## 移植时的约定（踩过的坑）

- 键盘尽量放 JS（初始化器 `Rune.UI.lib.module.js` 的事件委托 + `js/*.js` 模块），C# 只在需要状态时经 `rune-command` 收命令，避免 Server 模式每次按键往返。
- Server 模式下 JS 要等 C# 渲染的元素时用 requestAnimationFrame 轮询（见 cascader.js 的 whenRendered），不要假设同步出现。
- 组件转发参数给子组件时要显式声明（未声明的属性以字符串落到子组件，类型不对会抛异常）。
- Razor 标记里的字面量 @ 写成 `@@`；带引号的 C# 表达式不要直接写在属性值里，挪到 @code 的属性上。
- 分析器：RUNE006（用类型化常量）、RUNE007（Rune.UI 里不写 px）、RUNE008（lambda 不捕获循环变量）。
- bUnit：`InvokeAsync(() => x = SomeTaskReturningCall())` 会等那个 Task，要写成语句体 `{ x = ...; }`。
- Bash 里多行 heredoc 偶尔被解析器拒绝，改用 Write 工具或 `node - <<'EOF'` 小脚本做替换。
- 列是表格的子组件（同 QuickGrid）：表格渲染时各列把自己交给 `DataTableColumns`，表体在 `Defer` 里排到渲染队列末尾；套在别的组件里的列收集不到，列来自数据时用 `@foreach`。
- 要父级重新渲染的回调用 EventCallback（不用 Func）：`OnCellChange`、`LoadChildren` 都踩过；需要返回结果时在参数对象上写（`CellChange.Reject`）。
- 按名字传组件参数（`AddComponentParameter(…, "Sx", …)`）不经过隐式转换：Style 要先包成 Sx。
- 只读的派生值（排序后的行、显示中的列）用 Memo 缓存时，键里不能有每次新建的列表，否则每次渲染都重算（DataTable 的排序曾因此在滚动时反复执行）。
- 测试里的期望值别心算：总数、合计按示例的数据算清楚再写（这一轮两次写错）。
