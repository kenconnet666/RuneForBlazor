using System.Globalization;

namespace Rune.UI;

public sealed partial record Lang
{
    /// <summary>
    /// 按界面文化选语言包：先找代码完全相同的（zh-CN），再找语言相同的（en-GB 用 en-US），沿父文化逐级往上；都没有时为 null。
    /// </summary>
    /// <param name="culture">界面文化，通常是 <see cref="CultureInfo.CurrentUICulture"/>。</param>
    /// <param name="candidates">可选的语言包。</param>
    public static Lang? Match(CultureInfo culture, IEnumerable<Lang> candidates)
    {
        var list = candidates as IReadOnlyList<Lang> ?? [.. candidates];
        for (var c = culture; !Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
        {
            var (name, language) = (c.Name, c.TwoLetterISOLanguageName);
            if (list.FirstOrDefault(l => string.Equals(l.Code, name, StringComparison.OrdinalIgnoreCase)) is { } exact) return exact;
            if (list.FirstOrDefault(l => SameLanguage(l.Code, language)) is { } sameLanguage) return sameLanguage;
        }
        return null;
    }

    static bool SameLanguage(string code, string language) =>
        code.StartsWith(language, StringComparison.OrdinalIgnoreCase) && (code.Length == language.Length || code[language.Length] == '-');
}
