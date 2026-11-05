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

    /// <summary>带千分位的数字（最多三位小数），如 12,345。</summary>
    public static string Number(double value, string culture) => value.ToString("#,0.###", CultureOf(culture));

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
