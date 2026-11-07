using Rune.Styles;

namespace Rune.UI;

/// <summary>时间条图的样式（同 vue-tsx-pro 的 timelineChartStyles）：隔行的底色、时间条；悬停的一段由 charts.js 写 data-active。</summary>
internal static class TimelineChartStyles
{
    internal static Style Stripe => Css.Define(static () => new Style { Fill = Colors.SurfaceMuted });

    internal static Style Segment => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 0,
        [When.Attribute("[data-active]")] = new() { Stroke = Colors.Text, StrokeWidth = 1.5 },
    });
}
