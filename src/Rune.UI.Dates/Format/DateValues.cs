namespace Rune.UI;

/// <summary>日期组件共用的取值工具：值里有没有时间、换成时刻、time 元素的 datetime。</summary>
static class DateValues
{
    /// <summary>值里有没有时间：DateTime、DateTimeOffset、TimeOnly 有，DateOnly 没有。</summary>
    public static bool HasTime(object value) => value is not DateOnly;

    /// <summary>time 元素的 datetime（机器可读的 ISO 8601）。</summary>
    public static string Machine(object value) => value switch
    {
        DateOnly date => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
        DateTimeOffset offset => offset.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        DateTime time => time.ToString(time.Kind == DateTimeKind.Unspecified ? "yyyy-MM-ddTHH:mm:ss" : "O", System.Globalization.CultureInfo.InvariantCulture),
        _ => throw new ArgumentException($"不是日期：{value.GetType()}", nameof(value)),
    };

    /// <summary>
    /// 换成时刻：DateTimeOffset 原样；UTC 的 DateTime 按 UTC；其余（不带时区的日期时间、DateOnly 的 0 点）按 <paramref name="zone"/> 理解。
    /// </summary>
    public static DateTimeOffset ToInstant(object value, TimeZoneInfo zone) => value switch
    {
        DateTimeOffset offset => offset,
        DateTime { Kind: DateTimeKind.Utc } utc => new DateTimeOffset(utc),
        DateTime local => new DateTimeOffset(local, zone.GetUtcOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified))),
        DateOnly date => ToInstant(date.ToDateTime(TimeOnly.MinValue), zone),
        _ => throw new ArgumentException($"不是日期时间：{value.GetType()}", nameof(value)),
    };
}

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
