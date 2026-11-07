using System.Globalization;
using System.Text;

namespace Rune.UI;

/// <summary>日期输入精确到哪一段（同 vue-tsx-pro 的 DateFieldGranularity）。</summary>
public enum DateGranularity
{
    /// <summary>只有年。</summary>
    Year,

    /// <summary>年月（同原生 input type="month"）。</summary>
    Month,

    /// <summary>年月日（默认）。</summary>
    Day,

    /// <summary>年与季度。</summary>
    Quarter,

    /// <summary>年与周（ISO 8601，周一开始；同原生 input type="week"）。</summary>
    Week,

    /// <summary>日期加时。</summary>
    Hour,

    /// <summary>日期加时分（同原生 input type="datetime-local"）。</summary>
    Minute,

    /// <summary>日期加时分秒。</summary>
    Second,
}

/// <summary>时间输入精确到哪一段。</summary>
public enum TimeGranularity
{
    /// <summary>只有时。</summary>
    Hour,

    /// <summary>时分（默认）。</summary>
    Minute,

    /// <summary>时分秒。</summary>
    Second,
}

/// <summary>12 / 24 小时制（同 Intl 的 hourCycle h12、h23）。</summary>
public enum HourCycle
{
    /// <summary>12 小时制，带上午 / 下午。</summary>
    H12,

    /// <summary>24 小时制。</summary>
    H23,
}

/// <summary>分段输入的一段（JS 里的名字是 camelCase：dayPeriod）。</summary>
enum SegmentType
{
    Year,
    Month,
    Day,
    Quarter,
    Week,
    Hour,
    Minute,
    Second,
    DayPeriod,
}

/// <summary>排好的一段：可以输入的段（Type 有值），或分隔符（Literal）。</summary>
readonly record struct SegmentPart(SegmentType? Type, string Literal);

