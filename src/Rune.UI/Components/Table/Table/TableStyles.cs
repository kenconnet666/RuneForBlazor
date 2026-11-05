using Rune.Styles;

namespace Rune.UI;

/// <summary>静态表格的部件（对应原生 table 的各个元素）。</summary>
public enum TablePart
{
    /// <summary>外层（ScrollArea 组件，横向滚动）。</summary>
    Root,

    /// <summary>table。</summary>
    Table,

    /// <summary>标题（caption）。</summary>
    Caption,

    /// <summary>表头（thead）。</summary>
    Head,

    /// <summary>表体（tbody）。</summary>
    Body,

    /// <summary>表尾（tfoot）。</summary>
    Foot,

    /// <summary>行（tr）。</summary>
    Row,

    /// <summary>表头里的列标题（th）。</summary>
    HeaderCell,

    /// <summary>表体、表尾里的行标题（th scope="row"），叠在 <see cref="Cell"/> 上。</summary>
    RowHeader,

    /// <summary>单元格（td）。</summary>
    Cell,

    /// <summary>表尾的格，叠在 <see cref="Cell"/> 或 <see cref="RowHeader"/> 上。</summary>
    FootCell,
}

/// <summary>表格的密度（行高与字号）。</summary>
public enum TableDensity
{
    /// <summary>标准（默认）。</summary>
    Standard,

    /// <summary>紧凑：数据很多、要一屏看更多行（字号也小一档）。</summary>
    Compact,

    /// <summary>宽松：行里有头像、两行文字。</summary>
    Comfortable,
}

/// <summary>表格的样式。</summary>
public enum TableVariant
{
    /// <summary>表面色、外框与圆角（默认）。</summary>
    Surface,

    /// <summary>没有外框，底色同页面：放在卡片、对话框里。</summary>
    Ghost,
}

/// <summary>单元格的对齐。</summary>
public enum TableAlign
{
    /// <summary>起始边（默认）。</summary>
    Start,

    /// <summary>居中。</summary>
    Center,

    /// <summary>结束边：数字、金额。</summary>
    End,
}

/// <summary>单元格所在的区域：表头的 th 是列标题，表体与表尾的 th 是行标题。</summary>
public enum TableSection
{
    /// <summary>表体（默认）。</summary>
    Body,

    /// <summary>表头。</summary>
    Head,

    /// <summary>表尾。</summary>
    Foot,
}

/// <summary>表格的默认外观（UIConfig.Defaults 里配置）。</summary>
public sealed record TableDefaults : IComponentDefaults<TableDefaults>
{
    /// <summary>密度。</summary>
    public TableDensity? Density { get; init; }

    /// <summary>样式。</summary>
    public TableVariant? Variant { get; init; }

    /// <summary>斑马纹。</summary>
    public bool? Striped { get; init; }

    /// <inheritdoc/>
    public TableDefaults Over(TableDefaults outer) => new()
    {
        Density = Density ?? outer.Density,
        Variant = Variant ?? outer.Variant,
        Striped = Striped ?? outer.Striped,
    };
}

/// <summary>Table 交给行与单元格的外观（级联值，配方的键）。</summary>
/// <param name="Density">密度。</param>
/// <param name="Variant">样式。</param>
/// <param name="Striped">斑马纹。</param>
public sealed record TableAppearance(TableDensity Density, TableVariant Variant, bool Striped)
{
    /// <summary>不在 Table 里单独用行与单元格时的外观。</summary>
    public static readonly TableAppearance Standalone = new(TableDensity.Standard, TableVariant.Surface, false);
}

/// <summary>静态表格的样式（同 vue-tsx-pro 的 tableRecipe 与 dataTableStateStyles 里对齐、换行的部分）。</summary>
public static class TableStyles
{
    /// <summary>列宽（写在 th 上的变量）。</summary>
    public static readonly CssVar<CssLength> ColumnWidth = CssVar.Length("rune-table-column-width");

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<TablePart> Of(TableAppearance a, ComponentStyles styles) =>
        Recipe.Of("table", a, styles, static (TableAppearance a, TablePart part) =>
        {
            var padding = a.Density switch
            {
                TableDensity.Compact => CompactPadding,
                TableDensity.Comfortable => ComfortablePadding,
                _ => StandardPadding,
            };
            return part switch
            {
                TablePart.Root => Sx.Of(Root, a.Variant == TableVariant.Ghost ? GhostRoot : SurfaceRoot),
                TablePart.Table => Sx.Of(Table, a.Density == TableDensity.Compact ? CompactTable : null),
                TablePart.Caption => Caption,
                TablePart.Row => Sx.Of(a.Variant == TableVariant.Ghost ? GhostRow : SurfaceRow, a.Striped ? StripedRow : null),
                TablePart.HeaderCell => Sx.Of(HeaderCell, padding),
                TablePart.RowHeader => RowHeader,
                TablePart.Cell => Sx.Of(Cell, padding),
                TablePart.FootCell => FootCell,
                _ => Sx.Of(),
            };
        });

