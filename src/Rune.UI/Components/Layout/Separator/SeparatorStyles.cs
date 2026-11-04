using Rune.Styles;

namespace Rune.UI;

/// <summary>文字在分隔线上的位置。</summary>
public enum SeparatorLabelAlign
{
    /// <summary>起始边：文字在前、线在后，像一节的小标题。</summary>
    Start,

    /// <summary>居中（默认）：两边各一段线，如“或者”。</summary>
    Center,

    /// <summary>结束边。</summary>
    End,
}

/// <summary>分隔线的部件。</summary>
public enum SeparatorPart
{
    /// <summary>分隔线本身；带文字时是外层（不画线）。</summary>
    Root,

    /// <summary>线上的文字。</summary>
    Label,

    /// <summary>带文字时文字两边的线。</summary>
    Line,
}

/// <summary>分隔线的样式（同 vue-tsx-pro 的 Separator）。</summary>
public static class SeparatorStyles
{
    /// <summary>某个方向、是否带文字下各部件的默认层。</summary>
    public static PartLayers<SeparatorPart> Of(Orientation orientation, bool labelled, ComponentStyles styles) =>
        Recipe.Of("separator", (orientation, labelled), styles, static ((Orientation Orientation, bool Labelled) a, SeparatorPart part) => part switch
        {
            SeparatorPart.Root when a.Labelled => Labelled,
            SeparatorPart.Root => Sx.Of(LineColor, a.Orientation == Orientation.Vertical ? Vertical : Horizontal),
            SeparatorPart.Label => Label,
            _ => Sx.Of(LineColor, Line),
        });

    // Windows 高对比（forced-colors）下系统会把背景色改掉，线就看不见了：改用系统的文字色并保留它
    static Style LineColor => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        BackgroundColor = Colors.BorderMuted,
        [Media.ForcedColors] = new() { BackgroundColor = "CanvasText", ForcedColorAdjust = "none" },
    });

    static Style Horizontal => Css.Define(static () => new Style { Height = BorderWidths.Xs, Width = "100%" });

    // 在 flex 容器里随行高拉伸
    static Style Vertical => Css.Define(static () => new Style { Width = BorderWidths.Xs, AlignSelf = AlignSelf.Stretch });

    // 带文字：外层不画线，两段线各自伸展；文字比所在文字小一档（0.93em ≈ 默认字号下的 sm）
    static Style Labelled => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Md,
        Width = "100%",
        Color = Colors.TextMuted,
        FontSize = "0.93em",
        LineHeight = LineHeights.Tight,
    });

    static Style Label => Css.Define(static () => new Style { FlexShrink = 0, WhiteSpace = WhiteSpace.Nowrap });

    static Style Line => Css.Define(static () => new Style { FlexGrow = 1, Height = BorderWidths.Xs });
}
