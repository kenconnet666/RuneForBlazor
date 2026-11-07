namespace Rune.UI;

/// <summary>一页的结果：<paramref name="Items"/> 接在已有的后面；<paramref name="Next"/> 是取下一页的游标（页码、偏移、服务端的 cursor），<paramref name="HasNext"/> 为 false 时取完了。</summary>
/// <param name="Items">这一页的项。</param>
/// <param name="Next">下一页的游标。</param>
/// <param name="HasNext">还有下一页。</param>
public readonly record struct InfinitePage<T, TCursor>(IReadOnlyList<T> Items, TCursor? Next, bool HasNext);

/// <summary>取一页：<c>cursor</c> 在第一页时是 default；被后来的请求顶掉、刷新、组件销毁时 <c>cancellation</c> 取消。</summary>
public delegate Task<InfinitePage<T, TCursor>> InfinitePageLoader<T, TCursor>(TCursor? cursor, CancellationToken cancellation);

/// <summary><see cref="InfiniteList{T, TCursor}"/> 的选项。</summary>
public sealed class InfiniteListOptions<T>
{
    /// <summary>每项的 key：同一个 key 只留第一次出现的那项（翻页期间有新数据插进来时，下一页开头会和上一页结尾重复）。</summary>
    public Func<T, object>? Key { get; init; }

    /// <summary>可见范围离末尾不到几项时取下一页，默认 5。</summary>
    public int Threshold { get; init; } = 5;

    /// <summary>参数变了（<see cref="InfiniteList{T, TCursor}.ParamsChangedAsync"/>）以后等多久再请求，默认不等。</summary>
    public TimeSpan Debounce { get; init; }

    /// <summary>一创建就取第一页，默认开；关掉时等调用 RefreshAsync。</summary>
    public bool Immediate { get; init; } = true;

    /// <summary>自动请求的开关（弹层打开时才请求这类）：为 false 时参数变化、滚动都不请求，RefreshAsync、LoadMoreAsync 照常。</summary>
    public Func<bool>? Enabled { get; init; }

    /// <summary>一页失败后调用（取消的不算）。</summary>
    public Action<Exception>? OnError { get; init; }
}

/// <summary>滚动加载的列表交给 <see cref="InfiniteListStatus"/> 的状态（与项的类型无关）。</summary>
public interface IInfiniteList
{
    /// <summary>已经取到几项。</summary>
    int Count { get; }

    /// <summary>正在取（第一页或下一页）。</summary>
    bool Loading { get; }

    /// <summary>最后一页失败的原因；再取时清空。</summary>
    Exception? Error { get; }

    /// <summary>第一页回来过；之前 Count 是 0 不代表没有数据。</summary>
    bool Loaded { get; }

    /// <summary>还有下一页。</summary>
    bool HasMore { get; }

    /// <summary>取下一页（还没取到第一页时取第一页）；正在取、没有下一页时什么也不做。失败后用它重试。</summary>
    Task LoadMoreAsync();
}

/// <summary>
/// 滚动加载的列表（同 vue-tsx-pro 的 useInfiniteList，参考 TanStack Query 的 useInfiniteQuery）：一页一页取、接在已有的后面，
/// 按 Key 去掉重复的项，记住还有没有下一页。
/// <code>
/// orders = new InfiniteList&lt;Order, int&gt;(async (page, ct) =&gt;
/// {
///     var result = await api.OrdersAsync(keyword, page + 1, ct);
///     return new(result.Rows, page + 1, (page + 1) * 20 &lt; result.Total);
/// }, new() { Key = order =&gt; order.Id, Debounce = TimeSpan.FromMilliseconds(300) });
/// &lt;VirtualList Items="orders.Items" Loading="orders.Loading" OnRangeChange="orders.OnRangeChange" … /&gt;
/// &lt;InfiniteListStatus List="orders" /&gt;      列表下方：失败时重试、取完了“没有更多了”
/// </code>
/// 状态是信号：RuneComponent 读了就会在它变化时重新渲染。搜索词、筛选条件变了时调用 <see cref="ParamsChangedAsync"/>（防抖后从第一页重来）。
/// 从第一页重来时先清空、取消进行中的，只采用最后一次的结果。失败时停在那里，再滚动或调用 LoadMoreAsync 时接着取。
/// </summary>
public sealed class InfiniteList<T, TCursor> : IInfiniteList, IDisposable
{
    readonly InfinitePageLoader<T, TCursor> fetchPage;
    readonly InfiniteListOptions<T> options;
    readonly State<IReadOnlyList<T>> items = new([]);
    readonly State<bool> loading = new(false);
    readonly State<Exception?> error = new(null);
    readonly State<bool> loaded = new(false);
    readonly State<bool> hasMore = new(true);
    TCursor? cursor;
    HashSet<object> seen = [];
    CancellationTokenSource? current;
    CancellationTokenSource? waiting;
    // 每次请求一个编号：结果回来时不是最后一次的就丢掉（取一页的函数没理会取消时也不会接错）
    int latest;

