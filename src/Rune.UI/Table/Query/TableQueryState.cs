using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>取一页数据：查询条件 → 一页结果；被后来的请求顶掉时 <paramref name="cancellation"/> 取消。</summary>
public delegate Task<TablePage<TRow>> TableFetcher<TRow, TFilters>(TableQuery<TFilters> query, CancellationToken cancellation);

/// <summary><see cref="TableQueryState{TRow, TFilters}"/> 的选项。</summary>
public sealed record TableQueryOptions<TFilters>
{
    /// <summary>一开始已应用的筛选条件。</summary>
    public TFilters Filters { get; init; } = default!;

    /// <summary>每页条数，默认 10。</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>一开始的排序，前面的优先。</summary>
    public IReadOnlyList<ColumnSort> Sorts { get; init; } = [];

    /// <summary>关键字筛选的列（表头筛选写成 <c>search.列</c>）：同 <see cref="TableQuery{TFilters}.ToParameters"/>。</summary>
    public IReadOnlyList<string> KeywordColumns { get; init; } = [];

    /// <summary>
    /// 给了时页码（page）、条数（pageSize）、排序（sort）、筛选条件（写法同 <see cref="TableQuery{TFilters}.ToParameters"/>）同步到地址的查询参数：
    /// 刷新、分享链接、后退回来还是这些条件。页码、条数、排序由这里读回；筛选条件由页面用 <c>[SupplyParameterFromQuery]</c> 读出来，作为 <see cref="Filters"/> 交给这里。
    /// </summary>
    public NavigationManager? Navigation { get; init; }
}

/// <summary>
/// 服务端分页的列表状态（同 vue-tsx-pro 的 useTableQuery，参考 ProTable 的 request）：页码、每页条数、排序、表头筛选、已应用的筛选条件、
/// 总数、加载与出错一起管。不是组件：页面持有它，DataTable、分页绑它的属性。
/// <code>
/// orders = new(FetchAsync, new() { Filters = new OrderFilters(), PageSize = 20, Navigation = Navigation }, () => InvokeAsync(StateHasChanged));
/// protected override Task OnInitializedAsync() => orders.RefreshAsync();
/// &lt;DataTable Items="orders.Rows" Loading="orders.Loading" @bind-Sorts="orders.Sorts" … /&gt;
/// &lt;Pagination @bind-Page="orders.Page" Total="orders.Total" /&gt;
/// </code>
/// 改条数、排序、表头筛选回到第一页；<see cref="SearchAsync"/> 应用新的筛选条件（条件没变也重新请求、重新数总数）。只采用最后一次的结果，
/// 被顶掉的请求取消；删掉数据后当前页空了退到最后一页。只翻页、换排序或条数时交给取数的 Total 是上一次的总数，服务端可以不再数行数。
/// </summary>
/// <typeparam name="TRow">一行的类型。</typeparam>
/// <typeparam name="TFilters">筛选条件（查询栏）；没有时用 <see cref="NoFilters"/>。</typeparam>
public sealed class TableQueryState<TRow, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilters> : IDisposable
{
    readonly TableFetcher<TRow, TFilters> fetch;
    readonly TableQueryOptions<TFilters> options;
    readonly Func<Task> changed;
    CancellationTokenSource? current;
    // 上一次结果的总数与它所属的条件（筛选条件、表头筛选）：条件没变时交给取数，服务端可以不再数行数
    (string Scope, long Total)? known;
    int page = 1;
    int pageSize;
    IReadOnlyList<ColumnSort> sorts;
    IReadOnlyDictionary<string, IReadOnlyList<string>> columnFilters = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>用取数的函数、选项与“状态变了”的回调（<c>() => InvokeAsync(StateHasChanged)</c>）创建；不会自己请求，第一次由 <see cref="RefreshAsync"/> 开始。</summary>
    public TableQueryState(TableFetcher<TRow, TFilters> fetch, TableQueryOptions<TFilters>? options = null, Func<Task>? changed = null)
    {
        this.fetch = fetch;
        this.options = options ?? new();
        this.changed = changed ?? (static () => Task.CompletedTask);
        pageSize = this.options.PageSize;
        sorts = this.options.Sorts;
        Filters = this.options.Filters;
        ReadUrl();
    }

    /// <summary>第几页（从 1 开始）：改了立即请求。</summary>
    public int Page
    {
        get => page;
        set
        {
            if (value == page) return;
            page = Math.Max(1, value);
            _ = RequestAsync();
        }
    }

    /// <summary>每页条数：改了回到第一页。</summary>
    public int PageSize
    {
        get => pageSize;
        set
        {
            if (value == pageSize) return;
            pageSize = Math.Max(1, value);
            Restart();
        }
    }

    /// <summary>排序的第一列，给单列排序的表格绑定：设置时换掉全部排序，null 是默认顺序；改了回到第一页。</summary>
    public ColumnSort? Sort
    {
        get => sorts.Count > 0 ? sorts[0] : null;
        set => Sorts = value is { } sort ? [sort] : [];
    }

