namespace Rune.UI;

/// <summary>远程选项的加载函数：搜索词 → 选项；被后来的请求顶掉、弹层关闭、组件销毁时 <paramref name="cancellation"/> 取消。</summary>
public delegate Task<IEnumerable<TItem>> OptionsLoader<TItem>(string query, CancellationToken cancellation);

/// <summary>
/// 分页的远程选项：搜索词与第几页（从 1 起）→ 这一页（<c>Next</c> 是下一页的页码，<c>HasNext</c> 为 false 时取完了）。
/// 滚到列表末尾时取下一页，按选项的值去重（同 vue-tsx-pro 的 load 返回 <c>{ items, next }</c>）。
/// </summary>
public delegate Task<InfinitePage<TItem, int>> PagedOptionsLoader<TItem>(string query, int page, CancellationToken cancellation);

/// <summary>
/// 远程选项的状态（同 vue-tsx-pro 的 remote-items）：Select、Combobox、Mentions、TreeSelect 的 Load 用它，Select、Combobox、Mentions 的 LoadPage 分页。
/// 弹层打开时按当前搜索词立即请求，之后搜索词变了停下 300 ms 再请求；只采用最后一次的结果，被顶掉的请求取消；弹层关上时取消还在进行的。
/// 第一次结果回来之前 <see cref="Data"/> 是 null：调用方用本地的 Items（编辑页把已选的那一项放进去，触发框一开始就有文字）。
/// </summary>
/// <param name="changed">状态变了（开始请求、结果回来、失败）：调用方重新渲染（<c>() => InvokeAsync(StateHasChanged)</c>）。</param>
sealed class RemoteItems<TItem>(Func<Task> changed) : IDisposable
{
    /// <summary>搜索词停下多久再请求。</summary>
    static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    CancellationTokenSource? current;
    // 分页：这一串页的加载函数、搜索词、下一页与见过的值（去重）
    PagedOptionsLoader<TItem>? paged;
    Func<TItem, string>? valueOf;
    string pagedQuery = "";
    int nextPage;
    readonly HashSet<string> seen = [];

    /// <summary>最后一次成功的结果（分页时是取到的全部页）；还没有时为 null。</summary>
    public IReadOnlyList<TItem>? Data { get; private set; }

    /// <summary>正在请求第一页（或不分页的那一次）。</summary>
    public bool Loading { get; private set; }

    /// <summary>正在取下一页：已有的选项留着，骨架接在末尾。</summary>
    public bool LoadingMore { get; private set; }

    /// <summary>分页时还有下一页。</summary>
    public bool HasMore { get; private set; }

    /// <summary>最后一次请求失败了。</summary>
    public bool Failed { get; private set; }

    /// <summary>请求一次；<paramref name="debounce"/> 时先等搜索词停下（输入中），否则立即（刚打开）。</summary>
    public Task RequestAsync(OptionsLoader<TItem> load, string query, bool debounce) =>
        RunAsync(debounce, async token => Data = [.. await load(query, token)]);

    /// <summary>分页地请求第一页；之后 <see cref="LoadMoreAsync"/> 接着取。<paramref name="value"/> 是选项的值（去重用）。</summary>
    public Task RequestPageAsync(PagedOptionsLoader<TItem> load, Func<TItem, string> value, string query, bool debounce) =>
        RunAsync(debounce, async token =>
        {
            var page = await load(query, 1, token);
            token.ThrowIfCancellationRequested();
            (paged, valueOf, pagedQuery) = (load, value, query);
            seen.Clear();
            Data = [.. Unseen(page.Items)];
            (nextPage, HasMore) = (page.Next, page.HasNext);
        });

    /// <summary>取下一页（滚到列表末尾）：正在请求、没有下一页、没分页时什么也不做。失败时停下，再滚动时接着取。</summary>
    public async Task LoadMoreAsync()
    {
        if (paged is not { } load || Loading || LoadingMore || !HasMore) return;
        var run = current = new CancellationTokenSource();
        LoadingMore = true;
        Failed = false;
        await changed();
        try
        {
            var page = await load(pagedQuery, nextPage, run.Token);
            run.Token.ThrowIfCancellationRequested();
            Data = [.. Data ?? [], .. Unseen(page.Items)];
            (nextPage, HasMore) = (page.Next, page.HasNext);
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            Failed = true;
        }
        if (current != run) return;
        LoadingMore = false;
        await changed();
    }

    async Task RunAsync(bool debounce, Func<CancellationToken, Task> fetch)
    {
        current?.Cancel();
        var run = current = new CancellationTokenSource();
        try
        {
            if (debounce) await Task.Delay(Debounce, run.Token);
            Loading = true;
            LoadingMore = false;
            await changed();
            await fetch(run.Token);
            run.Token.ThrowIfCancellationRequested();
            Failed = false;
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            // 失败只在列表里提示“加载失败，请重试”：选项加载不是页面级错误，不往上抛
            Failed = true;
        }
        if (current != run) return;
        Loading = false;
        await changed();
    }

    IEnumerable<TItem> Unseen(IReadOnlyList<TItem> items) => valueOf is not { } value ? items : items.Where(item => seen.Add(value(item)));

    /// <summary>取消还在进行的请求（弹层关上）。</summary>
    public void Cancel()
    {
        current?.Cancel();
        current = null;
        Loading = false;
        LoadingMore = false;
    }

    /// <inheritdoc/>
    public void Dispose() => Cancel();
}
