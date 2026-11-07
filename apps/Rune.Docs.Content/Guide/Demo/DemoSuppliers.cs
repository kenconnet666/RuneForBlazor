using Rune.UI;

namespace Rune.Docs.Guide.Demo;

/// <summary>供应商。</summary>
public sealed record Supplier(string Id, string Name, string City, string Category);

/// <summary>
/// Data 指南演示用的“服务端”：内存里的供应商表，每次请求随机等 300–800 ms，筛选、排序、分页都在这里做；
/// <see cref="FailNext"/> 让下一次请求失败。应用里这是 HttpClient 调的接口。
/// </summary>
public static class DemoSuppliers
{
    static readonly string[] Cities = ["上海", "苏州", "西安", "深圳", "成都", "武汉"];
    static readonly string[] Categories = ["机加工", "注塑", "钣金", "电子", "包装"];
    static readonly string[] Names = ["华辰精密", "启航塑胶", "远山钣金", "青岩研发", "北辰电子", "星河紧固件", "林海包装", "云帆模具", "东篱五金", "南桥线缆", "致远科技", "恒通物流"];

    static readonly Supplier[] All =
    [
        .. Enumerable.Range(0, 36).Select(i =>
            new Supplier($"S{i + 1:000}", $"{Names[i % Names.Length]}{(i >= Names.Length ? $" {i / Names.Length + 1} 厂" : "")}", Cities[i % Cities.Length], Categories[i % Categories.Length])),
    ];

    /// <summary>下一次请求失败（演示出错时的样子）。</summary>
    public static bool FailNext { get; set; }

    /// <summary>按关键字（名称或城市）查一页。</summary>
    public static async Task<TablePage<Supplier>> SearchAsync(string keyword, int page, int pageSize, ColumnSort? sort, CancellationToken cancellation)
    {
        await Task.Delay(Random.Shared.Next(300, 800), cancellation);
        if (FailNext)
        {
            FailNext = false;
            throw new InvalidOperationException("供应商服务暂时不可用，请稍后重试");
        }
        IEnumerable<Supplier> rows = All.Where(s => keyword.Length == 0 || s.Name.Contains(keyword, StringComparison.Ordinal) || s.City.Contains(keyword, StringComparison.Ordinal));
        if (sort is { } by)
        {
            Func<Supplier, string> key = by.Key switch { "name" => s => s.Name, "city" => s => s.City, _ => s => s.Id };
            rows = by.Direction == SortDirection.Descending ? rows.OrderByDescending(key, StringComparer.Ordinal) : rows.OrderBy(key, StringComparer.Ordinal);
        }
        var list = rows.ToList();
        return new([.. list.Skip((page - 1) * pageSize).Take(pageSize)], list.Count);
    }
}
