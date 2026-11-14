using Rune.Styles;

namespace Rune.UI;

/// <summary>插图的说明放在内容的哪一边。</summary>
public enum FigureCaptionSide
{
    /// <summary>下面（默认）：图片、图表。</summary>
    Bottom,

    /// <summary>上面：表格。</summary>
    Top,
}

/// <summary>插图的内容与说明的对齐。</summary>
public enum FigureAlign
{
    /// <summary>靠起始边（默认）。</summary>
    Start,

    /// <summary>居中。</summary>
    Center,

    /// <summary>靠结束边。</summary>
    End,
}

/// <summary>插图的部件。</summary>
public enum FigurePart
{
    /// <summary>figure 元素。</summary>
    Root,

    /// <summary>说明（figcaption）。</summary>
    Caption,
}

/// <summary>带说明的插图的样式（同 vue-tsx-pro 的 figureStyles）。</summary>
public static class FigureStyles
{
    /// <summary>某种说明位置、对齐下各部件的默认层。</summary>
    public static PartLayers<FigurePart> Of(FigureCaptionSide side, FigureAlign align, ComponentStyles styles) =>
        Recipe.Of("figure", (side, align), styles, static ((FigureCaptionSide Side, FigureAlign Align) a, FigurePart part) => part == FigurePart.Caption
            ? Caption
            : Sx.Of(Root, a.Side == FigureCaptionSide.Top ? Top : null, AlignOf(a.Align)));

    // 去掉浏览器给 figure 的外边距（间距交给布局）；min-width: 0 让里面的宽内容（表格、代码）自己滚动
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        Margin = 0,
        MinWidth = 0,
    });

    static Style Top => Css.Define(static () => new Style { FlexDirection = FlexDirection.ColumnReverse });

    static Style AlignOf(FigureAlign align) => Css.Define(align, static align => align switch
    {
        FigureAlign.Center => new Style { AlignItems = AlignItems.Center, TextAlign = TextAlign.Center },
        FigureAlign.End => new Style { AlignItems = AlignItems.FlexEnd, TextAlign = TextAlign.End },
        _ => new Style { AlignItems = AlignItems.Stretch, TextAlign = TextAlign.Start },
    });

    static Style Caption => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = FontSizes.Sm, LineHeight = LineHeights.Normal });
}
