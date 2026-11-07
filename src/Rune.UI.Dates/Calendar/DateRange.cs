namespace Rune.UI;

/// <summary>日历的视图与选择的粒度（同 vue-tsx-pro 的 DateUnit）。</summary>
public enum DateUnit
{
    /// <summary>天：月份网格。</summary>
    Day,

    /// <summary>月：一年的各月。</summary>
    Month,

    /// <summary>年：一个十年。</summary>
    Year,
}

/// <summary>日期区间，两端都包含。</summary>
/// <param name="Start">开始（含）。</param>
/// <param name="End">结束（含）。</param>
public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
    /// <summary>两个日期排成区间：先的在前。</summary>
    public static DateRange Of(DateOnly a, DateOnly b) => a <= b ? new(a, b) : new(b, a);

    /// <summary>一共几天（含两端）。</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    /// <summary>某天在不在区间里。</summary>
    public bool Contains(DateOnly date) => date >= Start && date <= End;
}
