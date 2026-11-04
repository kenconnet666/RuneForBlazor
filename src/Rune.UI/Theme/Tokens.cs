using Rune.Styles;

namespace Rune.UI.Theme;

// 主题 tokens：每个 token 是一个 CSS 变量（--rune-组-名），取值由主题提供（wwwroot/rune-ui.css 是默认主题）。
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
