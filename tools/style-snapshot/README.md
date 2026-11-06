# 计算样式快照（style-snapshot）

改样式引擎、整理组件样式（承诺外观不变的改动）时，前后各拍一份测试台全部文档页的计算样式，逐项比对：
每个元素约 80 个外观属性，亮、暗主题各一遍；可交互的元素再强制 `:hover`、`:focus-visible`、`:active`，各记一次会变的属性。
同 vue-tsx-pro 样式改造的验收（计算样式与引擎无关，拿引擎自己的模型验证自己查不出输出的错）。不进 CI，本地跑。

## 用法

1. 测试台（端口 5300）：`.claude/launch.json` 的 `bench`，或

   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project apps/Rune.Demo -c Release --no-launch-profile --urls http://localhost:5300
   ```

2. 改动之前拍一份，改完重启测试台再拍一份，比对：

   ```bash
   node tools/style-snapshot/snapshot.mjs capture artifacts/style-snapshot/before.json
   node tools/style-snapshot/snapshot.mjs capture artifacts/style-snapshot/after.json
   node tools/style-snapshot/snapshot.mjs compare artifacts/style-snapshot/before.json artifacts/style-snapshot/after.json
   ```

   只拍几页：`capture out.json button,select`。其他选项：`--mode=server`（默认 static）、`--themes=light`、`--origin=…`。
   比对时列出每处不同（页 · 主题 · 元素序号 · 属性），没有差异时退出码为 0。

Playwright 用 vue-tsx-pro 的 `apps/host-demo` 装好的那份（同 `tools/parity`），vue-tsx-pro 不在默认位置时用 `--vue` 指定。
元素按文档顺序编号：改样式不改 DOM 结构时前后对得上；拍快照时减少动态效果，过渡瞬间完成。
