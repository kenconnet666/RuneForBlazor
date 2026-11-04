using Rune.Styles;

namespace Rune.UI;

/// <summary>图标的部件。</summary>
public enum IconPart
{
    /// <summary>svg 元素。</summary>
    Root,
}

/// <summary>图标的样式：大小跟着字号（1em），不被 flex 压缩，与文字基线对齐。</summary>
public static class IconStyles
{
    /// <summary>某个字号档位、某套定制下的部件默认层。</summary>
    public static PartLayers<IconPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("icon", size, styles, static (Size? size, IconPart part) =>
            Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null));

    static Style Root => Css.Define(static () => new Style
    {
        Width = "1em",
        Height = "1em",
        FlexShrink = 0,
        VerticalAlign = "-0.125em",
    });
}
