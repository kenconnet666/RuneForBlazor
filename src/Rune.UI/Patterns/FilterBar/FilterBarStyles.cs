using Rune.Styles;

namespace Rune.UI;

/// <summary>查询栏的部件。</summary>
public enum FilterBarPart
{
    /// <summary>form 元素。</summary>
    Root,

    /// <summary>起始边（QueryTable 的标题）。</summary>
    Start,

    /// <summary>Start 右边那一列。</summary>
    Body,

    /// <summary>第一排：条件与按钮区。</summary>
    Row,

    /// <summary>条件的栅格。</summary>
    Fields,

    /// <summary>按钮区。</summary>
    Actions,

    /// <summary>展开、收起。</summary>
    Toggle,

    /// <summary>重置。</summary>
    Reset,

    /// <summary>查询。</summary>
    Search,

    /// <summary>“展开”旁边收起的条件里填了几项。</summary>
    Count,
}

/// <summary>查询栏的默认值（全站的查询栏）。</summary>
public sealed record FilterBarDefaults : IComponentDefaults<FilterBarDefaults>
{
    /// <summary>标签位置，默认在起始边。</summary>
    public FieldLabelPlacement? LabelPlacement { get; init; }

    /// <summary>标签宽度，默认 auto（随文字）。</summary>
    public CssLength? LabelWidth { get; init; }

    /// <summary>每个条件的最小宽度，默认 14em。</summary>
    public CssLength? MinFieldWidth { get; init; }

    /// <inheritdoc/>
    public FilterBarDefaults Over(FilterBarDefaults outer) => new()
    {
        LabelPlacement = LabelPlacement ?? outer.LabelPlacement,
        LabelWidth = LabelWidth ?? outer.LabelWidth,
        MinFieldWidth = MinFieldWidth ?? outer.MinFieldWidth,
    };
}

/// <summary>查询栏的样式（同 vue-tsx-pro 的 filterBarRecipe）。</summary>
public static class FilterBarStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<FilterBarPart> Of(ComponentStyles styles) =>
        Recipe.Of("filter-bar", styles, static (FilterBarPart part) => part switch
        {
            FilterBarPart.Root => Root,
            FilterBarPart.Start => Start,
            FilterBarPart.Body => Body,
            FilterBarPart.Row => Row,
            FilterBarPart.Fields => Fields,
            FilterBarPart.Actions => Actions,
            _ => Sx.Of((Style?)null),
        });

    // Start（标题）单独一列，其余都在它右边：展开的条件与第一排的条件左边对齐。窄到放不下时标题自己一行
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.FlexStart,
        ColumnGap = Space.Xl,
        RowGap = Space.Md,
        MinWidth = 0,
    });

    // 标题和按钮都与第一行的控件一样高、居中对齐
    static Style Start => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, MinHeight = Sizes.Control, FlexShrink = 0 });

    static Style Body => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Md,
        FlexGrow = 1,
        FlexBasis = CssLength.Em(14),
        MinWidth = 0,
    });

    // 第一排：条件、按钮区；窄到一个条件也放不下时按钮区换到下一行
    static Style Row => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.FlexStart,
        ColumnGap = Space.Xl,
        RowGap = Space.Md,
        MinWidth = 0,
    });

    // 条件的栅格：列数由 JS 量出（--rune-filter-columns，第一排放得下几格）；量出来之前按最小宽度自动排
    static Style Fields => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "var(--rune-filter-template, repeat(auto-fit, minmax(min(14em, 100%), 1fr)))",
        ColumnGap = Space.Xl,
        RowGap = Space.Md,
        AlignItems = AlignItems.Start,
        FlexGrow = 1,
        FlexBasis = CssLength.Em(14),
        MinWidth = 0,
    });

    /// <summary>More 的外层：里面的条件直接排进栅格；收起且还没量过时不显示（JS 量过后写 display: contents）。</summary>
    internal static Style MoreCollapsed => Css.Define(static () => new Style { Display = Display.None });

    /// <inheritdoc cref="MoreCollapsed"/>
    internal static Style MoreShown => Css.Define(static () => new Style { Display = Display.Contents });

    // 不收缩，但不宽过整排：按钮多（QueryTable 的工具栏放在这里）、屏幕窄时在里面换行，不把页面撑宽
    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        JustifyContent = JustifyContent.FlexEnd,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MinHeight = Sizes.Control,
        FlexShrink = 0,
        MinWidth = 0,
        MaxWidth = CssLength.Percent(100),
        MarginInlineStart = CssLength.Auto,
    });
}
