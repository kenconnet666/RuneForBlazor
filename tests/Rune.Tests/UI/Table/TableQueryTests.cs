using Rune.UI;

namespace Rune.Tests.UI;

// 线上的格式与 EasyQuerySharp 的 TableRequest.Parse、vue-tsx-pro 的 tableParams 一一对应
public class TableQueryTests
{
    enum Status { Draft, Paid }

    sealed record DateRange(DateOnly? Start, DateOnly? End);

    sealed record OrderFilters(
        string? Keyword = null,
        IReadOnlyList<Status>? Statuses = null,
        DateRange? Created = null,
        NumberRange<decimal?>? Amount = null,
        bool? Urgent = null,
        decimal? MinTotal = null);

    static string Query<T>(TableQuery<T> query, params string[] keyword) =>
        string.Join("&", query.ToParameters(keyword).Select(p => $"{p.Key}={p.Value}"));

    [Fact]
    public void 排序写成减号表示倒序_解析回来一样()
    {
        IReadOnlyList<ColumnSort> sorts = [new("amount", SortDirection.Descending), new("name", SortDirection.Ascending)];

        Assert.Equal("-amount,name", ColumnSort.Format(sorts));
        Assert.Equal(sorts, ColumnSort.Parse("-amount, name,"));
        Assert.Empty(ColumnSort.Parse(""));
    }

    [Fact]
    public void 点表头_单列正序倒序再回到默认_多列追加再去掉()
    {
        var asc = ColumnSort.Next(null, "amount");
        Assert.Equal(new ColumnSort("amount", SortDirection.Ascending), asc);
        Assert.Equal(new ColumnSort("amount", SortDirection.Descending), ColumnSort.Next(asc, "amount"));
        Assert.Null(ColumnSort.Next(new ColumnSort("amount", SortDirection.Descending), "amount"));
        Assert.Equal(new ColumnSort("name", SortDirection.Ascending), ColumnSort.Next(asc, "name"));

        IReadOnlyList<ColumnSort> sorts = [new("amount", SortDirection.Ascending)];
        sorts = ColumnSort.NextMultiple(sorts, "name");
        Assert.Equal("amount,name", ColumnSort.Format(sorts));
        sorts = ColumnSort.NextMultiple(sorts, "amount");
        Assert.Equal("-amount,name", ColumnSort.Format(sorts));
        Assert.Equal("name", ColumnSort.Format(ColumnSort.NextMultiple(sorts, "amount")));
    }

    [Fact]
    public void 分页排序总数与表头筛选_关键字列写成search()
    {
        var query = new TableQuery<NoFilters>
        {
            Page = 2,
            PageSize = 20,
            Sorts = [new("amount", SortDirection.Descending)],
            Total = 368,
            ColumnFilters = new Dictionary<string, IReadOnlyList<string>> { ["status"] = ["draft", "paid", " "], ["supplier"] = ["启航"], ["empty"] = [] },
            Filters = NoFilters.Instance,
        };

        Assert.Equal("page=2&pageSize=20&sort=-amount&total=368&filter.status=draft&filter.status=paid&search.supplier=启航", Query(query, "supplier"));
    }

    [Fact]
    public void 筛选条件按属性平铺_空值不写_集合重复_区间两个同名参数_值按线上的文字()
    {
        var query = new TableQuery<OrderFilters>
        {
            Filters = new(
                Keyword: "泵",
                Statuses: [Status.Draft, Status.Paid],
                Created: new(new DateOnly(2026, 1, 1), null),
                Amount: new NumberRange<decimal?>(1000.50m, null),
                Urgent: true,
                MinTotal: 0.0000001m),
        };

        Assert.Equal(
            "page=1&pageSize=10&keyword=泵&statuses=draft&statuses=paid&created=2026-01-01&created=&amount=1000.5&amount=&urgent=true&minTotal=0.0000001",
            Query(query));
        Assert.Equal("page=1&pageSize=10", Query(new TableQuery<OrderFilters> { Filters = new() }));
    }

    [Fact]
    public void 查询串按地址栏编码()
    {
        var query = new TableQuery<OrderFilters> { Filters = new(Keyword: "a&b c") };

        Assert.Equal("page=1&pageSize=10&keyword=a%26b%20c", query.ToQueryString());
    }
}
