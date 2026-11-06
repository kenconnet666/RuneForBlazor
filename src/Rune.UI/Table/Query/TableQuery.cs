using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Rune.UI;

/// <summary>排序方向。</summary>
public enum SortDirection
{
    /// <summary>正序。</summary>
    Ascending,

    /// <summary>倒序。</summary>
    Descending,
}

/// <summary>一列的排序（同 vue-tsx-pro 的 SortState）；几列排序时前面的优先。</summary>
/// <param name="Key">列的 key（服务端的字段名）。</param>
/// <param name="Direction">方向。</param>
public readonly record struct ColumnSort(string Key, SortDirection Direction)
{
    /// <summary>排序写成线上的文字：<c>-amount,name</c>（减号是倒序，同 JSON:API）；没有排序时是空字符串。</summary>
    public static string Format(IEnumerable<ColumnSort> sorts) =>
        string.Join(",", sorts.Select(sort => sort.Direction == SortDirection.Descending ? "-" + sort.Key : sort.Key));

    /// <summary>解析线上的排序文字（<see cref="Format"/> 的反过来）；空的项跳过。</summary>
    public static IReadOnlyList<ColumnSort> Parse(string? text) =>
        [.. (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.StartsWith('-') ? new ColumnSort(item[1..], SortDirection.Descending) : new ColumnSort(item, SortDirection.Ascending))
            .Where(sort => sort.Key.Length > 0)];

    /// <summary>单列排序时点了一列的表头：换到这一列是正序，正序再点是倒序，倒序再点回到默认顺序（null）。</summary>
    public static ColumnSort? Next(ColumnSort? current, string key) =>
        current?.Key != key ? new(key, SortDirection.Ascending)
        : current.Value.Direction == SortDirection.Ascending ? new(key, SortDirection.Descending)
        : null;

    /// <summary>多列排序时点了一列的表头：没有这一列时加在最后（正序），正序再点是倒序，倒序再点去掉这一列。</summary>
    public static IReadOnlyList<ColumnSort> NextMultiple(IReadOnlyList<ColumnSort> current, string key)
    {
        var existing = current.FirstOrDefault(sort => sort.Key == key);
        if (existing.Key is null) return [.. current, new(key, SortDirection.Ascending)];
        return existing.Direction == SortDirection.Ascending
            ? [.. current.Select(sort => sort.Key == key ? sort with { Direction = SortDirection.Descending } : sort)]
            : [.. current.Where(sort => sort.Key != key)];
    }
}

/// <summary>一页结果（同 vue-tsx-pro 的 TablePage）。</summary>
/// <param name="Rows">这一页的行。</param>
/// <param name="Total">符合条件的总条数。</param>
public sealed record TablePage<TRow>(IReadOnlyList<TRow> Rows, long Total)
{
    /// <summary>没有结果。</summary>
    public static TablePage<TRow> Empty { get; } = new([], 0);
}

/// <summary>
/// 一次列表查询（同 vue-tsx-pro 的 TableQuery）：纯数据，不依赖任何 ORM。<see cref="ToParameters"/> 写成接口的参数，
/// 与 EasyQuerySharp 的 <c>TableRequest.Parse</c>（以及 vue 的 tableParams、Java 的 TableRequest.parse）一一对应。
/// </summary>
/// <typeparam name="TFilters">已应用的筛选条件（查询栏）：一个 record，公开属性按名字平铺成参数；没有筛选条件时用 <see cref="NoFilters"/>。</typeparam>
public sealed record TableQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilters>
{
    /// <summary>第几页，从 1 开始。</summary>
    public int Page { get; init; } = 1;

    /// <summary>每页条数，默认 10（同 Pagination）。</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>排序，前面的优先；空的是默认顺序。</summary>
    public IReadOnlyList<ColumnSort> Sorts { get; init; } = [];

    /// <summary>表头筛选：列 key → 选中的值（关键字筛选时是一个值）。</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ColumnFilters { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>已知的总条数：只翻页、换排序或条数时是上一次结果的总数，服务端可以不再数行数；条件变了、查询、刷新时为 null。</summary>
    public long? Total { get; init; }

    /// <summary>已应用的筛选条件。</summary>
    public TFilters Filters { get; init; } = default!;

