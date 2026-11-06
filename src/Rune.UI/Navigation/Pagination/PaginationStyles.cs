using Rune.Styles;

namespace Rune.UI;

/// <summary>分页的部件。</summary>
public enum PaginationPart
{
    /// <summary>导航（nav）：页码列表与总数并排。</summary>
    Root,

    /// <summary>页码列表（ul）。</summary>
    List,

    /// <summary>每一项（li，包着按钮或省略号）。</summary>
    Item,

    /// <summary>页码按钮（Button / LinkButton，保留按钮自己的部件标记，同 vue-tsx-pro）。</summary>
    Page,

    /// <summary>省略号。</summary>
    Ellipsis,

    /// <summary>总条数文字。</summary>
    Total,
}

/// <summary>分页栏的部件。</summary>
public enum PaginationBarPart
{
    /// <summary>外层（div）：按自己的宽度取舍（容器查询）。</summary>
    Root,

    /// <summary>起始边的总数（“第 1–20 条，共 320 条”）。</summary>
    Total,

    /// <summary>结束边那一组：页码、每页条数、跳页。</summary>
    Controls,

    /// <summary>跳页（PageJump）：窄于 36em 时不显示。</summary>
    Jump,
}

/// <summary>分页、分页栏、每页条数、跳页的样式（同 vue-tsx-pro 的 paginationStyles 等）。</summary>
public static class PaginationStyles
{
    /// <summary>分页各部件的默认层。</summary>
    public static PartLayers<PaginationPart> Of(ComponentStyles styles) =>
        Recipe.Of("pagination", styles, static (PaginationPart part) => part switch
        {
            PaginationPart.Root => Root,
            PaginationPart.List => Sx.Of(),
            PaginationPart.Item => Sx.Of(),
            PaginationPart.Page => Sx.Of(Page, Tabular),
            PaginationPart.Ellipsis => Ellipsis,
            _ => Sx.Of(Muted, Tabular),
        });

    /// <summary>分页栏各部件的默认层。</summary>
    public static PartLayers<PaginationBarPart> Bar(ComponentStyles styles) =>
        Recipe.Of("pagination-bar", styles, static (PaginationBarPart part) => part switch
        {
            PaginationBarPart.Root => BarRoot,
            PaginationBarPart.Total => Sx.Of(BarTotal, Muted, Tabular),
            PaginationBarPart.Controls => Controls,
            _ => Jump,
        });

    /// <summary>每页条数：所有选项的文字叠在同一格，按最宽的那个撑开，只显示选中的（换条数时按钮不变宽窄）。</summary>
    internal static Style SizeStack => Css.Define(static () => new Style { Display = Display.InlineGrid });

    /// <summary>每页条数的一个选项文字；没选中的加 <see cref="Hidden"/>。</summary>
    internal static Style SizeOption => Css.Define(static () => new Style { GridArea = "1 / 1" });

    /// <summary>占位但看不见。</summary>
    internal static Style Hidden => Css.Define(static () => new Style { Visibility = Visibility.Hidden });

    /// <summary>跳页的外层。</summary>
    internal static Style JumpRoot => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Sm });

    /// <summary>跳页的输入框：放得下三四位页码。</summary>
    internal static Style JumpInput => Css.Define(static () => new Style { Width = CssLength.Em(4.5) });

    /// <summary>跳页前后的提示文字。</summary>
    internal static Style JumpText => Css.Define(static () => new Style { Color = Colors.TextMuted, WhiteSpace = WhiteSpace.Nowrap });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Md,
    });

    // 页码按钮等宽，翻页时位置不晃
    static Style Page => Css.Define(static () => new Style
    {
        MinWidth = $"max({Sizes.Control}, {Sizes.MinTarget})",
        PaddingInline = Space.Xs,
    });

    static Style Ellipsis => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        MinWidth = $"max({Sizes.Control}, {Sizes.MinTarget})",
        Color = Colors.TextMuted,
    });

    static Style Muted => Css.Define(static () => new Style { Color = Colors.TextMuted });

    static Style Tabular => Css.Define(static () => new Style { FontVariantNumeric = FontVariantNumeric.TabularNums });

    // 按自己的宽度决定显示什么：放在侧栏、卡片里时也合适
    static Style BarRoot => Css.Define(static () => new Style
    {
        ContainerType = ContainerType.InlineSize,
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        RowGap = Space.Sm,
        ColumnGap = Space.Md,
    });

    static Style BarTotal => Css.Define(static () => new Style
    {
        FlexBasis = CssLength.Auto,
        [Containers.MaxWidth(CssLength.Em(24))] = new() { FlexBasis = CssLength.Percent(100) },
    });

    static Style Controls => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        RowGap = Space.Sm,
        ColumnGap = Space.Md,
        MarginInlineStart = CssLength.Auto,
    });

    static Style Jump => Css.Define(static () => new Style
    {
        [Containers.MaxWidth(CssLength.Em(36))] = new() { Display = Display.None },
    });
}
