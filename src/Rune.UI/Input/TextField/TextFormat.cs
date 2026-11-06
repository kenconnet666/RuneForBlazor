using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Rune.UI;

/// <summary>
/// 输入时自动分组显示的格式（同 vue-tsx-pro 的 text-format）：值是去掉分隔的原文（13800001111），输入框里显示分好组的文字（138 0000 1111）。
/// 交给 TextField 的 Format；手机号、身份证号、银行卡号有现成的，别的自己 new：
/// <code>
/// &lt;TextField @bind-Value="phone" Format="TextFormat.Phone" /&gt;                    // phone == "13800001111"
/// &lt;TextField @bind-Value="card" Format="@(new TextFormat([4, 4, 4, 4, 3]))" /&gt;
/// </code>
/// 只按位置分组、过滤字符，不校验内容（号段、校验位）：校验写在模型上（<see cref="ChinaPhoneAttribute"/>、<see cref="IdCardAttribute"/>）。
/// 输入时的分组与光标在 JS 里（data-rune-format），没有往返。
/// </summary>
/// <param name="groups">各组的长度：[3, 4, 4] 是 3-4-4；最后一组之后的字符再按最后一组的长度接着分。</param>
public sealed class TextFormat(IReadOnlyList<int> groups)
{
    /// <summary>各组的长度。</summary>
    public IReadOnlyList<int> Groups { get; } = groups.Count > 0 && groups.All(g => g > 0)
        ? groups
        : throw new ArgumentException("每组至少 1 位，且至少要有一组", nameof(groups));

    /// <summary>中国大陆手机号：3-4-4，11 位数字。</summary>
    public static readonly TextFormat Phone = new([3, 4, 4]) { InputMode = "tel" };

    /// <summary>居民身份证号：6-8-4（地区、出生日期、顺序码与校验位），最后一位可以是 X（自动转大写）。</summary>
    public static readonly TextFormat IdCard = new([6, 8, 4]) { Accept = "[0-9Xx]", Uppercase = true, InputMode = "text" };

    /// <summary>银行卡号：每 4 位一组，最长 19 位。</summary>
    public static readonly TextFormat BankCard = new([4]) { MaxLength = 19 };

    /// <summary>接受的一个字符（正则的字符类），默认数字 <c>[0-9]</c>。</summary>
    public string Accept { get; init; } = "[0-9]";

    /// <summary>原文最长几位；不写时是各组加起来。</summary>
    public int? MaxLength { get; init; }

    /// <summary>组之间的分隔，默认一个空格。</summary>
    public string Separator { get; init; } = " ";

    /// <summary>字母转成大写（身份证的 X）。</summary>
    public bool Uppercase { get; init; }

    /// <summary>手机上弹出的键盘（inputmode），默认 numeric。</summary>
    public string InputMode { get; init; } = "numeric";

    int Limit => MaxLength ?? Groups.Sum();

    Regex AcceptOne => field ??= new($"^{Accept}$", RegexOptions.CultureInvariant);

    /// <summary>输入框里的文字 → 原文：去掉分隔、过滤掉不接受的字符、截到最长。</summary>
    public string Parse(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var kept = new StringBuilder();
        foreach (var rune in (Uppercase ? text.ToUpperInvariant() : text).EnumerateRunes())
        {
            if (kept.Length >= Limit) break;
            var one = rune.ToString();
            if (AcceptOne.IsMatch(one)) kept.Append(one);
        }
        return kept.ToString();
    }

    /// <summary>原文 → 显示的文字。</summary>
    public string Apply(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var parts = new List<string>();
        var rest = raw;
        for (var index = 0; rest.Length > 0; index++)
        {
            var size = Groups[Math.Min(index, Groups.Count - 1)];
            parts.Add(rest[..Math.Min(size, rest.Length)]);
            rest = rest[Math.Min(size, rest.Length)..];
        }
        return string.Join(Separator, parts);
    }

    /// <summary>交给 JS 的约定（输入时分组、放回光标）：输入框上的 data-rune-format 等属性。</summary>
    internal Attrs ToAttrs() => attrs ??= new Attrs
    {
        ["inputmode"] = InputMode,
        ["data-rune-format"] = string.Join(',', Groups.Select(g => g.ToString(CultureInfo.InvariantCulture))),
        ["data-rune-format-accept"] = Accept,
        ["data-rune-format-max"] = Limit.ToString(CultureInfo.InvariantCulture),
        ["data-rune-format-separator"] = Separator,
        ["data-rune-format-upper"] = Uppercase ? true : null,
    };

    Attrs? attrs;
}
