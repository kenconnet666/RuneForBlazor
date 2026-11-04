namespace Rune;

/// <summary>
/// 由其他状态算出来的只读值：第一次读取时计算，依赖不变就一直用缓存，依赖变了下次读取时重算。
/// <code>
/// readonly Derived&lt;decimal&gt; total = new(() => Items.Sum(i => i.Price));
/// </code>
/// 只在计算代价高时使用；便宜的派生（拼字符串、取字段）直接写成普通属性，组件渲染时自然会重算。
/// </summary>
/// <remarks>计算必须是同步的纯函数：不要在里面改状态，也不要 await。</remarks>
public sealed class Derived<T> : Signal, ISignalObserver
{
    readonly Func<T> compute;
    readonly Subscriptions subscriptions;
    // 两把锁：重算锁保证同时只有一个线程在算；状态锁只保护 dirty / epoch，通知不会被漫长的重算挡住
    readonly Lock computeLock = new();
    readonly Lock stateLock = new();
    bool dirty = true;
    int epoch;
    T cached = default!;

    /// <summary>创建派生值；<paramref name="compute"/> 读到的状态与派生值自动成为依赖。</summary>
    public Derived(Func<T> compute)
    {
        this.compute = compute;
        subscriptions = new Subscriptions(this);
    }

    /// <summary>当前值；依赖变过就先重算。</summary>
    public T Value
    {
        get
        {
            TrackRead();
            lock (computeLock)
            {
                bool needsRecompute;
                lock (stateLock) needsRecompute = dirty;
                if (needsRecompute) Recompute();
                return cached;
            }
        }
    }

    void Recompute()
    {
        int epochAtStart;
        lock (stateLock) epochAtStart = epoch;

        Dictionary<Signal, long> reads;
        using (var frame = DependencyTracker.Begin())
        {
            cached = compute();
            reads = frame.Reads;
        }
        var missedChange = subscriptions.Update(reads);

        // 计算期间依赖又变了（通知来过，或订阅后核对发现版本变了）：结果可能已经过期，保持 dirty
        bool stillDirty;
        lock (stateLock)
        {
            stillDirty = missedChange || epoch != epochAtStart;
            dirty = stillDirty;
        }
        // 正在读取这个派生值的下游会核对版本，递增版本让它们发现并重读
        if (stillDirty) MarkStale();
    }

    void ISignalObserver.Notify()
    {
        bool wasClean;
        lock (stateLock)
        {
            epoch++;
            wasClean = !dirty;
            dirty = true;
        }
        if (wasClean) NotifyChanged();
        else MarkStale();
    }

    /// <inheritdoc/>
    public override string ToString() => "Derived";
}
