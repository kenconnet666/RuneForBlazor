using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Rune.Styles;

namespace Rune;

/// <summary>
/// 组件基类，用法与 <see cref="ComponentBase"/> 相同（<c>@inherits RuneComponent</c>），另外：
/// <list type="bullet">
/// <item><description>渲染时读过的 <see cref="State{T}"/> / <see cref="Derived{T}"/> 变化后，只重新渲染这个组件；</description></item>
/// <item><description>父级重新渲染时，参数都没变就跳过（规则见 <see cref="ParameterChangeDetector"/>）；</description></item>
/// <item><description>提供 <see cref="AdditionalAttributes"/>、<see cref="Sx"/> 与合成好的 <see cref="Rest"/>，交给 <see cref="Attrs.Merge"/>；</description></item>
/// <item><description>渲染期间登记用到的样式规则，由 <see cref="RuneStyles"/> 输出。</description></item>
/// </list>
/// </summary>
/// <remarks>
/// 自己实现 <see cref="IComponent"/> 而不是继承 <see cref="ComponentBase"/>：要在 BuildRenderTree 前后开关依赖收集，
/// ComponentBase 没有这个扩展点。生命周期的行为照 ComponentBase（.NET 10）复刻。
/// 需要释放资源时重写 <see cref="OnDispose"/>，不要再实现 IDisposable。
/// </remarks>
public abstract class RuneComponent : IComponent, IHandleEvent, IHandleAfterRender, ISignalObserver, IDisposable
{
    readonly RenderFragment renderFragment;
    readonly Subscriptions subscriptions;
    RenderHandle renderHandle;
    bool initialized;
    bool hasNeverRendered = true;
    bool hasPendingQueuedRender;
    bool hasCalledOnAfterRender;
    volatile bool disposed;

    /// <summary>创建组件。</summary>
    protected RuneComponent()
    {
        subscriptions = new Subscriptions(this);
        renderFragment = RenderWithTracking;
    }

    /// <summary>当前作用域的样式登记表（每个请求 / 电路一份）。</summary>
    [Inject] StyleRegistry StyleRegistry { get; set; } = default!;

    /// <summary>组件上没有声明的属性（<c>id</c>、<c>aria-*</c>、<c>data-*</c>、事件……），通常落到主元素上。</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>使用方的样式覆盖，排在组件自己的样式之后。</summary>
    [Parameter]
    public Sx? Sx { get; set; }

    /// <summary>
    /// <see cref="AdditionalAttributes"/> 与 <see cref="Sx"/> 合成的一层，作为“使用方”层交给主元素的合并：
    /// <c>@attributes="Attrs.Merge(s.Root, Rest, managed)"</c>。
    /// </summary>
    protected Attrs Rest => Attrs.From(AdditionalAttributes, Sx);

    /// <summary>渲染器信息（是否交互、在哪种平台上）。</summary>
    protected RendererInfo RendererInfo => renderHandle.RendererInfo;

    /// <summary>静态资源的指纹化路径。</summary>
    protected ResourceAssetCollection Assets => renderHandle.Assets;

    /// <summary>分配给这个组件的渲染模式（没有时为 null）。</summary>
    protected IComponentRenderMode? AssignedRenderMode => renderHandle.RenderMode;

    /// <summary>渲染内容，由 Razor 生成。</summary>
    protected virtual void BuildRenderTree(RenderTreeBuilder builder) { }

    /// <summary>首次收到参数后调用一次。</summary>
    protected virtual void OnInitialized() { }

    /// <summary>首次收到参数后调用一次（异步）。</summary>
    protected virtual Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>每次参数变化后调用（参数没变而被跳过时不调用）。</summary>
    protected virtual void OnParametersSet() { }

    /// <summary>每次参数变化后调用（异步）。</summary>
    protected virtual Task OnParametersSetAsync() => Task.CompletedTask;

    /// <summary>返回 false 可以阻止这次渲染（首次渲染与热重载不受影响）。</summary>
    protected virtual bool ShouldRender() => true;

    /// <summary>每次渲染完成、DOM 更新后调用（只在交互模式下）。</summary>
    protected virtual void OnAfterRender(bool firstRender) { }

    /// <summary>每次渲染完成、DOM 更新后调用（异步）。</summary>
    protected virtual Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

    /// <summary>组件销毁时调用：在这里释放计时器、取消订阅外部事件。</summary>
    protected virtual void OnDispose() { }

