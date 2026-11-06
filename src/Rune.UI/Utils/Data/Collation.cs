using System.Collections.Concurrent;
using System.Globalization;

namespace Rune.UI;

/// <summary>
/// 按语言比较值（同 vue-tsx-pro 的 createComparer、sortByValues）：表格的排序、表头筛选的选项顺序共用。
/// 数字按大小（各种数字类型混着也行，按 decimal 比较）；日期、时间按先后；布尔 false 在前；
/// 文字按语言的排序规则（中文按拼音），数字部分按数值（“第2章”在“第10章”前）。比较器按语言缓存。
/// </summary>
public static class Collation
{
    static readonly ConcurrentDictionary<string, Comparison<object?>> comparers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>某个语言（如 zh-CN）的比较：null 排在最后。</summary>
    public static Comparison<object?> Comparer(string culture) => comparers.GetOrAdd(culture, static name =>
    {
        var compare = CompareInfoOf(name);
        var options = TextOptions(compare);
        return (a, b) =>
        {
            if (a is null || b is null) return a is null ? (b is null ? 0 : 1) : -1;
            if (Numeric(a) is { } x && Numeric(b) is { } y) return x.CompareTo(y);
            if (a is IComparable comparable && a.GetType() == b.GetType() && a is not string) return comparable.CompareTo(b);
            return compare.Compare(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), options);
        };
    });

    /// <summary>
    /// 按几个键排序（返回新的列表，原来的不变）：前一个键相等时比下一个，都相等时保持原来的先后；空值不随方向翻转、总在最后。
    /// </summary>
    public static IReadOnlyList<T> SortBy<T>(IReadOnlyList<T> rows, IReadOnlyList<(Func<T, object?> Value, SortDirection Direction)> keys, string culture)
    {
        if (keys.Count == 0) return rows;
        var compare = Comparer(culture);
        var entries = rows.Select((row, index) => (Row: row, Index: index, Values: keys.Select(key => key.Value(row)).ToArray())).ToArray();
        Array.Sort(entries, (a, b) =>
        {
            for (var at = 0; at < keys.Count; at++)
            {
                var (x, y) = (a.Values[at], b.Values[at]);
                var order = compare(x, y);
                if (x is not null && y is not null && keys[at].Direction == SortDirection.Descending) order = -order;
                if (order != 0) return order;
            }
            return a.Index.CompareTo(b.Index);
        });
        return [.. entries.Select(static entry => entry.Row)];
    }

    // 各种数字类型统一按 decimal 比较；十进制字符串（"2.5"）是文字，数字列由调用方先转成数字
    static decimal? Numeric(object value) => value switch
    {
        decimal number => number,
        int number => number,
        long number => number,
        short number => number,
        byte number => number,
        uint number => number,
        ulong number => number,
        float number when float.IsFinite(number) => (decimal)number,
        double number when double.IsFinite(number) && Math.Abs(number) < 7.9e28 => (decimal)number,
        _ => null,
    };

    static CompareInfo CompareInfoOf(string culture)
    {
        try
        {
            return CultureInfo.GetCultureInfo(culture).CompareInfo;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture.CompareInfo;
        }
    }

    // 不分大小写与重音（同 Intl.Collator 的 sensitivity: base），数字按数值；不加载 ICU 的环境不支持时退回不分大小写
    static CompareOptions TextOptions(CompareInfo compare)
    {
        const CompareOptions Full = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.NumericOrdering;
        try
        {
            compare.Compare("a1", "a2", Full);
            return Full;
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or ArgumentException)
        {
            return CompareOptions.IgnoreCase;
        }
    }
}