    /// <summary>创建；<see cref="InfiniteListOptions{T}.Immediate"/>（默认）时马上取第一页。</summary>
    public InfiniteList(InfinitePageLoader<T, TCursor> fetchPage, InfiniteListOptions<T>? options = null)
    {
        this.fetchPage = fetchPage;
        this.options = options ?? new();
        if (this.options.Immediate && Enabled) _ = RunAsync(first: true);
    }

    /// <summary>已经取到的全部项（去重后）。</summary>
    public IReadOnlyList<T> Items => items.Value;

    /// <inheritdoc/>
    public int Count => items.Value.Count;

    /// <inheritdoc/>
    public bool Loading => loading.Value;

    /// <inheritdoc/>
    public Exception? Error => error.Value;

    /// <inheritdoc/>
    public bool Loaded => loaded.Value;

    /// <inheritdoc/>
    public bool HasMore => hasMore.Value;

    bool Enabled => options.Enabled?.Invoke() != false;

    /// <inheritdoc/>
    public Task LoadMoreAsync() =>
        loading.Peek() || (loaded.Peek() && !hasMore.Peek()) ? Task.CompletedTask : RunAsync(first: !loaded.Peek());

    /// <summary>从第一页重来：清空、取消进行中的。</summary>
    public Task RefreshAsync() => RunAsync(first: true);

    /// <summary>参数（搜索词、筛选条件）变了：等 <see cref="InfiniteListOptions{T}.Debounce"/> 后从第一页重来；期间又变了只算最后一次。</summary>
    public async Task ParamsChangedAsync()
    {
        waiting?.Cancel();
        if (!Enabled) return;
        if (options.Debounce > TimeSpan.Zero)
        {
            var wait = waiting = new CancellationTokenSource();
            try
            {
                await Task.Delay(options.Debounce, wait.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
        await RunAsync(first: true);
    }

    /// <summary>交给 VirtualList、DataTable 的 OnRangeChange：可见范围接近末尾时取下一页。</summary>
    public Task OnRangeChange(VisibleRange range) =>
        Enabled && range.EndIndex >= items.Peek().Count - options.Threshold ? LoadMoreAsync() : Task.CompletedTask;

    /// <summary>取消进行中的请求与等着的防抖。</summary>
    public void Cancel()
    {
        waiting?.Cancel();
        latest++;
        current?.Cancel();
        current = null;
        loading.Value = false;
    }

    /// <inheritdoc/>
    public void Dispose() => Cancel();

    async Task RunAsync(bool first)
    {
        Cancel();
        var id = latest;
        var run = current = new CancellationTokenSource();
        if (first)
        {
            items.Value = [];
            seen = [];
            cursor = default;
            loaded.Value = false;
            hasMore.Value = true;
        }
        loading.Value = true;
        error.Value = null;
        try
        {
            var page = await fetchPage(cursor, run.Token);
            if (id != latest) return;
            items.Value = [.. items.Peek(), .. Unseen(page.Items)];
            cursor = page.Next;
            hasMore.Value = page.HasNext;
            loaded.Value = true;
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested)
        {
        }
        catch (Exception reason)
        {
            // 失败停在这一页（InfiniteListStatus 显示重试），不往上抛
            if (id != latest) return;
            error.Value = reason;
            options.OnError?.Invoke(reason);
        }
        finally
        {
            if (id == latest)
            {
                loading.Value = false;
                current = null;
            }
        }
    }

    IEnumerable<T> Unseen(IReadOnlyList<T> page) => options.Key is not { } key ? page : page.Where(item => seen.Add(key(item)));
}
