using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 查询表格（同 vue-tsx-pro 的 QueryTable）：查询栏改草稿、查询时才交给 TableQueryState；重置回到一开始的条件；
/// 标题与工具栏在查询栏的第一排；列设置的列来自表格的列定义。
/// </summary>
public class QueryTableTests : BunitContext
{
    public QueryTableTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Order(string Id, string Supplier);

    sealed class Filters
    {
        public string? Keyword { get; set; }
    }

    static readonly Order[] All = [new("PO-1", "华辰"), new("PO-2", "启航"), new("PO-3", "华辰")];

    static Task<TablePage<Order>> Fetch(TableQuery<Filters> query, CancellationToken _)
    {
        var rows = All.Where(o => string.IsNullOrEmpty(query.Filters.Keyword) || o.Supplier.Contains(query.Filters.Keyword)).ToList();
        return Task.FromResult(new TablePage<Order>(rows, rows.Count));
    }

    [Fact]
    public async Task 查询栏改的是草稿_查询时才应用_重置回到一开始的条件()
    {
        var query = new TableQueryState<Order, Filters>(Fetch, new() { Filters = new Filters() });
        await query.RefreshAsync();
        Filters? draft = null;
        var cut = Render<QueryTable<Order, Filters>>(p => p
            .Add(x => x.Query, query)
            .Add(x => x.GetKey, o => o.Id)
            .Add(x => x.Title, "采购单")
            .Add(x => x.Filters, d => { draft = d; return b => b.AddMarkupContent(0, "<span>条件</span>"); })
            .Add(x => x.Columns, b =>
            {
                b.OpenComponent<PropertyColumn<Order, string>>(0);
                b.AddComponentParameter(1, nameof(PropertyColumn<Order, string>.Property), (System.Linq.Expressions.Expression<Func<Order, string>>)(o => o.Supplier));
                b.AddComponentParameter(2, nameof(PropertyColumn<Order, string>.Title), "供应商");
                b.CloseComponent();
            }));

        // 标题、工具栏在查询栏的第一排
        Assert.Equal("采购单", cut.Find("""form[role="search"] [data-part="title"]""").TextContent);
        Assert.NotNull(cut.Find("""form[role="search"] [data-scope="query-table"][data-part="tools"]"""));
        Assert.NotNull(draft);
        Assert.NotSame(query.Filters, draft);

        draft!.Keyword = "华辰";
        // 改草稿不影响已应用的条件
        Assert.Null(query.Filters.Keyword);
        await cut.InvokeAsync(() => cut.Find("""form[role="search"]""").Submit());
        Assert.Equal("华辰", query.Filters.Keyword);
        Assert.Equal(2, query.Total);
        Assert.NotSame(query.Filters, draft);

        await cut.InvokeAsync(() => cut.Find("""[data-scope="filter-bar"][data-part="reset"]""").Click());
        Assert.Null(query.Filters.Keyword);
        Assert.Equal(3, query.Total);
    }
}
