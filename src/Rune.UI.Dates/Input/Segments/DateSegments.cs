using System.Globalization;
using System.Text;

namespace Rune.UI;

/// <summary>
/// 分段输入的排法、段名与取值（同 vue-tsx-pro 的 date-segments.ts）。段的顺序、分隔符在这里按文化算好写进 data-*，
/// 浏览器里的状态机（js/date-segments.js）只按 DOM 上的段工作，两边显示一致。
/// </summary>
static partial class DateSegments
{
    static readonly DateSegmentType[] AllFields = [DateSegmentType.Year, DateSegmentType.Month, DateSegmentType.Day, DateSegmentType.Hour, DateSegmentType.Minute, DateSegmentType.Second];

    // 每种粒度一个固定的数组：排法按它缓存
    static readonly DateSegmentType[][] DateFields = [.. Enum.GetValues<DateGranularity>().Select(granularity => granularity switch
    {
        DateGranularity.Quarter => new[] { DateSegmentType.Year, DateSegmentType.Quarter },
        DateGranularity.Week => [DateSegmentType.Year, DateSegmentType.Week],
        _ => AllFields[..(Array.IndexOf(AllFields, Enum.Parse<DateSegmentType>(granularity.ToString())) + 1)],
    })];

    static readonly DateSegmentType[][] TimeFields = [[DateSegmentType.Hour], [DateSegmentType.Hour, DateSegmentType.Minute], [DateSegmentType.Hour, DateSegmentType.Minute, DateSegmentType.Second]];

    /// <summary>日期的段：从年到 granularity。</summary>
    public static DateSegmentType[] FieldsOf(DateGranularity granularity) => DateFields[(int)granularity];

    /// <summary>时间的段：从时到 granularity。</summary>
    public static DateSegmentType[] FieldsOf(TimeGranularity granularity) => TimeFields[(int)granularity];

    /// <summary>JS 里的名字。</summary>
    public static string Name(DateSegmentType type) => type == DateSegmentType.DayPeriod ? "dayPeriod" : type.ToString().ToLowerInvariant();

    /// <summary>12 小时制：组件写了的 &gt; 语言包的 &gt; 地区惯例。</summary>
    public static bool TwelveHour(HourCycle? cycle, Lang lang) => cycle is { } own ? own == HourCycle.H12 : LangFormat.TwelveHour(lang);

    // ───────── 取值 ─────────

    /// <summary>段上显示的值：12 小时制的时是 1–12，上午 / 下午是 0 / 1。</summary>
    public static int? Shown(DateSegmentType type, DateParts parts, bool hour12) => type switch
    {
        DateSegmentType.DayPeriod => parts.Hour is { } hour ? (hour >= 12 ? 1 : 0) : null,
        DateSegmentType.Hour when hour12 && parts.Hour is { } hour => hour % 12 == 0 ? 12 : hour % 12,
        _ => parts[type],
    };

    /// <summary>段的取值范围（aria-valuemin、aria-valuemax）。</summary>
    public static (int Min, int Max) Range(DateSegmentType type, DateParts parts, bool hour12) => type switch
    {
        DateSegmentType.Year => (1, 9999),
        DateSegmentType.Month => (1, 12),
        DateSegmentType.Day => (1, parts.Month is { } month ? DateTime.DaysInMonth(parts.Year ?? 2000, month) : 31),
        DateSegmentType.Quarter => (1, 4),
        DateSegmentType.Week => (1, parts.Year is { } year ? ISOWeek.GetWeeksInYear(year) : 53),
        DateSegmentType.Hour => hour12 ? (1, 12) : (0, 23),
        DateSegmentType.Minute or DateSegmentType.Second => (0, 59),
        _ => (0, 1),
    };

    /// <summary>段上的文字（年四位，季度与 12 小时制的时一位，其余两位）；没填时 null。</summary>
    public static string? Text(DateSegmentType type, int? value, bool hour12, (string Am, string Pm) periods) => value switch
    {
        null => null,
        { } v when type == DateSegmentType.DayPeriod => v == 0 ? periods.Am : periods.Pm,
        { } v => v.ToString(type switch
        {
            DateSegmentType.Year => "D4",
            DateSegmentType.Quarter => "D1",
            DateSegmentType.Hour when hour12 => "D1",
            _ => "D2",
        }, CultureInfo.InvariantCulture),
    };

    // ───────── 一段日期（年、季度、月、周、日） ─────────

