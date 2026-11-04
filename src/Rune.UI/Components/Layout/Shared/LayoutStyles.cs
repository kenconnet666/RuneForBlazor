using Rune.Styles;

namespace Rune.UI;

/// <summary>方向：分隔线、单选组、按钮组、滑块等共用（同 vue-tsx-pro 的 orientation）。各组件的默认方向不同，写在组件参数上。</summary>
public enum Orientation
{
    /// <summary>横向。</summary>
    Horizontal,

    /// <summary>竖向。</summary>
    Vertical,
}

/// <summary>flex 的主轴方向。</summary>
public enum LayoutDirection
{
    /// <summary>横排（默认）。</summary>
    Row,

    /// <summary>竖排：子元素从上到下。</summary>
    Column,

    /// <summary>横排，顺序反过来。</summary>
    RowReverse,

    /// <summary>竖排，顺序反过来。</summary>
    ColumnReverse,
}

/// <summary>子项在交叉轴上的对齐（align-items、justify-items）。</summary>
public enum ItemAlign
{
    /// <summary>贴着起点（横排时顶部对齐）。</summary>
    Start,

    /// <summary>居中。</summary>
    Center,

    /// <summary>贴着终点。</summary>
    End,

    /// <summary>拉伸填满（默认）。</summary>
    Stretch,

    /// <summary>按文字基线对齐：字号不同的文字、图标与文字混排。</summary>
    Baseline,
}

/// <summary>主轴上的分布（justify-content）。</summary>
public enum ContentJustify
{
    /// <summary>靠起点（默认）。</summary>
    Start,

    /// <summary>居中。</summary>
    Center,

    /// <summary>靠终点。</summary>
    End,

    /// <summary>两端对齐，中间等距：首尾贴边。</summary>
    Between,

    /// <summary>每项两侧留白相等：首尾留白是中间的一半。</summary>
    Around,

    /// <summary>所有空隙相等，包括首尾。</summary>
    Evenly,
}

/// <summary>grid 的自动排布方向（grid-auto-flow）。</summary>
public enum GridFlow
{
    /// <summary>逐行填充（默认）。</summary>
    Row,

    /// <summary>逐列填充。</summary>
    Column,

    /// <summary>逐行填充，并回填前面留下的空位（顺序可能变化）。</summary>
    RowDense,

    /// <summary>逐列填充，并回填空位。</summary>
    ColumnDense,
}

/// <summary>布局组件的部件。</summary>
public enum LayoutPart
{
    /// <summary>容器本身。</summary>
    Root,
}

/// <summary>Flex 的外观：交给 <see cref="LayoutStyles.Flex"/> 的档位组合，null 表示用 CSS 的默认值。</summary>
/// <param name="Direction">主轴方向。</param>
/// <param name="Align">交叉轴对齐。</param>
/// <param name="Justify">主轴分布。</param>
/// <param name="Wrap">换行。</param>
/// <param name="Gap">间隙。</param>
/// <param name="RowGap">行间隙，盖过 Gap。</param>
/// <param name="ColumnGap">列间隙，盖过 Gap。</param>
/// <param name="Inline">行内（inline-flex）。</param>
/// <param name="Grow">在父级的 flex 布局里占满剩余空间。</param>
/// <param name="List">列表（FlexList）：去掉圆点与缩进。</param>
public readonly record struct FlexAppearance(
    LayoutDirection? Direction = null,
    ItemAlign? Align = null,
    ContentJustify? Justify = null,
    bool Wrap = false,
    SpaceScale? Gap = null,
    SpaceScale? RowGap = null,
    SpaceScale? ColumnGap = null,
    bool Inline = false,
    bool Grow = false,
    bool List = false);

