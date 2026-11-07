using System.Globalization;
using System.Text;

namespace Rune.UI;

// 分段的排法（按文化算段的顺序与分隔符）、段名、占位文字、上午 / 下午
static partial class DateSegments
{
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
}
