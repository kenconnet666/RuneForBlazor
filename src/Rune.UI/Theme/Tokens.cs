using Rune.Styles;

namespace Rune.UI;

// 主题 tokens：每个 token 是一个 CSS 变量（--rune-组名-名字），取值由主题提供（Themes.g.cs，由 tools/theme-gen 从 vue-tsx-pro 的主题生成）。
// 组件里只用 token，不写裸的颜色、间距；token 带类型，颜色只能用在颜色属性上，长度只能用在长度属性上。
// 分组与取值同 vue-tsx-pro：
//   - 颜色与阴影取自 Radix Colors / Radix Themes，按 Radix 12 级的用途映射成语义 token（1–2 背景，3–5 交互填充，6–8 边框，9–10 实色，11–12 文字）；
//   - 间距、圆角用 em，随组件字号等比缩放（字号即尺寸档位）；边框宽度用 px，容器宽度用 rem；
//   - 动效时长与缓动参考 Material 3，用户要求减少动态效果时时长为 0（主题里的条件取值）。
// 按色调变化的颜色在 Tones.cs（Tones.Accent.Solid……）。

/// <summary>颜色：按用途命名（背景、填充、文字、边框），不按色相命名，换主题时语义不变。</summary>
public static class Colors
{
    /// <summary>页面背景。</summary>
    public static readonly CssColor Background = "var(--rune-colors-background)";

    /// <summary>卡片、输入框等表面。</summary>
    public static readonly CssColor Surface = "var(--rune-colors-surface)";

    /// <summary>弱化的区域背景：表头、侧栏、代码块。</summary>
    public static readonly CssColor SurfaceMuted = "var(--rune-colors-surface-muted)";

    /// <summary>浮起的表面：弹出层、对话框、消息提示。</summary>
    public static readonly CssColor SurfaceRaised = "var(--rune-colors-surface-raised)";

    /// <summary>半透明表面：吸顶栏、浮动工具栏（配合 backdrop-filter）。</summary>
    public static readonly CssColor SurfaceTranslucent = "var(--rune-colors-surface-translucent)";

    /// <summary>填充：标签、开关关闭态、弱按钮。</summary>
    public static readonly CssColor Fill = "var(--rune-colors-fill)";

    /// <summary>填充悬停；也用于幽灵按钮、菜单项的悬停背景。</summary>
    public static readonly CssColor FillHover = "var(--rune-colors-fill-hover)";

    /// <summary>填充按下、选中。</summary>
    public static readonly CssColor FillActive = "var(--rune-colors-fill-active)";

    /// <summary>主要文字。</summary>
    public static readonly CssColor Text = "var(--rune-colors-text)";

    /// <summary>次要文字、说明、图标。</summary>
    public static readonly CssColor TextMuted = "var(--rune-colors-text-muted)";

    /// <summary>输入框占位文字。</summary>
    public static readonly CssColor TextPlaceholder = "var(--rune-colors-text-placeholder)";

    /// <summary>不可用的文字。</summary>
    public static readonly CssColor TextDisabled = "var(--rune-colors-text-disabled)";

    /// <summary>弱边框：分割线、表格线。</summary>
    public static readonly CssColor BorderMuted = "var(--rune-colors-border-muted)";

    /// <summary>普通边框：输入框、卡片、浮层。</summary>
    public static readonly CssColor Border = "var(--rune-colors-border)";

    /// <summary>强调边框：悬停。</summary>
    public static readonly CssColor BorderStrong = "var(--rune-colors-border-strong)";

    /// <summary>反色表面：提示、实底的中性标记。</summary>
    public static readonly CssColor InverseSurface = "var(--rune-colors-inverse-surface)";

    /// <summary>反色表面上的文字。</summary>
    public static readonly CssColor InverseText = "var(--rune-colors-inverse-text)";

    /// <summary>焦点环。</summary>
    public static readonly CssColor FocusRing = "var(--rune-colors-focus-ring)";

