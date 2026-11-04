using System.Globalization;

namespace Rune.UI;

/// <summary>
/// 数字与文本之间的转换（NumberField 用）：按当前文化解析与格式化，认中文输入法打出的全角数字与标点；
/// 加减、对齐步长、夹到范围都换成 decimal 计算，double 的 0.1 + 0.2 也不会出现浮点误差。
/// </summary>
public static class NumberText
{
    /// <summary>解析输入的文字；空白返回 null 并成功，解析不了返回 false。</summary>
    public static bool TryParse(string? text, CultureInfo culture, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!decimal.TryParse(Normalize(text), NumberStyles.Number | NumberStyles.AllowCurrencySymbol, culture, out var parsed)) return false;
        value = parsed;
        return true;
    }

    /// <summary>
    /// 夹到 [min, max]，再对齐到 <c>min + n × step</c>（没有 min 时对齐到 n × step）。
    /// </summary>
    public static decimal Constrain(decimal value, decimal? min, decimal? max, decimal step)
    {
        if (step > 0)
        {
            var origin = min ?? 0;
            value = origin + Math.Round((value - origin) / step, MidpointRounding.AwayFromZero) * step;
        }
        if (min is { } low && value < low) value = low;
        if (max is { } high && value > high) value = high;
        return value;
    }

    /// <summary>值转成 decimal 计算：任何数字类型（int、long、decimal、double…，可以是可空的）；null、NaN、无穷大当作没有值。</summary>
    public static decimal? ToDecimal<T>(T value) => value switch
    {
        null => null,
        double d when double.IsNaN(d) || double.IsInfinity(d) => null,
        float f when float.IsNaN(f) || float.IsInfinity(f) => null,
        IConvertible convertible => convertible.ToDecimal(CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"{typeof(T)} 不是数字类型"),
    };

    /// <summary>
    /// decimal 转回值的类型（可空类型给 null 时返回 null）。超出类型范围时取边界。
    /// 不可空的类型给 null 时返回 false（值不能清空，调用方恢复原值）。
    /// </summary>
    public static bool TryFromDecimal<T>(decimal? value, out T result)
    {
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (value is not { } v)
        {
            result = default!;
            return Nullable.GetUnderlyingType(typeof(T)) is not null;
        }
        result = (T)Convert.ChangeType(Clamp(v, target), target, CultureInfo.InvariantCulture);
        return true;
    }

    // 超出整数类型范围时取边界（Convert.ChangeType 会抛溢出）
    static decimal Clamp(decimal value, Type target) => Type.GetTypeCode(target) switch
    {
        TypeCode.Byte => Math.Clamp(value, byte.MinValue, byte.MaxValue),
        TypeCode.SByte => Math.Clamp(value, sbyte.MinValue, sbyte.MaxValue),
        TypeCode.Int16 => Math.Clamp(value, short.MinValue, short.MaxValue),
        TypeCode.UInt16 => Math.Clamp(value, ushort.MinValue, ushort.MaxValue),
        TypeCode.Int32 => Math.Clamp(value, int.MinValue, int.MaxValue),
        TypeCode.UInt32 => Math.Clamp(value, uint.MinValue, uint.MaxValue),
        TypeCode.Int64 => Math.Clamp(value, long.MinValue, long.MaxValue),
        TypeCode.UInt64 => Math.Clamp(value, ulong.MinValue, ulong.MaxValue),
        _ => value,
    };

    /// <summary>按格式与文化显示值（null 显示为空）。</summary>
    public static string Format<T>(T value, string? format, CultureInfo culture) => value switch
    {
        null => "",
        IFormattable formattable => formattable.ToString(format, culture),
        _ => value.ToString() ?? "",
    };

    // 全角数字、全角句点与逗号、全角负号 → 半角
    static string Normalize(string text)
    {
        Span<char> buffer = text.Length <= 128 ? stackalloc char[text.Length] : new char[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            buffer[i] = c switch
            {
                >= '０' and <= '９' => (char)(c - '０' + '0'),
                '．' or '。' => '.',
                '，' => ',',
                '－' or '−' => '-',
                _ => c,
            };
        }
        return new string(buffer).Trim();
    }
}
