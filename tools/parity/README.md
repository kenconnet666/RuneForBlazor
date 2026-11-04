# 与 vue-tsx-pro 对照（parity）

同一个示例里，按 `(data-scope, data-part, 第几个)` 配对 vue-tsx-pro 文档站与本库演示页的元素，比较计算后的样式：
尺寸、内外边距、圆角、边框、字体、颜色（统一换成 rgba）、阴影、过渡与动画；亮、暗主题各一遍。
没有部件标记的元素另按出现顺序配对：语义标签（标题、段落、按钮、输入框……）、图标（svg）、只有文字的 span，
以及有 role 的元素（`[role=option]#3`；vue-tsx-pro 里用 css.create 的组件如 Listbox 没有部件标记）。看不见的元素不参与顺序配对。
不进 CI，组件做完、改完样式时本地跑。

## 准备

1. vue-tsx-pro 文档站：在 vue-tsx-pro 里 `pnpm dev:docs`（端口 5174）。
2. 本库演示项目（端口 5300）：

   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project apps/Rune.Demo --no-launch-profile --urls http://localhost:5300
   ```

3. Playwright 用 vue-tsx-pro 的 `apps/host-demo` 装好的那份（本库不加 node 依赖）；vue-tsx-pro 默认在本仓库旁边的
   `../../IdeaProjects/vue-tsx-pro`，别处用 `--vue` 指定。

## 用法

```bash
node tools/parity/parity.mjs button                       # 全部示例，亮、暗主题
node tools/parity/parity.mjs button basic,variants         # 只比这几个示例（小节标题的 id）
node tools/parity/parity.mjs text --page=/components/text  # 演示名与文档站页面不同名时
node tools/parity/parity.mjs button --states               # 另外逐个悬停、键盘聚焦、按下，比较会变的属性（减少动态效果下，过渡瞬间完成）
node tools/parity/parity.mjs button --shots                # 两边每个示例的截图存到 artifacts/parity/<演示>/
```

其他选项：`--themes=light`、`--mode=server`（默认 static）、`--tolerance=0.5`（像素）、`--width=1280`。
报告写到 `artifacts/parity/<演示>/report.md`；有差异时退出码为 1。

## 演示页怎么写才能对上

- 演示页的地址名与文档站页面一致（`/static/button` ↔ `/components/button`），示例用 `DemoSection`，`Id` 同文档站示例的 id，
  场景、顺序、文字照文档站（文字不同宽度就不同）。
- `DemoSection` 的效果区与文档站示例卡片的排布相同（表面色、网格、`Space.Xl` 内边距），脚本把它设成与文档站同宽，占满宽度的元素才可比。
- 报告里“只有 rune / 只有 vue”的部件是两边结构不同的地方：本库给 Flex、Grid、文字加了部件标记（vue-tsx-pro 没有），
  按钮的内容层、图标是单独的部件。这些不影响其他部件的配对。

## 差异怎么处理

逐条改到一致；确实不该一致的（Blazor 的结构限制、原生元素差异）在演示页注释与 DESIGN 里写原因。