/// <summary>Grid 的外观：交给 <see cref="LayoutStyles.Grid"/> 的档位组合。</summary>
/// <param name="Columns">等宽列数。</param>
/// <param name="AutoFit">按最小宽度自动决定列数（宽度本身经 CSS 变量写在元素上）。</param>
/// <param name="Gap">间隙。</param>
/// <param name="RowGap">行间隙。</param>
/// <param name="ColumnGap">列间隙。</param>
/// <param name="Align">单元格在块方向的对齐。</param>
/// <param name="Justify">单元格在行内方向的对齐。</param>
/// <param name="Flow">自动排布方向。</param>
/// <param name="List">列表（GridList）：去掉圆点与缩进。</param>
public readonly record struct GridAppearance(
    int? Columns = null,
    bool AutoFit = false,
    SpaceScale? Gap = null,
    SpaceScale? RowGap = null,
    SpaceScale? ColumnGap = null,
    ItemAlign? Align = null,
    ItemAlign? Justify = null,
    GridFlow? Flow = null,
    bool List = false);

/// <summary>容器的最大宽度档位，对应主题 sizes 的容器宽度（rem，跟随浏览器字号）。</summary>
public enum ContainerSize
{
    /// <summary>30rem：窄内容，如登录表单。</summary>
    Xs,

    /// <summary>40rem：表单页、设置页。</summary>
    Sm,

    /// <summary>48rem：文章、详情页。</summary>
    Md,

    /// <summary>64rem：常规页面（默认）。</summary>
    Lg,

    /// <summary>80rem：宽表格、仪表盘。</summary>
    Xl,
}

/// <summary>布局组件（Flex、Grid、GridItem）的样式（配方）。</summary>
public static class LayoutStyles
{
    /// <summary>
    /// 自动换列时每项的最小宽度。宽度是任意长度，写进样式会每个值多一条规则，所以经变量写在元素上。
    /// </summary>
    public static readonly CssVar<CssLength> MinItemWidth = CssVar.Length("rune-grid-min-item-width");

    /// <summary>Flex 的部件默认层。</summary>
    public static PartLayers<LayoutPart> Flex(FlexAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("flex", appearance, styles, static (FlexAppearance a, LayoutPart _) => Sx.Of(
            a.Inline ? InlineFlex : FlexRoot,
            a.Direction is { } direction ? DirectionOf(direction) : null,
            a.Align is { } align ? AlignItemsOf(align) : null,
            a.Justify is { } justify ? JustifyContentOf(justify) : null,
            a.Wrap ? WrapStyle : null,
            a.Gap is { } gap ? ScaleStyles.Gap(gap) : null,
            a.RowGap is { } rowGap ? ScaleStyles.RowGap(rowGap) : null,
            a.ColumnGap is { } columnGap ? ScaleStyles.ColumnGap(columnGap) : null,
            a.Grow ? Grow : null,
            a.List ? ListReset : null));

    /// <summary>Grid 的部件默认层。</summary>
    public static PartLayers<LayoutPart> Grid(GridAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("grid", appearance, styles, static (GridAppearance a, LayoutPart _) => Sx.Of(
            GridRoot,
            a.AutoFit ? AutoFitColumns : a.Columns is { } columns ? ColumnsOf(columns) : null,
            a.Gap is { } gap ? ScaleStyles.Gap(gap) : null,
            a.RowGap is { } rowGap ? ScaleStyles.RowGap(rowGap) : null,
            a.ColumnGap is { } columnGap ? ScaleStyles.ColumnGap(columnGap) : null,
            a.Align is { } align ? AlignItemsOf(align) : null,
            a.Justify is { } justify ? JustifyItemsOf(justify) : null,
            a.Flow is { } flow ? FlowOf(flow) : null,
            a.List ? ListReset : null));

    /// <summary>GridItem 的部件默认层：跨几列（<see cref="int.MaxValue"/> 表示占满一整行）、跨几行。</summary>
    public static PartLayers<LayoutPart> GridItem(int? span, int? rowSpan, ComponentStyles styles) =>
        Recipe.Of("grid-item", (span, rowSpan), styles, static ((int? Span, int? RowSpan) a, LayoutPart _) => Sx.Of(
            a.Span switch
            {
                int.MaxValue => FullRow,
                > 1 and int span => SpanOf(span),
                _ => null,
            },
            a.RowSpan is > 1 and int rows ? RowSpanOf(rows) : null));

    /// <summary>Container 的部件默认层。</summary>
    public static PartLayers<LayoutPart> Container(ContainerSize size, SpaceScale padding, ComponentStyles styles) =>
        Recipe.Of("container", (size, padding), styles, static ((ContainerSize Size, SpaceScale Padding) a, LayoutPart _) =>
            Sx.Of(ContainerRoot, ContainerWidth(a.Size), ScaleStyles.PaddingInline(a.Padding)));

    static Style ContainerRoot => Css.Define(static () => new Style { BoxSizing = BoxSizing.BorderBox, Width = "100%", MarginInline = "auto" });

    static Style ContainerWidth(ContainerSize size) => Css.Define(size, static size => new Style
    {
        MaxWidth = size switch
        {
            ContainerSize.Xs => Sizes.Xs,
            ContainerSize.Sm => Sizes.Sm,
            ContainerSize.Md => Sizes.Md,
            ContainerSize.Xl => Sizes.Xl,
            _ => Sizes.Lg,
        },
    });

    // 列表当布局容器：去掉默认的圆点与缩进（组件同时补上 role="list"）
    static Style ListReset => Css.Define(static () => new Style { ListStyle = "none", Margin = 0, Padding = 0 });

    static Style FlexRoot => Css.Define(static () => new Style { Display = Display.Flex });

    static Style InlineFlex => Css.Define(static () => new Style { Display = Display.InlineFlex });

    static Style WrapStyle => Css.Define(static () => new Style { FlexWrap = FlexWrap.Wrap });

    // 占满剩余空间，并允许收缩到比内容窄（里面的文字才能截断）
    static Style Grow => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0, MinHeight = 0 });

