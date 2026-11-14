using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Rune.Docs.Site;

/// <summary>排队渲染的设置。</summary>
/// <param name="NearViewportOnly">
/// 打开站点的第一页时，只渲染视口附近的小节，其余的滚到附近才渲染（留着预渲染的内容）。文档站开着；
/// 测试台不开：端到端测试等全部渲染完才操作。
/// </param>
public sealed record RenderQueueOptions(bool NearViewportOnly = false);

/// <summary>
/// 站内换页后让页面里重的部分（示例、API 表，见 <see cref="Queued"/>）排队、一个一个渲染：
/// 标题、说明、卡片外框先出来，重的部分依次补上，每隔一个时间片把主线程交还浏览器（绘制、响应滚动）。
/// 一页十来个示例加 API 表一次渲染完，在 WebAssembly 里要冻住一两秒；排队以后页面立刻可见，内容陆续出现。
/// 打开站点的第一页同样排队，但预渲染的内容不能先消失：重的部分先渲染一个空的占位（data-docs-hold），
/// 文档站的 JS 初始化器把预渲染时那一段节点原样放回占位里，轮到时再换成真正渲染的内容（见 <see cref="Queued"/> 的 Hold）。
/// 这样交互接管时第一批只有外壳与各小节的外框，不必一口气把整页算完（数字见 docs/PERFORMANCE.md 第 3 节的“首屏分批接管”）。
/// 第一页的这些占位按离视口的远近渲染（JS 量，<c>runeDocsNearHolds</c>）：带着 #锚点打开时先换眼前的那一节；
/// <see cref="RenderQueueOptions.NearViewportOnly"/> 时视口外的先不渲染，滚到附近才轮到。
/// </summary>
public sealed class RenderQueue : IDisposable
{
    readonly NavigationManager navigation;
    readonly IJSRuntime js;
    readonly bool nearViewportOnly;
    readonly string firstUri;
    readonly CancellationTokenSource disposed = new();
    bool navigated;
    Task tail = Task.CompletedTask;
    // 一个时间片：连着渲染这么久才让出一次主线程
    static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(60);
    long sliceStart;
    // 第一页的占位：名字 → 轮到时交给它“渲染完了”的回调
    readonly Dictionary<string, TaskCompletionSource<Action>> held = new(StringComparer.Ordinal);
    bool pumping;
    // 正在问 JS 的那一次：又有占位登记进来（API 表的数据后到）就作废重问，不然新来的轮不到
    CancellationTokenSource? asking;

    /// <summary>创建（scoped：每个电路、每个 WebAssembly 应用一个）。</summary>
    public RenderQueue(NavigationManager navigation, IJSRuntime js, RenderQueueOptions? options = null)
    {
        this.navigation = navigation;
        this.js = js;
        nearViewportOnly = options?.NearViewportOnly ?? false;
        firstUri = navigation.Uri;
        navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// 现在显示的不是打开站点时的那一页：要排队。
    /// 不能只靠 LocationChanged：路由先收到通知、新页面先渲染，这里的通知在后面。
    /// </summary>
    public bool Deferring => navigated || navigation.Uri != firstUri;

    int waiting;

    /// <summary>还有排着队没渲染的内容（端到端测试等它变成 false 再操作示例）。</summary>
    public bool Busy => waiting > 0;

    /// <summary><see cref="Busy"/> 变了。</summary>
    public event Action? BusyChanged;

    /// <summary>一段内容开始排队。</summary>
    public void Enter()
    {
        if (waiting++ == 0) BusyChanged?.Invoke();
    }

    /// <summary>排队的那段内容渲染好了（或不用渲染了）。</summary>
    public void Leave()
    {
        if (--waiting == 0) BusyChanged?.Invoke();
    }

    /// <summary>
    /// 等轮到自己：渲染完自己后调用返回的回调，下一个才开始。
    /// <paramref name="hold"/> 是第一页里占位的名字：这些按离视口的远近轮；没有名字的（站内换页后）按先后。
    /// </summary>
    public Task<Action> WaitTurnAsync(string? hold = null)
    {
        if (hold is null || Deferring) return WaitInOrderAsync();
        var turn = new TaskCompletionSource<Action>();
        held[hold] = turn;
        asking?.Cancel();
        if (!pumping)
        {
            pumping = true;
            _ = PumpAsync();
        }
        return turn.Task;
    }

    /// <summary>这一段不用渲染了（还没轮到，页面就换走了）。</summary>
    public void Forget(string hold) => held.Remove(hold);

    async Task<Action> WaitInOrderAsync()
    {
        var mine = new TaskCompletionSource();
        var previous = tail;
        tail = mine.Task;
        await previous;
        await YieldAfterSliceAsync();
        return mine.SetResult;
    }

    // 让出主线程：浏览器先绘制已经渲染出来的部分、响应滚动与点击。攒够一个时间片才让：
    // 每段都停一次的话，轻的十几段各带一次整页的排版与绘制，总时间反而长出不少
    async Task YieldAfterSliceAsync()
    {
        if (System.Diagnostics.Stopwatch.GetElapsedTime(sliceStart) < Slice) return;
        await Task.Delay(1);
        sliceStart = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    // 一轮一轮地问 JS 现在该渲染哪几段（离视口近的在前），按顺序放行；这一批占位是同一次渲染里出现的，先让出一次等它们都登记上
    async Task PumpAsync()
    {
        try
        {
            await Task.Delay(1);
            while (held.Count > 0 && !disposed.IsCancellationRequested)
            {
                foreach (var name in await NextAsync())
                {
                    if (!held.Remove(name, out var turn)) continue;
                    var done = new TaskCompletionSource();
                    turn.SetResult(done.SetResult);
                    await done.Task;
                    await YieldAfterSliceAsync();
                }
            }
        }
        finally
        {
            pumping = false;
        }
    }

    // 接下来渲染的几段。JS 用不了（服务端预渲染、电路断了、单元测试）时按登记的顺序全部渲染
    async Task<IReadOnlyList<string>> NextAsync()
    {
        string[] pending = [.. held.Keys];
        using var question = asking = CancellationTokenSource.CreateLinkedTokenSource(disposed.Token);
        try
        {
            var near = await js.InvokeAsync<string[]>("runeDocsNearHolds", question.Token, pending, nearViewportOnly);
            // 一个都不认得（占位已经不在页面上了）：不再等它们
            return near.Any(held.ContainsKey) ? near : pending;
        }
        catch (OperationCanceledException)
        {
            // 作废了（有新的占位，或队列销毁了）：回去重问
            return [];
        }
        catch (Exception) when (!disposed.IsCancellationRequested)
        {
            return pending;
        }
        finally
        {
            asking = null;
        }
    }

    void OnLocationChanged(object? sender, LocationChangedEventArgs e) => navigated = true;

    /// <inheritdoc/>
    public void Dispose()
    {
        navigation.LocationChanged -= OnLocationChanged;
        disposed.Cancel();
        disposed.Dispose();
    }
}
