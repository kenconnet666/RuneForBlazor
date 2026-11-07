using System.Globalization;

namespace Rune.UI;

/// <summary>日期组件共用的取值工具：值里有没有时间、换成时刻、time 元素的 datetime、日历格子上的 ISO 日期。</summary>
static class DateValues
{
    /// <summary>ISO 日期（yyyy-MM-dd）：日历格子的 data 属性、与 JS 往来的日期。</summary>
    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>读回 <see cref="Iso"/> 写的日期。</summary>
    public static bool TryParseIso(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>值里有没有时间：DateTime、DateTimeOffset、TimeOnly 有，DateOnly 没有。</summary>
    public static bool HasTime(object value) => value is not DateOnly;

    /// <summary>time 元素的 datetime（机器可读的 ISO 8601）。</summary>
    public static string Machine(object value) => value switch
    {
        DateOnly date => Iso(date),
        TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset offset => offset.ToString("O", CultureInfo.InvariantCulture),
        DateTime time => time.ToString(time.Kind == DateTimeKind.Unspecified ? "yyyy-MM-ddTHH:mm:ss" : "O", CultureInfo.InvariantCulture),
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
