using Rune.Styles;
using Rune.UI.Theme;

namespace Rune.UI;

/// <summary>折叠面板的外观。</summary>
public enum CollapsibleVariant
{
    /// <summary>无边框（默认）。</summary>
    Plain,

    /// <summary>带边框与圆角。</summary>
    Outline,
}

/// <summary>折叠面板各部件的默认层。</summary>
/// <param name="Root">外层容器。</param>
/// <param name="Trigger">展开收起的按钮。</param>
/// <param name="Content">内容区。</param>
public sealed record CollapsibleParts(Attrs Root, Attrs Trigger, Attrs Content);

/// <summary>折叠面板的样式（配方）：部件 × 档位，每个档位只创建一次。</summary>
public static class CollapsibleStyles
{
    /// <summary>某个外观下各部件的默认层。</summary>
    public static CollapsibleParts Of(CollapsibleVariant variant) => Css.Define(variant, static variant => new CollapsibleParts(
        Root: Part.Of("collapsible", "root", variant == CollapsibleVariant.Outline ? [Root, OutlineRoot] : Root),
        Trigger: Part.Of("collapsible", "trigger", Trigger),
        Content: Part.Of("collapsible", "content", Content)));

    static Style Root => Css.Define(static () => new Style
    {
        Display = "grid",
        Color = Colors.Text,
    });

    static Style OutlineRoot => Css.Define(static () => new Style
    {
        BorderWidth = 1,
        BorderStyle = "solid",
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        Overflow = "hidden",
    });

    static Style Trigger => Css.Define(static () => new Style
    {
        Display = "flex",
        AlignItems = "center",
        Gap = Space.Sm,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        BorderWidth = 0,
        BackgroundColor = "transparent",
        Color = "inherit",
        Font = "inherit",
        TextAlign = "start",
        Cursor = "pointer",
        TransitionProperty = "background-color",
        TransitionDuration = Durations.Fast,
        [When.Hover] = new() { BackgroundColor = Colors.SurfaceHover },
        [When.FocusVisible] = new() { Outline = $"2px solid {Colors.FocusRing}", OutlineOffset = -2 },
        [When.Expanded] = new() { FontWeight = FontWeights.Semibold },
        [Media.ReducedMotion] = new() { TransitionDuration = "0s" },
    });

    static Style Content => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
    });
}
