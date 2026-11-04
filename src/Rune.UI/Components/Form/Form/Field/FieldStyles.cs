using Rune.Styles;

namespace Rune.UI;

/// <summary>表单字段的部件。</summary>
public enum FieldPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>标签（label）。</summary>
    Label,

    /// <summary>必填标记（标签后的星号图标）。</summary>
    RequiredMark,

    /// <summary>标签在起始边时控件、说明、错误的容器；标签在上方时它们直接排在外层里。</summary>
    Control,

    /// <summary>说明（控件下方的小字）。</summary>
    Description,

    /// <summary>错误信息。</summary>
    Error,
}

/// <summary>标签的位置。</summary>
public enum FieldLabelPlacement
{
    /// <summary>在控件上方（默认）。</summary>
    Top,

    /// <summary>在起始边：标签一列、控件一列，第一行文字按基线对齐。</summary>
    Start,
}

/// <summary>标签在起始边时标签文字的对齐。</summary>
public enum FieldLabelAlign
{
    /// <summary>靠起始边。</summary>
    Start,

    /// <summary>靠结束边，贴着控件（默认）。</summary>
    End,
}

/// <summary>表单字段的外观：交给 <see cref="FieldStyles.Of"/> 的档位组合。</summary>
/// <param name="Placement">标签的位置。</param>
/// <param name="Align">标签在起始边时的对齐。</param>
/// <param name="LabelHidden">视觉上隐藏标签。</param>
/// <param name="HasLabel">有标签（没有时起始边布局里控件仍放在第二列）。</param>
/// <param name="Disabled">不可用：标签变淡。</param>
public readonly record struct FieldAppearance(FieldLabelPlacement Placement, FieldLabelAlign Align, bool LabelHidden, bool HasLabel, bool Disabled);

/// <summary>表单字段的样式（同 vue-tsx-pro 的 fieldStyles 与 labelStyles）。</summary>
public static class FieldStyles
{
    /// <summary>标签在起始边时标签列的宽度（任意长度，经 CSS 变量写在元素上），默认 8em。</summary>
    public static readonly CssVar<CssLength> LabelWidth = CssVar.Length("rune-field-label-width");

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<FieldPart> Of(FieldAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("field", appearance, styles, static (FieldAppearance a, FieldPart part) =>
        {
            var start = a.Placement == FieldLabelPlacement.Start && !a.LabelHidden;
            return part switch
            {
                FieldPart.Root => start ? StartRoot : Root,
                FieldPart.Label => Sx.Of(
                    Label,
                    a.Disabled ? DisabledLabel : null,
                    a.LabelHidden ? SharedStyles.VisuallyHidden : start ? (a.Align == FieldLabelAlign.End ? LabelEnd : LabelStart) : null),
                FieldPart.RequiredMark => RequiredMark,
                FieldPart.Control => Sx.Of(Control, a.HasLabel ? null : ControlWithoutLabel),
                FieldPart.Description => Sx.Of(Hint, SharedStyles.TextColor(TextTone.Muted)),
                _ => Sx.Of(Hint, SharedStyles.TextColor(TextTone.Danger), Error),
            };
        });

    /// <summary>错误信息前的图标：与第一行文字对齐。</summary>
    internal static Style ErrorIcon => Css.Define(static () => new Style { FlexShrink = 0, MarginBlock = "0.25em" });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        MinWidth = 0,
    });

    // 标签一列、控件一列；第一行按基线对齐：标签与输入框里的文字、勾选项的文字在同一条线上
    static Style StartRoot => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = $"var({LabelWidth.Name}, 8em) minmax(0, 1fr)",
        ColumnGap = Space.Md,
        RowGap = Space.Xs,
        AlignItems = "baseline",
    });

    static Style Label => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = "baseline",
        Gap = Space.Xs,
        FontSize = FontSizes.Sm,
        FontWeight = FontWeights.Medium,
        LineHeight = LineHeights.Tight,
        Color = Colors.Text,
    });

    static Style DisabledLabel => Css.Define(static () => new Style { Opacity = Opacities.Disabled });

    static Style LabelStart => Css.Define(static () => new Style { JustifySelf = "start", TextAlign = TextAlign.Start });

    static Style LabelEnd => Css.Define(static () => new Style { JustifySelf = "end", TextAlign = TextAlign.End });

    static Style RequiredMark => Css.Define(static () => new Style
    {
        AlignSelf = AlignSelf.FlexStart,
        Width = "0.7em",
        Height = "0.7em",
        Color = Tones.Danger.Text,
    });

    static Style Control => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        MinWidth = 0,
    });

    // 没有标签时控件仍放在第二列，与上下的字段对齐
    static Style ControlWithoutLabel => Css.Define(static () => new Style { GridColumnStart = "2" });

    static Style Hint => Css.Define(static () => new Style { FontSize = FontSizes.Sm, LineHeight = LineHeights.Normal });

    static Style Error => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Xs,
    });
}
