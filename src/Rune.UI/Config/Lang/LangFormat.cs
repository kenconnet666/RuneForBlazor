using System.Collections.Concurrent;
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
    public static string Date(object value, string culture, bool withTime = false)
    {
        var info = CultureOf(culture).DateTimeFormat;
        var dateTime = value switch
        {
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            DateTimeOffset offset => offset.DateTime,
            DateTime time => time,
            _ => throw new ArgumentException($"不是日期：{value.GetType()}", nameof(value)),
        };
        var pattern = MediumDatePattern(info) + (withTime ? " " + info.ShortTimePattern : "");
        return dateTime.ToString(pattern, info);
    }

    // 长日期去掉星期、月份用简写：最接近 Intl 的 dateStyle: medium
    static string MediumDatePattern(DateTimeFormatInfo info) =>
        info.LongDatePattern.Replace("dddd, ", "").Replace("dddd ", "").Replace("dddd", "").Replace("MMMM", "MMM").Trim();

    static CultureInfo CultureOf(string name) => cultures.GetOrAdd(name, static name =>
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
