namespace Rune;

/// <summary>依赖变化时收到通知的一方：组件（重新渲染）、派生值（标记过期）。</summary>
internal interface ISignalObserver
{
    /// <summary>某个依赖变了。可能在任意线程上调用，实现方自己负责切回需要的线程。</summary>
    void Notify();
}

/// <summary>
/// 可被追踪的值（<see cref="State{T}"/>、<see cref="Derived{T}"/>）的基类：
/// 读取时登记到当前的依赖收集帧，变化时通知订阅者。
/// </summary>
/// <remarks>
/// 每次变化递增 <see cref="Version"/>。订阅方先记下读取时的版本、订阅后再核对，
/// 就能发现“读到之后、订阅之前”发生的变化（见 <see cref="Subscriptions"/>），跨线程写入不会丢通知。
/// </remarks>
public abstract class Signal
{
    readonly HashSet<ISignalObserver> observers = [];
    readonly Lock observersLock = new();
    long version;

    /// <summary>变化的次数；只用来判断“是否变过”，不表示具体值。</summary>
    internal long Version => Volatile.Read(ref version);

    /// <summary>登记一次读取：当前若在收集依赖（组件渲染、派生值计算），就把自己记进去。</summary>
    protected void TrackRead() => DependencyTracker.RecordRead(this);

    /// <summary>值已经变了：先递增版本，再通知订阅者。</summary>
    protected void NotifyChanged()
    {
        Interlocked.Increment(ref version);
        ISignalObserver[] snapshot;
        lock (observersLock) snapshot = [.. observers];
        foreach (var observer in snapshot) observer.Notify();
    }

    /// <summary>只递增版本、不通知：派生值重算时发现依赖又变了，让正在核对版本的下游重读。</summary>
    protected void MarkStale() => Interlocked.Increment(ref version);

    internal void Subscribe(ISignalObserver observer)
    {
        lock (observersLock) observers.Add(observer);
    }

    internal void Unsubscribe(ISignalObserver observer)
    {
        lock (observersLock) observers.Remove(observer);
    }
}