    /// <summary>请求重新渲染。用了状态的地方不需要手动调用。</summary>
    protected void StateHasChanged()
    {
        if (hasPendingQueuedRender || disposed) return;
        if (hasNeverRendered || ShouldRender() || renderHandle.IsRenderingOnMetadataUpdate)
        {
            hasPendingQueuedRender = true;
            try
            {
                renderHandle.Render(renderFragment);
            }
            catch
            {
                hasPendingQueuedRender = false;
                throw;
            }
        }
    }

    /// <summary>在渲染器的同步上下文上执行。</summary>
    protected Task InvokeAsync(Action work) => renderHandle.Dispatcher.InvokeAsync(work);

    /// <summary>在渲染器的同步上下文上执行（异步）。</summary>
    protected Task InvokeAsync(Func<Task> work) => renderHandle.Dispatcher.InvokeAsync(work);

    /// <summary>把异常交给渲染器，按组件抛出的异常处理（交给错误边界）。</summary>
    protected Task DispatchExceptionAsync(Exception exception) => renderHandle.DispatchExceptionAsync(exception);

    void RenderWithTracking(RenderTreeBuilder builder)
    {
        hasPendingQueuedRender = false;
        hasNeverRendered = false;

        bool missedChange;
        using (StyleRegistry.Enter(StyleRegistry))
        using (var frame = DependencyTracker.Begin())
        {
            BuildRenderTree(builder);
            missedChange = subscriptions.Update(frame.Reads);
        }
        // 渲染期间（或订阅之前）依赖又变了：那次通知可能已经错过，补一次渲染
        if (missedChange) ((ISignalObserver)this).Notify();
    }

    void IComponent.Attach(RenderHandle handle)
    {
        if (renderHandle.IsInitialized)
            throw new InvalidOperationException($"组件 {GetType().Name} 已经挂到渲染器上，不能重复挂载");
        renderHandle = handle;
    }

    /// <summary>
    /// 设置参数。父级重新渲染而参数都没变时直接返回，不调用 OnParametersSet、不渲染；
    /// 热重载时一律当作变了，让新代码生效。
    /// </summary>
    public virtual Task SetParametersAsync(ParameterView parameters)
    {
        var changed = !initialized
                      || renderHandle.IsRenderingOnMetadataUpdate
                      || ParameterChangeDetector.HasChanged(this, parameters, AdditionalAttributes);
        parameters.SetParameterProperties(this);
        if (!initialized)
        {
            initialized = true;
            return RunInitAndSetParametersAsync();
        }
        return changed ? CallOnParametersSetAsync() : Task.CompletedTask;
    }

    async Task RunInitAndSetParametersAsync()
    {
        OnInitialized();
        var task = OnInitializedAsync();
        if (!task.IsCompleted)
        {
            // 异步初始化期间先渲染一次（显示加载中）
            StateHasChanged();
            try
            {
                await task;
            }
            catch when (task.IsCanceled)
            {
                // 取消视为正常结束
            }
        }
        await CallOnParametersSetAsync();
    }

    Task CallOnParametersSetAsync()
    {
        OnParametersSet();
        var task = OnParametersSetAsync();
        var shouldAwait = !task.IsCompleted;
        StateHasChanged();
        return shouldAwait ? RenderWhenCompleted(task) : Task.CompletedTask;
    }

    async Task RenderWhenCompleted(Task task)
    {
        try
        {
            await task;
        }
        catch when (task.IsCanceled)
        {
            return;
        }
        StateHasChanged();
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        // 与 ComponentBase 相同：事件处理后重新渲染，没用状态的普通字段也能刷新
        var task = callback.InvokeAsync(arg);
        var shouldAwait = !task.IsCompleted;
        StateHasChanged();
        return shouldAwait ? RenderWhenCompleted(task) : Task.CompletedTask;
    }

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        var firstRender = !hasCalledOnAfterRender;
        hasCalledOnAfterRender = true;
        OnAfterRender(firstRender);
        return OnAfterRenderAsync(firstRender);
    }

    void ISignalObserver.Notify()
    {
        // 通知可能来自任意线程（后台任务写了共享状态），切回渲染器的线程再渲染
        if (disposed || !renderHandle.IsInitialized) return;
        if (renderHandle.Dispatcher.CheckAccess()) StateHasChanged();
        else _ = renderHandle.Dispatcher.InvokeAsync(StateHasChanged);
    }

    void IDisposable.Dispose()
    {
        disposed = true;
        subscriptions.Clear();
        OnDispose();
    }
}
