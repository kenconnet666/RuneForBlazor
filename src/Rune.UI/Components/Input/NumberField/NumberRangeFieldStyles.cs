using Rune.Styles;

namespace Rune.UI;

/// <summary>数值区间的部件。</summary>
public enum NumberRangeFieldPart
{
    /// <summary>外层（div role="group"）。</summary>
    Root,

    /// <summary>两端的数字框（交给 NumberField 的外框）。</summary>
    Field,

    /// <summary>中间的分隔（默认“–”）。</summary>
    Separator,
}

/// <summary>数值区间的样式（配方，同 vue-tsx-pro 的 numberRangeFieldStyles）：两个框平分宽度，分隔跟框一样大。</summary>
public static class NumberRangeFieldStyles
{
    /// <summary>某个字号档位、某套定制下各部件的默认层。</summary>
    public static PartLayers<NumberRangeFieldPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("number-range-field", size, styles, static (Size? size, NumberRangeFieldPart part) => part switch
        {
            NumberRangeFieldPart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            NumberRangeFieldPart.Field => Field,
            _ => Separator,
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm, MinWidth = 0 });

    static Style Field => Css.Define(static () => new Style { FlexGrow = 1, FlexBasis = 0, MinWidth = 0 });

    static Style Separator => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });
}