    static Style DirectionOf(LayoutDirection direction) => Css.Define(direction, static direction => new Style
    {
        FlexDirection = direction switch
        {
            LayoutDirection.Column => FlexDirection.Column,
            LayoutDirection.RowReverse => FlexDirection.RowReverse,
            LayoutDirection.ColumnReverse => FlexDirection.ColumnReverse,
            _ => FlexDirection.Row,
        },
    });

    static Style AlignItemsOf(ItemAlign align) => Css.Define(align, static align => new Style { AlignItems = AlignValue(align) });

    static Style JustifyItemsOf(ItemAlign align) => Css.Define(align, static align => new Style { JustifyItems = AlignValue(align) });

    // flex 与 grid 都认 start / end，统一用它们（flex-start 只对 flex 有意义）
    static string AlignValue(ItemAlign align) => align switch
    {
        ItemAlign.Start => "start",
        ItemAlign.Center => "center",
        ItemAlign.End => "end",
        ItemAlign.Baseline => "baseline",
        _ => "stretch",
    };

    static Style JustifyContentOf(ContentJustify justify) => Css.Define(justify, static justify => new Style
    {
        JustifyContent = justify switch
        {
            ContentJustify.Center => "center",
            ContentJustify.End => "end",
            ContentJustify.Between => "space-between",
            ContentJustify.Around => "space-around",
            ContentJustify.Evenly => "space-evenly",
            _ => "start",
        },
    });

    static Style GridRoot => Css.Define(static () => new Style { Display = Display.Grid });

    // minmax(0, 1fr)：列宽不被长内容撑开
    static Style ColumnsOf(int columns) => Css.Define(columns, static columns => new Style
    {
        GridTemplateColumns = $"repeat({columns}, minmax(0, 1fr))",
    });

    // auto-fit：项目比放得下的列数少时，空列收起、各项拉宽占满；min(…, 100%) 让窄容器里单列不溢出
    static Style AutoFitColumns => Css.Define(static () => new Style
    {
        GridTemplateColumns = $"repeat(auto-fit, minmax(min({MinItemWidth.Reference}, 100%), 1fr))",
    });

    static Style FlowOf(GridFlow flow) => Css.Define(flow, static flow => new Style
    {
        GridAutoFlow = flow switch
        {
            GridFlow.Column => "column",
            GridFlow.RowDense => "row dense",
            GridFlow.ColumnDense => "column dense",
            _ => "row",
        },
    });

    static Style FullRow => Css.Define(static () => new Style { GridColumn = "1 / -1" });

    static Style SpanOf(int span) => Css.Define(span, static span => new Style { GridColumn = $"span {span} / span {span}" });

    static Style RowSpanOf(int rows) => Css.Define(rows, static rows => new Style { GridRow = $"span {rows} / span {rows}" });
}