    /// <summary>某天所在那一段的 ISO 文字：2026、2026-Q4、2026-10、2026-W43、2026-10-20（带时间的粒度按日）。</summary>
    public static string PeriodIso(DateOnly day, DateGranularity granularity) => granularity switch
    {
        DateGranularity.Year => day.ToString("yyyy", CultureInfo.InvariantCulture),
        DateGranularity.Month => day.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        DateGranularity.Quarter => $"{day.Year:D4}-Q{(day.Month + 2) / 3}",
        DateGranularity.Week => $"{ISOWeek.GetYear(day.ToDateTime(TimeOnly.MinValue)):D4}-W{ISOWeek.GetWeekOfYear(day.ToDateTime(TimeOnly.MinValue)):D2}",
        _ => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    };

    /// <summary>填好的各段 → 那一段的第一天（周是周一）；没有年时 null。</summary>
    public static DateOnly? PeriodStart(DateParts parts, DateGranularity granularity)
    {
        if (parts.Year is not { } year) return null;
        return granularity switch
        {
            DateGranularity.Quarter => new DateOnly(year, (parts.Quarter!.Value - 1) * 3 + 1, 1),
            DateGranularity.Week => DateOnly.FromDateTime(ISOWeek.ToDateTime(year, Math.Min(parts.Week!.Value, ISOWeek.GetWeeksInYear(year)), DayOfWeek.Monday)),
            _ => new DateOnly(year, parts.Month ?? 1, Math.Min(parts.Day ?? 1, DateTime.DaysInMonth(year, parts.Month ?? 1))),
        };
    }

    /// <summary>那一段的最后一天（年末、季末、月末、周日；按日时就是这一天）。</summary>
    public static DateOnly PeriodEnd(DateOnly start, DateGranularity granularity) => granularity switch
    {
        DateGranularity.Year => start.AddYears(1).AddDays(-1),
        DateGranularity.Month => start.AddMonths(1).AddDays(-1),
        DateGranularity.Quarter => start.AddMonths(3).AddDays(-1),
        DateGranularity.Week => start.AddDays(6),
        _ => start,
    };

    // ───────── ISO 文字 ─────────

    /// <summary>各段的值 → ISO 文字（按有哪些段：2026、2026-10、2026-10-20、2026-W43、2026-Q4、2026-10-20T14:30、14:30）。</summary>
    public static string ToIso(DateParts parts, IReadOnlyList<DateSegmentType> fields)
    {
        static string Pad(int? value, int width = 2) => (value ?? 0).ToString(new string('0', width), CultureInfo.InvariantCulture);
        if (fields.Contains(DateSegmentType.Quarter)) return $"{Pad(parts.Year, 4)}-Q{parts.Quarter ?? 1}";
        if (fields.Contains(DateSegmentType.Week)) return $"{Pad(parts.Year, 4)}-W{Pad(parts.Week ?? 1)}";
        var date = string.Join('-', new[]
        {
            fields.Contains(DateSegmentType.Year) ? Pad(parts.Year, 4) : null,
            fields.Contains(DateSegmentType.Month) ? Pad(parts.Month ?? 1) : null,
            fields.Contains(DateSegmentType.Day) ? Pad(parts.Day ?? 1) : null,
        }.OfType<string>());
        if (!fields.Contains(DateSegmentType.Hour)) return date;
        var time = $"{Pad(parts.Hour)}:{Pad(parts.Minute)}" + (fields.Contains(DateSegmentType.Second) ? $":{Pad(parts.Second)}" : "");
        return fields.Contains(DateSegmentType.Year) ? $"{date}T{time}" : time;
    }

    /// <summary>ISO 文字 → 各段的值；格式不对、缺了要的段时 null（同 JS 的 isoToFields）。</summary>
    public static DateParts? FromIso(string? iso, IReadOnlyList<DateSegmentType> fields)
    {
        if (string.IsNullOrEmpty(iso)) return null;
        var parts = new DateParts();
        var period = System.Text.RegularExpressions.Regex.Match(iso, @"^(\d{4})-(?:Q([1-4])|W(\d{2}))$");
        if (period.Success)
        {
            parts.Year = int.Parse(period.Groups[1].Value, CultureInfo.InvariantCulture);
            if (period.Groups[2].Success) parts.Quarter = int.Parse(period.Groups[2].Value, CultureInfo.InvariantCulture);
            if (period.Groups[3].Success) parts.Week = int.Parse(period.Groups[3].Value, CultureInfo.InvariantCulture);
        }
        else
        {
            var match = System.Text.RegularExpressions.Regex.Match(iso, @"^(?:(\d{4})(?:-(\d{2})(?:-(\d{2}))?)?)?(?:T?(\d{2}):(\d{2})(?::(\d{2}))?)?$");
            if (!match.Success) return null;
            for (var index = 0; index < AllFields.Length; index++)
                if (fields.Contains(AllFields[index]) && match.Groups[index + 1].Success)
                    parts[AllFields[index]] = int.Parse(match.Groups[index + 1].Value, CultureInfo.InvariantCulture);
        }
        return fields.All(field => parts[field] is not null) ? parts : null;
    }
}
