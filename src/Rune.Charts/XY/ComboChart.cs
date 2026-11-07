using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>组合图里一个系列的画法。</summary>
public enum ComboMark
{
    /// <summary>柱（默认）。</summary>
    Bar,

    /// <summary>线。</summary>
    Line,
}

/// <summary>系列画在哪条数值轴上。</summary>
public enum ChartValueAxis
{
    /// <summary>左侧的主轴（默认）。</summary>
    Primary,

    /// <summary>右侧的第二数值轴：单位不同的量（比率、单价）。</summary>
    Secondary,
}

/// <summary>组合图的一个系列：在 <see cref="ChartSeries{TItem}"/> 上加画法与数值轴。</summary>
/// <param name="Key">标识。</param>
/// <param name="Label">名称。</param>
/// <param name="Value">取值。</param>
public sealed record ComboSeries<TItem>(string Key, string Label, Func<TItem, decimal?> Value) : ChartSeries<TItem>(Key, Label, Value)
{
    /// <summary>画法，默认柱。</summary>
    public ComboMark Mark { get; init; }

    /// <summary>数值轴，默认主轴。</summary>
    public ChartValueAxis Axis { get; init; }
}

/// <summary>
/// 组合图（同 vue-tsx-pro 的 ComboChart）：同一组类目上既有柱又有线（金额与比率、数量与单价），单位不同的系列画在右侧的第二数值轴上。
/// <code>Series = [new ComboSeries&lt;Month&gt;("actual", "实际销售", m => m.Actual),
///           new ComboSeries&lt;Month&gt;("rate", "完成率", m => m.Rate) { Mark = ComboMark.Line, Axis = ChartValueAxis.Secondary }]</code>
/// 柱子并排（StackBars 时叠起），线可以带数据点。第二数值轴的刻度与主轴对齐（同一组网格线），它的单位写在右上方，
/// 提示框、数据表里按各自的格式与单位显示。其余（图例、目标线、标注、区间、下钻）同 <see cref="XYChartBase{TItem}"/>。
/// 系列写成 <see cref="ChartSeries{TItem}"/> 时是主轴上的柱。
/// </summary>
public sealed class ComboChart<TItem> : XYChart<TItem>
{
    /// <summary>第二数值轴的格式（比率写 <c>NumberFormat.Percent</c>）。</summary>
    [Parameter] public NumberFormat? SecondaryFormat { get; set; }

    /// <summary>第二数值轴的单位：显示在右上方。</summary>
    [Parameter] public string? SecondaryUnit { get; set; }

    /// <summary>柱子堆叠：同一类目的柱叠成一根；默认并排。</summary>
    [Parameter] public bool StackBars { get; set; }

    /// <summary>线的形状，默认折线。</summary>
    [Parameter] public LineCurve Curve { get; set; }

    /// <summary>线上的数据点，默认实心。</summary>
    [Parameter] public LineDots Dots { get; set; } = LineDots.Filled;

    /// <summary>柱子末端的圆角（px），默认 4。</summary>
    [Parameter] public double Radius { get; set; } = 4;

    static bool IsLine(ChartSeries<TItem> series) => series is ComboSeries<TItem> { Mark: ComboMark.Line };

    ChartSecondaryAxis? secondary;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        var keys = Series.OfType<ComboSeries<TItem>>().Where(item => item.Axis == ChartValueAxis.Secondary).Select(item => item.Key).ToList();
        secondary = keys.Count > 0 ? new ChartSecondaryAxis(keys) { Format = SecondaryFormat, Unit = SecondaryUnit } : null;
        base.OnParametersSet();
    }

    private protected override bool Band => true;
    private protected override bool SkeletonBars => true;
    private protected override bool Stacked => StackBars;
    private protected override bool CrosshairDots => true;
    private protected override ChartSecondaryAxis? Secondary => secondary;

    // 只有柱叠起，线按各自的值；十字线只在线上画圆点
    private protected override bool StacksSeries(ResolvedSeries<TItem> series) => !IsLine(series.Source);
    private protected override bool DotsSeries(ResolvedSeries<TItem> series) => IsLine(series.Source);

    // 主轴的范围：堆叠的柱按正负各自的总和，线按各自的值
    private protected override IEnumerable<double>? DomainValues(IReadOnlyList<TItem> rows, IReadOnlyList<ResolvedSeries<TItem>> shown) =>
        rows.SelectMany(row =>
        {
            var bars = shown.Where(item => !IsLine(item.Source)).Select(item => (double)(item.ValueOf(row) ?? 0)).ToList();
            var lines = shown.Where(item => IsLine(item.Source)).Select(item => (double)(item.ValueOf(row) ?? 0));
            IEnumerable<double> barValues = StackBars ? [bars.Sum(value => Math.Max(value, 0)), bars.Sum(value => Math.Min(value, 0))] : bars;
            return barValues.Concat(lines);
        });

    private protected override RenderFragment Marks(XYContext<TItem> context) => builder =>
    {
        var bars = context.Series.Where(item => !IsLine(item.Source)).ToList();
        var lines = context.Series.Where(item => IsLine(item.Source)).ToList();
        builder.AddContent(0, BarMarks(context, bars, StackBars, Radius));
        builder.AddContent(1, LineMarks(context, lines, Curve, 2));
        if (Dots != LineDots.None) builder.AddContent(2, DotMarks(context, lines, Dots == LineDots.Hollow));
    };
}
