using Rune.Styles;

namespace Rune.UI;

/// <summary>表单字段的部件。</summary>
public enum FieldPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>标签（label）。</summary>
    Label,

    /// <summary>标签后的必填标记（*，对读屏隐藏：控件上已有 aria-required）。</summary>
    RequiredMark,

    /// <summary>说明。</summary>
    Description,

    /// <summary>错误信息（带警示图标）。</summary>
    Error,
}

/// <summary>表单字段的样式（配方）。</summary>
public static class FieldStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<FieldPart> Of(ComponentStyles styles) =>
        Recipe.Of("field", styles, static (FieldPart part) => part switch
        {
            FieldPart.Root => Root,
            FieldPart.Label => Label,
            FieldPart.RequiredMark => RequiredMark,
            FieldPart.Description => Description,
            _ => Error,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Xs,
        MinWidth = 0,
    });

    static Style Label => Css.Define(static () => new Style
    {
        Width = "fit-content",
        FontSize = FontSizes.Sm,
        FontWeight = FontWeights.Medium,
        LineHeight = LineHeights.Tight,
    });

    static Style RequiredMark => Css.Define(static () => new Style
    {
        MarginInlineStart = Space.Xxs,
        Color = Tones.Danger.Text,
    });

    static Style Description => Css.Define(static () => new Style
    {
        FontSize = FontSizes.Xs,
        Color = Colors.TextMuted,
    });

    static Style Error => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Xs,
        FontSize = FontSizes.Xs,
        Color = Tones.Danger.Text,
    });
}
