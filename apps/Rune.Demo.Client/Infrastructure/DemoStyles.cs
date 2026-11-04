using Rune.Styles;
using Rune.UI;

namespace Rune.Demo.Client.Infrastructure;

/// <summary>演示页面自己的样式（不属于组件库）。</summary>
public static class DemoStyles
{
    /// <summary>一块演示区域。</summary>
    public static Style Section => Css.Define(static () => new Style
    {
        Display = "grid",
        Gap = Space.Sm,
        Padding = Space.Md,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceMuted,
    });

    /// <summary>一排横向排列的内容。</summary>
    public static Style Row => Css.Define(static () => new Style
    {
        Display = "flex",
        FlexWrap = "wrap",
        Gap = Space.Sm,
        AlignItems = "center",
    });

    /// <summary>渲染次数、运行环境这类小字。</summary>
    public static Style RenderInfo => Css.Define(static () => new Style
    {
        Margin = 0,
        FontSize = 12,
        Color = Colors.TextMuted,
    });

    /// <summary>演示页的标题行：标题 + 渲染模式切换。</summary>
    public static Style PageHeader => Css.Define(static () => new Style
    {
        Display = "flex",
        FlexWrap = "wrap",
        AlignItems = "center",
        JustifyContent = "space-between",
        Gap = Space.Md,
    });

    /// <summary>渲染模式切换。</summary>
    public static Style ModeSwitch => Css.Define(static () => new Style
    {
        Display = "flex",
        Gap = Space.Xs,
        Padding = Space.Xs,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceMuted,
    });

    /// <summary>渲染模式切换里的一项；当前模式（aria-current）高亮。</summary>
    public static Style ModeLink => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Sm,
        Color = Colors.Text,
        TextDecoration = "none",
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Current] = new() { BackgroundColor = Tones.Accent.Solid, Color = Tones.Accent.Contrast },
    });

    /// <summary>一排演示前面的说明文字（固定宽度，几排对齐）。</summary>
    public static Style RowLabel => Css.Define(static () => new Style
    {
        Width = 72,
        FontSize = 12,
        Color = Colors.TextMuted,
    });

    /// <summary>图标网格。</summary>
    public static Style IconGrid => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "repeat(auto-fill, minmax(96px, 1fr))",
        Gap = Space.Sm,
    });

    /// <summary>图标网格里的一格。</summary>
    public static Style IconCell => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        JustifyItems = JustifyItems.Center,
        Gap = Space.Xs,
        Padding = Space.Sm,
        BorderRadius = Radii.Md,
        OverflowWrap = "anywhere",
        TextAlign = TextAlign.Center,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });

    /// <summary>强调色文字（按部件定制的例子）。</summary>
    public static Style AccentText => Css.Define(static () => new Style { Color = Tones.Accent.Text });

    /// <summary>主题预览区：用主题的背景色，切换主题时看得出来。</summary>
    public static Style ThemePreview => Css.Define(static () => new Style
    {
        Padding = Space.Md,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Background,
        Color = Colors.Text,
    });

    /// <summary>使用方覆盖的例子：展开时把标题改成强调色（只改这一个状态下的颜色）。</summary>
    public static Style AccentWhenExpanded => Css.Define(static () => new Style
    {
        [When.Expanded] = new() { Color = Tones.Accent.Text },
    });

    /// <summary>头像的替身（Avatar 在阶段 7 做）：与 Avatar 同大的圆形缩写。</summary>
    public static Style Initials => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Width = "2.5em",
        Height = "2.5em",
        FlexShrink = 0,
        BorderRadius = Radii.Full,
        BackgroundColor = Tones.Accent.Soft,
        Color = Tones.Accent.Text,
        FontWeight = FontWeights.Semibold,
    });
}