    /// <summary>模态的遮罩。</summary>
    public static readonly CssColor Overlay = "var(--rune-colors-overlay)";

    /// <summary>选中文字的背景。</summary>
    public static readonly CssColor Selection = "var(--rune-colors-selection)";

    /// <summary>搜索匹配的高亮。</summary>
    public static readonly CssColor Highlight = "var(--rune-colors-highlight)";
}

/// <summary>代码高亮的颜色。</summary>
public static class CodeColors
{
    /// <summary>关键字。</summary>
    public static readonly CssColor Keyword = "var(--rune-code-colors-keyword)";

    /// <summary>字符串。</summary>
    public static readonly CssColor String = "var(--rune-code-colors-string)";

    /// <summary>数字。</summary>
    public static readonly CssColor Number = "var(--rune-code-colors-number)";

    /// <summary>函数名。</summary>
    public static readonly CssColor Function = "var(--rune-code-colors-function)";

    /// <summary>类型名。</summary>
    public static readonly CssColor Type = "var(--rune-code-colors-type)";

    /// <summary>注释。</summary>
    public static readonly CssColor Comment = "var(--rune-code-colors-comment)";

    /// <summary>标签（HTML、XML）。</summary>
    public static readonly CssColor Tag = "var(--rune-code-colors-tag)";

    /// <summary>属性名。</summary>
    public static readonly CssColor Attribute = "var(--rune-code-colors-attribute)";

    /// <summary>差异里新增的行的背景。</summary>
    public static readonly CssColor Inserted = "var(--rune-code-colors-inserted)";

    /// <summary>差异里删除的行的背景。</summary>
    public static readonly CssColor Deleted = "var(--rune-code-colors-deleted)";
}

/// <summary>图表的系列颜色：按顺序分给各个系列，相邻的颜色色相拉开。</summary>
public static class ChartColors
{
    /// <summary>第 1 个系列（品牌色）。</summary>
    public static readonly CssColor Series1 = "var(--rune-chart-colors-series1)";

    /// <summary>第 2 个系列。</summary>
    public static readonly CssColor Series2 = "var(--rune-chart-colors-series2)";

    /// <summary>第 3 个系列。</summary>
    public static readonly CssColor Series3 = "var(--rune-chart-colors-series3)";

    /// <summary>第 4 个系列。</summary>
    public static readonly CssColor Series4 = "var(--rune-chart-colors-series4)";

    /// <summary>第 5 个系列。</summary>
    public static readonly CssColor Series5 = "var(--rune-chart-colors-series5)";

    /// <summary>第 6 个系列。</summary>
    public static readonly CssColor Series6 = "var(--rune-chart-colors-series6)";

    /// <summary>第 7 个系列。</summary>
    public static readonly CssColor Series7 = "var(--rune-chart-colors-series7)";

    /// <summary>第 8 个系列。</summary>
    public static readonly CssColor Series8 = "var(--rune-chart-colors-series8)";
}

/// <summary>字体族。</summary>
public static class Fonts
{
    /// <summary>正文（无衬线）。</summary>
    public static readonly CssKeyword Sans = "var(--rune-fonts-sans)";

    /// <summary>等宽：代码、编号、按键。</summary>
    public static readonly CssKeyword Mono = "var(--rune-fonts-mono)";

    /// <summary>衬线：长文、纸张风格的主题。</summary>
    public static readonly CssKeyword Serif = "var(--rune-fonts-serif)";

    /// <summary>标题（默认与正文相同，有的主题换成衬线或等宽）。</summary>
    public static readonly CssKeyword Heading = "var(--rune-fonts-heading)";
}

/// <summary>字号。组件根元素设字号，内部尺寸用 em 跟着缩放。</summary>
public static class FontSizes
{
    /// <summary>12px：辅助说明、标记。</summary>
    public static readonly CssLength Xs = "var(--rune-font-sizes-xs)";

