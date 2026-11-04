using Rune.Styles;

namespace Rune.UI;

// 主题 tokens：每个 token 是一个 CSS 变量（--rune-组-名），取值由主题提供（Themes.cs 里是内置的亮色与暗色主题）。
// 组件里只用 token，不写裸的颜色、间距；token 带类型，颜色只能用在颜色属性上，间距只能用在长度属性上。

/// <summary>间距：内边距、外边距、间隙。</summary>
public static class Space
{
    /// <summary>2px：图标与文字之间的细缝。</summary>
    public static readonly CssLength Xxs = "var(--rune-space-xxs)";

    /// <summary>4px：紧凑元素内部。</summary>
    public static readonly CssLength Xs = "var(--rune-space-xs)";

    /// <summary>8px：控件内边距、相邻控件间隙。</summary>
    public static readonly CssLength Sm = "var(--rune-space-sm)";

    /// <summary>12px：卡片、面板内边距。</summary>
    public static readonly CssLength Md = "var(--rune-space-md)";

    /// <summary>16px：区块之间。</summary>
    public static readonly CssLength Lg = "var(--rune-space-lg)";

    /// <summary>24px：页面级区块之间。</summary>
    public static readonly CssLength Xl = "var(--rune-space-xl)";
}

/// <summary>颜色：按用途命名（文字、表面、边框、强调），不按色相命名，换主题时语义不变。</summary>
public static class Colors
{
    /// <summary>正文文字。</summary>
    public static readonly CssColor Text = "var(--rune-colors-text)";

    /// <summary>次要文字（说明、占位）。</summary>
    public static readonly CssColor TextMuted = "var(--rune-colors-text-muted)";

    /// <summary>页面背景。</summary>
    public static readonly CssColor Background = "var(--rune-colors-background)";

    /// <summary>卡片、面板等表面。</summary>
    public static readonly CssColor Surface = "var(--rune-colors-surface)";

    /// <summary>表面的悬停态。</summary>
    public static readonly CssColor SurfaceHover = "var(--rune-colors-surface-hover)";

    /// <summary>边框、分隔线。</summary>
    public static readonly CssColor Border = "var(--rune-colors-border)";

    /// <summary>品牌强调色（主按钮、选中项）。</summary>
    public static readonly CssColor Accent = "var(--rune-colors-accent)";

    /// <summary>强调色上的文字。</summary>
    public static readonly CssColor AccentContrast = "var(--rune-colors-accent-contrast)";

    /// <summary>焦点环。</summary>
    public static readonly CssColor FocusRing = "var(--rune-colors-focus-ring)";
}

/// <summary>圆角。</summary>
public static class Radii
{
    /// <summary>小圆角：标记、输入框。</summary>
    public static readonly CssLength Sm = "var(--rune-radii-sm)";

    /// <summary>中圆角：按钮、卡片。</summary>
    public static readonly CssLength Md = "var(--rune-radii-md)";

    /// <summary>大圆角：对话框。</summary>
    public static readonly CssLength Lg = "var(--rune-radii-lg)";
}

/// <summary>字重。</summary>
public static class FontWeights
{
    /// <summary>正文。</summary>
    public static readonly CssValue Normal = "var(--rune-font-weights-normal)";

    /// <summary>强调、标题。</summary>
    public static readonly CssValue Semibold = "var(--rune-font-weights-semibold)";
}

/// <summary>动效时长。</summary>
public static class Durations
{
    /// <summary>悬停、按下等即时反馈。</summary>
    public static readonly CssValue Fast = "var(--rune-durations-fast)";

    /// <summary>展开收起、进出场。</summary>
    public static readonly CssValue Normal = "var(--rune-durations-normal)";
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

    /// <summary>20px：标题。</summary>
    public static readonly CssLength Xl = "var(--rune-font-sizes-xl)";
}

/// <summary>行高（不带单位）。</summary>
public static class LineHeights
{
    /// <summary>1.25：标题、单行控件。</summary>
    public static readonly CssValue Tight = "var(--rune-line-heights-tight)";

    /// <summary>1.5：正文。</summary>
    public static readonly CssValue Normal = "var(--rune-line-heights-normal)";
}

/// <summary>阴影。</summary>
public static class Shadows
{
    /// <summary>轻微浮起：卡片悬停。</summary>
    public static readonly CssValue Sm = "var(--rune-shadows-sm)";

    /// <summary>浮层：下拉、弹出框。</summary>
    public static readonly CssValue Md = "var(--rune-shadows-md)";

    /// <summary>对话框。</summary>
    public static readonly CssValue Lg = "var(--rune-shadows-lg)";
}

/// <summary>缓动曲线。</summary>
public static class Easings
{
    /// <summary>常规进出。</summary>
    public static readonly CssValue Standard = "var(--rune-easings-standard)";

    /// <summary>进场（减速）。</summary>
    public static readonly CssValue Enter = "var(--rune-easings-enter)";

    /// <summary>离场（加速）。</summary>
    public static readonly CssValue Exit = "var(--rune-easings-exit)";
}

/// <summary>层级：浮层之间的上下关系集中在这里定，组件里不写裸的 z-index。</summary>
public static class ZIndices
{
    /// <summary>下拉、弹出框。</summary>
    public static readonly CssValue Dropdown = "var(--rune-z-indices-dropdown)";

    /// <summary>对话框、抽屉。</summary>
    public static readonly CssValue Modal = "var(--rune-z-indices-modal)";

    /// <summary>消息提示（盖在对话框上）。</summary>
    public static readonly CssValue Toast = "var(--rune-z-indices-toast)";

    /// <summary>悬停提示（最上层）。</summary>
    public static readonly CssValue Tooltip = "var(--rune-z-indices-tooltip)";
}

/// <summary>尺寸。</summary>
public static class Sizes
{
    /// <summary>可点击区域的最小尺寸（WCAG 2.5.8：24px）。</summary>
    public static readonly CssLength MinTarget = "var(--rune-sizes-min-target)";

    /// <summary>控件的默认高度。</summary>
    public static readonly CssLength Control = "var(--rune-sizes-control)";
}
