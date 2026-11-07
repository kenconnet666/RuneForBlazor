namespace Rune.UI;

/// <summary>一个系列在一行上叠出的一段。</summary>
/// <param name="Low">下沿。</param>
/// <param name="High">上沿。</param>
/// <param name="Value">原值；空值是 null，这时上下沿都在当前的正向累计处（高度为 0，面积连续）。</param>
public readonly record struct StackSegment(double Low, double High, double? Value);

/// <summary>
/// 堆叠（同 vue-tsx-pro 的 stackValues、d3 的 stackOffsetDiverging）：各系列在同一类目上依次叠起，正值从 0 往上叠、负值从 0 往下叠。
/// 柱状图、面积图、组合图的堆叠与十字线圆点、标注圈的位置共用。
/// <code>ChartStack.Of([[3, 5], [2, -1]])   // 系列 0：(0,3) (0,5)；系列 1：(3,5) (-1,0)</code>
/// </summary>
public static class ChartStack
{
    /// <summary>values[系列][行] → 每个系列在每一行的一段。</summary>
    public static StackSegment[][] Of(IReadOnlyList<IReadOnlyList<double?>> values)
    {
        var rows = values.Count == 0 ? 0 : values.Max(list => list.Count);
        var positive = new double[rows];
        var negative = new double[rows];
        return [.. values.Select(list => Enumerable.Range(0, rows).Select(row =>
        {
            if (row >= list.Count || list[row] is not { } value || !double.IsFinite(value)) return new StackSegment(positive[row], positive[row], null);
            if (value >= 0)
            {
                var low = positive[row];
                positive[row] = low + value;
                return new StackSegment(low, low + value, value);
            }
            var high = negative[row];
            negative[row] = high + value;
            return new StackSegment(high + value, high, value);
        }).ToArray())];
    }
}
