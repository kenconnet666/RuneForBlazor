using Rune.Styles;

namespace Rune.UI;

/// <summary>条件构造器的部件。</summary>
public enum ConditionBuilderPart
{
    /// <summary>外层 div（role="group"）。</summary>
    Root,

    /// <summary>根组。</summary>
    Group,

    /// <summary>嵌套的组：浅底加描边。</summary>
    NestedGroup,

    /// <summary>组的表头：满足方式、删除整组。</summary>
    Header,

    /// <summary>组里的各行。</summary>
    Rows,

    /// <summary>一行：连接词与比较（或嵌套的组）。</summary>
    Row,

    /// <summary>连接词（且、或）。</summary>
    Joiner,

    /// <summary>一条比较（role="group"）。</summary>
    Rule,

    /// <summary>字段选择的外框。</summary>
    Field,

    /// <summary>比较方式选择的外框。</summary>
    Operator,

    /// <summary>值的输入的外层。</summary>
    Value,

    /// <summary>添加条件、添加分组。</summary>
    Actions,

    /// <summary>根组里还没有条件时的添加按钮（不缩进）。</summary>
    RootActions,

    /// <summary>删除整组的按钮。</summary>
    RemoveGroup,
}

/// <summary>条件构造器的样式（同 vue-tsx-pro 的 conditionBuilderStyles）。</summary>
public static class ConditionBuilderStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<ConditionBuilderPart> Of(ComponentStyles styles) =>
        Recipe.Of("condition-builder", styles, static (ConditionBuilderPart part) => part switch
        {
            ConditionBuilderPart.Root => Column,
            ConditionBuilderPart.Group => Column,
            ConditionBuilderPart.NestedGroup => Sx.Of(Column, Nested),
            ConditionBuilderPart.Header => Header,
            ConditionBuilderPart.Rows => Rows,
            ConditionBuilderPart.Row => Row,
            ConditionBuilderPart.Joiner => Joiner,
            ConditionBuilderPart.Rule => Rule,
            ConditionBuilderPart.Field => Field,
            ConditionBuilderPart.Operator => Operator,
            ConditionBuilderPart.Value => Value,
            ConditionBuilderPart.Actions => Actions,
            ConditionBuilderPart.RootActions => Sx.Of(Actions, RootActions),
            _ => RemoveGroup,
        });

    static Style Column => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm, MinWidth = 0 });

    // 嵌套的组：浅底加描边，和外层的条件分开
    static Style Nested => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        Padding = Space.Sm,
        BackgroundColor = Colors.SurfaceMuted,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
    });

    static Style Header => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm });

    static Style Rows => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm });

    static Style Row => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.FlexStart, Gap = Space.Sm, MinWidth = 0 });

    // 连接词（且、或）一列，宽度固定：各条的字段对齐；与控件同高，对着这一条的第一行
    static Style Joiner => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.FlexEnd,
        FlexShrink = 0,
        Width = CssLength.Em(2),
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        Color = Colors.TextMuted,
    });

    static Style Rule => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Sm,
        FlexGrow = 1,
        MinWidth = 0,
    });

    static Style Field => Css.Define(static () => new Style { FlexGrow = 1, FlexShrink = 1, FlexBasis = CssLength.Rem(9), MinWidth = CssLength.Rem(7), MaxWidth = CssLength.Rem(14) });

    static Style Operator => Css.Define(static () => new Style { FlexGrow = 0, FlexShrink = 1, FlexBasis = CssLength.Rem(8), MinWidth = CssLength.Rem(6) });

    // grid：里面的输入框拉满这一格（多选的标签按这个宽度排）
    static Style Value => Css.Define(static () => new Style { Display = Display.Grid, FlexGrow = 2, FlexShrink = 1, FlexBasis = CssLength.Rem(12), MinWidth = CssLength.Rem(8) });

    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Xs,
        PaddingInlineStart = "calc(2em + 0.5rem)",
    });

    static Style RootActions => Css.Define(static () => new Style { PaddingInlineStart = 0 });

    static Style RemoveGroup => Css.Define(static () => new Style { MarginInlineStart = CssLength.Auto });
}
