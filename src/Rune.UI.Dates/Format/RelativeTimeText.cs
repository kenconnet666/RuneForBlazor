using System.Globalization;

namespace Rune.UI;

/// <summary>相对时间的详略（同 Intl.RelativeTimeFormat 的 style）。</summary>
public enum RelativeTimeStyle
{
    /// <summary>完整：“3 分钟前”“3 minutes ago”（默认）。</summary>
    Long,

    /// <summary>简短：“3 min. ago”。</summary>
    Short,

    /// <summary>最短：“3m ago”。</summary>
    Narrow,
}

/// <summary>相对时间的单位。</summary>
public enum RelativeTimeUnit
{
    /// <summary>秒。</summary>
    Second,

    /// <summary>分。</summary>
    Minute,

    /// <summary>时。</summary>
    Hour,

    /// <summary>天。</summary>
    Day,

    /// <summary>月。</summary>
    Month,

    /// <summary>年。</summary>
    Year,
}

/// <summary>
/// 相对时间的文字（同 vue-tsx-pro 的 format.relative：Intl.RelativeTimeFormat，numeric: 'auto'）：“刚刚”的写法、“3 分钟前”“昨天”“2 年后”。
/// .NET 没有 RelativeTimeFormat，这里按 CLDR 的写法内置 zh、en 两种语言（其余语言用 en）；单位的取法同 vue 的 relativeUnit：
/// 45 秒以内用秒，45 分钟以内用分，22 小时以内用时，26 天以内用天，320 天以内用月，再远用年（参考 moment.js 的取整阈值）。
/// </summary>
public static class RelativeTimeText
{
    // [单位, 一个单位多少秒, 小于多少秒时用它]
    static readonly (RelativeTimeUnit Unit, double Size, double Below)[] Units =
    [
        (RelativeTimeUnit.Second, 1, 45),
        (RelativeTimeUnit.Minute, 60, 45 * 60),
        (RelativeTimeUnit.Hour, 3600, 22 * 3600),
        (RelativeTimeUnit.Day, 86_400, 26 * 86_400),
        (RelativeTimeUnit.Month, 30 * 86_400, 320 * 86_400),
        (RelativeTimeUnit.Year, 365 * 86_400, double.PositiveInfinity),
    ];

    /// <summary>相差多久 → 数值与单位：<c>Of(TimeSpan.FromMinutes(-3))</c> → (-3, Minute)。负数是过去。</summary>
    public static (long Value, RelativeTimeUnit Unit) Of(TimeSpan difference)
    {
        var seconds = difference.TotalSeconds;
        var (unit, size, _) = Units.First(entry => Math.Abs(seconds) < entry.Below);
        // 同 JS 的 Math.round：.5 往正无穷取整（-2.5 → -2）
        return ((long)Math.Floor(seconds / size + 0.5), unit);
    }

    /// <summary>目标时刻相对“现在”的文字：<c>Format(target - now, "zh-CN")</c> → “3分钟前”。</summary>
    public static string Format(TimeSpan difference, string culture, RelativeTimeStyle style = RelativeTimeStyle.Long)
    {
        var (value, unit) = Of(difference);
        return Format(value, unit, culture, style);
    }

    /// <summary>数值与单位的文字（numeric: auto：“昨天”而不是“1 天前”，0 秒是“现在”）。</summary>
    public static string Format(long value, RelativeTimeUnit unit, string culture, RelativeTimeStyle style = RelativeTimeStyle.Long) =>
        culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? Chinese(value, unit, style) : English(value, unit, style);

    static string Chinese(long value, RelativeTimeUnit unit, RelativeTimeStyle style)
    {
        var special = (unit, value) switch
        {
            (RelativeTimeUnit.Second, 0) => "现在",
            (RelativeTimeUnit.Minute, 0) => "此刻",
            (RelativeTimeUnit.Hour, 0) => "这一时间 / 此时",
            (RelativeTimeUnit.Day, -2) => "前天",
            (RelativeTimeUnit.Day, -1) => "昨天",
            (RelativeTimeUnit.Day, 0) => "今天",
            (RelativeTimeUnit.Day, 1) => "明天",
            (RelativeTimeUnit.Day, 2) => "后天",
            (RelativeTimeUnit.Month, -1) => "上个月",
            (RelativeTimeUnit.Month, 0) => "本月",
            (RelativeTimeUnit.Month, 1) => "下个月",
            (RelativeTimeUnit.Year, -1) => "去年",
            (RelativeTimeUnit.Year, 0) => "今年",
            (RelativeTimeUnit.Year, 1) => "明年",
            _ => null,
        };
        if (special is not null) return special;
        var word = unit switch
        {
            RelativeTimeUnit.Second => style == RelativeTimeStyle.Long ? "秒钟" : "秒",
            RelativeTimeUnit.Minute => "分钟",
            RelativeTimeUnit.Hour => "小时",
            RelativeTimeUnit.Day => "天",
            RelativeTimeUnit.Month => "个月",
            _ => "年",
        };
        return $"{Math.Abs(value).ToString(CultureInfo.InvariantCulture)}{word}{(value < 0 ? "前" : "后")}";
    }

    static string English(long value, RelativeTimeUnit unit, RelativeTimeStyle style)
    {
        var shortForm = style != RelativeTimeStyle.Long;
        var special = (unit, value) switch
        {
            (RelativeTimeUnit.Second, 0) => "now",
            (RelativeTimeUnit.Minute, 0) => "this minute",
            (RelativeTimeUnit.Hour, 0) => "this hour",
            (RelativeTimeUnit.Day, -1) => "yesterday",
            (RelativeTimeUnit.Day, 0) => "today",
            (RelativeTimeUnit.Day, 1) => "tomorrow",
            (RelativeTimeUnit.Month, -1) => shortForm ? "last mo." : "last month",
            (RelativeTimeUnit.Month, 0) => shortForm ? "this mo." : "this month",
            (RelativeTimeUnit.Month, 1) => shortForm ? "next mo." : "next month",
            (RelativeTimeUnit.Year, -1) => shortForm ? "last yr." : "last year",
            (RelativeTimeUnit.Year, 0) => shortForm ? "this yr." : "this year",
            (RelativeTimeUnit.Year, 1) => shortForm ? "next yr." : "next year",
            _ => null,
        };
        if (special is not null) return special;
        var count = Math.Abs(value);
        var number = count.ToString("#,0", CultureInfo.InvariantCulture);
        var amount = style switch
        {
            // narrow：数字后面直接跟单位的缩写
            RelativeTimeStyle.Narrow => number + unit switch
            {
                RelativeTimeUnit.Second => "s",
                RelativeTimeUnit.Minute => "m",
                RelativeTimeUnit.Hour => "h",
                RelativeTimeUnit.Day => "d",
                RelativeTimeUnit.Month => "mo",
                _ => "y",
            },
            RelativeTimeStyle.Short => $"{number} " + unit switch
            {
                RelativeTimeUnit.Second => "sec.",
                RelativeTimeUnit.Minute => "min.",
                RelativeTimeUnit.Hour => "hr.",
                RelativeTimeUnit.Day => count == 1 ? "day" : "days",
                RelativeTimeUnit.Month => "mo.",
                _ => "yr.",
            },
            _ => $"{number} {unit.ToString().ToLowerInvariant()}{(count == 1 ? "" : "s")}",
        };
        return value < 0 ? $"{amount} ago" : $"in {amount}";
    }
}
