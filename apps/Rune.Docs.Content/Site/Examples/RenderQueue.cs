using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Rune.Docs.Site;

/// <summary>
/// 站内换页后让页面里重的部分（示例、API 表，见 <see cref="Queued"/>）排队、一个一个渲染：
/// 标题、说明、卡片外框先出来，重的部分依次补上，每隔一个时间片把主线程交还浏览器（绘制、响应滚动）。
/// 一页十来个示例加 API 表一次渲染完，在 WebAssembly 里要冻住一两秒；排队以后页面立刻可见，内容陆续出现。
/// 打开站点的第一页同样排队，但预渲染的内容不能先消失：重的部分先渲染一个空的占位（data-docs-hold），
/// 文档站的 JS 初始化器把预渲染时那一段节点原样放回占位里，轮到时再换成真正渲染的内容（见 <see cref="Queued"/> 的 Hold）。
/// 这样交互接管时第一批只有外壳与各小节的外框，不必一口气把整页算完（数字见 docs/PERFORMANCE.md 第 3 节的“首屏分批接管”）。
/// </summary>
public sealed class RenderQueue : IDisposable
{
    readonly NavigationManager navigation;
    readonly string firstUri;
    bool navigated;
    Task tail = Task.CompletedTask;
    // 一个时间片：连着渲染这么久才让出一次主线程
    static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(60);
    long sliceStart;

    /// <summary>创建（scoped：每个电路、每个 WebAssembly 应用一个）。</summary>
    public RenderQueue(NavigationManager navigation)
    {
        this.navigation = navigation;
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
    /// 等轮到自己：前一个渲染完、再让出一次主线程后返回。渲染完自己后调用返回的回调，下一个才开始。
    /// </summary>
    public async Task<Action> WaitTurnAsync()
    {
        var mine = new TaskCompletionSource();
        var previous = tail;
        tail = mine.Task;
        await previous;
        // 让出主线程：浏览器先绘制已经渲染出来的部分、响应滚动与点击。攒够一个时间片才让：
        // 每段都停一次的话，轻的十几段各带一次整页的排版与绘制，总时间反而长出不少
        if (System.Diagnostics.Stopwatch.GetElapsedTime(sliceStart) >= Slice)
        {
            await Task.Delay(1);
            sliceStart = System.Diagnostics.Stopwatch.GetTimestamp();
        }
        return mine.SetResult;
    }

    void OnLocationChanged(object? sender, LocationChangedEventArgs e) => navigated = true;

    /// <inheritdoc/>
    public void Dispose() => navigation.LocationChanged -= OnLocationChanged;
}
