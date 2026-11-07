namespace Rune.UI;

/// <summary>一条数值轴：范围与刻度。</summary>
/// <param name="Domain">范围（最小, 最大），两端都是刻度。</param>
/// <param name="Ticks">刻度，从小到大。</param>
public sealed record AxisTicks((double Start, double End) Domain, IReadOnlyList<double> Ticks)
{
    /// <summary>不小于 <paramref name="raw"/> 的“好看”步长：1、2、2.5、5 乘 10 的整数次幂。</summary>
    public static double NiceStep(double raw)
    {
        if (!(raw > 0)) return 1;
        var power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var fraction = raw / power;
        var nice = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;
        return nice * power;
    }

    /// <summary>
    /// 两组值的对齐刻度（组合图的双数值轴，同 vue-tsx-pro 的 alignedTicks、ECharts 的 alignTicks）：
    /// 主轴约 <paramref name="count"/> 格（范围扩到刻度上，含 0），第二数值轴取同样多的格、每格也是好看的步长，两边的刻度落在同一组网格线上。
    /// <code>var axes = AxisTicks.Aligned([0, 1280, 960], [0.12, 0.35]);
    /// axes.Primary.Ticks     // [0, 200, 400, …, 1400]
    /// axes.Secondary.Ticks   // [0, 0.05, 0.1, …, 0.35]</code>
    /// </summary>
    public static AlignedAxes Aligned(IEnumerable<double> primary, IEnumerable<double> secondary, int count = 5)
    {
        var p = primary.Where(double.IsFinite).Append(0).ToList();
        var (p0, p1) = ChartScale.Nice((p.Min(), p.Max() is var max && max != 0 ? max : 1), count);
        var ticks = ChartScale.Ticks(p0, p1, count).Select(Tidy).ToList();
        var intervals = Math.Max(ticks.Count - 1, 1);

        var s = secondary.Where(double.IsFinite).Append(0).ToList();
        var low = s.Min();
        var high = s.Max() is var top && top != 0 ? top : 1;
        var step = NiceStep((high - low) / intervals);
        var start = Math.Floor(low / step) * step;
        // 起点向下取整后可能盖不住最大值：换下一档步长
        for (var guard = 0; start + intervals * step < high && guard < 10; guard++)
        {
            step = NiceStep(step * 1.01);
            start = Math.Floor(low / step) * step;
        }
        var secondaryTicks = Enumerable.Range(0, intervals + 1).Select(i => Tidy(start + i * step)).ToList();
        return new AlignedAxes(new AxisTicks((p0, p1), ticks), new AxisTicks((secondaryTicks[0], secondaryTicks[^1]), secondaryTicks));
    }

    // 浮点误差：0.1 + 0.2 这类算出来的刻度取到 12 位有效数字
    static double Tidy(double value) => double.Parse(value.ToString("G12", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>对齐的两条数值轴（见 <see cref="AxisTicks.Aligned"/>）。</summary>
/// <param name="Primary">主轴（左侧）。</param>
/// <param name="Secondary">第二数值轴（右侧）：刻度数与主轴相同。</param>
public sealed record AlignedAxes(AxisTicks Primary, AxisTicks Secondary)
{
    /// <summary>第二轴的值换算到主轴的坐标。</summary>
    public double ToPrimary(double value)
    {
        var (p0, p1) = Primary.Domain;
        var (s0, s1) = Secondary.Domain;
        return p0 + (value - s0) / (s1 - s0) * (p1 - p0);
    }
}
