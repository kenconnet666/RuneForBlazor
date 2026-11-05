using Rune.Styles;

namespace Rune.UI;

/// <summary>悬停卡片的部件。</summary>
public enum HoverCardPart
{
    /// <summary>卡片（div，popover="manual"）。</summary>
    Root,
}

/// <summary>悬停卡片的样式（同 vue-tsx-pro 的 hoverCardStyles）：浮层表面，缩放淡入。</summary>
public static class HoverCardStyles
{
    /// <summary>各部件的默认层；<paramref name="arrow"/> 时不裁掉伸出去的箭头。</summary>
    public static PartLayers<HoverCardPart> Of(bool arrow, ComponentStyles styles) =>
        Recipe.Of("hover-card", arrow, styles, static (bool arrow, HoverCardPart _) =>
            Sx.Of(FloatingStyles.Anchored, FloatingStyles.Surface, Panel, OverlayTransitions.Scale, arrow ? FloatingStyles.WithArrow : null));

    static Style Panel => Css.Define(static () => new Style
    {
        Padding = Space.Md,
        MaxWidth = "min(22em, calc(100vw - 2em))",
    });
}
