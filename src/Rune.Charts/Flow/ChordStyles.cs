using Rune.Styles;

namespace Rune.UI;

/// <summary>弦图的样式（同 vue-tsx-pro 的 chordChartStyles）：悬停时 charts.js 写 data-active（突出）、data-dimmed（变淡）。</summary>
internal static class ChordStyles
{
    internal static Style Node => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Transition = $"opacity {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-dimmed]")] = new() { Opacity = 0.3 },
    });

    internal static Style Link => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        FillOpacity = 0.45,
        Stroke = Colors.Surface,
        StrokeOpacity = 0.6,
        StrokeWidth = 0.5,
        Transition = $"fill-opacity {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-active]")] = new() { FillOpacity = 0.85 },
        [When.Attribute("[data-dimmed]")] = new() { FillOpacity = 0.12 },
    });

    internal static Style Label => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs, PointerEvents = PointerEvents.None });
}
