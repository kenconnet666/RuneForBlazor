namespace Rune;

/// <summary>
/// 依赖收集：组件渲染、派生值计算期间读到了哪些信号。
/// 收集帧放在当前线程上（渲染与计算都是同步的）；帧可以嵌套（渲染里读派生值时，派生值有自己的帧）。
/// </summary>
internal static class DependencyTracker
{
    [ThreadStatic] static Frame? current;

    /// <summary>开始收集；用 using 结束，结束后从 <see cref="Frame.Reads"/> 取结果。</summary>
    public static Frame Begin() => current = new Frame(current);

    /// <summary>信号被读取时调用：记下它与读取时的版本（同一信号只记第一次）。</summary>
    public static void RecordRead(Signal signal) => current?.Reads.TryAdd(signal, signal.Version);

    /// <summary>一次收集。</summary>
    public sealed class Frame(Frame? parent) : IDisposable
    {
        readonly int ownerThread = Environment.CurrentManagedThreadId;

        /// <summary>读到的信号 → 读取时的版本。</summary>
        public Dictionary<Signal, long> Reads { get; } = [];

        /// <summary>结束收集，恢复外层的帧。</summary>
        public void Dispose()
        {
            // 帧放在线程上，跨 await 后会在别的线程结束，既丢依赖又污染那个线程：直接报错
            if (ownerThread != Environment.CurrentManagedThreadId)
                throw new InvalidOperationException("依赖收集不能跨 await：渲染与派生值的计算必须是同步的");
            current = parent;
        }
    }
}
