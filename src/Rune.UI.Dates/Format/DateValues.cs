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
