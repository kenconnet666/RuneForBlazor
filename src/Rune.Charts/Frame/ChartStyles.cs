using Rune.Styles;

namespace Rune.UI;

/// <summary>图表外层结构的部件（同 vue-tsx-pro 的 ChartFrameParts）。</summary>
public enum ChartPart
{
    /// <summary>根元素（div role="figure"）。</summary>
    Root,

    /// <summary>上方一行：单位、图例、操作。</summary>
    Header,

    /// <summary>单位。</summary>
    Unit,

    /// <summary>图例（role="group"）。</summary>
    Legend,

    /// <summary>图例的一项（按钮，aria-pressed）。</summary>
    LegendItem,

    /// <summary>绘图区：SVG 画在这里，对读屏隐藏。</summary>
    Plot,

    /// <summary>给读屏的数据表（视觉上隐藏）。</summary>
    Table,
}

/// <summary>图表外层结构的样式（同 vue-tsx-pro 的 chartStyles）。</summary>
public static class ChartStyles
{
    /// <summary>系列颜色（色块、图形的填充与描边）：每个元素写在自己的 style 上。</summary>
    internal static readonly CssVar<CssColor> Color = CssVar.Color("rune-chart-color");

    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<ChartPart> Of(ComponentStyles styles) =>
        Recipe.Of("chart", styles, static (ChartPart part) => part switch
        {
            ChartPart.Root => Root,
            ChartPart.Header => Header,
            ChartPart.Unit => Unit,
            ChartPart.Legend => Legend,
            ChartPart.LegendItem => LegendItem,
            ChartPart.Plot => Plot,
            _ => SharedStyles.VisuallyHidden,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        // 在居中、弹性布局的容器里也占满一行：绘图区按容器宽度自适应
        Width = CssLength.Percent(100),
        MinWidth = 0,
        FontSize = FontSizes.Sm,
    });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Md,
        MinHeight = CssLength.Em(1.5),
    });

    static Style Unit => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = FontSizes.Xs });

    static Style Legend => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = $"{Space.Xs} {Space.Md}",
        MarginInlineStart = CssLength.Auto,
    });

    // 图例项：ButtonBase 给出焦点环与按下的反馈，这里只定文字；隐藏的系列划线（不降低不透明度，文字仍要满足对比度）
    static Style LegendItem => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        BorderRadius = Radii.Sm,
        [When.Hover] = new() { Color = Colors.Text },
        [When.Attribute("[aria-pressed=\"false\"]")] = new() { TextDecorationLine = TextDecorationLine.LineThrough },
    });

    /// <summary>色块：图例、提示框里系列的颜色；隐藏的系列是空心的。</summary>
    internal static Style Swatch => Css.Define(static () => new Style
    {
        Width = CssLength.Em(0.625),
        Height = CssLength.Em(0.625),
        BorderRadius = Radii.Sm,
        FlexShrink = 0,
        BackgroundColor = Color,
        [When.Attribute("[data-off]")] = new() { BackgroundColor = CssColors.Transparent, BoxShadow = $"inset 0 0 0 {BorderWidths.Md} {Color}" },
    });

    // contain: inline-size：画好的 SVG 写死了宽度，不能让它反过来撑住按内容定宽的外层（表格式的滚动区、起始对齐的弹性布局）；
    // 不然容器变窄时图表还是原来那么宽，量不到变化也就不重画
    static Style Plot => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        MinWidth = 0,
        Contain = Contain.InlineSize,
        Height = PlotHeight,
        // 点了能下钻：手形指针
        [When.Attribute("[data-clickable]")] = new() { Cursor = Cursor.Pointer },
    });

    /// <summary>绘图区的高度。</summary>
    internal static readonly CssVar<CssLength> PlotHeight = CssVar.Length("rune-chart-plot-height", "240px");

    // 加载中的骨架：一整块
    internal static Style SkeletonBlock => Css.Define(static () => new Style { Display = Display.Block, Width = CssLength.Percent(100), Height = CssLength.Percent(100) });

    internal static Style Center => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Height = CssLength.Percent(100) });

    // 提示框：放在指针右侧，过了绘图区的一半放到左侧（位置由 charts.js 写在 style 上）；消失时淡出
    internal static Style Tooltip => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = 0,
        Left = 0,
        ZIndex = 1,
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceRaised,
        Color = Colors.Text,
        BoxShadow = Shadows.Md,
        FontSize = FontSizes.Xs,
        WhiteSpace = WhiteSpace.Nowrap,
        PointerEvents = PointerEvents.None,
        Opacity = 0,
        Transition = $"opacity {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-open]")] = new() { Opacity = 1 },
        // 第一次定位之后才跟着指针滑动：不从左上角飞过来
        [When.Attribute("[data-placed]")] = new() { Transition = $"opacity {Durations.Sm} {Easings.Standard}, transform {Durations.Sm} {Easings.Standard}" },
    });

    internal static Style TooltipTitle => Css.Define(static () => new Style { FontWeight = FontWeights.Medium });

    internal static Style TooltipRow => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm });

    internal static Style TooltipLabel => Css.Define(static () => new Style { Color = Colors.TextMuted, FlexGrow = 1 });

    internal static Style TooltipValue => Css.Define(static () => new Style { FontVariantNumeric = FontVariantNumeric.TabularNums, FontWeight = FontWeights.Medium, MarginInlineStart = Space.Lg });
}

/// <summary>图表 SVG 的共用样式（同 vue-tsx-pro 的 chartSvgStyles、chartMarkStyles）：颜色、字号都是主题 tokens，进场的时长是 durations。</summary>
internal static class ChartSvgStyles
{
    internal static Style Svg => Css.Define(static () => new Style { Display = Display.Block, Overflow = Overflow.Visible, UserSelect = UserSelect.None, TouchAction = TouchAction.PanY });

