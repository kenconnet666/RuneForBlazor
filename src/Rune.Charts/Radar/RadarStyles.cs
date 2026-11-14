using Rune.Styles;

namespace Rune.UI;

/// <summary>雷达图的样式（同 vue-tsx-pro 的 radarChartStyles）：悬停的维度由 charts.js 写 data-active。</summary>
internal static class RadarStyles
{
    // 字号是固定画布（360 × 300 的 viewBox）里的单位，随整张图缩放：不是页面上的 px
#pragma warning disable RUNE007
    internal static Style Svg => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Overflow = Overflow.Visible,
        UserSelect = UserSelect.None,
        Direction = Direction.Ltr,
    });

    internal static Style Grid => Css.Define(static () => new Style { Fill = Fill.None, Stroke = Colors.BorderMuted, StrokeWidth = 1 });

    internal static Style Spoke => Css.Define(static () => new Style
    {
        Stroke = Colors.BorderMuted,
        StrokeWidth = 1,
        [When.Attribute("[data-active]")] = new() { Stroke = Colors.TextMuted, StrokeWidth = 1.5 },
    });

    internal static Style Tick => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = 9 });

    internal static Style Label => Css.Define(static () => new Style
    {
        Fill = Colors.Text,
        FontSize = 11,
        [When.Attribute("[data-active]")] = new() { FontWeight = FontWeights.Semibold },
    });

    // 几个系列叠在一起：填充很浅，混在一起也不发灰
    internal static Style Area => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        FillOpacity = 0.08,
        Stroke = ChartStyles.Color,
        StrokeWidth = 2,
        StrokeLinejoin = StrokeLinejoin.Round,
    });

    // 悬停的维度上顶点放大（2.5 → 4）
    internal static Style Vertex => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        StrokeWidth = 0,
        TransformBox = TransformBox.FillBox,
        TransformOrigin = TransformOrigin.Center,
        PointerEvents = PointerEvents.None,
        [When.Attribute("[data-active]")] = new() { Scale = "1.6" },
    });

    // 顶点旁的数值：只在悬停这一维时显示
    internal static Style Value => Css.Define(static () => new Style
    {
        Display = Display.None,
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 3,
        PaintOrder = PaintOrder.Stroke,
        FontSize = 10,
        FontWeight = FontWeights.Semibold,
        PointerEvents = PointerEvents.None,
        [When.Attribute("[data-active]")] = new() { Display = Display.Inline },
    });

    // 悬停用的扇区：透明，只接指针
    internal static Style Sector => Css.Define(static () => new Style { Fill = CssColors.Transparent });
#pragma warning restore RUNE007
}
