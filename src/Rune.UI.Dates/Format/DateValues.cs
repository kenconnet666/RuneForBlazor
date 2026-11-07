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

    /// <summary>值 → 时区里的日期时间（墙上时间）：DateOnly 是那天的 0 点，UTC 的 DateTime 与 DateTimeOffset 换到 <paramref name="zone"/>。</summary>
    public static DateTime? Wall(object? value, TimeZoneInfo zone) => value switch
    {
        null => null,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        DateTime { Kind: DateTimeKind.Utc } utc => TimeZoneInfo.ConvertTimeFromUtc(utc, zone),
        DateTime time => time,
        DateTimeOffset offset => TimeZoneInfo.ConvertTime(offset, zone).DateTime,
        _ => throw new InvalidOperationException($"日期的值要是 DateOnly、DateTime 或 DateTimeOffset，不是 {value.GetType()}。"),
    };

    /// <summary>
    /// 墙上时间 → <paramref name="type"/>（DateOnly、DateTime、DateTimeOffset）：DateTimeOffset 带上时区的偏移；
    /// <paramref name="utc"/> 时（原来的值是 UTC 的 DateTime）换回 UTC。
    /// </summary>
    public static object FromWall(DateTime wall, Type type, TimeZoneInfo zone, bool utc) => type switch
    {
        _ when type == typeof(DateOnly) => DateOnly.FromDateTime(wall),
        _ when type == typeof(DateTimeOffset) => new DateTimeOffset(wall, zone.GetUtcOffset(wall)),
        _ when utc => TimeZoneInfo.ConvertTimeToUtc(wall, zone),
        _ => wall,
    };

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
