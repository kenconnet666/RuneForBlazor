using System.Globalization;
using System.Text;

namespace Rune.UI;

/// <summary>
/// 分段输入的排法、段名与取值（同 vue-tsx-pro 的 date-segments.ts）。段的顺序、分隔符在这里按文化算好写进 data-*，
/// 浏览器里的状态机（js/date-segments.js）只按 DOM 上的段工作，两边显示一致。
/// </summary>
static partial class DateSegments
{
    static readonly SegmentType[] AllFields = [SegmentType.Year, SegmentType.Month, SegmentType.Day, SegmentType.Hour, SegmentType.Minute, SegmentType.Second];

    // 每种粒度一个固定的数组：排法按它缓存
    static readonly SegmentType[][] DateFields = [.. Enum.GetValues<DateGranularity>().Select(granularity => granularity switch
    {
        DateGranularity.Quarter => new[] { SegmentType.Year, SegmentType.Quarter },
        DateGranularity.Week => [SegmentType.Year, SegmentType.Week],
        _ => AllFields[..(Array.IndexOf(AllFields, Enum.Parse<SegmentType>(granularity.ToString())) + 1)],
    })];

    static readonly SegmentType[][] TimeFields = [[SegmentType.Hour], [SegmentType.Hour, SegmentType.Minute], [SegmentType.Hour, SegmentType.Minute, SegmentType.Second]];

    /// <summary>日期的段：从年到 granularity。</summary>
    public static SegmentType[] FieldsOf(DateGranularity granularity) => DateFields[(int)granularity];

    /// <summary>时间的段：从时到 granularity。</summary>
    public static SegmentType[] FieldsOf(TimeGranularity granularity) => TimeFields[(int)granularity];

    /// <summary>JS 里的名字。</summary>
    public static string Name(SegmentType type) => type == SegmentType.DayPeriod ? "dayPeriod" : type.ToString().ToLowerInvariant();

    /// <summary>12 小时制：组件写了的 &gt; 语言包的 &gt; 地区惯例。</summary>
    public static bool TwelveHour(HourCycle? cycle, Lang lang) => cycle is { } own ? own == HourCycle.H12 : LangFormat.TwelveHour(lang);

    // ───────── 取值 ─────────

    /// <summary>段上显示的值：12 小时制的时是 1–12，上午 / 下午是 0 / 1。</summary>
    public static int? Shown(SegmentType type, DateParts parts, bool hour12) => type switch
    {
        SegmentType.DayPeriod => parts.Hour is { } hour ? (hour >= 12 ? 1 : 0) : null,
        SegmentType.Hour when hour12 && parts.Hour is { } hour => hour % 12 == 0 ? 12 : hour % 12,
        _ => parts[type],
    };

    /// <summary>段的取值范围（aria-valuemin、aria-valuemax）。</summary>
    public static (int Min, int Max) Range(SegmentType type, DateParts parts, bool hour12) => type switch
    {
        SegmentType.Year => (1, 9999),
        SegmentType.Month => (1, 12),
        SegmentType.Day => (1, parts.Month is { } month ? DateTime.DaysInMonth(parts.Year ?? 2000, month) : 31),
        SegmentType.Quarter => (1, 4),
        SegmentType.Week => (1, parts.Year is { } year ? ISOWeek.GetWeeksInYear(year) : 53),
        SegmentType.Hour => hour12 ? (1, 12) : (0, 23),
        SegmentType.Minute or SegmentType.Second => (0, 59),
        _ => (0, 1),
    };

    /// <summary>段上的文字（年四位，季度与 12 小时制的时一位，其余两位）；没填时 null。</summary>
    public static string? Text(SegmentType type, int? value, bool hour12, (string Am, string Pm) periods) => value switch
    {
        null => null,
        { } v when type == SegmentType.DayPeriod => v == 0 ? periods.Am : periods.Pm,
        { } v => v.ToString(type switch
        {
            SegmentType.Year => "D4",
            SegmentType.Quarter => "D1",
            SegmentType.Hour when hour12 => "D1",
            _ => "D2",
        }, CultureInfo.InvariantCulture),
    };

    // ───────── ISO 文字 ─────────

    /// <summary>各段的值 → ISO 文字（按有哪些段：2026、2026-10、2026-10-20、2026-W43、2026-Q4、2026-10-20T14:30、14:30）。</summary>
    public static string ToIso(DateParts parts, IReadOnlyList<SegmentType> fields)
    {
        static string Pad(int? value, int width = 2) => (value ?? 0).ToString(new string('0', width), CultureInfo.InvariantCulture);
        if (fields.Contains(SegmentType.Quarter)) return $"{Pad(parts.Year, 4)}-Q{parts.Quarter ?? 1}";
        if (fields.Contains(SegmentType.Week)) return $"{Pad(parts.Year, 4)}-W{Pad(parts.Week ?? 1)}";
        var date = string.Join('-', new[]
        {
            fields.Contains(SegmentType.Year) ? Pad(parts.Year, 4) : null,
            fields.Contains(SegmentType.Month) ? Pad(parts.Month ?? 1) : null,
            fields.Contains(SegmentType.Day) ? Pad(parts.Day ?? 1) : null,
        }.OfType<string>());
        if (!fields.Contains(SegmentType.Hour)) return date;
        var time = $"{Pad(parts.Hour)}:{Pad(parts.Minute)}" + (fields.Contains(SegmentType.Second) ? $":{Pad(parts.Second)}" : "");
        return fields.Contains(SegmentType.Year) ? $"{date}T{time}" : time;
    }

    /// <summary>ISO 文字 → 各段的值；格式不对、缺了要的段时 null（同 JS 的 isoToFields）。</summary>
    public static DateParts? FromIso(string? iso, IReadOnlyList<SegmentType> fields)
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
