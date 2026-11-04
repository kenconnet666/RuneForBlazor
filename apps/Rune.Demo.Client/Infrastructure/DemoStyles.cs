using Rune.Styles;
using Rune.UI.Theme;

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
        BackgroundColor = Colors.Surface,
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
        Gap = Space.Xxs,
        Padding = Space.Xxs,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
    });

    /// <summary>渲染模式切换里的一项；当前模式（aria-current）高亮。</summary>
    public static Style ModeLink => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Sm,
        Color = Colors.Text,
        TextDecoration = "none",
        [When.Hover] = new() { BackgroundColor = Colors.SurfaceHover },
        [When.Current] = new() { BackgroundColor = Colors.Accent, Color = Colors.AccentContrast },
    });

    /// <summary>使用方覆盖的例子：展开时把标题改成强调色（只改这一个状态下的颜色）。</summary>
    public static Style AccentWhenExpanded => Css.Define(static () => new Style
    {
        [When.Expanded] = new() { Color = Colors.Accent },
    });
}
