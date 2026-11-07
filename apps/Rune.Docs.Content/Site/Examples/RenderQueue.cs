using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Rune.Docs.Site;

/// <summary>
/// 站内换页后让页面里重的部分（示例、API 表，见 <see cref="Queued"/>）排队、一个一个渲染：
/// 标题、说明、卡片外框先出来，重的部分依次补上，每个之间把主线程交还浏览器（绘制、响应滚动）。
/// 一页十来个示例加 API 表一次渲染完，在 WebAssembly 里要冻住一两秒；排队以后页面立刻可见，内容陆续出现。
/// 打开站点的第一页不排队：预渲染的内容正显示着，交互接管时原样渲染，免得先消失再出现。
/// </summary>
public sealed class RenderQueue : IDisposable
{
    readonly NavigationManager navigation;
    readonly string firstUri;
    bool navigated;
    Task tail = Task.CompletedTask;

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

    /// <summary>
    /// 等轮到自己：前一个渲染完、再让出一次主线程后返回。渲染完自己后调用返回的回调，下一个才开始。
    /// </summary>
    public async Task<Action> WaitTurnAsync()
    {
        var mine = new TaskCompletionSource();
        var previous = tail;
        tail = mine.Task;
        await previous;
        // 让出主线程：浏览器先绘制已经渲染出来的部分
        await Task.Delay(1);
        return mine.SetResult;
    }

    void OnLocationChanged(object? sender, LocationChangedEventArgs e) => navigated = true;

    /// <inheritdoc/>
    public void Dispose() => navigation.LocationChanged -= OnLocationChanged;
}
