using Rune.Styles;

namespace Rune.UI;

/// <summary>图标的部件。</summary>
public enum IconPart
{
    /// <summary>svg 元素。</summary>
    Root,
}

/// <summary>图标的样式：大小跟着字号（1em），颜色跟着文字或色调，不被 flex 压缩，与文字基线对齐。</summary>
public static class IconStyles
{
    /// <summary>某个字号档位、色调、是否在从右到左的语言里翻转、某套定制下的部件默认层。</summary>
    public static PartLayers<IconPart> Of(Size? size, TextTone? tone, bool flipInRtl, ComponentStyles styles) =>
        Recipe.Of("icon", (size, tone, flipInRtl), styles, static ((Size? Size, TextTone? Tone, bool FlipInRtl) a, IconPart part) => Sx.Of(
            Root,
            a.Size is { } size ? ScaleStyles.FontSize(size) : null,
            a.Tone is { } tone ? SharedStyles.TextColor(tone) : null,
            a.FlipInRtl ? FlipInRtl : null));

    // 线条粗细取主题的 token（CSS 的 stroke-width 优先于 svg 上的属性）
    static Style Root => Css.Define(static () => new Style
    {
        Width = CssLength.Em(1),
        Height = CssLength.Em(1),
        FlexShrink = 0,
        VerticalAlign = CssLength.Em(-0.125),
        StrokeWidth = StrokeWidths.Md,
    });

    // 有方向的图标（箭头、返回）在从右到左的语言里左右翻转
    static Style FlipInRtl => Css.Define(static () => new Style { [When.Rtl] = new() { Transform = "scaleX(-1)" } });
}
