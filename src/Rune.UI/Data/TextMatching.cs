using System.Collections.Concurrent;
using System.Globalization;

namespace Rune.UI;

/// <summary>文字匹配：<paramref name="text"/>（选项的文字）是否算命中 <paramref name="query"/>（输入的搜索词）。</summary>
public delegate bool TextMatcher(string text, string query);

/// <summary>
/// 按语言匹配文字（同 vue-tsx-pro 的 createMatcher）：Select、Combobox 的过滤共用。包含即命中，忽略大小写、重音与全半角
/// （“Ångström”能用 ang 搜到）。另配的 <see cref="UIConfig.TextMatcher"/>（如拼音）与它任一命中都算。
/// </summary>
public static class TextMatching
{
    const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;

    static readonly ConcurrentDictionary<string, TextMatcher> builtin = new();

    /// <summary>某个语言（如 zh-CN）的内置匹配；搜索词为空时总是命中。</summary>
    public static TextMatcher For(string culture) => builtin.GetOrAdd(culture, static name =>
    {
        var compare = CultureInfo.GetCultureInfo(name).CompareInfo;
        return (text, query) => query.Length == 0 || IndexOf(compare, text, query) >= 0;
    });

    // 不加载 ICU 的环境（InvariantGlobalization）不支持忽略重音：退回只忽略大小写
    static int IndexOf(CompareInfo compare, string text, string query)
    {
        try
        {
            return compare.IndexOf(text, query, Loose);
        }
        catch (PlatformNotSupportedException)
        {
            return text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
