using Rune.Styles;

namespace Rune.UI;

/// <summary>地图的样式（同 vue-tsx-pro 的 mapChartStyles）：悬停的区域由 charts.js 写 data-active。</summary>
internal static class MapStyles
{
    internal static Style Viewport => Css.Define(static () => new Style { Position = Position.Absolute, Inset = 0, TouchAction = TouchAction.PanY, Overflow = Overflow.Hidden });

    // 地图跟随主题：底色、边界是表面色；缩放时边界不跟着变粗（vector-effect）
    internal static Style Feature => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 0.8,
        FillRule = FillRule.Evenodd,
        Transition = $"fill {Durations.Md} {Easings.Standard}",
        [When.Attribute("[data-active]")] = new() { Stroke = Colors.Text, StrokeWidth = 1.5 },
    });

    internal static Style Sphere => Css.Define(static () => new Style { Fill = Colors.SurfaceMuted, Stroke = Colors.Border, StrokeWidth = 1 });

    internal static Style Link => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Stroke = ChartStyles.Color,
        StrokeOpacity = 0.7,
        StrokeLinecap = StrokeLinecap.Round,
        PointerEvents = PointerEvents.None,
    });

    // 流动的粒子：“减少动态效果”时不播
    internal static Style Particle => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        PointerEvents = PointerEvents.None,
        [Media.ReducedMotion] = new() { Display = Display.None },
    });

    internal static Style Point => Css.Define(static () => new Style { Fill = ChartStyles.Color, Stroke = Colors.Surface, StrokeWidth = 1.5 });

    internal static Style Clickable => Css.Define(static () => new Style { Cursor = Cursor.Pointer });

    // 地点的名字、区域名：描一圈表面色的边，压在区域、连线上也看得清；字号是 SVG 里的像素
#pragma warning disable RUNE007
    internal static Style PointLabel => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = 11, FontWeight = FontWeights.Semibold });

    internal static Style AreaLabel => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = 10 });
#pragma warning restore RUNE007

    internal static Style Leader => Css.Define(static () => new Style { Stroke = Colors.TextMuted, StrokeWidth = 1 });

    // 缩放按钮：在标题行的结束边（导出菜单前面）
    internal static Style Zoom => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = CssLength.Rem(0.125), MarginInlineStart = CssLength.Auto });

    internal static Style WheelHint => Css.Define(static () => new Style
    {
        MarginInlineEnd = Space.Sm,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        Display = Display.None,
        [When.Attribute("[data-open]")] = new() { Display = Display.Inline },
    });

    // 图例：连续是色带，分档是一排色块
    internal static Style Legend => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        ColumnGap = Space.Md,
        RowGap = Space.Xs,
        FontSize = FontSizes.Xs,
        Color = Colors.TextMuted,
        PaddingInline = Space.Sm,
    });

    static Style RampStyle => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MaxWidth = CssLength.Rem(16),
        Height = CssLength.Rem(0.5),
        BorderRadius = Radii.Full,
    });

    /// <summary>连续着色的色带：从最小到最大（渐变随数据变，写在元素自己的 style 上）。</summary>
    internal static Attrs Ramp(IEnumerable<string> stops) => new() { Sx = RampStyle, InlineStyle = $"background-image: linear-gradient(to right, {string.Join(", ", stops)})" };

    internal static Style Bin => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = CssLength.Em(0.375), WhiteSpace = WhiteSpace.Nowrap });

    internal static Style Swatch => Css.Define(static () => new Style
    {
        Width = CssLength.Rem(0.875),
        Height = CssLength.Rem(0.625),
        BorderRadius = CssLength.Rem(0.125),
        BackgroundColor = ChartStyles.Color,
    });
}
