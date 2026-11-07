using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Rune.UI;

/// <summary>一个字段的比较结果（<see cref="Diff.Fields"/>）。</summary>
/// <param name="Key">字段名（首字母小写，同表格列的 key）。</param>
/// <param name="Before">之前的值；那一份没有时 null。</param>
/// <param name="After">之后的值。</param>
/// <param name="Changed">变了没有（结构相等：数字按数值、集合逐项）。</param>
/// <param name="Delta">两边都是数字时的差：After − Before（decimal 精确运算）。</param>
/// <param name="Ratio">两边都是数字且之前不为 0 时的变化率：Delta / Before（-0.2 是降了 20%）。</param>
public sealed record FieldDiff(string Key, object? Before, object? After, bool Changed, decimal? Delta = null, decimal? Ratio = null);

/// <summary>一行和之前比怎么了。</summary>
public enum RowChange
{
    /// <summary>之前没有。</summary>
    Inserted,

    /// <summary>有字段变了。</summary>
    Updated,

    /// <summary>现在没有了。</summary>
    Deleted,
}

/// <summary>一行的比较结果（<see cref="Diff.Rows"/>）。</summary>
/// <param name="Key">行的 key。</param>
/// <param name="Row">现在的行；删除了的行是之前的样子。</param>
/// <param name="Change">没变时为 null。</param>
/// <param name="Before">修改了的行之前的样子。</param>
/// <param name="ChangedFields">修改了的行里变了的字段（首字母小写）。</param>
public sealed record RowDiff<T>(string Key, T Row, RowChange? Change = null, T? Before = default, IReadOnlyList<string>? ChangedFields = null);

/// <summary>
/// 比较两份记录、两份列表（同 vue-tsx-pro 的 diffFields、diffRows）：测算版本对比、编辑前后、两次导入——哪些字段、哪些行变了，数值变了多少。
/// <code>
/// Diff.Fields(v1, v2, ["amount", "quantity", "reason"])   // amount：120 → 98，Delta -22，Ratio -0.183
/// Diff.Rows(before, after, row => row.Id)                  // 新增、修改（带 ChangedFields）、删除的行
/// </code>
/// </summary>
public static class Diff
{
    /// <summary>按字段比较两份记录；keys 决定比哪些字段与顺序（首字母小写），不写时是全部公开属性。某一份是 null 时它的每个字段都当作没有。</summary>
    public static IReadOnlyList<FieldDiff> Fields<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T? before, T? after, IReadOnlyList<string>? keys = null)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(static property => property.GetIndexParameters().Length == 0)
            .ToDictionary(static property => KeyOf(property.Name));
        return [.. (keys ?? [.. properties.Keys]).Select(key =>
        {
            var property = properties.GetValueOrDefault(key);
            var from = before is null || property is null ? null : property.GetValue(before);
            var to = after is null || property is null ? null : property.GetValue(after);
            if (Numeric.Of(from) is { } a && Numeric.Of(to) is { } b)
            {
                var delta = b - a;
                return new FieldDiff(key, from, to, a != b, delta, a == 0 ? null : delta / a);
            }
            return new FieldDiff(key, from, to, !Same(from, to));
        })];
    }

    /// <summary>两份列表按 key 比较：顺序是现在的行，删除了的排在最后；字段变没变同 <see cref="Fields"/>。</summary>
    public static IReadOnlyList<RowDiff<T>> Rows<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IReadOnlyList<T> before, IReadOnlyList<T> after, Func<T, string> getKey)
    {
        var remaining = before.ToDictionary(getKey);
        var rows = new List<RowDiff<T>>();
        foreach (var row in after)
        {
            var key = getKey(row);
            if (!remaining.Remove(key, out var previous))
            {
                rows.Add(new(key, row, RowChange.Inserted));
                continue;
            }
            var changed = Fields(previous, row).Where(static field => field.Changed).Select(static field => field.Key).ToList();
            rows.Add(changed.Count > 0 ? new(key, row, RowChange.Updated, previous, changed) : new(key, row));
        }
        // 删除了的：按之前的顺序排在最后
        rows.AddRange(before.Where(row => remaining.ContainsKey(getKey(row))).Select(row => new RowDiff<T>(getKey(row), row, RowChange.Deleted)));
        return rows;
    }

    /// <summary>结构相等：数字按数值（1.0m 与 1 相等），集合逐项，其余用 Equals（record 按值）。</summary>
    public static bool Same(object? a, object? b)
    {
        if (Equals(a, b)) return true;
        if (Numeric.Of(a) is { } x && Numeric.Of(b) is { } y) return x == y;
        if (a is IEnumerable left and not string && b is IEnumerable right and not string)
        {
            var first = left.Cast<object?>().ToList();
            var second = right.Cast<object?>().ToList();
            return first.Count == second.Count && first.Zip(second).All(static pair => Same(pair.First, pair.Second));
        }
        return false;
    }

    static string KeyOf(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}

/// <summary>FieldCompare 的排法。</summary>
public enum FieldCompareLayout
{
    /// <summary>表格：字段、原值、新值、变化四列（默认列出全部字段，没改的淡显）。</summary>
    Table,

    /// <summary>列表：一行一个字段，“原值 → 新值 变化”（默认只列改了的）。</summary>
    List,
}
