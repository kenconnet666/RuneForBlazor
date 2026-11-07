using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>数据点的样式。</summary>
public enum LineDots
{
    /// <summary>不画数据点（默认），只在悬停时由十字线标出。</summary>
    None,

    /// <summary>实心圆点。</summary>
    Filled,

    /// <summary>空心圆点：描边是系列颜色，中间是背景色。</summary>
    Hollow,
}

/// <summary>
/// 折线图（同 vue-tsx-pro 的 LineChart）：每个系列一条线，类目（通常是时间）沿横轴排开；悬停时十字线在各条线上标出当前点，提示框列出各系列的值。
/// <code>&lt;LineChart Items="months" Category="m => m.Month" Series="series" Unit="万元" Dots="LineDots.Hollow" aria-label="月度销售趋势" /&gt;</code>
/// 空值处断开；颜色、坐标轴、提示框、图例、读屏的数据表见 <see cref="XYChart{TItem}"/>。
/// </summary>
public sealed class LineChart<TItem> : XYChart<TItem>
{
    /// <summary>线的形状，默认折线。</summary>
    [Parameter] public LineCurve Curve { get; set; }

    /// <summary>数据点，默认不画。</summary>
    [Parameter] public LineDots Dots { get; set; }

    /// <summary>线宽（px），默认 2。</summary>
    [Parameter] public double LineWidth { get; set; } = 2;

    private protected override bool CrosshairDots => true;

    // 两端的点不贴着绘图区边缘，圆点与十字线的圆点能完整显示
    private protected override (double Left, double Right, double Top, double Bottom) Padding => (12, 12, 8, 0);

    private protected override RenderFragment Marks(XYContext<TItem> context) => builder =>
    {
        builder.AddContent(0, LineMarks(context, context.Series, Curve, LineWidth));
        if (Dots != LineDots.None) builder.AddContent(1, DotMarks(context, context.Series, Dots == LineDots.Hollow));
    };
}
