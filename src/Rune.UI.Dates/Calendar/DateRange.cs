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

/// <summary>
/// 区间，两端都包含（同 vue-tsx-pro 的 DateRange）：日历与按天的区间选择器用 <c>DateRange&lt;DateOnly&gt;</c>，
/// 带时间的区间（DateRangePicker 的 Granularity 是 Hour、Minute、Second）用 <c>DateRange&lt;DateTime&gt;</c> 或 <c>DateRange&lt;DateTimeOffset&gt;</c>。
/// 排序、天数、快捷区间见 <see cref="DateRange"/>。
/// </summary>
/// <param name="Start">开始（含）。</param>
/// <param name="End">结束（含）。</param>
public readonly record struct DateRange<T>(T Start, T End) where T : struct, IComparable<T>;

/// <summary>区间的辅助：两端排好序、日期区间的天数与包含、按今天算的快捷区间。</summary>
public static class DateRange
{
    /// <summary>两端排成区间：先的在前。</summary>
    public static DateRange<T> Of<T>(T a, T b) where T : struct, IComparable<T> => a.CompareTo(b) <= 0 ? new(a, b) : new(b, a);

    extension(DateRange<DateOnly> range)
    {
        /// <summary>一共几天（含两端）。</summary>
        public int Days => range.End.DayNumber - range.Start.DayNumber + 1;

        /// <summary>某天在不在区间里。</summary>
        public bool Contains(DateOnly date) => date >= range.Start && date <= range.End;
    }

    /// <summary>
    /// 按今天算出快捷区间（同 vue-tsx-pro 的 presetRange）：<c>DateRange.Preset(DateRangePreset.Last7Days, today, DayOfWeek.Monday)</c>。
    /// 本周按一周的第一天算。
    /// </summary>
    public static DateRange<DateOnly> Preset(DateRangePreset preset, DateOnly today, DayOfWeek firstDayOfWeek)
    {
        var month = new DateOnly(today.Year, today.Month, 1);
        return preset switch
        {
            DateRangePreset.Yesterday => new(today.AddDays(-1), today.AddDays(-1)),
            DateRangePreset.Last7Days => new(today.AddDays(-6), today),
            DateRangePreset.Last30Days => new(today.AddDays(-29), today),
            DateRangePreset.ThisWeek => CalendarText.StartOfWeek(today, firstDayOfWeek) is var start ? new(start, start.AddDays(6)) : default,
            DateRangePreset.ThisMonth => new(month, month.AddMonths(1).AddDays(-1)),
            DateRangePreset.LastMonth => new(month.AddMonths(-1), month.AddDays(-1)),
            DateRangePreset.ThisYear => new(new(today.Year, 1, 1), new(today.Year, 12, 31)),
            _ => new(today, today),
        };
    }
}

/// <summary>快捷区间（文字取语言包的 DateRangePreset）。</summary>
public enum DateRangePreset
{
    /// <summary>今天。</summary>
    Today,

    /// <summary>昨天。</summary>
    Yesterday,

    /// <summary>最近 7 天（含今天）。</summary>
    Last7Days,

    /// <summary>最近 30 天（含今天）。</summary>
    Last30Days,

    /// <summary>本周（按语言的一周第一天）。</summary>
    ThisWeek,

    /// <summary>本月。</summary>
    ThisMonth,

    /// <summary>上月。</summary>
    LastMonth,

    /// <summary>今年。</summary>
    ThisYear,
}

/// <summary>
/// DateRangePicker 的一个快捷区间：内置的（<see cref="DateRangePreset"/>，可以直接写枚举值）或自己的（文字加区间）。
/// <code>Presets="[DateRangePreset.Last7Days, new RangePreset("本结算周期", SettlementPeriod)]"</code>
/// </summary>
public sealed class RangePreset
{
    readonly DateRangePreset? builtIn;
    readonly string? label;
    readonly Func<DateRange<DateOnly>>? range;

    /// <summary>自己的快捷区间：每次打开按当时算（跟着今天变）。带时间的区间选择器里，开始取那天的第一刻、结束取最后一刻。</summary>
    /// <param name="label">按钮上的文字。</param>
    /// <param name="range">区间（按天）。</param>
    public RangePreset(string label, Func<DateRange<DateOnly>> range)
    {
        this.label = label;
        this.range = range;
    }

    RangePreset(DateRangePreset preset) => builtIn = preset;

    /// <summary>内置的快捷区间：文字取语言包，按今天与语言的一周第一天算。</summary>
    public static implicit operator RangePreset(DateRangePreset preset) => new(preset);

    /// <summary>按钮上的文字。</summary>
    internal string LabelIn(Lang lang) => builtIn is { } preset ? lang.DateRangePreset[char.ToLowerInvariant(preset.ToString()[0]) + preset.ToString()[1..]] : label!;

    /// <summary>这个快捷区间现在是哪一段。</summary>
    internal DateRange<DateOnly> RangeOn(DateOnly today, DayOfWeek firstDayOfWeek) => builtIn is { } preset ? DateRange.Preset(preset, today, firstDayOfWeek) : range!();
}

