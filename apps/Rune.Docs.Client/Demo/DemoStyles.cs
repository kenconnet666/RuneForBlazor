using Rune.Styles;
using Rune.UI.Theme;

namespace Rune.Docs.Client.Demo;

/// <summary>演示页面用的样式。</summary>
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

    /// <summary>一排按钮。</summary>
    public static Style Row => Css.Define(static () => new Style
    {
        Display = "flex",
        FlexWrap = "wrap",
        Gap = Space.Sm,
        AlignItems = "center",
    });

    /// <summary>渲染次数的小标记。</summary>
    public static Style RenderBadge => Css.Define(static () => new Style
    {
        FontSize = 12,
        Color = Colors.TextMuted,
    });

    /// <summary>使用方覆盖：展开时把标题改成强调色（只改这一个状态下的颜色）。</summary>
    public static Style AccentWhenExpanded => Css.Define(static () => new Style
    {
        [When.Expanded] = new() { Color = Colors.Accent },
    });
}
