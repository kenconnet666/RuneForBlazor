using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;

namespace Rune.UI;

/// <summary>
/// 语言包里格式化数字用的小工具（Lang.g.cs 里 vue-tsx-pro 的 toLocaleString 翻译成它）：按语言代码取文化，
/// 取不到时（WebAssembly 只加载了部分文化数据、不变全球化模式）用不变文化，不抛异常。
/// </summary>
public static class LangFormat
{
    static readonly ConcurrentDictionary<string, CultureInfo> cultures = new(StringComparer.OrdinalIgnoreCase);

    static readonly string[] FileSizeUnits = ["B", "kB", "MB", "GB", "TB"];

    /// <summary>带千分位的数字（最多三位小数），如 12,345。</summary>
    public static string Number(double value, string culture) => value.ToString("#,0.###", CultureOf(culture));

    /// <summary>
    /// 按格式与语言显示数字（同 vue-tsx-pro 的 format.number）：<c>LangFormat.Number(0.123m, NumberFormat.Percent, lang)</c> → 12%。
    /// 用 decimal 计算，金额不经过浮点数；四舍五入同 Intl（0.5 远离零）。
    /// </summary>
    public static string Number(decimal value, NumberFormat format, Lang lang)
    {
        var culture = CultureOf(lang.Code);
        var info = culture.NumberFormat;
        var number = value / format.Scale;
        if (format.Style == NumberStyle.Percent) number *= 100;
        var (min, max) = format.FractionDigits;
        number = Math.Round(number, max, MidpointRounding.AwayFromZero);
        var pattern = (format.UseGrouping ? "#,0" : "0") + (max > 0 ? "." + new string('0', min) + new string('#', max - min) : "");
        var digits = Math.Abs(number).ToString(pattern, info);
        var sign = format.SignDisplay switch
        {
            SignDisplay.Never => "",
            SignDisplay.Auto => number < 0 ? info.NegativeSign : "",
            SignDisplay.ExceptZero => number == 0 ? "" : number < 0 ? info.NegativeSign : info.PositiveSign,
            _ => number < 0 ? info.NegativeSign : info.PositiveSign,
        };
        var text = sign + format.Style switch
        {
            NumberStyle.Percent => info.PercentPositivePattern switch
            {
                0 => $"{digits} {info.PercentSymbol}",
                2 => $"{info.PercentSymbol}{digits}",
                3 => $"{info.PercentSymbol} {digits}",
                _ => $"{digits}{info.PercentSymbol}",
            },
            NumberStyle.Currency => WithCurrency(digits, format.Currency ?? lang.Currency, culture),
            _ => digits,
        };
        return format.Suffix is null ? text : $"{text} {format.Suffix}";
    }

    // 币种是这个文化本地的：用它的符号与位置（zh-CN 的 CNY → ¥1.00）；不是时写币种代码（同 Intl 在别的地区显示外币）
    static string WithCurrency(string digits, string currency, CultureInfo culture)
    {
        var info = culture.NumberFormat;
        if (!string.Equals(LocalCurrencyOf(culture), currency, StringComparison.OrdinalIgnoreCase)) return $"{currency} {digits}";
        return info.CurrencyPositivePattern switch
        {
            1 => $"{digits}{info.CurrencySymbol}",
            2 => $"{info.CurrencySymbol} {digits}",
            3 => $"{digits} {info.CurrencySymbol}",
            _ => $"{info.CurrencySymbol}{digits}",
        };
    }

