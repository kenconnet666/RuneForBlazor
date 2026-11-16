using Rune.Styles;

namespace Rune.UI;

/// <summary>查询表格的部件。</summary>
public enum QueryTablePart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>查询栏（FilterBar）。</summary>
    FilterBar,

    /// <summary>工具栏与表格所在的区域（全屏的就是它）。</summary>
    Panel,

    /// <summary>没有查询栏或全屏时表格上方单独的一行。</summary>
    ToolbarRow,

    /// <summary>标题。</summary>
    Title,

    /// <summary>工具栏：自己的按钮与内置按钮。</summary>
    Toolbar,

    /// <summary>内置按钮：刷新、行高、列设置、全屏。</summary>
    Tools,

    /// <summary>批量操作栏（SelectionBar）。</summary>
    SelectionBar,

    /// <summary>表格（DataTable）。</summary>
    Table,

    /// <summary>分页栏（PaginationBar）。</summary>
    Pagination,
}

/// <summary>查询表格的样式（同 vue-tsx-pro 的 queryTableStyles）。</summary>
public static class QueryTableStyles
{
    static readonly Condition Fullscreen = When.Attribute("[data-rune-fullscreen=true]");

    /// <summary>各部件的默认层；<paramref name="fill"/>：占满到窗口底边。</summary>
    public static PartLayers<QueryTablePart> Of(bool fill, ComponentStyles styles) =>
        Recipe.Of("query-table", fill, styles, static (bool fill, QueryTablePart part) => part switch
        {
            QueryTablePart.Root => fill ? Sx.Of(Root, RootFill) : Sx.Of(Root),
            QueryTablePart.Panel => fill ? Sx.Of(Panel, PanelFill) : Sx.Of(Panel),
            QueryTablePart.ToolbarRow => ToolbarRow,
            QueryTablePart.Title => Title,
            QueryTablePart.Toolbar => Toolbar,
            QueryTablePart.Tools => Tools,
            _ => Sx.Of((Style?)null),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Md, MinWidth = 0 });

    // fill 时外面的高度定了，整块与区域可以压矮，压不下的在表格里滚动
    static Style RootFill => Css.Define(static () => new Style { FlexGrow = 1, MinHeight = 0 });

    // 全屏时铺满屏幕、四周留白，表格占满剩下的高度。浏览器不让全屏（iframe 没写 allow="fullscreen"）时照样铺满窗口
    static Style Panel => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        MinWidth = 0,
        BackgroundColor = Colors.Surface,
        [Fullscreen] = new() { Position = Position.Fixed, Inset = 0, ZIndex = 50, Padding = Space.Lg, Overflow = Overflow.Hidden },
    });

    static Style PanelFill => Css.Define(static () => new Style { MinHeight = 0 });

    // 没有查询栏或全屏时，表格上方单独一行：标题在起始边，按钮在结束边
    static Style ToolbarRow => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, FlexWrap = FlexWrap.Wrap, Gap = Space.Sm });

    // 自己的按钮与内置按钮：在查询栏第一排的按钮区里（查询后面），或在单独那一行的结束边
    static Style Toolbar => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Sm,
        MinWidth = 0,
        MaxWidth = CssLength.Percent(100),
        MarginInlineStart = CssLength.Auto,
    });

    static Style Title => Css.Define(static () => new Style { MinWidth = 0, FontWeight = FontWeights.Semibold, FontSize = FontSizes.Lg });

    static Style Tools => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs });
}
