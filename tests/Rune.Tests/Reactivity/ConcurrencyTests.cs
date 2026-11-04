namespace Rune.Tests.Reactivity;

/// <summary>
/// Blazor Server 下后台任务可能在任意线程写共享状态，渲染在电路的线程上读。
/// 模拟“组件”：收到通知就重新读取派生值并重新订阅，写入全部结束后，它最后看到的必须是最终值（不能丢通知）。
/// </summary>
public class ConcurrencyTests
{
    const int Writers = 4;
    const int WritesPerWriter = 300;
    const int Trials = 20;

    [Fact]
    public async Task 多线程写入时渲染方不会停在旧值上()
    {
        for (var trial = 0; trial < Trials; trial++)
        {
            // 每个写线程只写自己的状态，避免“读-改-写”本身的竞争干扰判断
            var states = Enumerable.Range(0, Writers).Select(_ => new State<int>(0)).ToArray();
            var sum = new Derived<int>(() => states.Sum(s => s.Value));
            var renderer = new LoopRenderer(sum);

            var running = renderer.RunAsync();
            await Task.WhenAll(states.Select(state => Task.Run(() =>
            {
                for (var i = 1; i <= WritesPerWriter; i++) state.Value = i;
            })));

            var settled = await renderer.WaitForAsync(Writers * WritesPerWriter, TimeSpan.FromSeconds(5));
            renderer.Stop();
            await running;
            Assert.True(settled, $"第 {trial} 轮：渲染方停在 {renderer.LastRendered}，最终值是 {Writers * WritesPerWriter}");
        }
    }

    /// <summary>收到通知就重新“渲染”（读派生值、更新订阅）的观察者，行为同 RuneComponent。</summary>
    sealed class LoopRenderer : ISignalObserver
    {
        readonly Derived<int> source;
        readonly Subscriptions subscriptions;
        readonly SemaphoreSlim notified = new(0);
        readonly CancellationTokenSource stop = new();
        int lastRendered = -1;

        public LoopRenderer(Derived<int> source)
        {
            this.source = source;
            subscriptions = new Subscriptions(this);
        }

        public int LastRendered => Volatile.Read(ref lastRendered);

        public void Notify() => notified.Release();

        public async Task RunAsync()
        {
            Render();
            try
            {
                while (true)
                {
                    await notified.WaitAsync(stop.Token);
                    Render();
                }
            }
            catch (OperationCanceledException)
            {
                // 停止
            }
        }

        public async Task<bool> WaitForAsync(int expected, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (LastRendered == expected) return true;
                await Task.Delay(5);
            }
            return LastRendered == expected;
        }

        public void Stop() => stop.Cancel();

        void Render()
        {
            using var frame = DependencyTracker.Begin();
            Volatile.Write(ref lastRendered, source.Value);
            if (subscriptions.Update(frame.Reads)) Notify();
        }
    }
}
