namespace Rune.UI;

/// <summary>
/// 随时间更新的“现在”（RelativeTime、Countdown 共用，同 vue-tsx-pro 的 useNow）：每隔一段时间通知一次，对齐到整秒、整分，
/// 同一页上的几个计时器一起跳；间隔为 null 时停下。“现在”取 TimeProvider（测试、截图给固定的）。
/// </summary>
sealed class Clock(TimeProvider time, Func<DateTimeOffset, TimeSpan?> interval, Func<Task> tick) : IDisposable
{
    ITimer? timer;

    public DateTimeOffset Now => time.GetUtcNow();

    /// <summary>按当前的“现在”重新排下一次更新（值、精度变了时也调用）。</summary>
    public void Schedule()
    {
        timer?.Dispose();
        timer = null;
        if (interval(Now) is not { } every || every <= TimeSpan.Zero) return;
        // 对齐到间隔的整数倍：多个计时器同一刻跳，倒计时的秒数不会一会儿快一会儿慢
        var period = (long)every.TotalMilliseconds;
        var delay = period - Now.ToUnixTimeMilliseconds() % period;
        timer = time.CreateTimer(_ => _ = OnTickAsync(), null, TimeSpan.FromMilliseconds(delay), Timeout.InfiniteTimeSpan);
    }

    async Task OnTickAsync()
    {
        await tick();
        Schedule();
    }

    public void Dispose()
    {
        timer?.Dispose();
        timer = null;
    }
}
