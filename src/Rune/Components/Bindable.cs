using Microsoft.AspNetCore.Components;

namespace Rune;

/// <summary>
/// 可双向绑定的参数，按 Blazor 的 <c>Xxx</c> + <c>XxxChanged</c> 协议工作，三种用法都支持：
/// <list type="bullet">
/// <item><description>父级 <c>@bind-Open="x"</c>（或给了 <c>OpenChanged</c>）：受控，值以父级为准，改动交给父级决定；</description></item>
/// <item><description>父级只给 <c>Open="true"</c>：作为初始值，之后组件自己维护；父级传来新值时跟着父级；</description></item>
/// <item><description>都不给：组件自己的状态。</description></item>
/// </list>
/// <code>
/// [Parameter] public bool Open { get; set; }
/// [Parameter] public EventCallback&lt;bool&gt; OpenChanged { get; set; }
/// Bindable&lt;bool&gt; open { get => field ??= new(this, () => Open, () => OpenChanged); }
/// // 读：open.Value；写：await open.SetAsync(true)
/// </code>
/// </summary>
/// <remarks>
/// 受控时改值要等父级重新渲染、把新值作为参数传回来。在那之前组件会先用旧参数渲染几次：组件自己处理完事件要渲染，
/// 外层的 CascadingValue（Field 交下来的上下文）被父级重新渲染时也会通知它，并带上它上一次的直接参数（旧值）。
/// 旧值写回输入框，Server 模式下快速输入就吞字（验证码输入框的 E2E 发现）。
/// 所以改值请求发出后、参数变化之前（父级采纳了），<see cref="Value"/> 先用请求的值；这一批渲染结束时参数还没变
/// （父级没采纳，或回调的接收者不是组件、父级根本没重新渲染），回到父级的值，与请求的不同时组件再渲染一次。
/// </remarks>
public sealed class Bindable<T> : IRenderListener
{
    readonly Func<T> readParameter;
    readonly Func<EventCallback<T>> readChanged;
    readonly State<T> local;
    T lastParameter;
    // 受控时发出、父级还没回应的改值，与发出时的参数
    bool requesting;
    T requested = default!;
    T requestedFrom = default!;

    /// <summary>用所属组件与读取参数、回调的函数创建（参数会被父级反复赋值，所以传函数而不是值）。</summary>
    public Bindable(RuneComponent owner, Func<T> readParameter, Func<EventCallback<T>> readChanged)
    {
        this.readParameter = readParameter;
        this.readChanged = readChanged;
        lastParameter = readParameter();
        local = new State<T>(lastParameter);
        owner.ListenToRenders(this);
    }

    /// <summary>父级是否在接管这个值（给了 XxxChanged）。</summary>
    public bool IsControlled => readChanged().HasDelegate;

    /// <summary>当前值。</summary>
    public T Value
    {
        get
        {
            var parameter = readParameter();
            if (IsControlled)
            {
                if (requesting && EqualityComparer<T>.Default.Equals(parameter, requestedFrom)) return requested;
                requesting = false;
                return parameter;
            }
            // 非受控时父级传来了新值：跟着父级
            if (!EqualityComparer<T>.Default.Equals(parameter, lastParameter))
            {
                lastParameter = parameter;
                local.Value = parameter;
            }
            return local.Value;
        }
    }

    /// <summary>改值：受控时通知父级（父级决定是否采纳），否则直接改组件自己的状态。</summary>
    public Task SetAsync(T value)
    {
        if (!IsControlled)
        {
            local.Value = value;
            return Task.CompletedTask;
        }
        requested = value;
        requestedFrom = readParameter();
        requesting = true;
        return readChanged().InvokeAsync(value);
    }

    bool IRenderListener.AfterRender()
    {
        if (!requesting) return false;
        requesting = false;
        return !EqualityComparer<T>.Default.Equals(readParameter(), requested);
    }
}

/// <summary>要知道组件何时渲染完一批的对象：<see cref="Bindable{T}"/>。</summary>
internal interface IRenderListener
{
    /// <summary>组件这一批渲染完了；返回 true 时组件要再渲染一次。</summary>
    bool AfterRender();
}
