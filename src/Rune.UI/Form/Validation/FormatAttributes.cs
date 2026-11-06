using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Rune.UI;

/// <summary>
/// 中国大陆手机号（同 vue-tsx-pro 的 phoneSchema）：1 开头、第二位 3–9、共 11 位数字。配 TextField 的 <see cref="TextFormat.Phone"/>，值里没有空格。
/// 空值算通过（必填用 Required）。
/// <code>[ChinaPhone(ErrorMessage = "请填写 11 位手机号")] public string? Phone { get; set; }</code>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed partial class ChinaPhoneAttribute() : ValidationAttribute("{0}格式不对")
{
    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is not string text || text.Length == 0 || Pattern().IsMatch(text);

    [GeneratedRegex("^1[3-9][0-9]{9}$")]
    private static partial Regex Pattern();
}

/// <summary>
/// 居民身份证号（GB 11643，同 vue-tsx-pro 的 idCardSchema）：18 位，出生日期是真实的日期，最后一位校验码对得上。配 <see cref="TextFormat.IdCard"/>。
/// 空值算通过（必填用 Required）。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed partial class IdCardAttribute() : ValidationAttribute("{0}不对")
{
    static readonly int[] Weights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
    const string CheckCodes = "10X98765432";

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is not string text || text.Length == 0 || IsValidIdCard(text);

    /// <summary>身份证号是否有效：格式、1900 年以后真实的出生日期、校验码。</summary>
    public static bool IsValidIdCard(string value)
    {
        if (!Pattern().IsMatch(value)) return false;
        var year = int.Parse(value[6..10], System.Globalization.CultureInfo.InvariantCulture);
        var month = int.Parse(value[10..12], System.Globalization.CultureInfo.InvariantCulture);
        var day = int.Parse(value[12..14], System.Globalization.CultureInfo.InvariantCulture);
        if (year < 1900 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        var sum = 0;
        for (var i = 0; i < Weights.Length; i++) sum += Weights[i] * (value[i] - '0');
        return CheckCodes[sum % 11] == value[17];
    }

    [GeneratedRegex("^[0-9]{17}[0-9X]$")]
    private static partial Regex Pattern();
}
