namespace Rune.UI;

/// <summary>
/// 线性比例尺（同 vue-tsx-pro 的 linearScale）：数值 → 坐标，<see cref="Invert"/> 反过来；数值范围两端相同时取坐标的中间。
/// <code>var y = new LinearScale(ChartScale.Nice((0, 236)), (200, 0));   // 数值轴：0 在下沿
/// y.Map(125)   // 100</code>
/// </summary>
/// <param name="Domain">数值的范围（起, 止）。</param>
/// <param name="Range">坐标的范围（起, 止）；数值轴的起点通常在下方，坐标是从大到小。</param>
public readonly record struct LinearScale((double Start, double End) Domain, (double Start, double End) Range)
{
    /// <summary>数值换成坐标。</summary>
    public double Map(double value)
    {
        var span = Domain.End - Domain.Start;
        return span == 0 ? (Range.Start + Range.End) / 2 : Range.Start + (value - Domain.Start) / span * (Range.End - Range.Start);
    }

    /// <summary>坐标换回数值。</summary>
    public double Invert(double position) => Range.End == Range.Start
        ? Domain.Start
        : Domain.Start + (position - Range.Start) / (Range.End - Range.Start) * (Domain.End - Domain.Start);
}

/// <summary>
/// 刻度与取整的范围（同 vue-tsx-pro 的 scale.ts，即 d3-array 的 ticks、d3-scale 的 nice 的精简版），
/// 时间刻度（月初、周、日、整点，同 d3 的 scaleTime().ticks）。
/// <code>ChartScale.Ticks(0, 250, 5)        // [0, 50, 100, 150, 200, 250]
/// ChartScale.Nice((0, 236))          // (0, 250)
/// ChartScale.TimeTicks(start, end, 6)   // 跨几个月时是各月的 1 日</code>
/// </summary>
public static class ChartScale
{
    // 步长取 1、2、5 乘 10 的幂：误差超过这几个界时换大一档（同 d3-array）
    static readonly double E10 = Math.Sqrt(50);
    static readonly double E5 = Math.Sqrt(10);
    static readonly double E2 = Math.Sqrt(2);

    // 刻度的整数区间 [i1, i2] 与步长：inc 为正时刻度是 i × inc；为负时是 i / -inc（小数步长用除法表示，0.1 × 3 这类乘法会带进浮点误差）
    static (double I1, double I2, double Inc) Spec(double start, double stop, double count)
    {
        var step = (stop - start) / Math.Max(0, count);
        var power = Math.Floor(Math.Log10(step));
        var error = step / Math.Pow(10, power);
        var factor = error >= E10 ? 10 : error >= E5 ? 5 : error >= E2 ? 2 : 1;
        double i1, i2, inc;
        if (power < 0)
        {
            inc = Math.Pow(10, -power) / factor;
            i1 = JsRound(start * inc);
            i2 = JsRound(stop * inc);
            if (i1 / inc < start) i1 += 1;
            if (i2 / inc > stop) i2 -= 1;
            inc = -inc;
        }
        else
        {
            inc = Math.Pow(10, power) * factor;
            i1 = JsRound(start / inc);
            i2 = JsRound(stop / inc);
            if (i1 * inc < start) i1 += 1;
            if (i2 * inc > stop) i2 -= 1;
        }
        if (i2 < i1 && count is >= 0.5 and < 2) return Spec(start, stop, count * 2);
        return (i1, i2, inc);
    }

    static double JsRound(double value) => Math.Floor(value + 0.5);

    /// <summary>约 <paramref name="count"/> 格的刻度步长：正数是步长本身，负数 -k 表示步长 1 / k（同 d3 的 tickIncrement）。</summary>
    public static double TickIncrement(double start, double stop, double count) => Spec(start, stop, count).Inc;

    /// <summary>[start, stop] 里约 <paramref name="count"/> 格的“好看”刻度：1、2、5 乘 10 的幂的整数倍，从小到大。</summary>
    public static IReadOnlyList<double> Ticks(double start, double stop, double count = 5)
    {
        if (!(count > 0) || !double.IsFinite(start) || !double.IsFinite(stop)) return [];
        if (start == stop) return [start];
        var (low, high) = start < stop ? (start, stop) : (stop, start);
        var (i1, i2, inc) = Spec(low, high, count);
        if (!(i2 >= i1)) return [];
        var ticks = new double[(int)(i2 - i1) + 1];
        for (var i = 0; i < ticks.Length; i++) ticks[i] = inc < 0 ? (i1 + i) / -inc : (i1 + i) * inc;
        return ticks;
    }

    /// <summary>范围的两端扩到刻度上（同 d3 的 linear.nice）：(0, 236) → (0, 250)。</summary>
    public static (double Start, double End) Nice((double Start, double End) domain, double count = 5)
    {
        var (start, stop) = domain.Start <= domain.End ? domain : (domain.End, domain.Start);
        double? previous = null;
        for (var guard = 0; guard < 10; guard++)
        {
            var step = TickIncrement(start, stop, count);
            if (step == previous || !double.IsFinite(step)) break;
            if (step > 0)
            {
                start = Math.Floor(start / step) * step;
                stop = Math.Ceiling(stop / step) * step;
            }
            else if (step < 0)
            {
                start = Math.Ceiling(start * step) / step;
                stop = Math.Floor(stop * step) / step;
            }
            else break;
            previous = step;
        }
        return (start, stop);
    }

