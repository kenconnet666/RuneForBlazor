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

    /// <summary>控件、说明、错误的容器（标签在起始边时是第二列）。</summary>
    Control,

    /// <summary>说明。</summary>
    Description,

    /// <summary>错误信息（带警示图标）。</summary>
    Error,
}

/// <summary>标签的位置。</summary>
public enum FieldLabelPlacement
{
    /// <summary>上方（默认）。</summary>
    Top,

    /// <summary>起始边：标签一列、控件一列，第一行文字按基线对齐；窄屏（小于 40rem）时回到上方。</summary>
    Start,
}

/// <summary>表单字段的样式（配方）。</summary>
public static class FieldStyles
{
    /// <summary>标签在起始边时标签列的宽度（经变量写在元素上，默认 8em）。</summary>
    public static readonly CssVar<CssLength> LabelWidth = CssVar.Length("rune-field-label-width");

    /// <summary>某种标签位置、是否隐藏标签、某套定制下各部件的默认层。</summary>
    public static PartLayers<FieldPart> Of(FieldLabelPlacement placement, bool labelHidden, ComponentStyles styles) =>
        Recipe.Of("field", (placement, labelHidden), styles, static ((FieldLabelPlacement Placement, bool LabelHidden) a, FieldPart part) => part switch
        {
            FieldPart.Root => Sx.Of(Root, a.Placement == FieldLabelPlacement.Start ? StartRoot : null),
            FieldPart.Label => Sx.Of(Label, a.LabelHidden ? SharedStyles.VisuallyHidden : null),
            FieldPart.RequiredMark => RequiredMark,
            FieldPart.Control => Control,
            FieldPart.Description => Description,
            _ => Error,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Xs,
        MinWidth = 0,
    });

    // 标签在起始边：宽屏时两列，标签与控件的第一行文字按基线对齐；窄屏时仍是上下排
    static Style StartRoot => Css.Define(static () => new Style
    {
        [Breakpoints.Sm] = new()
        {
            GridTemplateColumns = $"var({LabelWidth.Name}, 8em) minmax(0, 1fr)",
            ColumnGap = Space.Md,
            AlignItems = "baseline",
        },
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
        MarginInlineStart = Space.Xs,
        Color = Tones.Danger.Text,
    });

    static Style Control => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Xs,
        MinWidth = 0,
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
