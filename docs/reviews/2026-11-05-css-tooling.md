# 审查：CSS 工具与尺寸单位（2026-11-05）

范围：`src/Rune/Styles`（强类型 CSS、css-gen 生成的属性与关键字）、`src/Rune.UI` 全部组件样式、演示项目，对照 vue-tsx-pro 的
`packages/tsx/src/styles`（样式引擎）、`packages/ui/COMPONENTS.md` §3（尺寸约定）与 `config/theme/tokens.ts`。
统计用脚本扫描源码（不含生成文件），数字是写这份报告时的状态。本次只审查，没有改代码。

## 处理结果（2026-11-05，同日）

用户确认按建议执行，已提交到 master：
- A：焦点环偏移、菜单分隔线改用 BorderWidths token，Toast、演示里的 px 改成 em / token（dd6548b）；vue-tsx-pro 一侧同类问题与禁用 Select 的指针在那边修。
  边框、焦点环、分隔线保持 px（经 token），不改 em：细线换 em 后渲染粗细随字号与设备像素比不一致，焦点环在小字号下低于 2px（WCAG 2.4.13）。
- B1、B2、B3、B4、C1、C2、C4：`CssKeyword`（值里没有数字的属性不收数字）、`CssLength.Em/Rem/Percent/Vw/Dvh/Ch/Auto/None`、`CssValue.Deg/Turn`、`CssColors`（4472e57）。
- C3：分析器 RUNE006（附代码修复 `Rune.CodeFixes`）、RUNE007；仓库里的 230 多处字符串已全部换掉（839bc27）。
- C5、D1：容器查询、`Media.Supports`、回退值、`Css.Marker` + `When.Inside(状态, 标记)`，去掉无范围的 `When.Inside`。
- 整理：各分组的 Shared 文件夹、Form/Field 拆层、NativeSelect 并进 InputStyles（3af125c）。

## 结论

1. **尺寸单位的约定与 vue-tsx-pro 一致，执行上有少量偏差。** vue-tsx-pro 的基调是“根元素定字号，内部尺寸全用 em，整体缩放”，
   例外只有边框、焦点环、分隔线用 px，最小点击区域 24px；字号 token 本身是 px（12/13/14/16/18），间距、圆角、控件高度是 em，
   容器宽度是 rem。本库的 token 由 theme-gen 从 tokens.ts 生成，单位逐个相同；组件里非零的数字长度只有 6 处，其中 3 处违反约定（见 A）。
2. **数字自动补 px 是从 vue-tsx-pro（StyleX 规则）照搬的，不是本库独有的问题**；但 C# 没有 TS 的字符串字面量类型，
   结果是“em 只能写字符串、px 写数字最顺手”，工具在鼓励与约定相反的写法。这是 B 要补的核心缺口。
3. **CSS 工具的缺口集中在三处**：长度单位没有类型（只能 `"0.875em"`）、颜色关键字没有常量（`"transparent"`）、
   已有的关键字常量没被用上（75 处写成字符串）。另有几处生成的属性类型不准、条件缺容器查询与回退值。
4. **审查中另外发现一个会在后面阶段出错的设计问题**：`When.Inside` 匹配任意祖先，没有 vue-tsx-pro `defineMarker` 那样的范围（见 D1）。

## A. 尺寸单位：违反“内部用 em”的地方

| 位置 | 现状 | vue-tsx-pro | 建议 |
|---|---|---|---|
| `SharedStyles.FocusRing` / `FocusRingInset` | `OutlineOffset = 2` / `-2`（写死的 px） | `outlineOffset: '_sm'` → `borderWidths.sm` token | 改用 `BorderWidths.Sm`（主题改边框宽度时跟着变） |
| `MenuStyles.Separator` | `Height = 1` | 分隔线用 `borderWidths.xs` | 改用 `BorderWidths.Xs` |
| `ToastStyles` | `min(24em, calc(100vw - 32px))`、进场 `translate: 0 8px` | `min(26em, 100vw)`、`0 0.75em` | 阶段 6 补齐 Toast 时一起对齐（宽度也不同） |
| `DemoStyles`（演示） | `FontSize = 12`、`Width = 72` | — | 演示代码，改成 `FontSizes.Xs` 与 em，免得被抄 |

合规的 px：`Sizes.MinTarget = 24px`（WCAG 2.5.8）、`BorderWidths.*`、`VisuallyHidden` 的 1px、JS 里浮层与视口的间距 8px、
默认偏移 6px（与 vue-tsx-pro 的 floating.ts 相同）。`Listbox` 虚拟滚动的估计行高 36px 是 px：字号档位大时估计偏小，
Virtualize 会按实测纠正，只影响初次滚动条长度，可以改成按字号换算后再交给 Virtualize（低优先级）。