    /// <summary>对齐（单元格、列标题共用；之后的 DataTable 也用）。</summary>
    public static Style AlignOf(TableAlign align) => align switch
    {
        TableAlign.Center => AlignCenter,
        TableAlign.End => AlignEnd,
        _ => AlignStart,
    };

    /// <summary>可以换行（静态表格的单元格默认如此；不换行时超出的部分省略号）。</summary>
    public static Style Wrap => Css.Define(static () => new Style { WhiteSpace = WhiteSpace.Normal, OverflowWrap = OverflowWrap.Anywhere });

    /// <summary>写了列宽时：宽度取 <see cref="ColumnWidth"/>。</summary>
    public static Style Width => Css.Define(static () => new Style { Width = ColumnWidth });

    static Style AlignStart => Css.Define(static () => new Style { TextAlign = TextAlign.Start });

    static Style AlignCenter => Css.Define(static () => new Style { TextAlign = TextAlign.Center });

    static Style AlignEnd => Css.Define(static () => new Style { TextAlign = TextAlign.End });

    // contain: inline-size：宽表格不撑宽外层的 grid / flex 容器，放不下的在这里横向滚动
    static Style Root => Css.Define(static () => new Style { MinWidth = 0, Contain = Contain.InlineSize });

    static Style SurfaceRoot => Css.Define(static () => new Style
    {
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Lg,
        BackgroundColor = Colors.Surface,
    });

    static Style GhostRoot => Css.Define(static () => new Style { BackgroundColor = Colors.Background });

    // 列宽按内容分配：DataTable 为虚拟滚动与列宽拖动用固定布局，静态表格不需要
    static Style Table => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(100),
        TableLayout = TableLayout.Auto,
        BorderCollapse = BorderCollapse.Separate,
        BorderSpacing = 0,
        Color = Colors.Text,
        LineHeight = LineHeights.Normal,
    });

    static Style CompactTable => Css.Define(static () => new Style { FontSize = FontSizes.Sm });

    static Style Caption => Css.Define(static () => new Style
    {
        CaptionSide = CaptionSide.Top,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
        TextAlign = TextAlign.Start,
    });

    static Style SurfaceRow => Css.Define(static () => new Style
    {
        BackgroundColor = Colors.Surface,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });

    static Style GhostRow => Css.Define(static () => new Style
    {
        BackgroundColor = Colors.Background,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });

    static Style StripedRow => Css.Define(static () => new Style { [new Condition(":nth-child(even)")] = new() { BackgroundColor = Colors.Fill } });

    static Style HeaderCell => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BoxSizing = BoxSizing.BorderBox,
        PaddingInline = Space.Md,
        BorderBottomWidth = BorderWidths.Xs,
        BorderBottomStyle = BorderStyle.Solid,
        BorderBottomColor = Colors.Border,
        BackgroundColor = Colors.Fill,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
        FontWeight = FontWeights.Semibold,
        TextAlign = TextAlign.Start,
        WhiteSpace = WhiteSpace.Nowrap,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
    });

    // 表体、表尾里的行标题：和普通单元格一样的底色与字号，只是字重高一点
    static Style RowHeader => Css.Define(static () => new Style { FontWeight = FontWeights.Medium, TextAlign = TextAlign.Start });

    // 数字等宽：金额、数量、日期上下对齐（文字不受影响）
    static Style Cell => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        PaddingInline = Space.Md,
        BorderBottomWidth = BorderWidths.Xs,
        BorderBottomStyle = BorderStyle.Solid,
        BorderBottomColor = Colors.BorderMuted,
        BackgroundColor = CssWide.Inherit,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
        VerticalAlign = VerticalAlign.Middle,
    });

    // 表尾（合计行）：上面一条分隔线，文字加粗，th 与 td 一致
    static Style FootCell => Css.Define(static () => new Style
    {
        FontWeight = FontWeights.Semibold,
        BorderTopWidth = BorderWidths.Xs,
        BorderTopStyle = BorderStyle.Solid,
        BorderTopColor = Colors.Border,
        BorderBottomWidth = 0,
    });

    // 单元格的上下内边距按密度
    static Style CompactPadding => Css.Define(static () => new Style { PaddingBlock = Space.Xs });

    static Style StandardPadding => Css.Define(static () => new Style { PaddingBlock = Space.Sm });

    static Style ComfortablePadding => Css.Define(static () => new Style { PaddingBlock = Space.Md });
}
