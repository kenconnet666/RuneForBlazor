namespace Rune.Tests.Reactivity;

/// <summary>记录收到了几次通知的观察者。</summary>
internal sealed class RecordingObserver : ISignalObserver
{
    int count;

    public int Count => Volatile.Read(ref count);

    public void Notify() => Interlocked.Increment(ref count);
}