    /// <summary>全部排序，前面的优先，给多列排序的表格绑定：改了回到第一页。</summary>
    public IReadOnlyList<ColumnSort> Sorts
    {
        get => sorts;
        set
        {
            if (sorts.SequenceEqual(value)) return;
            sorts = value;
            Restart();
        }
    }

    /// <summary>表头筛选（列 key → 选中的值），给表格的表头筛选绑定：改了回到第一页。</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ColumnFilters
    {
        get => columnFilters;
        set
        {
            columnFilters = value;
            Restart();
        }
    }

    /// <summary>已应用的筛选条件。</summary>
    public TFilters Filters { get; private set; }

    /// <summary>最后一次成功的结果；第一次结果回来之前为 null。</summary>
    public TablePage<TRow>? Data { get; private set; }

    /// <summary>当前页的行；没有结果时为空。</summary>
    public IReadOnlyList<TRow> Rows => Data?.Rows ?? [];

    /// <summary>总条数；第一次结果回来之前为 0。</summary>
    public long Total => Data?.Total ?? 0;

    /// <summary>正在请求。</summary>
    public bool Loading { get; private set; }

    /// <summary>最后一次失败的原因；成功后清掉。</summary>
    public Exception? Error { get; private set; }

    /// <summary>当前的查询条件（交给取数的就是它，Total 另按已知的总数给）。</summary>
    public TableQuery<TFilters> Query => new()
    {
        Page = page,
        PageSize = pageSize,
        Sorts = sorts,
        ColumnFilters = columnFilters,
        Filters = Filters,
    };

    /// <summary>应用新的筛选条件（查询栏的“查询”）：回到第一页，条件没变也重新请求并重新数总数。</summary>
    public Task SearchAsync(TFilters filters)
    {
        Filters = filters;
        known = null;
        page = 1;
        return RequestAsync();
    }

    /// <summary>按当前条件重新请求（新建、删除之后，或第一次加载），重新数总数。</summary>
    public Task RefreshAsync()
    {
        known = null;
        return RequestAsync();
    }

    /// <summary>取消还在进行的请求。</summary>
    public void Dispose() => current?.Cancel();

    void Restart()
    {
        page = 1;
        _ = RequestAsync();
    }

    // 筛选条件与表头筛选：它们没变时总数不变
    string Scope(TableQuery<TFilters> query) =>
        string.Join("&", query.ToParameters(options.KeywordColumns).Where(static p => p.Key is not ("page" or "pageSize" or "sort" or "total")).Select(static p => $"{p.Key}={p.Value}"));

    async Task RequestAsync()
    {
        current?.Cancel();
        var run = current = new CancellationTokenSource();
        var query = Query;
        var scope = Scope(query);
        query = query with { Total = known is { } k && k.Scope == scope ? k.Total : null };
        WriteUrl();
        Loading = true;
        await changed();
        try
        {
            var result = await fetch(query, run.Token);
            if (run != current) return;
            Data = result;
            Error = null;
            known = (scope, result.Total);
            // 删掉数据后当前页空了：退到最后一页
            var last = (int)Math.Max(1, (result.Total + query.PageSize - 1) / query.PageSize);
            if (result.Rows.Count == 0 && query.Page > last)
            {
                page = last;
                _ = RequestAsync();
                return;
            }
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (run == current)
        {
            // 取数失败只记在 Error 上，由页面提示（列表加载失败不是整页的错误）
            Error = exception;
        }
        Loading = false;
        await changed();
    }

    // 地址栏：page、pageSize、sort 与默认值相同时不写；筛选条件的空值不写
    void ReadUrl()
    {
        if (options.Navigation is not { } navigation) return;
        var parameters = new Uri(navigation.Uri).Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(static pair => pair.Split('=', 2))
            .GroupBy(static pair => Uri.UnescapeDataString(pair[0].Replace('+', ' ')))
            .ToDictionary(static group => group.Key, static group => Uri.UnescapeDataString((group.First().ElementAtOrDefault(1) ?? "").Replace('+', ' ')));
        if (parameters.TryGetValue("page", out var p) && int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 1) page = number;
        if (parameters.TryGetValue("pageSize", out var s) && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var size) && size >= 1) pageSize = size;
        if (parameters.TryGetValue("sort", out var sort)) sorts = ColumnSort.Parse(sort);
    }

    void WriteUrl()
    {
        if (options.Navigation is not { } navigation) return;
        var parameters = new Dictionary<string, object?>
        {
            ["page"] = page == 1 ? null : page,
            ["pageSize"] = pageSize == options.PageSize ? null : pageSize,
            ["sort"] = sorts.SequenceEqual(options.Sorts) ? null : ColumnSort.Format(sorts),
        };
        foreach (var name in TableQuery<TFilters>.FilterParameterNames) parameters[name] = null;
        foreach (var group in Query.FilterParameters().GroupBy(static p => p.Key)) parameters[group.Key] = group.Select(static p => p.Value).ToArray();
        var target = navigation.GetUriWithQueryParameters(parameters);
        if (target != navigation.Uri) navigation.NavigateTo(target, replace: true);
    }
}