    enum TimeUnit { Second, Minute, Hour, Day, Week, Month, Year }

    static readonly TimeSpan YearSpan = TimeSpan.FromDays(365);

    // 可选的间隔：单位、几个单位一格、大约多长（同 d3 的 tickIntervals）
    static readonly (TimeUnit Unit, int Every, TimeSpan Duration)[] Intervals =
    [
        (TimeUnit.Second, 1, TimeSpan.FromSeconds(1)),
        (TimeUnit.Second, 5, TimeSpan.FromSeconds(5)),
        (TimeUnit.Second, 15, TimeSpan.FromSeconds(15)),
        (TimeUnit.Second, 30, TimeSpan.FromSeconds(30)),
        (TimeUnit.Minute, 1, TimeSpan.FromMinutes(1)),
        (TimeUnit.Minute, 5, TimeSpan.FromMinutes(5)),
        (TimeUnit.Minute, 15, TimeSpan.FromMinutes(15)),
        (TimeUnit.Minute, 30, TimeSpan.FromMinutes(30)),
        (TimeUnit.Hour, 1, TimeSpan.FromHours(1)),
        (TimeUnit.Hour, 3, TimeSpan.FromHours(3)),
        (TimeUnit.Hour, 6, TimeSpan.FromHours(6)),
        (TimeUnit.Hour, 12, TimeSpan.FromHours(12)),
        (TimeUnit.Day, 1, TimeSpan.FromDays(1)),
        (TimeUnit.Day, 2, TimeSpan.FromDays(2)),
        (TimeUnit.Week, 1, TimeSpan.FromDays(7)),
        (TimeUnit.Month, 1, TimeSpan.FromDays(30)),
        (TimeUnit.Month, 3, TimeSpan.FromDays(90)),
        (TimeUnit.Year, 1, YearSpan),
    ];

    // 这个单位的起点：月取 1 日 0 点、周取周日 0 点
    static DateTime Floor(DateTime date, TimeUnit unit) => unit switch
    {
        TimeUnit.Second => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind),
        TimeUnit.Minute => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, date.Kind),
        TimeUnit.Hour => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, date.Kind),
        TimeUnit.Week => date.Date.AddDays(-(int)date.DayOfWeek),
        TimeUnit.Month => new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind),
        TimeUnit.Year => new DateTime(date.Year, 1, 1, 0, 0, 0, date.Kind),
        _ => date.Date,
    };

    static DateTime Next(DateTime date, TimeUnit unit) => unit switch
    {
        TimeUnit.Second => date.AddSeconds(1),
        TimeUnit.Minute => date.AddMinutes(1),
        TimeUnit.Hour => date.AddHours(1),
        TimeUnit.Day => date.AddDays(1),
        TimeUnit.Week => date.AddDays(7),
        TimeUnit.Month => date.AddMonths(1),
        _ => date.AddYears(1),
    };

    // 几个单位一格时按哪个字段取整：每 3 个月取 1、4、7、10 月，每 6 小时取 0、6、12、18 点
    static int Field(DateTime date, TimeUnit unit) => unit switch
    {
        TimeUnit.Second => date.Second,
        TimeUnit.Minute => date.Minute,
        TimeUnit.Hour => date.Hour,
        TimeUnit.Day => date.Day - 1,
        TimeUnit.Month => date.Month - 1,
        TimeUnit.Year => date.Year,
        _ => 0,
    };

    /// <summary>
    /// [start, stop] 之间约 <paramref name="count"/> 个整齐的时间刻度：整秒、整分、整点、日、周日、月初、年初。
    /// 按日期时间自己的字段算（墙上时间，不换时区）。
    /// </summary>
    public static IReadOnlyList<DateTime> TimeTicks(DateTime start, DateTime stop, int count = 6)
    {
        if (stop < start) return [];
        var target = (stop - start) / count;
        var index = Array.FindIndex(Intervals, interval => interval.Duration > target);
        TimeUnit unit;
        int every;
        if (index < 0)
        {
            // 比一年还长：年份按“好看”的步长
            unit = TimeUnit.Year;
            every = (int)Math.Max(1, TickIncrement(start.Ticks / (double)YearSpan.Ticks, stop.Ticks / (double)YearSpan.Ticks, count));
        }
        else if (index == 0) (unit, every) = (TimeUnit.Second, 1);
        else
        {
            // 取与目标间隔之比更接近 1 的那一档
            var (before, after) = (Intervals[index - 1], Intervals[index]);
            (unit, every, _) = target / before.Duration < after.Duration / target ? before : after;
        }
        var ticks = new List<DateTime>();
        for (var date = Floor(start, unit); date <= stop; date = Next(date, unit))
            if (date >= start && Field(date, unit) % every == 0) ticks.Add(date);
        return ticks;
    }
}
