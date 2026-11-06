# 交接文档（2026-11-06）

换电脑继续时先读这一页，再看 `docs/ROADMAP.md`（进度的权威记录）与 `docs/DESIGN.md`（设计约定）。

## 目标与工作方式

- RuneForBlazor 是 vue-tsx-pro（`~\IdeaProjects\vue-tsx-pro`）的 Blazor（.NET 10）移植：样式、交互都要和 vue 一致，API 的形状按 Blazor 习惯（ROADMAP 的“Blazor 化的约定”）。
  EasyQuerySharp（`~\RiderProjects\EasyQuerySharp`）是表格查询契约与全栈示例的参考（ROADMAP 阶段 4、8）。
- 用户的要求：**无人值守持续执行完整路线图**（2026-11-06 重新规划：R → 阶段 1 → 9）；直接提交推送 master，不开 PR。
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
- 新加的 `.razor` 组件语言服务器要重开才认识（报“找不到类型”，实际编译没问题）。

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

## 最后一次提交（交接时推送）

- Mentions 修正：转发 Textarea 的参数（Rows、MaxRows……，之前未声明的 Rows 以字符串传给 Textarea 导致 500）；文本框是列表宿主（data-rune-list="text"）以便高亮，通用列表键盘排除提及框。已在 server/wasm 两种模式下对照 vue 验证：弹出、过滤、Enter 选中、Mentioned 更新、Esc 后不再弹出都一致。
- E2E 打开页面放宽到 60 秒、等 DOMContentLoaded（CI 上偶发 30 秒加载超时，本地全过）。
- **还没做**：Mentions 的单元测试与 E2E；ROADMAP 里 Mentions 的“已做”标注（下一步顺手补）。

## CI 现状

- 最近几次失败都是 E2E 打开页面超时（menu、popconfirm）或 runner 被关停，本地复现全过；这次放宽了时限，推送后看结果。
- 单元测试、构建、对照检查都是绿的。

## 下一步（按路线图）

1. R 整理与纠正：R.1 先让 CI 恢复全绿（端到端任务的 runner 关停、页面超时）并补 Mentions 测试，再做 R.2–R.4。
2. 然后阶段 1 → 9（见 ROADMAP；已有页面等其他组件的示例缺口记在提供那个组件的阶段里）。

## 移植时的约定（踩过的坑）

- 键盘尽量放 JS（初始化器 `Rune.UI.lib.module.js` 的事件委托 + `js/*.js` 模块），C# 只在需要状态时经 `rune-command` 收命令，避免 Server 模式每次按键往返。
- Server 模式下 JS 要等 C# 渲染的元素时用 requestAnimationFrame 轮询（见 cascader.js 的 whenRendered），不要假设同步出现。
- 组件转发参数给子组件时要显式声明（未声明的属性以字符串落到子组件，类型不对会抛异常）。
- Razor 标记里的字面量 @ 写成 `@@`；带引号的 C# 表达式不要直接写在属性值里，挪到 @code 的属性上。
- 分析器：RUNE006（用类型化常量）、RUNE007（Rune.UI 里不写 px）、RUNE008（lambda 不捕获循环变量）。
- bUnit：`InvokeAsync(() => x = SomeTaskReturningCall())` 会等那个 Task，要写成语句体 `{ x = ...; }`。
- Bash 里多行 heredoc 偶尔被解析器拒绝，改用 Write 工具或 `node - <<'EOF'` 小脚本做替换。
