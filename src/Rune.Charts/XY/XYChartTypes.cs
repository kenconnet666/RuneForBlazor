namespace Rune.UI;

/// <summary>目标线（参考线）：数值轴上的一条虚线，如年度销售指标；数值轴的范围会包含它。</summary>
/// <param name="Value">位置：数值轴上的值。</param>
public sealed record ChartTarget(decimal Value)
{
    /// <summary>标签：写在线的一端，后面带上格式化的数值（“年度指标 1,200”）。</summary>
    public string? Label { get; init; }

    /// <summary>颜色，默认 Danger（要够到的线）。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>标注：类目上发生的事（竖线），或圈出某个系列在这个类目上的值。</summary>
/// <param name="At">标在哪个类目上：类目的值（与 Category 取出的值相等）；散点图是横轴上的数值。</param>
/// <param name="Label">文字。</param>
public sealed record ChartAnnotation(object At, string Label)
{
    /// <summary>圈出这个系列（<see cref="ChartSeries{TItem}.Key"/>）在这里的值；不写时是一条竖线（换供应商、调价生效这类事件）。</summary>
    public string? Series { get; init; }

    /// <summary>颜色；竖线默认是次要色，圈默认是系列色。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>区间在哪条轴上。</summary>
public enum ChartBandAxis
{
    /// <summary>类目轴（试运行期、促销期）。</summary>
    Index,

    /// <summary>数值轴（目标区间）。</summary>
    Value,
}

/// <summary>区间：类目轴上的一段，或数值轴上的一段，画成很浅的底色。</summary>
/// <param name="From">起点：类目轴是类目的值（含这一项），数值轴是数值。</param>
/// <param name="To">终点：同 From（含这一项）。</param>
public sealed record ChartBand(object From, object To)
{
    /// <summary>在哪条轴上，默认类目轴。</summary>
    public ChartBandAxis Axis { get; init; }

    /// <summary>文字：类目轴上的写在带的左下角，数值轴上的写在左上角。</summary>
    public string? Label { get; init; }

    /// <summary>颜色，默认 Neutral。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>第二数值轴（右侧，组合图）：哪些系列画在它上面，它的格式与单位。</summary>
/// <param name="Keys">画在第二数值轴上的系列。</param>
public sealed record ChartSecondaryAxis(IReadOnlyList<string> Keys)
{
    /// <summary>数值的格式。</summary>
    public NumberFormat? Format { get; init; }

    /// <summary>单位：显示在右上方，提示框与数据表的数值后面带上。</summary>
    public string? Unit { get; init; }
}

/// <summary>横轴是连续的数值（散点图）。</summary>
/// <param name="Value">取值。</param>
/// <param name="Format">刻度与提示框里的写法。</param>
/// <param name="Label">数据表的列名、提示框里的名字。</param>
/// <param name="Title">轴下方的标题。</param>
internal sealed record ContinuousX<TItem>(Func<TItem, double?> Value, Func<double, string> Format, string Label, string? Title);

/// <summary>
/// 交给各种图的绘制上下文（同 vue-tsx-pro 的 XYChartContext）：坐标都是 SVG 里的 px。
/// </summary>
internal sealed class XYContext<TItem>
{
    /// <summary>显示中的系列（去掉了图例里隐藏的），颜色已定。</summary>
    public required IReadOnlyList<ResolvedSeries<TItem>> Series { get; init; }

    /// <summary>画的行（缩放时是选中的一段）。</summary>
    public required IReadOnlyList<TItem> Rows { get; init; }

    /// <summary>横向：类目在纵轴、数值在横轴。</summary>
    public required bool Horizontal { get; init; }

    /// <summary>绘图区（坐标轴以内）。</summary>
    public required PlotRect Plot { get; init; }

    /// <summary>一个类目在类目轴上占的长度：柱按它定宽。</summary>
    public required double Step { get; init; }

    /// <summary>数值轴的范围。</summary>
    public required (double Start, double End) Domain { get; init; }

    /// <summary>第 index 行在类目轴上的坐标（格子的中心）；散点图是横轴数值的位置。</summary>
    public required Func<TItem, int, double> Category { get; init; }

    /// <summary>数值轴的比例尺。</summary>
    public required LinearScale ValueScale { get; init; }

    /// <summary>系列在一行上画的值：百分比堆叠时是占比，第二数值轴上的换算到主轴；空值 null。</summary>
    public required Func<ResolvedSeries<TItem>, TItem, double?> ValueOf { get; init; }

    /// <summary>堆叠后各系列每一行的一段：系列的 key → 每行一段。</summary>
    public required IReadOnlyDictionary<string, StackSegment[]> Stacks { get; init; }

    /// <summary>数值 → 数值轴上的坐标。</summary>
    public double Value(double value) => ValueScale.Map(value);

    /// <summary>类目坐标、数值坐标 → SVG 的 (x, y)（横向时对调）。</summary>
    public (double X, double Y) Point(double category, double value) => Horizontal ? (value, category) : (category, value);

    /// <summary>一个系列在一行上的值的坐标：堆叠时是它在这一摞里的外端。</summary>
    public double? Position(ResolvedSeries<TItem> series, TItem row, int index)
    {
        if (Stacks.TryGetValue(series.Key, out var stack))
            return stack[index] is { Value: { } value } segment ? value < 0 ? segment.Low : segment.High : null;
        return ValueOf(series, row);
    }
}
