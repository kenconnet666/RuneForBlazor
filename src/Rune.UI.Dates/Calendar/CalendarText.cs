using System.Globalization;

namespace Rune.UI;

/// <summary>
/// 日历上的文字（同 vue-tsx-pro 用 Intl 生成的那些）：标题、星期名、月份名、年份，取 CultureInfo 的名称，按 Intl 的写法调整；
/// 一周的排法与周数。期望值见单元测试（来自 Node 的 Intl）。
/// </summary>
static class CalendarText
{
    static DateTimeFormatInfo Info(Lang lang) => LangFormat.CultureOf(lang.Code).DateTimeFormat;

    /// <summary>年（Intl 的 year: numeric）：2026年、2026。</summary>
    public static string Year(int year, Lang lang)
    {
        var part = DateSegments.Layout([DateSegmentType.Year], lang, false);
        return string.Concat(part.Select(p => p.Type is null ? p.Literal : year.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>年月（Intl 的 year: numeric, month: long）：2026年10月、October 2026。</summary>
    public static string YearMonth(DateOnly month, Lang lang) => month.ToString(Info(lang).YearMonthPattern, Info(lang));

    /// <summary>页的标题：Day 视图是年月，Month 视图是年，Year 视图是“2020 – 2029”。</summary>
    public static string Title(DateOnly page, DateUnit view, Lang lang) => view switch
    {
        DateUnit.Day => YearMonth(page, lang),
        DateUnit.Month => Year(page.Year, lang),
        _ => $"{Year(page.Year, lang)} – {Year(page.Year + 9, lang)}",
    };

    /// <summary>月份格的简写（Intl 的 month: short）：10月、Oct。</summary>
    public static string MonthShort(int month, Lang lang) => Info(lang).AbbreviatedMonthNames[month - 1];

    /// <summary>从一周第一天起的七天星期名：最短的（一、二 / S、M）与全称（星期一 / Monday）。</summary>
    public static (string Narrow, string Long)[] Weekdays(DayOfWeek first, Lang lang)
    {
        var info = Info(lang);
        var chinese = lang.Code.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        return [.. Enumerable.Range(0, 7).Select(index =>
        {
            var day = (int)(first + index) % 7;
            var full = info.DayNames[day];
            // Intl 的 narrow：中文是“星期一”的最后一个字，其余语言是第一个字（S、M、月、월）。
            // 不取 ShortestDayNames：各平台的 ICU 不一样（Linux 的 zh-CN 是“周一”）
            var narrow = chinese ? full[^1..] : full[..1].ToUpper(LangFormat.CultureOf(lang.Code));
            return (narrow, full);
        })];
    }

    /// <summary>一个月的日期网格：按周分行，首尾用上个月、下个月的日期补齐；fixedWeeks 时总是 6 行。</summary>
    public static List<DateOnly[]> MonthGrid(DateOnly month, DayOfWeek first, bool fixedWeeks)
    {
        var start = StartOfWeek(new DateOnly(month.Year, month.Month, 1), first);
        var last = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
        var weeks = new List<DateOnly[]>();
        for (var day = start; weeks.Count < 6; day = day.AddDays(7))
        {
            if (!fixedWeeks && day > last) break;
            var week = day;
            weeks.Add([.. Enumerable.Range(0, 7).Select(week.AddDays)]);
        }
        return weeks;
    }

    /// <summary>这周的第一天。</summary>
    public static DateOnly StartOfWeek(DateOnly date, DayOfWeek first) => date.AddDays(-(((int)date.DayOfWeek - (int)first + 7) % 7));

    /// <summary>
    /// 一年里的第几周：一周从周一开始时按 ISO 8601（含 1 月 4 日的那周是第 1 周），
    /// 其他（周日、周六开始）按含 1 月 1 日的那周是第 1 周（美国、日本的惯例）。
    /// </summary>
    public static int WeekOfYear(DateOnly date, DayOfWeek first)
    {
        var monday = first == DayOfWeek.Monday;
        var weekStart = StartOfWeek(date, first);
        // 一周归属的年份：ISO 按这周的周四，其他按这周的最后一天
        var anchor = weekStart.AddDays(monday ? 3 : 6);
        var firstWeek = StartOfWeek(new DateOnly(anchor.Year, 1, monday ? 4 : 1), first);
        return (weekStart.DayNumber - firstWeek.DayNumber) / 7 + 1;
    }
}
