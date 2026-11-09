---
name: rune-dev
description: RuneForBlazor 仓库的开发流程：构建与测试命令、端到端与截图基线、与 vue-tsx-pro 对照和跟进、打包与发版到 nuget.org、C# 语言服务器。在这个仓库里写组件、跑测试、更新截图基线、跟进 vue、打包或发版之前读；改组件前再看 pitfalls.md 里踩过的坑。
---

# RuneForBlazor 开发流程

RuneForBlazor 是 vue-tsx-pro（`~\IdeaProjects\vue-tsx-pro`）的 Blazor（.NET 10）移植：外观、交互、无障碍和名字以 vue 为准，API 的形状按 Blazor（ROADMAP 的“Blazor 化的约定”）。
EasyQuerySharp（`~\RiderProjects\EasyQuerySharp`）是表格查询契约与全栈示例的参考。

## 文档在哪

| 文件 | 内容 |
| --- | --- |
| `docs/ROADMAP.md` | 目标、已定的决定、Blazor 化的约定、还没做的阶段、待决 |
| `docs/DESIGN.md` | 各模块的写法与取舍（响应式、属性合并、样式、浮层与 JS、表单、表格、日期、图表、构建与发布） |
| `docs/STYLE-ALIGNMENT.md` | 跟进 vue 时与 vue 不同的取舍、还没跟的改动 |
| `docs/DOCS-SITE.md` | 文档站与 vue 文档站的对照：有意的差别、上线前要做的 |
| `docs/PERFORMANCE.md` | 基准数字与结构性的待议项 |
| `CHANGELOG.md` | 每个版本的变化与对齐的 vue 版本 |
| `tools/parity/README.md`、`tools/style-snapshot/README.md` | 对照工具的用法 |

结论写进这些文档（决定进 ROADMAP、写法与坑进 DESIGN 的对应节、与 vue 的差别进 STYLE-ALIGNMENT），过程留在提交说明里，不写流水账、不另建交接文档。

## 工作方式

- 直接在 master 上改、提交、推送，不开 PR；整理与功能分开提交。提交说明用英文，末尾带 Co-Authored-By。
- 推送前 `gh run list --limit 3` 看上一次 CI，红了先修。本地只跑与改动相关的测试，全量交给 CI，不等它；长的（端到端整批、全页对照、基线）放后台或 CI。
- 遇到这些情况停下、写进 ROADMAP 的“待决”，转去做不依赖它的项：要加新依赖；要凭据、账号；同一个 CI 失败修了两次仍不明原因；
  vue 的做法在 Blazor 里做不到又有多种替代方向；改公共 API 且影响已完成组件的用法（小的改名、补参数直接改，写进提交说明）。

### 每个组件的完成标准

1. 样式：配方按 vue 逐条移植（尺寸、内边距、圆角、颜色 token、状态、过渡），`tools/parity` 没有未解释的差异（亮、暗）。
2. API：参数、默认值、部件与 vue 对应；`data-scope` / `data-part`、`PropsXxx` 部件属性、`XxxDefaults`、`[EditorRequired]`、中文文档注释；`SpecParityTests` 不新增差异。
3. bUnit：受控、只给值、都不给三种用法与主要分支、aria；输入类另测 `EditForm` 里的用法。
4. 浏览器：静态 SSR、Server、WebAssembly、Auto 都打开点过；有键盘、焦点、拖动的有端到端用例。
5. 性能：按需创建（`Css.Define`、`??=`、`Memo`）、收起的内容不渲染、渲染路径不分配、大列表虚拟滚动。
6. 文档页照 vue 文档站的示例写，有 API 表与键盘一节。

## 命令

本机 PATH 上的 dotnet 没有 10.x，先：

```bash
export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH"
```

| 做什么 | 命令 |
| --- | --- |
| 构建 | `dotnet build RuneForBlazor.slnx`（警告即错误）；先结束占着文件的 `Rune.Demo.exe`、`Rune.Docs.exe` |
| 单元测试 | `dotnet build tests/Rune.Tests` 后 `dotnet tests/Rune.Tests/bin/Debug/net10.0/Rune.Tests.dll -class Rune.Tests.XxxTests` |
| 端到端 | `dotnet build tests/Rune.E2E` 后 `dotnet tests/Rune.E2E/bin/Debug/net10.0/Rune.E2E.dll -class Rune.E2E.XxxTests`（测试自己启动 Rune.Demo） |
| 测试台、文档站 | 预览面板按 `.claude/launch.json` 启动 `bench`（5300，`/{static\|server\|wasm}/{页面}`）、`docs`（5310）；都固定了“现在” |
| vue 文档站 | 在 `vue-tsx-pro/apps/docs` 下 `npx vite --port 5174 --strictPort` |
| 对照 | `MSYS_NO_PATHCONV=1 node tools/parity/parity.mjs <页面> --themes=light --shots --mode=server`；全站 `node tools/parity/all.mjs --shots --mode=server && python tools/parity/shots.py`；整页 `pages.mjs`、排布 `columns.mjs`、状态 `--states`（见 tools/parity/README.md） |
| 生成器检查 | `node tools/{css,theme,lang,area}-gen/generate.mjs --check --source <vue-tsx-pro>`、`node tools/{icon,vendor}-gen/generate.mjs --check` |
| 打包 | Release 构建后 `bash tools/pack/pack.sh <输出目录> [版本]`（同时检查包的内容） |
| 部署文档站 | `gh workflow run docs.yml --ref master`：导出静态站点，部署到 https://rune-for-blazor.zerodep.dpdns.org（令牌在 `docs` 环境） |

