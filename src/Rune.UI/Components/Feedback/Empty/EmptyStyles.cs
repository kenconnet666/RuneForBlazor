using Rune.Styles;

namespace Rune.UI;

/// <summary>空状态的部件。</summary>
public enum EmptyPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>图标（交给 Icon）。</summary>
    Icon,

    /// <summary>标题。</summary>
    Title,

    /// <summary>说明（p）。</summary>
    Description,

    /// <summary>操作区。</summary>
    Action,
}

/// <summary>空状态的样式（同 vue-tsx-pro 的 emptyRecipe）：图标、标题、说明、操作居中排成一列。</summary>
public static class EmptyStyles
{
    /// <summary>各部件的默认层；<paramref name="size"/> 是字号档位，不写时继承上下文字号。</summary>
    public static PartLayers<EmptyPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("empty", size, styles, static (Size? size, EmptyPart part) => part switch
        {
            EmptyPart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            EmptyPart.Icon => Icon,
            EmptyPart.Title => Title,
            EmptyPart.Description => Description,
            _ => Action,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Xl,
        PaddingInline = Space.Lg,
        TextAlign = TextAlign.Center,
        Color = Colors.Text,
    });

    static Style Icon => Css.Define(static () => new Style
    {
        Width = CssLength.Em(2.5),
        Height = CssLength.Em(2.5),
        Color = Colors.TextMuted,
        StrokeWidth = StrokeWidths.Sm,
    });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });

    static Style Description => Css.Define(static () => new Style { MaxWidth = CssLength.Em(32), Color = Colors.TextMuted });

    static Style Action => Css.Define(static () => new Style { MarginBlockStart = Space.Sm });
}
