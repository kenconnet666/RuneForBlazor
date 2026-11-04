namespace Rune;

/// <summary>
/// 一个观察者（组件或派生值）当前订阅的信号。每次重新收集依赖后调用 <see cref="Update"/>：
/// 先订阅新依赖、再退订不再需要的（中间没有空窗），最后核对版本。
/// </summary>
internal sealed class Subscriptions(ISignalObserver observer)
{
    HashSet<Signal> current = [];

    /// <summary>换成这一次读到的依赖。</summary>
    /// <returns>读取之后有依赖又变了（那次通知可能在订阅之前就发完了），调用方应当再处理一次。</returns>
    public bool Update(Dictionary<Signal, long> reads)
    {
        foreach (var signal in reads.Keys)
            if (!current.Contains(signal)) signal.Subscribe(observer);
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
        foreach (var signal in current) signal.Unsubscribe(observer);
        current = [];
    }
}