## B. 强类型 CSS 的缺口

扫描结果：组件与演示里样式属性的赋值，用常量或 token 的 611 处；写成字符串的 273 处（另有插值字符串）。字符串里：

### B1. 已有常量却写成字符串：75 处，35 种（纯整理，无行为变化）

最多的：`Overflow = "hidden"` ×8、`PointerEvents = "none"` ×5、`OutlineStyle = "none"` ×5、`Visibility = "hidden"` ×4、
`PlaceItems = "center"` ×4、`TextDecoration = "none"` ×3、`FontVariantNumeric = "tabular-nums"` ×3、`OverscrollBehavior = "contain"` ×3。
这次 Select 里的 `OutlineStyle = "none"`、`Visibility = "hidden"`、`Contain = "inline-size"` 也在其中（你截图里的那几处）。
原因：写的时候没有工具提醒，补全也不会主动提示常量类。

### B2. 没有对应常量（缺类型）：198 处，132 种

| 类别 | 例子（次数） | 缺什么 |
|---|---|---|
| em / rem / % / 视口单位 | `"100%"` ×13、`"2.5em"`、`"0.875em"`、`"1em"`、`"20rem"`、`"100vw"`、`"100dvh"` 等，约 90 处 | 长度只有“数字 = px”与“任意字符串”两种写法，没有 em、rem、%、vw、dvh 的类型化写法 |
| 颜色关键字 | `"transparent"` ×14、`"currentColor"`、`"inherit"`、系统色 `"CanvasText"`、`"GrayText"` | css-gen 有意跳过了颜色关键字（`SKIPPED_KEYWORD_GROUPS`），但其中 transparent、currentColor 与强制颜色模式的几个系统色很常用 |
| 只有一个专属关键字的属性 | `Inset/Top/Bottom/MarginInline = "auto"`、`Translate/Transform = "none"`、`AnimationIterationCount = "infinite"` | `MIN_KEYWORDS = 2` 让这些属性没有关键字类；`auto`、`none` 应作为长度通用常量 |
| 函数与组合值 | `"translateX(-100%)"`、`"scaleX(-1)"`、`"rotate(360deg)"`、`Rotate = "180deg"`、`"1 / -1"`、`"span 2"`、`"16 / 9"` | 没有角度、变换、网格线的辅助；这类写字符串可以接受，但角度单位（deg）建议有类型 |
| 重复的组合值 | `MaxWidth = "calc(100vw - 2em)"` ×4（Popover、Menu、Dialog、ListPopover） | 应收成 `FloatingStyles` 里的一个常量 |

### B3. 数字的含义没有约束

- 294 个属性按“数字补 px”生成，其中有 `Display`、`Cursor`、`Transition`、`BorderStyle`、`OutlineStyle` 这类根本不接受长度的属性：
  `Display = 5` 能编译，输出 `5px`。vue-tsx-pro 在 TS 里同样能写，但那边有值类型提示；C# 这边应在生成时让纯关键字属性不接受数字。
- `Rotate` 按 StyleX 规则不补单位，`Rotate = 180` 输出无效的 `180`（两边都有这个坑）。
- 长度属性接受任意数字：在组件代码里写 `Padding = 8` 不会有任何提示，与 em 约定冲突（见 C 的建议）。

### B4. 生成的属性类型不准（css-gen 按 vue-tsx-pro 的 token 分组推断类型）

- `Font` 是 `CssLength`（因为 fontSizes 分组也管 font），`Translate`、`GridTemplateColumns` 是 `CssLength`：能用，但补全与转换语义不对。
- 建议 css-gen 对简写属性、多值属性单独标注为 `CssValue`，或改成按 MDN/webref 的值语法分类，而不是只看 token 分组。

### B5. 条件与值的能力缺口（后面阶段会用到）

| 能力 | vue-tsx-pro | 本库 | 什么时候需要 |
|---|---|---|---|
| 回退值（同一属性写几个值） | 数组 `['100vh', '100dvh']` | 没有 | 视口单位、新特性降级 |
| 容器查询 | 组件里用了 18 处 `@container` | 只有 `Media.*` | 阶段 7 起的布局组件、卡片 |
| `@supports` | 可写 | 没有辅助 | 新属性渐进增强 |
| 关系条件的范围（marker） | `defineMarker` + `css.when.ancestor/descendant/sibling*` | `When.Parent/Inside/After`，没有范围 | 见 D1 |

