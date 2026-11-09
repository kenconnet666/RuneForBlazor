using Rune.Styles;

namespace Rune.UI;

/// <summary>描述列表的部件。</summary>
public enum DescriptionsPart
{
    /// <summary>外层（div）：容器查询的容器。</summary>
    Root,

    /// <summary>标题（div）。</summary>
    Title,

    /// <summary>列表（dl）。</summary>
    List,

    /// <summary>一项（div，包着 dt 与 dd）。</summary>
    Item,

    /// <summary>名称（dt）。</summary>
    Label,

    /// <summary>值（dd）。</summary>
    Value,

    /// <summary>值后面的复制按钮（CopyButton 组件）。</summary>
    Copy,
}

/// <summary>标签与值的排列。</summary>
public enum DescriptionsLayout
{
    /// <summary>横排（默认）：标签在左、值在右。</summary>
    Horizontal,

    /// <summary>竖排：标签在上、值在下。</summary>
    Vertical,

    /// <summary>行内：各项一行排开、放不下换行（页头里的编号、团队、主办这类关键信息）；栏数不起作用。</summary>
    Inline,
}

/// <summary>描述列表的外观（配方的键）。</summary>
/// <param name="Layout">标签与值的排列。</param>
/// <param name="Bordered">带边框（表格样式）。</param>
/// <param name="Size">字号档位；null 时继承。</param>
/// <param name="Columns">固定栏数（<paramref name="MinItemWidth"/> 为 null 时）。</param>
/// <param name="MinItemWidth">自适应栏数时每栏的最小宽度；null 时用固定栏数。</param>
/// <param name="MaxColumns">自适应栏数时最多几栏。</param>
public readonly record struct DescriptionsAppearance(
    DescriptionsLayout Layout, bool Bordered, Size? Size, int Columns, CssLength? MinItemWidth, int MaxColumns) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Layout, Size];

    /// <summary>跨几栏的样式：固定栏数时按栏数封顶、容器窄于 32em 时不跨；自适应时按算出的栏数封顶（同 vue-tsx-pro）。</summary>
    public Sx? SpanOf(int span) =>
        Layout == DescriptionsLayout.Inline || span <= 1 ? null
        : MinItemWidth is { } width ? DescriptionsStyles.AutoSpan(span, width, MaxColumns)
        : Columns > 1 ? (Sx)DescriptionsStyles.Span(Math.Min(span, Columns))
        : null;
}

/// <summary>Descriptions 交给各项的级联值：外观、各部件的默认层与使用方给每项的部件属性。</summary>
/// <param name="Appearance">外观（栏数、跨栏要用）。</param>
/// <param name="Parts">各部件的默认层。</param>
/// <param name="PropsItem">每一项的部件属性。</param>
/// <param name="PropsLabel">名称的部件属性。</param>
/// <param name="PropsValue">值的部件属性。</param>
/// <param name="PropsCopy">复制按钮的部件属性。</param>
public sealed record DescriptionsContext(
    DescriptionsAppearance Appearance, PartLayers<DescriptionsPart> Parts, Attrs? PropsItem, Attrs? PropsLabel, Attrs? PropsValue, Attrs? PropsCopy);

/// <summary>描述列表的样式（同 vue-tsx-pro 的 descriptionsStyles）。</summary>
public static class DescriptionsStyles
{
    /// <summary>容器窄于它时固定栏数的列表排成一列。</summary>
    static readonly Condition Narrow = Containers.MaxWidth(CssLength.Em(32));

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<DescriptionsPart> Of(DescriptionsAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("descriptions", appearance, styles, static (DescriptionsAppearance a, DescriptionsPart part) => part switch
        {
            DescriptionsPart.Root => Sx.Of(Root, a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            DescriptionsPart.Title => Title,
            DescriptionsPart.List => Sx.Create([
                List,
                .. a.Layout == DescriptionsLayout.Inline ? [ListInline]
                    : a.MinItemWidth is { } width ? AutoColumns(width, a.MaxColumns)
                    : [Columns(a.Columns)],
                a.Bordered ? ListBordered : null]),
            DescriptionsPart.Item => Sx.Of(
                a.Layout switch
                {
                    DescriptionsLayout.Vertical => ItemVertical,
                    DescriptionsLayout.Inline => ItemInline,
                    _ => ItemHorizontal,
                },
                a.Bordered ? ItemBordered : null,
                Item),
            DescriptionsPart.Label => Sx.Of(Label, a.Bordered ? CellBordered : null, a.Bordered ? LabelBordered : null),
            DescriptionsPart.Value => Sx.Of(Value, a.Bordered ? CellBordered : null),
            _ => Copy,
        });

    /// <summary>有复制按钮的值：按钮紧跟在文字后面，文字换行时按钮在第一行。</summary>
    internal static Style ValueCopyable => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Baseline,
        Gap = Space.Xs,
    });

