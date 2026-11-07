using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rune.UI;

/// <summary>数字的样式（同 Intl.NumberFormat 的 style）。</summary>
public enum NumberStyle
{
    /// <summary>普通数字：默认最多三位小数。</summary>
    Decimal,

    /// <summary>百分比：乘以 100 加百分号，默认不留小数（0.123 → 12%）。</summary>
    Percent,

    /// <summary>金额：币种的符号，默认两位小数；币种不写时用语言包的 <see cref="Lang.Currency"/>。</summary>
    Currency,
}

/// <summary>数字的写法（同 Intl.NumberFormat 的 notation）。</summary>
public enum NumberNotation
{
    /// <summary>完整的数字：8,200,000。</summary>
    Standard,

    /// <summary>
    /// 简写：中文、日文按万、亿（820万），其余按 K、M、B、T（8.2M）；整数部分只有一位时保留两位有效数字，否则取整（同 Intl 的 compact）。
    /// 写了小数位数时按写的。
    /// </summary>
    Compact,
}

/// <summary>正负号怎么显示（同 Intl.NumberFormat 的 signDisplay）。</summary>
public enum SignDisplay
{
    /// <summary>只有负数带符号。</summary>
    Auto,

    /// <summary>正数带 +、负数带 -，零不带：涨跌、环比。</summary>
    ExceptZero,

    /// <summary>都带，零是 +0。</summary>
    Always,

    /// <summary>都不带。</summary>
    Never,
}

/// <summary>
/// 数字的格式（同 vue-tsx-pro 的 NumericFormat：Intl.NumberFormat 的选项，加上缩放与单位文字）：
/// <c>new NumberFormat { Scale = 10_000, MaximumFractionDigits = 2, Suffix = "万元" }</c>，155353.2 → 15.54 万元。
/// AnimatedNumber、Stat 的数值格式都是它；按语言格式化用 <see cref="LangFormat.Number(decimal, NumberFormat, Lang)"/>。
/// </summary>
public sealed record NumberFormat
{
    /// <summary>默认的格式：千分位、最多三位小数。</summary>
    public static NumberFormat Default { get; } = new();

    /// <summary>百分比，不留小数：0.123 → 12%。</summary>
    public static NumberFormat Percent { get; } = new() { Style = NumberStyle.Percent };

    /// <summary>样式。</summary>
    public NumberStyle Style { get; init; }

    /// <summary>最少几位小数（不够补 0）；不写时按样式（金额 2，其余 0）。</summary>
    public int? MinimumFractionDigits { get; init; }

    /// <summary>最多几位小数（多的四舍五入）；不写时按样式（普通 3，百分比 0，金额 2）。</summary>
    public int? MaximumFractionDigits { get; init; }

    /// <summary>先除以它再显示：<c>10_000</c> 表示以“万”为单位（155353.2 → 15.53532，再按小数位取舍）。</summary>
    public decimal Scale { get; init; } = 1;

    /// <summary>跟在数字后面的单位文字，如“万元”“元/件”，与数字之间一个空格。</summary>
    public string? Suffix { get; init; }

    /// <summary>写法：完整（默认）或简写（820万、8.2M）。</summary>
    public NumberNotation Notation { get; init; }

    /// <summary>正负号。</summary>
    public SignDisplay SignDisplay { get; init; }

    /// <summary>金额的币种（ISO 4217，如 CNY）；不写时用语言包的。</summary>
    public string? Currency { get; init; }

    /// <summary>千分位分隔，默认开。</summary>
    public bool UseGrouping { get; init; } = true;

    /// <summary>实际的小数位数范围：没写的按样式的默认值，只写了一头时另一头跟着它（同 Intl）。</summary>
    internal (int Min, int Max) FractionDigits
    {
        get
        {
            var (min, max) = Style switch
            {
                NumberStyle.Percent => (0, 0),
                NumberStyle.Currency => (2, 2),
                _ => (0, 3),
            };
            if (MinimumFractionDigits is { } givenMin) min = givenMin;
            if (MaximumFractionDigits is { } givenMax) max = givenMax;
            if (MaximumFractionDigits is null && min > max) max = min;
            if (MinimumFractionDigits is null && max < min) min = max;
            return (Math.Clamp(min, 0, 20), Math.Clamp(Math.Max(min, max), 0, 20));
        }
    }

    /// <summary>浏览器里逐帧格式化用的选项（AnimatedNumber 的过渡）：Intl.NumberFormat 的选项加 scale、suffix 的 JSON。</summary>
    internal string ToIntlJson(string currency)
    {
        var (min, max) = FractionDigits;
        var buffer = new ArrayBufferWriter<byte>();
        // 单位文字原样写，不转成 \uXXXX（属性值由 Blazor 转义）
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            json.WriteStartObject();
            json.WriteString("style", Style switch { NumberStyle.Percent => "percent", NumberStyle.Currency => "currency", _ => "decimal" });
            if (Style == NumberStyle.Currency) json.WriteString("currency", Currency ?? currency);
            json.WriteNumber("minimumFractionDigits", min);
            json.WriteNumber("maximumFractionDigits", max);
            json.WriteString("signDisplay", SignDisplay switch
            {
                SignDisplay.ExceptZero => "exceptZero",
                SignDisplay.Always => "always",
                SignDisplay.Never => "never",
                _ => "auto",
            });
            json.WriteBoolean("useGrouping", UseGrouping);
            if (Notation == NumberNotation.Compact) json.WriteString("notation", "compact");
            if (Scale != 1) json.WriteNumber("scale", Scale);
            if (Suffix is not null) json.WriteString("suffix", Suffix);
            json.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
