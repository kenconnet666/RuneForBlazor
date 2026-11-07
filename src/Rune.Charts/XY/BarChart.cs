using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 柱状图（同 vue-tsx-pro 的 BarChart）：每个类目一组柱子，系列并排（分组）或叠成一根（堆叠），可以横向；
/// 按占比堆叠时每根叠满 100%（看构成）。
/// <code>&lt;BarChart Items="projects" Category="p => p.Name" Series="series" Unit="万元" aria-label="各项目销售额" /&gt;</code>
/// 颜色、坐标轴、提示框、图例、读屏的数据表见 <see cref="XYChart{TItem}"/>；悬停一个类目时这一格铺一层浅底，提示框列出这一组的全部系列。
/// </summary>
public sealed class BarChart<TItem> : XYChart<TItem>
{
    /// <summary>堆叠，默认并排。</summary>
    [Parameter] public ChartStacking Stacking { get; set; }

    /// <summary>横向的条：类目名较长、类目较多时。</summary>
    [Parameter] public bool HorizontalBars { get; set; }

    /// <summary>柱子末端的圆角（px），默认 4。</summary>
    [Parameter] public double Radius { get; set; } = 4;

    private protected override bool Band => true;
    private protected override bool Horizontal => HorizontalBars;
    private protected override bool SkeletonBars => true;
    private protected override bool Stacked => Stacking != ChartStacking.None;
    private protected override bool Percent => Stacking == ChartStacking.Percent;

    private protected override RenderFragment Marks(XYContext<TItem> context) => BarMarks(context, context.Series, Stacked, Radius);
}