    internal static Style Grid => Css.Define(static () => new Style { Stroke = Colors.BorderMuted, StrokeWidth = 1, ShapeRendering = ShapeRendering.CrispEdges });

    // 零线（数值轴有负数时）
    internal static Style Baseline => Css.Define(static () => new Style { Stroke = Colors.Border, StrokeWidth = 1, ShapeRendering = ShapeRendering.CrispEdges });

    internal static Style Tick => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs });

    // 十字线：指针所在类目的竖线
    internal static Style Crosshair => Css.Define(static () => new Style
    {
        Stroke = Colors.BorderStrong,
        StrokeWidth = 1,
        StrokeDasharray = "4 3",
        PointerEvents = PointerEvents.None,
        ShapeRendering = ShapeRendering.CrispEdges,
    });

    internal static Style CrosshairDot => Css.Define(static () => new Style { Fill = ChartStyles.Color, Stroke = Colors.Surface, StrokeWidth = 2, PointerEvents = PointerEvents.None });

    // 柱状图悬停的类目：整格一层浅底
    internal static Style HoverBand => Css.Define(static () => new Style { Fill = Colors.Fill, PointerEvents = PointerEvents.None });

    // 文字描一圈表面色的边：压在图形上也看得清
    internal static Style Halo => Css.Define(static () => new Style { PaintOrder = PaintOrder.Stroke, Stroke = Colors.Surface, StrokeWidth = 3, StrokeLinejoin = StrokeLinejoin.Round });

    internal static Style Label => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xs, PointerEvents = PointerEvents.None });

    internal static Style LabelMuted => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs, PointerEvents = PointerEvents.None });

    // 目标线、标注的文字用自己的颜色
    internal static Style LabelColored => Css.Define(static () => new Style { Fill = ChartStyles.Color, FontSize = FontSizes.Xs, PointerEvents = PointerEvents.None });

    internal static Style TargetLine => Css.Define(static () => new Style { Stroke = ChartStyles.Color, StrokeWidth = 1.5, StrokeDasharray = "6 4", PointerEvents = PointerEvents.None });

    internal static Style EventLine => Css.Define(static () => new Style { Stroke = ChartStyles.Color, StrokeWidth = 1, StrokeDasharray = "3 3", PointerEvents = PointerEvents.None });

    internal static Style Mark => Css.Define(static () => new Style { Fill = Fill.None, Stroke = ChartStyles.Color, StrokeWidth = 2, PointerEvents = PointerEvents.None });

    internal static Style BandFill => Css.Define(static () => new Style { Fill = $"color-mix(in srgb, {ChartStyles.Color} 12%, transparent)", PointerEvents = PointerEvents.None });

    // 柱：从基线那一端长出来
    internal static Sx Bar(BarEnd grow) => Bars[(int)grow];

    static readonly Sx[] Bars = [Sx.Of(BarBase, GrowUp), Sx.Of(BarBase, GrowDown), Sx.Of(BarBase, GrowLeft), Sx.Of(BarBase, GrowRight), Sx.Of(BarBase, GrowUp)];

    static Style BarBase => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        TransformBox = TransformBox.FillBox,
        Transition = $"scale {Durations.Lg} {Easings.Standard}, opacity {Durations.Sm} {Easings.Standard}",
    });

    static Style GrowUp => Css.Define(static () => new Style { TransformOrigin = TransformOrigin.Bottom, Scale = "1", [When.Starting] = new() { Scale = "1 0" } });
    static Style GrowDown => Css.Define(static () => new Style { TransformOrigin = TransformOrigin.Top, Scale = "1", [When.Starting] = new() { Scale = "1 0" } });
    static Style GrowRight => Css.Define(static () => new Style { TransformOrigin = TransformOrigin.Left, Scale = "1", [When.Starting] = new() { Scale = "0 1" } });
    static Style GrowLeft => Css.Define(static () => new Style { TransformOrigin = TransformOrigin.Right, Scale = "1", [When.Starting] = new() { Scale = "0 1" } });

    // 线：pathLength 归一成 1，虚线的偏移从 1 到 0，看起来是从头描到尾
    internal static Style Line => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Stroke = ChartStyles.Color,
        StrokeLinejoin = StrokeLinejoin.Round,
        StrokeLinecap = StrokeLinecap.Round,
        StrokeDasharray = "1",
        StrokeDashoffset = 0,
        [When.Starting] = new() { StrokeDashoffset = 1 },
        Transition = $"stroke-dashoffset {Durations.Xl} {Easings.Standard}",
        PointerEvents = PointerEvents.None,
    });

    internal static Style Area => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Stroke.None,
        PointerEvents = PointerEvents.None,
        Opacity = 1,
        [When.Starting] = new() { Opacity = 0 },
        Transition = $"opacity {Durations.Lg} {Easings.Standard}",
    });

    static Style Dot => Css.Define(static () => new Style
    {
        TransformBox = TransformBox.FillBox,
        TransformOrigin = TransformOrigin.Center,
        Scale = "1",
        [When.Starting] = new() { Scale = "0" },
        Transition = $"scale {Durations.Lg} {Easings.Standard}",
        PointerEvents = PointerEvents.None,
    });

    internal static readonly Sx HollowDot = Sx.Of(Dot, Hollow);

    internal static readonly Sx FilledDot = Sx.Of(Dot, Filled);

    static Style Hollow => Css.Define(static () => new Style { Fill = Colors.Surface, Stroke = ChartStyles.Color, StrokeWidth = 2 });

    static Style Filled => Css.Define(static () => new Style { Fill = ChartStyles.Color, Stroke = Colors.Surface, StrokeWidth = 1.5 });
}
