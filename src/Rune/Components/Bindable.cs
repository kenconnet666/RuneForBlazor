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
/// Bindable&lt;bool&gt; open { get => field ??= new(() => Open, () => OpenChanged); }
/// // 读：open.Value；写：await open.SetAsync(true)
/// </code>
/// </summary>
public sealed class Bindable<T>
{
    readonly Func<T> readParameter;
    readonly Func<EventCallback<T>> readChanged;
    readonly State<T> local;
    T lastParameter;

    /// <summary>用读取参数与回调的函数创建（参数会被父级反复赋值，所以传函数而不是值）。</summary>
    public Bindable(Func<T> readParameter, Func<EventCallback<T>> readChanged)
    {
        this.readParameter = readParameter;
        this.readChanged = readChanged;
        lastParameter = readParameter();
        local = new State<T>(lastParameter);
    }

    /// <summary>父级是否在接管这个值（给了 XxxChanged）。</summary>
    public bool IsControlled => readChanged().HasDelegate;

    /// <summary>当前值。</summary>
    public T Value
    {
        get
        {
            var parameter = readParameter();
            if (IsControlled) return parameter;
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
        if (IsControlled) return readChanged().InvokeAsync(value);
        local.Value = value;
        return Task.CompletedTask;
    }
}
