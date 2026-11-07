using Rune.Styles;

namespace Rune.UI;

/// <summary>环形图的样式（同 vue-tsx-pro 的 donutChartStyles）。</summary>
internal static class DonutStyles
{
    // 进场：整个环从略小淡入放大
    internal static Style Ring => Css.Define(static () => new Style
    {
        TransformBox = TransformBox.FillBox,
        TransformOrigin = TransformOrigin.Center,
        Opacity = 1,
        Scale = "1",
        [When.Starting] = new() { Opacity = 0, Scale = "0.9" },
        Transition = $"opacity {Durations.Lg} {Easings.Standard}, scale {Durations.Lg} {Easings.Standard}",
    });

    internal static Style Background => Css.Define(static () => new Style { Fill = Colors.Fill });

    // 扇区之间描一圈表面色；悬停别的扇区时变淡（charts.js 写 data-dimmed）
    internal static Style Segment => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 1,
        Transition = $"opacity {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-dimmed]")] = new() { Opacity = 0.25 },
    });

    internal static Style CenterValue => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xl, FontWeight = FontWeights.Semibold });

    internal static Style CenterLabel => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs });
}