    /// <summary>13px：紧凑控件。</summary>
    public static readonly CssLength Sm = "var(--rune-font-sizes-sm)";

    /// <summary>14px：正文、控件默认。</summary>
    public static readonly CssLength Md = "var(--rune-font-sizes-md)";

    /// <summary>16px：强调正文、小标题。</summary>
    public static readonly CssLength Lg = "var(--rune-font-sizes-lg)";

    /// <summary>18px：三级标题。</summary>
    public static readonly CssLength Xl = "var(--rune-font-sizes-xl)";

    /// <summary>24px：二级标题。</summary>
    public static readonly CssLength Xxl = "var(--rune-font-sizes-xxl)";

    /// <summary>30px：页面主标题。</summary>
    public static readonly CssLength Xxxl = "var(--rune-font-sizes-xxxl)";
}

/// <summary>字重。</summary>
public static class FontWeights
{
    /// <summary>300：大号的数字、装饰性的标题。</summary>
    public static readonly CssValue Light = "var(--rune-font-weights-light)";

    /// <summary>400：正文。</summary>
    public static readonly CssValue Normal = "var(--rune-font-weights-normal)";

    /// <summary>500：略加强调：导航项、表头。</summary>
    public static readonly CssValue Medium = "var(--rune-font-weights-medium)";

    /// <summary>600：强调、标题。</summary>
    public static readonly CssValue Semibold = "var(--rune-font-weights-semibold)";

    /// <summary>700：粗体。</summary>
    public static readonly CssValue Bold = "var(--rune-font-weights-bold)";
}

/// <summary>行高（不带单位）。</summary>
public static class LineHeights
{
    /// <summary>1：只有一行的图标、数字。</summary>
    public static readonly CssValue None = "var(--rune-line-heights-none)";

    /// <summary>1.25：标题、单行控件。</summary>
    public static readonly CssValue Tight = "var(--rune-line-heights-tight)";

    /// <summary>1.375：多行的标题、紧凑的列表。</summary>
    public static readonly CssValue Snug = "var(--rune-line-heights-snug)";

    /// <summary>1.5：正文。</summary>
    public static readonly CssValue Normal = "var(--rune-line-heights-normal)";

    /// <summary>1.75：长文。</summary>
    public static readonly CssValue Relaxed = "var(--rune-line-heights-relaxed)";
}

/// <summary>字间距。</summary>
public static class LetterSpacings
{
    /// <summary>-0.02em：特大标题。</summary>
    public static readonly CssLength Tighter = "var(--rune-letter-spacings-tighter)";

    /// <summary>-0.01em：大标题。</summary>
    public static readonly CssLength Tight = "var(--rune-letter-spacings-tight)";

    /// <summary>0。</summary>
    public static readonly CssLength Normal = "var(--rune-letter-spacings-normal)";

    /// <summary>0.02em：小号的大写字母。</summary>
    public static readonly CssLength Wide = "var(--rune-letter-spacings-wide)";

    /// <summary>0.05em：分组标题、标签。</summary>
    public static readonly CssLength Wider = "var(--rune-letter-spacings-wider)";
}

/// <summary>间距（em，随字号缩放）：内边距、外边距、间隙。</summary>
public static class Space
{
    /// <summary>0。</summary>
    public static readonly CssLength None = "var(--rune-space-none)";

    /// <summary>0.25em：图标与文字之间、紧凑的元素之间。</summary>
    public static readonly CssLength Xs = "var(--rune-space-xs)";

    /// <summary>0.5em：控件内部、相关的元素之间。</summary>
    public static readonly CssLength Sm = "var(--rune-space-sm)";

    /// <summary>0.75em：一组内容的元素之间。</summary>
    public static readonly CssLength Md = "var(--rune-space-md)";

    /// <summary>1em：表单项之间、段落之间。</summary>
    public static readonly CssLength Lg = "var(--rune-space-lg)";

