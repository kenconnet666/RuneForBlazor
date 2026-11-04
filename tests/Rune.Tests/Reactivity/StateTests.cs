namespace Rune.Tests.Reactivity;

public class StateTests
{
    [Fact]
    public void 写入不同的值才通知()
    {
        var state = new State<int>(1);
        var observer = new RecordingObserver();
        state.Subscribe(observer);

        state.Value = 2;
        state.Value = 2;

        Assert.Equal(1, observer.Count);
    }

    [Fact]
    public void 收集依赖时读取Value会被记录_Peek不会()
    {
        var tracked = new State<int>(1);
        var peeked = new State<int>(2);

        using var frame = DependencyTracker.Begin();
        _ = tracked.Value;
        _ = peeked.Peek();

        Assert.Equal([tracked], frame.Reads.Keys);
    }

    [Fact]
    public void 读取之后订阅之前的写入会被发现()
    {
        var state = new State<int>(0);
        var subscriptions = new Subscriptions(new RecordingObserver());

        Dictionary<Signal, long> reads;
        using (var frame = DependencyTracker.Begin())
        {
            _ = state.Value;
            reads = frame.Reads;
        }
        state.Value = 1;

        Assert.True(subscriptions.Update(reads));
    }

    [Fact]
    public void 依赖收集不能跨线程结束()
    {
        var frame = DependencyTracker.Begin();
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { frame.Dispose(); }
            catch (Exception e) { error = e; }
        });
        thread.Start();
        thread.Join();
        frame.Dispose();

        Assert.IsType<InvalidOperationException>(error);
    }
}