## C. 建议的改法（待你定）

1. **保留“数字补 px”**（与 vue-tsx-pro、StyleX 一致，去掉会改动 611 处之外的大量写法），**另加类型化的单位**：
   `CssLength` 增加 `Em(0.875)`、`Rem(20)`、`Percent(100)`、`Vw(100)`、`Dvh(100)` 等静态方法，常用值加常量（`CssLength.Full`、`Auto`、`None`）；
   角度加 `CssAngle`（`Deg(180)`），给 `Rotate` 与变换用。
   - 备选：C# 14 的扩展成员写成 `0.875.Em`。读起来更短，但会在 `double` 上加成员，影响使用方的补全，不推荐。
2. **颜色关键字常量**：生成 `CssColors.Transparent`、`CurrentColor`，以及强制颜色模式用到的系统色（`Canvas`、`CanvasText`、`GrayText`、`Highlight`、`ButtonText` 等），
   放在 css-gen 里（数据来自 webref），不手写。
3. **分析器（接在现有 RUNE001–005 后面）**，把约定变成编译期提示，这是解决“写的时候没人提醒”的根本办法：
   - RUNE006：字符串值等于已有关键字常量 → 提示改用常量，带代码修复（一键替换 B1 的 75 处）；
   - RUNE007：组件库（`Rune.UI`）里长度属性写非零数字（px）→ 提示改用 em 或 token；边框、描边类属性与 `Sizes.MinTarget` 不提示；
   - （可选）RUNE008：纯关键字属性写了数字。
4. **css-gen 调整**：纯关键字属性不接受数字（生成成只收字符串与关键字的类型）；`MIN_KEYWORDS` 降到 1，或把 `auto`、`none` 放进 `CssLength` 通用常量；
   修正 B4 的属性类型。
5. **条件补齐**：`Container.*`（容器查询）、`Supports(...)`、属性回退值；`When.Inside` 加范围（见 D1）。
6. **整理**：A 表里的 3 处 px、`calc(100vw - 2em)` 收成一个常量。

建议顺序：1、2 先做（只增加 API，不破坏现有代码）→ 3 的分析器与代码修复，顺带把 B1、B2 一次改完 → 4 → 5（容器查询在阶段 7 前做完）。
1–4 动的是 `Rune`（公共 API）与 css-gen，按工作约定先给你看方案；你同意后我单独提交（整理与功能分开）。

## D. 其他问题

1. **`When.Inside` 没有范围，会误伤嵌套的组件。** 它生成 `:is([aria-selected="true"] *)`，匹配任意一层祖先：
   - `ListboxStyles.Check`：选中标记按祖先的 `aria-selected` 显示。将来把 Select 放进可选中的表格行（DataTable 的行带 `aria-selected`）时，
     弹层在行的 DOM 里，所有选项的对勾都会显示；
   - `ButtonStyles` 的显示密码、复制图标按祖先的 `aria-pressed`、`data-state=copied` 切换：放在按下状态的 ToggleGroup 项里时会显示错图标；
   - `ChoiceStyles` 按祖先的 `aria-invalid` 变红：放进校验未通过的复合控件里时会整组变红。
   vue-tsx-pro 用 `defineMarker` 给观察的元素打标记，条件写成 `:where(.marker[aria-selected] *)`，只认带标记的祖先。
   建议本库加同样的 `Css.Marker("listbox-option")` + `When.Inside(state, marker)`，现有 6 处改用；这次 Select 的箭头已经改用 `When.Parent`（只看直接父元素）避开了。
2. **IDE 提示**：`TagLimit.Responsive = new((int?)null)` 的转换是多余的（你截图里的警告），随 Select 一起改。
3. **Select 还没完成**：组件、演示、对照（关闭与打开的面板都与 vue-tsx-pro 一致）、bUnit（197 个全过）已做；端到端 48 个里 5 个失败：
   多选、新建两个用例在两种模式下超时（大概率是测试找选项的写法，要看一下），静态 SSR 的同宽断言差 0.8px（量的是外壳与浮层的边框盒，断言要放宽到 1px）。
   还没提交；你审阅完这份报告后我先把 Select 收尾提交，再按你的决定做 C。
4. **禁用的 Select 指针样式**：vue-tsx-pro 里不可用的 Select 是手形指针（`selectStyles.root` 盖过了禁用的 `not-allowed`，像是那边的疏漏）；
   本库保留 `not-allowed`，与其他输入框一致。对照报告里会一直有这一条差异。
