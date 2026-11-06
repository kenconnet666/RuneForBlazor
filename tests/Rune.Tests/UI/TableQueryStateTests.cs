using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 状态的规则：改条数、排序回到第一页；只采用最后一次的结果；条件没变时把已知的总数交给取数；当前页空了退到最后一页；页码等同步到地址
public class TableQueryStateTests : BunitContext
{
    sealed record Filters(string? Keyword = null);

    // 100 行，按查询切一页；记下每次交给取数的查询
    sealed class Server
    {
        public List<TableQuery<Filters>> Queries { get; } = [];
        public int Rows { get; set; } = 100;

        public Task<TablePage<int>> FetchAsync(TableQuery<Filters> query, CancellationToken cancellation)
        {
            Queries.Add(query);
            var rows = Enumerable.Range(1, Rows).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
            return Task.FromResult(new TablePage<int>(rows, Rows));
        }
    }

    [Fact]
    public async Task 翻页带上已知总数_改条数排序回到第一页_查询与刷新重新数总数()
    {
        var server = new Server();
        var state = new TableQueryState<int, Filters>(server.FetchAsync, new() { Filters = new() });

        await state.RefreshAsync();
        Assert.Equal((100L, 10), (state.Total, state.Rows.Count));
        Assert.Null(server.Queries[^1].Total);

        state.Page = 3;
        Assert.Equal((3, 100L), (server.Queries[^1].Page, server.Queries[^1].Total));
        Assert.Equal(21, state.Rows[0]);

        state.Sort = new ColumnSort("amount", SortDirection.Descending);
        Assert.Equal((1, "-amount", 100L), (state.Page, ColumnSort.Format(server.Queries[^1].Sorts), server.Queries[^1].Total));

        state.Page = 2;
        state.PageSize = 20;
        Assert.Equal((1, 20), (server.Queries[^1].Page, server.Queries[^1].PageSize));

        await state.SearchAsync(new("泵"));
        Assert.Equal(("泵", 1, (long?)null), (server.Queries[^1].Filters.Keyword, server.Queries[^1].Page, server.Queries[^1].Total));

        state.ColumnFilters = new Dictionary<string, IReadOnlyList<string>> { ["status"] = ["paid"] };
        Assert.Null(server.Queries[^1].Total);
    }

    [Fact]
    public async Task 只采用最后一次的结果_被顶掉的请求取消()
    {
        var slow = new TaskCompletionSource<TablePage<int>>();
        CancellationToken first = default;
        var calls = 0;
        var state = new TableQueryState<int, Filters>((query, cancellation) =>
        {
            if (++calls == 1)
            {
                first = cancellation;
                return slow.Task;
            }
            return Task.FromResult(new TablePage<int>([query.Page], 50));
        }, new() { Filters = new() });

        var refresh = state.RefreshAsync();
        Assert.True(state.Loading);
        state.Page = 2;
        Assert.True(first.IsCancellationRequested);
        slow.SetResult(new TablePage<int>([999], 50));
        await refresh;

        Assert.Equal([2], state.Rows);
        Assert.False(state.Loading);
    }

    [Fact]
    public async Task 删掉数据后当前页空了退到最后一页_失败记在Error上()
    {
        var server = new Server();
        var state = new TableQueryState<int, Filters>(server.FetchAsync, new() { Filters = new() });
        state.Page = 10;
        server.Rows = 85;

        await state.RefreshAsync();
        Assert.Equal((9, 81), (state.Page, state.Rows[0]));

        var failing = new TableQueryState<int, Filters>((_, _) => throw new InvalidOperationException("超时"), new() { Filters = new() });
        await failing.RefreshAsync();
        Assert.Equal("超时", failing.Error?.Message);
        Assert.False(failing.Loading);
    }

    [Fact]
    public async Task 页码条数排序从地址读出并写回_筛选条件写进地址_默认值空值不写()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("orders?page=3&sort=-amount&tab=x");
        var server = new Server();
        var state = new TableQueryState<int, Filters>(server.FetchAsync, new() { Filters = new(), Navigation = navigation });

        Assert.Equal((3, 10, "-amount"), (state.Page, state.PageSize, ColumnSort.Format(state.Sorts)));

        await state.RefreshAsync();
        state.PageSize = 20;
        Assert.EndsWith("orders?sort=-amount&tab=x&pageSize=20", navigation.Uri);

        state.Sort = null;
        Assert.EndsWith("orders?tab=x&pageSize=20", navigation.Uri);

        await state.SearchAsync(new("泵 A"));
        Assert.EndsWith("orders?tab=x&pageSize=20&keyword=%E6%B3%B5%20A", navigation.Uri);
        await state.SearchAsync(new());
        Assert.EndsWith("orders?tab=x&pageSize=20", navigation.Uri);
    }
}