    /// <summary>固定栏数时跨 <paramref name="span"/> 栏（已按栏数封顶）。</summary>
    internal static Style Span(int span) => Css.Define(span, static span => new Style
    {
        GridColumn = $"span {span}",
        [Narrow] = new() { GridColumn = CssLength.Auto },
    });

    /// <summary>自适应栏数时跨 <paramref name="span"/> 栏：容器宽到放得下 k 栏时跨 k 栏，直到 span（后写的盖过先写的）。</summary>
    internal static Sx AutoSpan(int span, CssLength width, int maxColumns) =>
        Sx.Create([.. Enumerable.Range(2, Math.Max(0, Math.Min(span, maxColumns) - 1)).Select(count => SpanFrom(count, width))]);

    static Style SpanFrom(int count, CssLength width) => Css.Define((count, width), static k => new Style
    {
        [AtLeast(k.count, k.width)] = new() { GridColumn = $"span {k.count}" },
    });

    // 容器查询的容器：dl 与各项按它的宽度决定栏数、跨度
    static Style Root => Css.Define(static () => new Style { ContainerType = ContainerType.InlineSize, Display = Display.Grid, Gap = Space.Md });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Medium, FontSize = FontSizes.Md });

    static Style List => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        ColumnGap = Space.Xl,
        RowGap = Space.Md,
        Margin = 0,
    });

    static Style Columns(int columns) => Css.Define(columns, static columns => new Style
    {
        GridTemplateColumns = $"repeat({columns}, minmax(0, 1fr))",
        [Narrow] = new() { GridTemplateColumns = "1fr" },
    });

    // 栏数 = 容器宽度 ÷ 最小宽度（向下取整），夹在 1 与最大栏数之间（同 vue-tsx-pro 用 ResizeObserver 算的）；
    // 这里用容器查询，静态 SSR 也对、不需要往返。后写的条件盖过先写的，栏数从少到多排
    static Style?[] AutoColumns(CssLength width, int maxColumns) =>
        [OneColumn, .. Enumerable.Range(2, maxColumns - 1).Select(count => ColumnsFrom(count, width))];

    static Style OneColumn => Css.Define(static () => new Style { GridTemplateColumns = "minmax(0, 1fr)" });

    static Style ColumnsFrom(int count, CssLength width) => Css.Define((count, width), static k => new Style
    {
        [AtLeast(k.count, k.width)] = new() { GridTemplateColumns = $"repeat({k.count}, minmax(0, 1fr))" },
    });

    static Condition AtLeast(int count, CssLength width) => Containers.MinWidth($"calc({count} * {width})");

    static Style ListBordered => Css.Define(static () => new Style
    {
        // 用长写：盖过 List 的 ColumnGap / RowGap
        ColumnGap = 0,
        RowGap = 0,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
        Overflow = Overflow.Hidden,
    });

    static Style ListInline => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Xl,
        RowGap = Space.Xs,
    });

    static Style Item => Css.Define(static () => new Style { MinWidth = 0 });

    static Style ItemHorizontal => Css.Define(static () => new Style { Display = Display.Flex, Gap = Space.Md, AlignItems = AlignItems.Baseline });

    static Style ItemVertical => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xs });

    static Style ItemInline => Css.Define(static () => new Style { Display = Display.Flex, Gap = Space.Xs, AlignItems = AlignItems.Baseline });

    // 每格画上、起始边的线，外框由 dl 画；第一行、第一列的线被 dl 的边框盖住
    static Style ItemBordered => Css.Define(static () => new Style
    {
        AlignItems = AlignItems.Stretch,
        Gap = 0,
        MarginBlockStart = $"calc(-1 * {BorderWidths.Xs})",
        MarginInlineStart = $"calc(-1 * {BorderWidths.Xs})",
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderInlineStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
    });

    static Style Label => Css.Define(static () => new Style { FlexShrink = 0, Margin = 0, Color = Colors.TextMuted });

    static Style CellBordered => Css.Define(static () => new Style { PaddingBlock = Space.Sm, PaddingInline = Space.Md });

    // 暗色主题的 SurfaceMuted 与 Surface 同色，标签格要在两种模式下都看得出来
    static Style LabelBordered => Css.Define(static () => new Style { BackgroundColor = Colors.Fill, MinWidth = CssLength.Em(8) });

    // 占满标签后剩下的宽度：值是表格、可滚动的块时不被压到最窄
    static Style Value => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Margin = 0,
        Color = Colors.Text,
        OverflowWrap = OverflowWrap.Anywhere,
    });

    static Style Copy => Css.Define(static () => new Style { FlexShrink = 0, MarginBlock = CssLength.Em(-0.25) });
}
