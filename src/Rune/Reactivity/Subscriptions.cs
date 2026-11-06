namespace Rune;

/// <summary>
/// 一个观察者（组件或派生值）当前订阅的信号。每次重新收集依赖后调用 <see cref="Update"/>：
/// 先订阅新依赖、再退订不再需要的（中间没有空窗），最后核对版本。
/// </summary>
internal sealed class Subscriptions(ISignalObserver observer)
{
    // 订阅着的信号；没有时为 null（多数组件的渲染不读信号，每次渲染都走这里，不为它分配）
    HashSet<Signal>? current;

    /// <summary>换成这一次读到的依赖（一个也没读时为 null）。</summary>
    /// <returns>读取之后有依赖又变了（那次通知可能在订阅之前就发完了），调用方应当再处理一次。</returns>
    public bool Update(Dictionary<Signal, long>? reads)
    {
        if (reads is null || reads.Count == 0)
        {
            Clear();
            return false;
        }
        foreach (var signal in reads.Keys)
            if (current?.Contains(signal) != true) signal.Subscribe(observer);
        if (current is not null)
            foreach (var signal in current)
                if (!reads.ContainsKey(signal)) signal.Unsubscribe(observer);
        current = [.. reads.Keys];

        foreach (var (signal, versionWhenRead) in reads)
            if (signal.Version != versionWhenRead) return true;
        return false;
    }

    /// <summary>退订全部（观察者销毁时）。</summary>
    public void Clear()
    {
        if (current is null) return;
        foreach (var signal in current) signal.Unsubscribe(observer);
        current = null;
    }
}
