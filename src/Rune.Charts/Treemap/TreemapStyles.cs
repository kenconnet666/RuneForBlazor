using Rune.Styles;

namespace Rune.UI;

/// <summary>矩形树图的样式（同 vue-tsx-pro 的 treemapChartStyles）：悬停的块由 charts.js 写 data-active。</summary>
internal static class TreemapStyles
{
    internal static Style Group => Css.Define(static () => new Style { Fill = ChartStyles.Color, Stroke = Stroke.None });

    // 悬停的块描一圈正文色
    internal static Style Tile => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 0,
        Transition = $"stroke-width {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-active]")] = new() { Stroke = Colors.Text, StrokeWidth = 1.5 },
    });

    internal static Style Name => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xs, FontWeight = FontWeights.Medium, PointerEvents = PointerEvents.None });

    internal static Style Value => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xs, Opacity = 0.75, PointerEvents = PointerEvents.None });

    internal static Style GroupName => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs, FontWeight = FontWeights.Medium, PointerEvents = PointerEvents.None });
}
