using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>面积、柱子的堆叠方式。</summary>
public enum ChartStacking
{
    /// <summary>不叠：面积重叠、半透明（看得出谁高谁低），柱子并排。</summary>
    None,

    /// <summary>依次叠起：看总量与构成。</summary>
    Stacked,

    /// <summary>按占比叠满 100%：只看构成的变化，数值轴是百分比，提示框带上占比。</summary>
    Percent,
}

/// <summary>
/// 面积图（同 vue-tsx-pro 的 AreaChart）：每个系列一片面积，上沿描一条线；默认各系列重叠、半透明，
/// <see cref="Stacking"/> 依次叠起或按占比叠满 100%。
/// <code>&lt;AreaChart Items="months" Category="m => m.Month" Series="channels" Stacking="ChartStacking.Stacked" Unit="万次" /&gt;</code>
/// 坐标轴、十字线（带圆点）与提示框、图例、单位、加载与空状态、给读屏的数据表同 LineChart（见 <see cref="XYChart{TItem}"/>）。
/// </summary>
public sealed class AreaChart<TItem> : XYChart<TItem>
{
    /// <summary>堆叠，默认重叠、半透明。</summary>
    [Parameter] public ChartStacking Stacking { get; set; }

    /// <summary>曲线的形状，默认平滑。</summary>
    [Parameter] public LineCurve Curve { get; set; } = LineCurve.Monotone;

    /// <summary>面积的不透明度（0–1）；默认重叠时 0.25、堆叠时 0.8。</summary>
    [Parameter] public double? Opacity { get; set; }

    private protected override bool CrosshairDots => true;
    private protected override bool Stacked => Stacking != ChartStacking.None;
    private protected override bool Percent => Stacking == ChartStacking.Percent;
    private protected override (double Left, double Right, double Top, double Bottom) Padding => (12, 12, 8, 0);

    private protected override RenderFragment Marks(XYContext<TItem> context) =>
        AreaMarks(context, context.Series, Curve, Stacked, Opacity ?? (Stacked ? 0.8 : 0.25));
}
