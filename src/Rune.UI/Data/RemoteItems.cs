namespace Rune.UI;

/// <summary>远程选项的加载函数：搜索词 → 选项；被后来的请求顶掉、弹层关闭、组件销毁时 <paramref name="cancellation"/> 取消。</summary>
public delegate Task<IEnumerable<TItem>> OptionsLoader<TItem>(string query, CancellationToken cancellation);

/// <summary>
/// 远程选项的状态（同 vue-tsx-pro 的 remote-items）：Select、Combobox、TreeSelect 的 Load 用它。
/// 弹层打开时按当前搜索词立即请求，之后搜索词变了停下 300 ms 再请求；只采用最后一次的结果，被顶掉的请求取消；弹层关上时取消还在进行的。
/// 第一次结果回来之前 <see cref="Data"/> 是 null：调用方用本地的 Items（编辑页把已选的那一项放进去，触发框一开始就有文字）。
/// </summary>
/// <param name="changed">状态变了（开始请求、结果回来、失败）：调用方重新渲染（<c>() => InvokeAsync(StateHasChanged)</c>）。</param>
sealed class RemoteItems<TItem>(Func<Task> changed) : IDisposable
{
    /// <summary>搜索词停下多久再请求。</summary>
    static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    CancellationTokenSource? current;

    /// <summary>最后一次成功的结果；还没有时为 null。</summary>
    public IReadOnlyList<TItem>? Data { get; private set; }

    /// <summary>正在请求。</summary>
    public bool Loading { get; private set; }

    /// <summary>最后一次请求失败了。</summary>
    public bool Failed { get; private set; }

    /// <summary>请求一次；<paramref name="debounce"/> 时先等搜索词停下（输入中），否则立即（刚打开）。</summary>
    public async Task RequestAsync(OptionsLoader<TItem> load, string query, bool debounce)
    {
        current?.Cancel();
        var run = current = new CancellationTokenSource();
        try
        {
            if (debounce) await Task.Delay(Debounce, run.Token);
            Loading = true;
            await changed();
            var result = await load(query, run.Token);
            run.Token.ThrowIfCancellationRequested();
            Data = [.. result];
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

    /// <summary>取消还在进行的请求（弹层关上）。</summary>
    public void Cancel()
    {
        current?.Cancel();
        current = null;
        Loading = false;
    }

    /// <inheritdoc/>
    public void Dispose() => Cancel();
}
