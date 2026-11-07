namespace Rune.UI;

/// <summary>倒计时的精度。</summary>
public enum CountdownPrecision
{
    /// <summary>到秒（默认）：每秒更新。</summary>
    Second,

    /// <summary>到分：每分钟更新，不显示秒。</summary>
    Minute,
}

/// <summary>还剩多久，拆成天、时、分、秒（Countdown 的自定义显示拿到它）。</summary>
/// <param name="Total">剩余的时长，到点后是 0。</param>
/// <param name="Days">整天数。</param>
/// <param name="Hours">不满一天的小时（0–23）。</param>
/// <param name="Minutes">不满一小时的分钟（0–59）。</param>
/// <param name="Seconds">不满一分钟的秒（0–59）；精度到分时总是 0。</param>
/// <param name="Ended">已经到点。</param>
public readonly record struct CountdownParts(TimeSpan Total, long Days, int Hours, int Minutes, int Seconds, bool Ended)
{
    /// <summary>剩余的时长 → 天、时、分、秒（按精度向上取整：还剩 0.4 秒时显示 1 秒，到点才是 0）。</summary>
    public static CountdownParts Of(TimeSpan remaining, CountdownPrecision precision)
    {
        var total = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        var unit = precision == CountdownPrecision.Minute ? 60_000L : 1000L;
        var rounded = (long)Math.Ceiling(total.TotalMilliseconds / unit) * unit;
        var seconds = rounded / 1000;
        return new(total, seconds / 86_400, (int)(seconds / 3600 % 24), (int)(seconds / 60 % 60), (int)(seconds % 60), remaining <= TimeSpan.Zero);
    }
}
