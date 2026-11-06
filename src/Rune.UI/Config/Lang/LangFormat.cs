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

    /// <summary>文件大小，按 1000 进位取合适的单位（同 vue-tsx-pro 的 format.fileSize）：1234567 → 1.2 MB。</summary>
    public static string FileSize(long bytes, string culture)
    {
        var exponent = Math.Clamp((int)Math.Floor(Math.Log10(Math.Max(bytes, 1)) / 3), 0, FileSizeUnits.Length - 1);
        var value = bytes / Math.Pow(1000, exponent);
        return $"{value.ToString(exponent == 0 ? "#,0" : "#,0.#", CultureOf(culture))} {FileSizeUnits[exponent]}";
    }

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
