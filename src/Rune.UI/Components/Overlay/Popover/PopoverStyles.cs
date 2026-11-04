using Rune.Styles;

namespace Rune.UI;

/// <summary>弹出框的部件。</summary>
public enum PopoverPart
{
    /// <summary>弹出层（div，popover）。</summary>
    Root,
}

/// <summary>弹出框的样式（同 vue-tsx-pro 的 popoverStyles 与 floatingStyles）。</summary>
public static class PopoverStyles
{
    /// <summary>各部件的默认层；<paramref name="arrow"/> 时不裁掉伸出去的箭头。</summary>
    public static PartLayers<PopoverPart> Of(bool arrow, ComponentStyles styles) =>
        Recipe.Of("popover", arrow, styles, static (bool arrow, PopoverPart _) =>
            Sx.Of(FloatingStyles.Anchored, FloatingStyles.Surface, Panel, OverlayTransitions.Scale, arrow ? FloatingStyles.WithArrow : null));

    static Style Panel => Css.Define(static () => new Style
    {
        Padding = Space.Md,
        MaxWidth = "calc(100vw - 2em)",
    });
}
