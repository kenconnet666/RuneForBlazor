using System.Globalization;

namespace Rune.UI;

/// <summary>把各种数字统一成 decimal（同 vue-tsx-pro 的 numeric.ts）：表格的格式化、合计、透视、字段对比共用，不经过浮点相加。</summary>
static class Numeric
{
    /// <summary>
    /// 数字的 decimal 值：整数、decimal、有限且不超出范围的 float / double；<paramref name="text"/> 时十进制字符串（接口常把金额给成 "19.90"）也算。
    /// 不是数字时为 null。
    /// </summary>
    public static decimal? Of(object? value, bool text = false) => value switch
    {
        null => null,
        decimal number => number,
        int or long or short or byte or uint or ulong or ushort or sbyte => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
        double number when double.IsFinite(number) && Math.Abs(number) < 7.9e28 => (decimal)number,
        float number when float.IsFinite(number) && Math.Abs(number) < 7.9e28f => (decimal)number,
        string digits when text && decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null,
    };

    /// <summary>是数字类型（不论能不能转成 decimal）。</summary>
    public static bool IsNumber(object value) => value is decimal or int or long or short or byte or uint or ulong or ushort or sbyte or float or double;
}
