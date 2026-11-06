using Rune.Styles;

namespace Rune.UI;

/// <summary>级联选择的部件。</summary>
public enum CascaderPart
{
    /// <summary>各列横排的那一层（横向滚动区域）。</summary>
    Columns,

    /// <summary>一列（滚动区域）。</summary>
    Column,

    /// <summary>一列的视口（role="listbox"，焦点在它上面）。</summary>
    ColumnViewport,

    /// <summary>搜索结果那一列：占满宽度。</summary>
    Results,

    /// <summary>一个选项（role="option"）。</summary>
    Option,

    /// <summary>选项的文字。</summary>
    Label,

    /// <summary>有下一级的选项右边的箭头。</summary>
    Chevron,

    /// <summary>已选的叶子右边的对勾。</summary>
    Check,

    /// <summary>加载中、没有匹配时的提示。</summary>
    Empty,
}

/// <summary>级联选择的样式（同 vue-tsx-pro 的 cascaderStyles）。</summary>
public static class CascaderStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<CascaderPart> Of(ComponentStyles styles) =>
        Recipe.Of("cascader", styles, static (CascaderPart part) => part switch
        {
            CascaderPart.Columns => Columns,
            CascaderPart.Column => Column,
            CascaderPart.ColumnViewport => ColumnViewport,
            CascaderPart.Results => Sx.Of(Column, Results),
            CascaderPart.Option => Sx.Of(ItemStyles.Row, ItemStyles.Hoverable, ItemStyles.Highlighted, OnPath, FocusedActive, ItemStyles.Selected, ItemStyles.Disabled),
            CascaderPart.Label => ItemStyles.Label,
            CascaderPart.Chevron => Chevron,
            CascaderPart.Check => Check,
            _ => Empty,
        });

    // 列有焦点时它的高亮项画焦点环（列本身不画）
    static readonly Condition InFocusedColumn = new("[data-highlighted]:where(:focus *)");

    static readonly Condition Active = new("[data-active]");

    static Style Columns => Css.Define(static () => new Style { FlexGrow = 1, MinHeight = 0, MinWidth = 0 });

    static Style Column => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        FlexShrink = 0,
        MinWidth = CssLength.Em(10),
        MaxWidth = CssLength.Em(16),
        BorderInlineEndWidth = BorderWidths.Xs,
        BorderInlineEndStyle = BorderInlineEndStyle.Solid,
        BorderInlineEndColor = Colors.BorderMuted,
        [When.LastChild] = new() { BorderInlineEndWidth = 0 },
    });

    static Style ColumnViewport => Css.Define(static () => new Style { Padding = Space.Xs, OutlineStyle = OutlineStyle.None });

    static Style Results => Css.Define(static () => new Style { FlexGrow = 1, MaxWidth = CssWide.Initial });

    // 已展开的那一项（各列里在当前路径上的）
    static Style OnPath => Css.Define(static () => new Style { [Active] = new() { BackgroundColor = Colors.FillHover } });

    static Style FocusedActive => Css.Define(static () => new Style
    {
        [InFocusedColumn] = new()
        {
            Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}",
            OutlineOffset = $"calc(-1 * {BorderWidths.Sm})",
        },
    });

    static Style Chevron => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });

    static Style Check => Css.Define(static () => new Style { FlexShrink = 0 });

    static Style Empty => Css.Define(static () => new Style { PaddingBlock = Space.Sm, PaddingInline = Space.Sm, Color = Colors.TextMuted });
}