    /// <summary>
    /// 写成接口的参数（同名参数可以出现几次）：page、pageSize；有排序时 sort（<c>-amount,name</c>）；已知总数时 total；
    /// 表头筛选里多选的列写成 <c>filter.列</c>（可重复），<paramref name="keywordColumns"/> 里的列写成 <c>search.列</c>（关键字）；
    /// 筛选条件的公开属性按名字（首字母小写）平铺：空值不写，集合写成重复的参数，区间（Start / End 或 Low / High）写成两个同名参数。
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> ToParameters(IEnumerable<string>? keywordColumns = null)
    {
        yield return new("page", Text(Page));
        yield return new("pageSize", Text(PageSize));
        if (ColumnSort.Format(Sorts) is { Length: > 0 } sort) yield return new("sort", sort);
        if (Total is { } total) yield return new("total", Text(total));
        var keyword = (keywordColumns ?? []).ToHashSet(StringComparer.Ordinal);
        foreach (var (key, values) in ColumnFilters)
        {
            var kept = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (kept.Count == 0) continue;
            if (keyword.Contains(key)) yield return new($"search.{key}", kept[0]);
            else foreach (var value in kept) yield return new($"filter.{key}", value);
        }
        foreach (var parameter in FilterParameters()) yield return parameter;
    }

    /// <summary>筛选条件平铺成的参数（<see cref="ToParameters"/> 的最后一部分）。</summary>
    internal IEnumerable<KeyValuePair<string, string>> FilterParameters()
    {
        if (Filters is null) yield break;
        foreach (var property in FilterProperties)
        {
            foreach (var value in FilterValues(property.GetValue(Filters))) yield return new(ParameterName(property.Name), value);
        }
    }

    /// <summary>筛选条件可能写出的参数名：同步到地址时，没有值的要从地址里去掉。</summary>
    internal static IEnumerable<string> FilterParameterNames => FilterProperties.Select(static property => ParameterName(property.Name));

    static IEnumerable<PropertyInfo> FilterProperties =>
        typeof(TFilters).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(static property => property.GetIndexParameters().Length == 0);

    /// <summary>写成查询串（不带开头的 ?），值按地址栏编码。</summary>
    public string ToQueryString(IEnumerable<string>? keywordColumns = null) =>
        string.Join("&", ToParameters(keywordColumns).Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    // 一个筛选条件写成参数值：空值不写；集合、区间是几个值；其余是线上的文字
    static IEnumerable<string> FilterValues(object? value)
    {
        switch (value)
        {
            case null:
                yield break;
            case string text:
                if (text.Length > 0) yield return text;
                yield break;
            case IEnumerable items:
                foreach (var item in items)
                    if (item is not null && Text(item) is { Length: > 0 } itemText) yield return itemText;
                yield break;
        }
        if (Range(value) is var (start, end))
        {
            if (start is null && end is null) yield break;
            yield return start is null ? "" : Text(start);
            yield return end is null ? "" : Text(end);
            yield break;
        }
        yield return Text(value);
    }

    // 区间：有 Start / End（日期区间）或 Low / High（NumberRange）两个属性
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "只读区间类型自己的公开属性；找不到时当作普通值")]
    static (object? Start, object? End)? Range(object value)
    {
        var type = value.GetType();
        foreach (var (from, to) in new[] { ("Start", "End"), ("Low", "High") })
        {
            if (type.GetProperty(from) is { } start && type.GetProperty(to) is { } end) return (start.GetValue(value), end.GetValue(value));
        }
        return null;
    }

    static string ParameterName(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>值的线上文字：不变区域，数字不用科学计数法，日期是 ISO 8601，布尔是 true / false，枚举是名字（首字母小写）。</summary>
    static string Text(object value) => value switch
    {
        bool flag => flag ? "true" : "false",
        Enum item => ParameterName(item.ToString()),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset offset => offset.ToString("O", CultureInfo.InvariantCulture),
        decimal number => number.ToString("0.############################", CultureInfo.InvariantCulture),
        double number => number.ToString("0.#################", CultureInfo.InvariantCulture),
        float number => number.ToString("0.#########", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}

/// <summary>没有筛选条件的列表（<c>TableQuery&lt;NoFilters&gt;</c>）。</summary>
public sealed record NoFilters
{
    /// <summary>唯一的实例。</summary>
    public static NoFilters Instance { get; } = new();
}