    /// <summary>1.5em：区块之间、卡片与对话框的内边距。</summary>
    public static readonly CssLength Xl = "var(--rune-space-xl)";
}

/// <summary>尺寸：控件高度、可点击区域、容器宽度。</summary>
public static class Sizes
{
    /// <summary>控件的默认高度（2.25em，随字号缩放）。</summary>
    public static readonly CssLength Control = "var(--rune-sizes-control)";

    /// <summary>可点击区域的最小尺寸（WCAG 2.5.8：24px）。</summary>
    public static readonly CssLength MinTarget = "var(--rune-sizes-min-target)";

    /// <summary>30rem：小对话框。</summary>
    public static readonly CssLength Xs = "var(--rune-sizes-xs)";

    /// <summary>40rem：对话框、窄内容。</summary>
    public static readonly CssLength Sm = "var(--rune-sizes-sm)";

    /// <summary>48rem：宽对话框、文章。</summary>
    public static readonly CssLength Md = "var(--rune-sizes-md)";

    /// <summary>64rem：页面内容。</summary>
    public static readonly CssLength Lg = "var(--rune-sizes-lg)";

    /// <summary>80rem：宽页面。</summary>
    public static readonly CssLength Xl = "var(--rune-sizes-xl)";

    /// <summary>100%。</summary>
    public static readonly CssLength Full = "var(--rune-sizes-full)";
}

/// <summary>圆角（em，随字号缩放）。</summary>
public static class Radii
{
    /// <summary>直角：表格、密集列表里与直线对齐。</summary>
    public static readonly CssLength None = "var(--rune-radii-none)";

    /// <summary>0.15em。</summary>
    public static readonly CssLength Xs = "var(--rune-radii-xs)";

    /// <summary>0.25em：标记、按键。</summary>
    public static readonly CssLength Sm = "var(--rune-radii-sm)";

    /// <summary>0.4em：控件与按钮的默认。</summary>
    public static readonly CssLength Md = "var(--rune-radii-md)";

    /// <summary>0.7em：卡片、面板。</summary>
    public static readonly CssLength Lg = "var(--rune-radii-lg)";

    /// <summary>1em：大卡片、对话框。</summary>
    public static readonly CssLength Xl = "var(--rune-radii-xl)";

    /// <summary>胶囊形（宽高相等时是圆形）。</summary>
    public static readonly CssLength Full = "var(--rune-radii-full)";
}

/// <summary>边框宽度（px）。</summary>
public static class BorderWidths
{
    /// <summary>0。</summary>
    public static readonly CssLength None = "var(--rune-border-widths-none)";

    /// <summary>1px：普通边框。</summary>
    public static readonly CssLength Xs = "var(--rune-border-widths-xs)";

    /// <summary>2px：焦点环、选中的指示线。</summary>
    public static readonly CssLength Sm = "var(--rune-border-widths-sm)";

    /// <summary>3px。</summary>
    public static readonly CssLength Md = "var(--rune-border-widths-md)";

    /// <summary>4px：提示条的色条。</summary>
    public static readonly CssLength Lg = "var(--rune-border-widths-lg)";

    /// <summary>6px。</summary>
    public static readonly CssLength Xl = "var(--rune-border-widths-xl)";
}

/// <summary>阴影。</summary>
public static class Shadows
{
    /// <summary>没有阴影。</summary>
    public static readonly CssValue None = "var(--rune-shadows-none)";

    /// <summary>贴着表面：按钮、输入框。</summary>
    public static readonly CssValue Xs = "var(--rune-shadows-xs)";

    /// <summary>轻微浮起：卡片悬停。</summary>
    public static readonly CssValue Sm = "var(--rune-shadows-sm)";

    /// <summary>浮层：下拉、弹出框。</summary>
    public static readonly CssValue Md = "var(--rune-shadows-md)";

    /// <summary>对话框。</summary>
    public static readonly CssValue Lg = "var(--rune-shadows-lg)";

