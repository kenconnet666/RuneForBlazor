namespace Rune.UI;

/// <summary>
/// 加载指示防闪烁（同 vue-tsx-pro 的 useDelayedLoading、Ant Design Spin 的 delay）：很快完成的加载不出现指示，
/// 出现了就至少显示一段时间。只管视觉：aria-busy、禁止点击等仍按真实的 loading 立即生效。
/// 第一次更新时已经在加载就立即显示（没有内容可看，空白比指示更糟）。
/// 不可交互的渲染（静态 SSR、预渲染）不推迟：响应发出去以后计时器到点也没有下一次渲染，“至少显示一会儿”会让指示一直留在页面上。
/// <code>
/// DelayedLoading indicator => delayedLoading ??= new(() => InvokeAsync(StateHasChanged), () => RendererInfo.IsInteractive);
/// protected override void OnParametersSet() => indicator.Update(Loading);   // 渲染里用 indicator.Shown
/// public void Dispose() => indicator.Dispose();
/// </code>
/// </summary>
/// <param name="rerender">显示状态在计时器里变了之后调用：组件据此重新渲染。</param>
/// <param name="interactive">
/// 组件是否在可交互地渲染（<c>RendererInfo.IsInteractive</c>）；否则显示状态始终等于真实的 loading。
/// 加载状态第二次变化时才读（第一次总是立即跟随），读一次。
/// </param>
public sealed class DelayedLoading(Func<Task> rerender, Func<bool>? interactive = null) : IDisposable
{
    /// <summary>开始加载后过多久才显示。</summary>
    public static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(200);

    /// <summary>显示后至少保持多久。</summary>
    public static readonly TimeSpan MinDuration = TimeSpan.FromMilliseconds(500);

    bool? loading;
    bool? isInteractive;
    // 显示出来的时间戳（单调时钟，不受改系统时间影响）
    long shownAt;
    CancellationTokenSource? pending;

    /// <summary>加载指示是否显示。</summary>
    public bool Shown { get; private set; }

    /// <summary>告诉它现在是否在加载（每次参数更新时调用，值没变时什么也不做）。</summary>
    public void Update(bool loading)
    {
        if (this.loading == loading) return;
        var first = this.loading is null;
        this.loading = loading;
        if (first || !(isInteractive ??= interactive?.Invoke() ?? true))
        {
            Shown = loading;
            if (loading) shownAt = TimeProvider.System.GetTimestamp();
            return;
        }
        if (loading)
        {
            // 正在等着隐藏时又开始加载：取消隐藏，继续显示
            if (Shown) Cancel();
            else Schedule(Delay, true);
            return;
        }
        if (!Shown)
        {
            Cancel();
            return;
        }
        var remaining = MinDuration - TimeProvider.System.GetElapsedTime(shownAt);
        Schedule(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, false);
    }

    /// <inheritdoc/>
    public void Dispose() => Cancel();

    void Schedule(TimeSpan wait, bool shown)
    {
        Cancel();
        pending = new CancellationTokenSource();
        _ = SwitchLaterAsync(wait, shown, pending.Token);
    }

    async Task SwitchLaterAsync(TimeSpan wait, bool shown, CancellationToken token)
    {
        try
        {
            await Task.Delay(wait, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        Shown = shown;
        if (shown) shownAt = TimeProvider.System.GetTimestamp();
        await rerender();
    }

    void Cancel()
    {
        pending?.Cancel();
        pending?.Dispose();
        pending = null;
    }
}
