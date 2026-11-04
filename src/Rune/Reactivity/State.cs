namespace Rune;

/// <summary>
/// 可读写的响应式状态。组件渲染时读了 <see cref="Value"/>，它变化后组件自动重新渲染。
/// <code>
/// readonly State&lt;int&gt; count = new(0);
/// // 渲染：@count.Value；事件：count.Value++
/// </code>
/// 组件与普通类里更推荐用生成器的写法 <c>[State] public partial int Count { get; set; }</c>，读写不用 .Value。
/// </summary>
/// <remarks>
/// 新值与旧值相等（<see cref="EqualityComparer{T}.Default"/>）时不通知。
/// 对象、集合不做深层追踪：改了内部内容要整体换成新值（record 的 <c>with</c>、不可变集合的 <c>Add</c>）。
/// 可以在任意线程上写；组件会切回自己的渲染线程再更新。
/// </remarks>
public sealed class State<T>(T initial) : Signal
{
    // 读写都在锁里：值是大的结构体时也不会读到写了一半的值
    readonly Lock valueLock = new();
    T value = initial;

    /// <summary>当前值。读取会登记依赖；写入新值会通知读过它的组件与派生值。</summary>
    public T Value
    {
        get
        {
            TrackRead();
            lock (valueLock) return value;
        }
        set
        {
            lock (valueLock)
            {
                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;
                this.value = value;
            }
            NotifyChanged();
        }
    }

    /// <summary>只取当前值、不登记依赖（渲染里有意只要一次性的值时用）。</summary>
    public T Peek()
    {
        lock (valueLock) return value;
    }

    /// <inheritdoc/>
    public override string ToString() => $"State({Peek()})";
}