    /// <summary>抽屉、全屏面板。</summary>
    public static readonly CssValue Xl = "var(--rune-shadows-xl)";
}

/// <summary>透明度。</summary>
public static class Opacities
{
    /// <summary>不可用的控件。</summary>
    public static readonly CssValue Disabled = "var(--rune-opacities-disabled)";
}

/// <summary>图标的线条粗细（SVG 的 stroke-width，不带单位）。</summary>
public static class StrokeWidths
{
    /// <summary>1：大号图标。</summary>
    public static readonly CssValue Xs = "var(--rune-stroke-widths-xs)";

    /// <summary>1.5。</summary>
    public static readonly CssValue Sm = "var(--rune-stroke-widths-sm)";

    /// <summary>2：默认（Lucide 的设计值）。</summary>
    public static readonly CssValue Md = "var(--rune-stroke-widths-md)";

    /// <summary>2.5：小号图标、勾选框里的对勾。</summary>
    public static readonly CssValue Lg = "var(--rune-stroke-widths-lg)";

    /// <summary>3。</summary>
    public static readonly CssValue Xl = "var(--rune-stroke-widths-xl)";
}

/// <summary>动效时长。用户要求减少动态效果时都是 0（主题的条件取值）：用了它们的过渡自动关掉，组件不用另写。</summary>
public static class Durations
{
    /// <summary>0ms。</summary>
    public static readonly CssValue None = "var(--rune-durations-none)";

    /// <summary>75ms：按下的反馈。</summary>
    public static readonly CssValue Xs = "var(--rune-durations-xs)";

    /// <summary>120ms：悬停、颜色变化、浮层离场。</summary>
    public static readonly CssValue Sm = "var(--rune-durations-sm)";

    /// <summary>200ms：浮层进场、展开收起。</summary>
    public static readonly CssValue Md = "var(--rune-durations-md)";

    /// <summary>300ms：大面积的移动（抽屉）。</summary>
    public static readonly CssValue Lg = "var(--rune-durations-lg)";

    /// <summary>500ms：强调的动画。</summary>
    public static readonly CssValue Xl = "var(--rune-durations-xl)";
}

/// <summary>缓动曲线（Material 3）。</summary>
public static class Easings
{
    /// <summary>常规：位置、大小在屏幕内变化。</summary>
    public static readonly CssKeyword Standard = "var(--rune-easings-standard)";

    /// <summary>进场（减速）。</summary>
    public static readonly CssKeyword Enter = "var(--rune-easings-enter)";

    /// <summary>离场（加速）。</summary>
    public static readonly CssKeyword Exit = "var(--rune-easings-exit)";
}

/// <summary>
/// 层级：不在顶层（top layer）里的浮动元素之间的上下关系（本库独有：提示、消息提示没有用 popover）。
/// 弹出层、对话框用原生 popover / dialog，渲染在顶层，不参与层叠顺序。
/// </summary>
public static class ZIndices
{
    /// <summary>消息提示。</summary>
    public static readonly CssValue Toast = "var(--rune-z-indices-toast)";

    /// <summary>悬停提示（最上层）。</summary>
    public static readonly CssValue Tooltip = "var(--rune-z-indices-tooltip)";
}

/// <summary>断点（同 vue-tsx-pro）：<c>[Breakpoints.Md] = new() { … }</c> 是宽度至少 48rem 时的样式（移动优先）。</summary>
public static class Breakpoints
{
    /// <summary>≥ 40rem（640px）。</summary>
    public static readonly Condition Sm = Media.Query("(min-width: 40rem)");

    /// <summary>≥ 48rem（768px）。</summary>
    public static readonly Condition Md = Media.Query("(min-width: 48rem)");

    /// <summary>≥ 64rem（1024px）。</summary>
    public static readonly Condition Lg = Media.Query("(min-width: 64rem)");

    /// <summary>≥ 80rem（1280px）。</summary>
    public static readonly Condition Xl = Media.Query("(min-width: 80rem)");
}