截图基线（`tests/Rune.E2E/Visual`，CI 的“截图基线”任务）：示例画面改了以后基线会报不同。重新截：

```bash
gh workflow run CI -f update-visual-baselines=true
gh run download <run-id> -n visual-baselines -D tests/Rune.E2E/Visual/Baselines
```

看过下载的图再提交。

## 跟进 vue-tsx-pro

- vue 发版或通知一批改动时：读它的提交与 RELEASE.md，按 `/spec/components.json` 逐项核对；CI 的 `VUE_TSX_PRO_REF`（`.github/workflows/ci.yml`）前移到跟进到的提交，
  重新跑 css-gen、theme-gen、lang-gen、area-gen（不带 `--check`）生成。
- 规格比对：`tests/Rune.Tests/Parity/SpecParityTests` 对 vue 的规格 JSON 比组件与参数，差异记在基线；确认差异合理后 `RUNE_SPEC_UPDATE=1` 跑一次重写基线。
  组件改名或把参数原样交给里面的组件时，登记在测试的 `Renamed` / `Forwards` 表。
- 取舍与没跟的改动写进 STYLE-ALIGNMENT.md。vue 的 bug、无障碍问题、不合理的做法：在 kenconnet666/vue-tsx-pro 开 issue（标签 `parity`），等它修好再对齐，不直接改 vue 的仓库。

## 发版

0. 端到端测试跑的是没裁剪的 Demo：发版前用 `node tools/docs-export/export.mjs --preview` 导出一份裁剪过的文档站，在 5320 上点一遍有 JS 交互的页面（虚拟滚动、键盘命令），看控制台没有报错。
1. `Directory.Build.props` 的 `VersionPrefix` 是下一个版本；CHANGELOG.md 把 `## [Unreleased]` 改成 `## [x.y.z] - 日期`，写清对齐的 vue 版本与破坏性改动，文末加链接。
2. 提交推送 master，再在同一个提交上打 tag：`git tag -a vx.y.z -m "RuneForBlazor x.y.z" <sha> && git push origin vx.y.z`。
3. `release.yml` 核对版本与 CHANGELOG、等这个提交的 CI 全部通过，然后打包、经 nuget.org 的可信发布推送（策略“RuneForBlazor release”：本仓库、release.yml、环境 `release`、包名 `RuneForBlazor*`），最后建 GitHub Release。
   **release 等 CI 期间不要再推 master**：CI 的并发组对同一分支取消还在跑的旧运行，release 等的那次 CI 被取消就会失败。
4. CI 没过时 release 会拒绝发布：删掉 tag（`git push origin :refs/tags/vx.y.z`、`git tag -d vx.y.z`），修好后在新提交上重打。nuget.org 上的版本号发了就不能复用、包删不掉（只能 unlist）。
5. 发完把 `VersionPrefix` 改成下一个版本，CHANGELOG 加回 `## [Unreleased]`。

包 ID 是 `RuneForBlazor.*`，程序集与命名空间是 `Rune.*`。静态资源的路径跟着包 ID：`_content/RuneForBlazor.UI/…`；JS 初始化器必须叫 `{PackageId}.lib.module.js`；
跨包的 JS import 写包 ID 的路径。改包 ID 时这三处都要跟着改，否则交互全部失效（端到端测试卡在等 `data-rune-ready`，直到超时）。

## C# 语言服务器

- `.claude/settings.json` 登记仓库内的插件市场 `.claude/marketplace`，启用 `rune-csharp-lsp`（`dotnet-tools.json` 固定版本的 csharp-ls，加载 slnx，约 20 秒）。
  桌面版有时只登记了市场没装插件：在 claude 终端里 `/plugin install rune-csharp-lsp@rune-for-blazor`，重开会话。
  会话里加了别的 C# 仓库（如 EasyQuerySharp）时它的 LSP 插件会抢走 `.cs`：在 `.claude/settings.local.json` 里设 `"easy-query-sharp-lsp@easy-query-sharp": false`。
- 启动脚本 `tools/dotnet.mjs` 按 DOTNET_ROOT → `~/.dotnet` → PATH 找 SDK；把生成器、代码修复的输出拷到 `bin/ls/` 让语言服务器加载副本（不锁正式构建），生成器改了要重开会话。
- csharp-ls 不认 `.razor`：启动脚本监视 `.razor` 的变化，替它发所属 `.csproj` 的变更通知，约半分钟后新组件可解析。`.razor` 里的语义查询用 Rider 的 MCP。