/// <summary>
/// 分段输入的排法、段名与取值（同 vue-tsx-pro 的 date-segments.ts）。段的顺序、分隔符在这里按文化算好写进 data-*，
/// 浏览器里的状态机（js/date-segments.js）只按 DOM 上的段工作，两边显示一致。
/// </summary>
static class DateSegments
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

    // ───────── 排法 ─────────

    /// <summary>
    /// 按语言排好的段与分隔符（同 Intl.DateTimeFormat 的 formatToParts：年 numeric，其余 2-digit）。
    /// 日期取文化的短日期（年月取年月格式，带月份名时改用短日期去掉日），时间取 <see cref="LangFormat"/> 同一套时间格式；
    /// 季度、周按语言包的排法（Intl 不排这两种）。
    /// </summary>
    public static SegmentPart[] Layout(IReadOnlyList<SegmentType> fields, Lang lang, bool hour12)
    {
        if (fields.FirstOrDefault(field => field is SegmentType.Quarter or SegmentType.Week) is var period and (SegmentType.Quarter or SegmentType.Week))
            return PeriodLayout(period, lang);
        var info = LangFormat.CultureOf(lang.Code).DateTimeFormat;
        var date = DateLayout(fields, info);
        var time = TimeLayout(fields, info, hour12, lang.Code);
        if (date.Count == 0 || time.Count == 0) return Tidy([.. date, .. time]);
        // 日期与时间之间：同 LangFormat.Date 的短日期
        var joiner = lang.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? ", " : " ";
        return Tidy([.. date, new SegmentPart(null, joiner), .. time]);
    }

    static SegmentPart[] PeriodLayout(SegmentType period, Lang lang)
    {
        var name = Name(period);
        var pattern = lang.DateFieldPeriodPatterns.TryGetValue(name, out var own) ? own : $"{{year}}-{{{name}}}";
        var parts = new List<SegmentPart>();
        var rest = pattern;
        while (rest.Length > 0)
        {
            var open = rest.IndexOf('{');
            var close = open < 0 ? -1 : rest.IndexOf('}', open);
            if (close < 0)
            {
                parts.Add(new(null, rest));
                break;
            }
            if (open > 0) parts.Add(new(null, rest[..open]));
            parts.Add(new(rest[(open + 1)..close] == "year" ? SegmentType.Year : period, ""));
            rest = rest[(close + 1)..];
        }
        return [.. parts];
    }

    static List<SegmentPart> DateLayout(IReadOnlyList<SegmentType> fields, DateTimeFormatInfo info)
    {
        if (!fields.Contains(SegmentType.Year)) return [];
        if (fields.Contains(SegmentType.Day)) return Keep(Tokens(info.ShortDatePattern, info), SegmentType.Year, SegmentType.Month, SegmentType.Day);
        var yearMonth = info.YearMonthPattern.Contains("MMM", StringComparison.Ordinal) ? null : Tokens(info.YearMonthPattern, info);
        if (fields.Contains(SegmentType.Month)) return Keep(yearMonth ?? Tokens(info.ShortDatePattern, info), SegmentType.Year, SegmentType.Month);
        // 只有年：年后面紧跟的单位字留着（2026年），其余去掉
        var tokens = Tokens(info.YearMonthPattern, info);
        var year = tokens.FindIndex(part => part.Type == SegmentType.Year);
        List<SegmentPart> result = [new(SegmentType.Year, "")];
        if (year >= 0 && year + 1 < tokens.Count && tokens[year + 1] is { Type: null } unit && unit.Literal.Trim().Any(char.IsLetter))
            result.Add(new(null, unit.Literal.TrimEnd()));
        return result;
    }

    static List<SegmentPart> TimeLayout(IReadOnlyList<SegmentType> fields, DateTimeFormatInfo info, bool hour12, string culture)
    {
        if (!fields.Contains(SegmentType.Hour)) return [];
        var style = fields.Contains(SegmentType.Second) ? TimeStyle.Medium : TimeStyle.Short;
        var tokens = Tokens(LangFormat.TimePattern(info, style, hour12, culture), info);
        return fields.Contains(SegmentType.Minute)
            ? Keep(tokens, SegmentType.Hour, SegmentType.Minute, SegmentType.Second, SegmentType.DayPeriod)
            : Keep(tokens, SegmentType.Hour, SegmentType.DayPeriod);
    }

    /// <summary>只留这几种段：去掉的段连同它前面的分隔符一起去掉（在最前面时去掉后面的）。</summary>
    static List<SegmentPart> Keep(List<SegmentPart> tokens, params SegmentType[] wanted)
    {
        var result = new List<SegmentPart>(tokens);
        for (var index = 0; index < result.Count;)
        {
            if (result[index].Type is not { } type || wanted.Contains(type))
            {
                index++;
                continue;
            }
            result.RemoveAt(index);
            if (index > 0 && result[index - 1].Type is null) result.RemoveAt(--index);
            else if (index < result.Count && result[index].Type is null) result.RemoveAt(index);
        }
        return result;
    }

    /// <summary>.NET 的日期时间格式 → 段与分隔符（/、: 换成文化的分隔符，引号里的原样）。</summary>
    static List<SegmentPart> Tokens(string pattern, DateTimeFormatInfo info)
    {
        var parts = new List<SegmentPart>();
        var literal = new StringBuilder();
        void Flush()
        {
            if (literal.Length == 0) return;
            parts.Add(new(null, literal.ToString()));
            literal.Clear();
        }
        for (var index = 0; index < pattern.Length;)
        {
            var c = pattern[index];
            var run = 1;
            while (index + run < pattern.Length && pattern[index + run] == c) run++;
            SegmentType? type = c switch
            {
                'y' => SegmentType.Year,
                'M' when run <= 2 => SegmentType.Month,
                'd' when run <= 2 => SegmentType.Day,
                'h' or 'H' => SegmentType.Hour,
                'm' => SegmentType.Minute,
                's' => SegmentType.Second,
                't' => SegmentType.DayPeriod,
                _ => null,
            };
            if (type is not null)
            {
                Flush();
                parts.Add(new(type, ""));
                index += run;
                continue;
            }
            switch (c)
            {
                case '\'' or '"':
                    var end = pattern.IndexOf(c, index + 1);
                    literal.Append(end < 0 ? pattern[(index + 1)..] : pattern[(index + 1)..end]);
                    index = end < 0 ? pattern.Length : end + 1;
                    continue;
                case '\\' when index + 1 < pattern.Length:
                    literal.Append(pattern[index + 1]);
                    index += 2;
                    continue;
                case '/':
                    literal.Append(info.DateSeparator);
                    break;
                case ':':
                    literal.Append(info.TimeSeparator);
                    break;
                // 星期、纪元、毫秒不是可以输入的段
                case 'd' or 'M' or 'g' or 'f' or 'F' or 'z' or 'K':
                    index += run;
                    continue;
                default:
                    literal.Append(c);
                    break;
            }
            index++;
        }
        Flush();
        return parts;
    }

    // 分隔符里的各种空白统一成普通空格（同 vue）；首尾只有空白的分隔符没有意义
    static SegmentPart[] Tidy(List<SegmentPart> parts)
    {
        var tidy = parts.Select(part => part.Type is null ? part with { Literal = part.Literal.Replace(' ', ' ').Replace(' ', ' ').Replace(' ', ' ') } : part).ToList();
        while (tidy.Count > 0 && tidy[0] is { Type: null } first && first.Literal.Trim().Length == 0) tidy.RemoveAt(0);
        while (tidy.Count > 0 && tidy[^1] is { Type: null } last && last.Literal.Trim().Length == 0) tidy.RemoveAt(tidy.Count - 1);
        return [.. tidy];
    }

    // ───────── 段名、占位、上午 / 下午 ─────────

    // 段名（同 Intl.DisplayNames 的 dateTimeField）：.NET 没有这份数据，zh、en 按 Intl 手写，别的语言用英文
    static readonly Dictionary<SegmentType, string> ZhLabels = new()
    {
        [SegmentType.Year] = "年", [SegmentType.Month] = "月", [SegmentType.Day] = "日", [SegmentType.Quarter] = "季度", [SegmentType.Week] = "周",
        [SegmentType.Hour] = "小时", [SegmentType.Minute] = "分钟", [SegmentType.Second] = "秒", [SegmentType.DayPeriod] = "上午/下午",
    };

    static readonly Dictionary<SegmentType, string> EnLabels = new()
    {
        [SegmentType.Year] = "year", [SegmentType.Month] = "month", [SegmentType.Day] = "day", [SegmentType.Quarter] = "quarter", [SegmentType.Week] = "week",
        [SegmentType.Hour] = "hour", [SegmentType.Minute] = "minute", [SegmentType.Second] = "second", [SegmentType.DayPeriod] = "AM/PM",
    };

    /// <summary>段名（读屏念的“年”“月”）。</summary>
    public static string Label(SegmentType type, string culture) =>
        (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? ZhLabels : EnLabels)[type];

    /// <summary>
    /// 空段的占位：语言包里的单位字（年、月、日）；后面紧跟的文字已经是这个单位时（中文的“2026年”）改用横线，显示成“––年”，否则是“年年”。
    /// </summary>
    public static string Placeholder(SegmentType type, string? next, Lang lang) =>
        lang.DateFieldPlaceholders.TryGetValue(Name(type), out var unit) && !(next?.TrimStart().StartsWith(unit, StringComparison.Ordinal) ?? false) ? unit : "––";

    /// <summary>上午、下午的写法。</summary>
    public static (string Am, string Pm) Periods(Lang lang)
    {
        var info = LangFormat.CultureOf(lang.Code).DateTimeFormat;
        return (info.AMDesignator is { Length: > 0 } am ? am : "AM", info.PMDesignator is { Length: > 0 } pm ? pm : "PM");
    }

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

/// <summary>各段的值（时是 0–23）；没填的段是 null。</summary>
sealed class DateParts
{
    readonly int?[] values = new int?[8];

    public int? this[SegmentType type]
    {
        get => type == SegmentType.DayPeriod ? null : values[(int)type];
        set => values[(int)type] = value;
    }

    public int? Year { get => this[SegmentType.Year]; set => this[SegmentType.Year] = value; }

    public int? Month { get => this[SegmentType.Month]; set => this[SegmentType.Month] = value; }

    public int? Day { get => this[SegmentType.Day]; set => this[SegmentType.Day] = value; }

    public int? Quarter { get => this[SegmentType.Quarter]; set => this[SegmentType.Quarter] = value; }

    public int? Week { get => this[SegmentType.Week]; set => this[SegmentType.Week] = value; }

    public int? Hour { get => this[SegmentType.Hour]; set => this[SegmentType.Hour] = value; }

    public int? Minute { get => this[SegmentType.Minute]; set => this[SegmentType.Minute] = value; }

    public int? Second { get => this[SegmentType.Second]; set => this[SegmentType.Second] = value; }
}