    static string? LocalCurrencyOf(CultureInfo culture)
    {
        try
        {
            return culture.IsNeutralCulture || culture.Equals(CultureInfo.InvariantCulture) ? null : new RegionInfo(culture.Name).ISOCurrencySymbol;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>文件大小，按 1000 进位取合适的单位（同 vue-tsx-pro 的 format.fileSize）：1234567 → 1.2 MB。</summary>
    public static string FileSize(long bytes, string culture)
    {
        var exponent = Math.Clamp((int)Math.Floor(Math.Log10(Math.Max(bytes, 1)) / 3), 0, FileSizeUnits.Length - 1);
        var value = bytes / Math.Pow(1000, exponent);
        return $"{value.ToString(exponent == 0 ? "#,0" : "#,0.#", CultureOf(culture))} {FileSizeUnits[exponent]}";
    }

    /// <summary>
    /// 日期（同 vue-tsx-pro 的 format.date，dateStyle: medium）：zh-CN 是 2026年1月1日，en-US 是 Jan 1, 2026；
    /// <paramref name="withTime"/> 时后面加上时、分（timeStyle: short）。值是 DateOnly、DateTime 或 DateTimeOffset。
    /// </summary>
    public static string Date(object value, Lang lang, bool withTime = false) =>
        Date(value, lang, DateStyle.Medium, withTime ? TimeStyle.Short : null);

    /// <summary>
    /// 日期时间按详略格式化（同 Intl.DateTimeFormat 的 dateStyle、timeStyle）：日期、时间的写法取文化的模式，
    /// 再按 Intl 的习惯调整（完整日期带星期；24 小时制的时补零；en-US 的短日期两位年份；en 的日期与时间之间是“, ”或“ at ”）。
    /// 值是 DateOnly、TimeOnly、DateTime 或 DateTimeOffset；dateStyle 为 null 时只有时间。
    /// 时刻（DateTimeOffset、UTC 的 DateTime）按语言的时区显示，其余原样（同 vue-tsx-pro）。
    /// </summary>
    public static string Date(object value, Lang lang, DateStyle? dateStyle, TimeStyle? timeStyle)
    {
        var dateTime = value switch
        {
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            TimeOnly time => DateOnly.MinValue.ToDateTime(time),
            DateTimeOffset offset => TimeZoneInfo.ConvertTime(offset, ZoneOf(lang)).DateTime,
            DateTime { Kind: DateTimeKind.Utc } utc => TimeZoneInfo.ConvertTimeFromUtc(utc, ZoneOf(lang)),
            DateTime time => time,
            _ => throw new ArgumentException($"不是日期：{value.GetType()}", nameof(value)),
        };
        var culture = lang.Code;
        var info = CultureOf(culture).DateTimeFormat;
        var english = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var day = dateStyle is { } style ? dateTime.ToString(DatePattern(info, style, culture), info) : null;
        var clock = timeStyle is { } t ? dateTime.ToString(TimePattern(info, t, TwelveHour(lang), culture), info) : null;
        if (day is null) return clock ?? "";
        if (clock is null) return day;
        // CLDR 的日期时间连接：en 的完整、长日期是“{日期} at {时间}”，中、短日期是“{日期}, {时间}”；其余语言之间一个空格
        var joiner = !english ? " " : dateStyle is DateStyle.Full or DateStyle.Long ? " at " : ", ";
        return day + joiner + clock;
    }

    static string DatePattern(DateTimeFormatInfo info, DateStyle style, string culture)
    {
        var withoutWeekday = info.LongDatePattern.Replace("dddd, ", "").Replace("dddd ", "").Replace("dddd", "").Trim();
        return style switch
        {
            // 完整日期带星期（zh-CN 的长日期没有：接在后面，2026年10月20日星期二）
            DateStyle.Full => info.LongDatePattern.Contains("dddd", StringComparison.Ordinal) ? info.LongDatePattern : withoutWeekday + "dddd",
            DateStyle.Long => withoutWeekday,
            // 长日期去掉星期、月份用简写：最接近 Intl 的 medium
            DateStyle.Medium => withoutWeekday.Replace("MMMM", "MMM"),
            // Intl 的 en-US 短日期是两位年份（10/20/26）
            _ => string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase) ? info.ShortDatePattern.Replace("yyyy", "yy") : info.ShortDatePattern,
        };
    }

    // 24 小时制的时补零（Intl 的 zh-CN 是 09:05，.NET 的模式是 H:mm）；新版 ICU 在“AM”前用窄不换行空格（U+202F），浏览器的 Intl 用普通空格
    // 小时制以语言包（或地区惯例）为准，文化模式里的只取写法：WebAssembly 的精简 ICU 没有地区的小时制数据，zh-CN 也给出“下午2:30”
    internal static string TimePattern(DateTimeFormatInfo info, TimeStyle style, bool twelveHour, string culture)
    {
        var pattern = (style == TimeStyle.Medium ? info.LongTimePattern : info.ShortTimePattern).Replace('\u202F', ' ');
        var patternTwelveHour = pattern.Contains('h', StringComparison.Ordinal);
        if (patternTwelveHour && !twelveHour) pattern = pattern.Replace("tt", "", StringComparison.Ordinal).Replace('h', 'H').Trim();
        else if (!patternTwelveHour && twelveHour)
        {
            // 24 小时制的文化要 12 小时制：中、日的上下午在前（下午2:30），其余在后（2:30 PM）
            var hour = pattern.Replace("HH", "h", StringComparison.Ordinal).Replace('H', 'h');
            return culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "tt" + hour : hour + " tt";
        }
        return pattern.Contains("HH", StringComparison.Ordinal) || pattern.Contains('h', StringComparison.Ordinal) ? pattern : pattern.Replace("H", "HH", StringComparison.Ordinal);
    }

    /// <summary>
    /// 12 小时制：语言包的 <see cref="Lang.HourCycle"/>（h11、h12 是，h23、h24 不是）；不写时按地区惯例（同 Intl 的默认）。
    /// </summary>
    internal static bool TwelveHour(Lang lang) => lang.HourCycle switch
    {
        "h11" or "h12" => true,
        "h23" or "h24" => false,
        _ => lang.Code is not ("fr-CA" or "ku-SY") && TwelveHourRegions.Contains(RegionOf(lang.Code)),
    };

    // 地区代码：语言代码里两个字母的子标签（zh-Hans-CN → CN）；没有时取这个语言最常用的地区
    static string RegionOf(string code)
    {
        if (code.Split('-').LastOrDefault(part => part.Length == 2 && part.All(char.IsAsciiLetter)) is { Length: 2 } region) return region.ToUpperInvariant();
        try
        {
            var specific = CultureInfo.CreateSpecificCulture(code).Name;
            return specific.Contains('-', StringComparison.Ordinal) ? specific[(specific.LastIndexOf('-') + 1)..].ToUpperInvariant() : "";
        }
        catch (CultureNotFoundException)
        {
            return "";
        }
    }

    // 惯用 12 小时制的地区：CLDR 49 supplemental/timeData 里 _preferred 是 h 的（fr-CA、ku-SY 例外，上面单独排除）
    static readonly FrozenSet<string> TwelveHourRegions = new[]
    {
        "AE", "AG", "AL", "AS", "AU", "BB", "BD", "BH", "BM", "BN", "BO", "BS", "BT", "CA", "CO", "CR", "CU", "CY", "DJ", "DM",
        "DO", "DZ", "EC", "EG", "EH", "ER", "ET", "FJ", "FM", "GD", "GH", "GM", "GR", "GT", "GU", "GY", "HK", "HN", "IN", "IQ",
        "JM", "JO", "KH", "KI", "KN", "KP", "KR", "KW", "KY", "LB", "LC", "LR", "LS", "LY", "MH", "MO", "MP", "MR", "MW", "MX",
        "MY", "NA", "NI", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PR", "PS", "PW", "QA", "SA", "SB", "SD", "SG", "SL", "SO",
        "SS", "SV", "SY", "SZ", "TC", "TD", "TN", "TO", "TT", "TW", "UM", "US", "VC", "VE", "VG", "VI", "VU", "WS", "YE",
    }.ToFrozenSet();

    /// <summary>语言的时区（IANA 名称，如 Asia/Shanghai）；没写或找不到时是本地时区。</summary>
    internal static TimeZoneInfo ZoneOf(Lang lang)
    {
        if (lang.TimeZone is not { } id) return TimeZoneInfo.Local;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    internal static CultureInfo CultureOf(string name) => cultures.GetOrAdd(name, static name =>
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    });
}

/// <summary>日期的详略（同 Intl.DateTimeFormat 的 dateStyle）。</summary>
public enum DateStyle
{
    /// <summary>完整，带星期：2026年10月20日星期二、Tuesday, October 20, 2026。</summary>
    Full,

    /// <summary>长：2026年10月20日、October 20, 2026。</summary>
    Long,

    /// <summary>中（默认）：2026年10月20日、Oct 20, 2026。</summary>
    Medium,

    /// <summary>短：2026/10/20、10/20/26。</summary>
    Short,
}

/// <summary>时间的详略（同 Intl.DateTimeFormat 的 timeStyle）。</summary>
public enum TimeStyle
{
    /// <summary>时、分：09:05、9:05 AM。</summary>
    Short,

    /// <summary>时、分、秒：09:05:07、9:05:07 AM。</summary>
    Medium,
}
