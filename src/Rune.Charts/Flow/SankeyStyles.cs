using Rune.Styles;

namespace Rune.UI;

/// <summary>桑基图的样式（同 vue-tsx-pro 的 sankeyChartStyles）：悬停时 charts.js 写 data-active（突出）、data-dimmed（变淡）。</summary>
internal static class SankeyStyles
{
    internal static Style Node => Css.Define(static () => new Style { Fill = ChartStyles.Color, Stroke = Colors.Surface, StrokeWidth = 1 });

    internal static Style Link => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Stroke = ChartStyles.Color,
        StrokeOpacity = 0.35,
        Transition = $"stroke-opacity {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-active]")] = new() { StrokeOpacity = 0.6 },
        [When.Attribute("[data-dimmed]")] = new() { StrokeOpacity = 0.12 },
    });

    internal static Style Name => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xs, FontWeight = FontWeights.Medium, PointerEvents = PointerEvents.None });

    internal static Style Value => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs, PointerEvents = PointerEvents.None });
}
