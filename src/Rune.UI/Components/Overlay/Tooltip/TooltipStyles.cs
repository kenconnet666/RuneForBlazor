using Rune.Styles;

namespace Rune.UI;

/// <summary>文字提示的部件。</summary>
public enum TooltipPart
{
    /// <summary>提示（div，popover="manual"）。</summary>
    Root,
}

/// <summary>文字提示的样式（同 vue-tsx-pro 的 tooltipStyles）：反色的小块，淡入淡出。</summary>
public static class TooltipStyles
{
    /// <summary>各部件的默认层；<paramref name="arrow"/> 时不裁掉伸出去的箭头。</summary>
    public static PartLayers<TooltipPart> Of(bool arrow, ComponentStyles styles) =>
        Recipe.Of("tooltip", arrow, styles, static (bool arrow, TooltipPart _) =>
            Sx.Of(FloatingStyles.Anchored, Panel, OverlayTransitions.Fade, arrow ? FloatingStyles.WithArrow : null));

    static Style Panel => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        MaxWidth = "min(20em, calc(100vw - 2em))",
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderWidth = 0,
        BorderRadius = Radii.Sm,
        BackgroundColor = Colors.InverseSurface,
        Color = Colors.InverseText,
        FontSize = FontSizes.Sm,
        LineHeight = LineHeights.Normal,
        OverflowWrap = "break-word",
    });
}
