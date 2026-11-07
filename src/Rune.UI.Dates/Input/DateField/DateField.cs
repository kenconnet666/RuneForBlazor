using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 日期输入框（同 vue-tsx-pro 的 DateField）：分段输入年、月、日（不带弹层），段的顺序按语言（zh-CN 年/月/日，en-US 月/日/年）。
/// 要弹出日历用 DatePicker。
/// <code>
/// &lt;Field Label="生日"&gt;&lt;DateField @bind-Value="birthday" Max="today" /&gt;&lt;/Field&gt;
/// &lt;DateField @bind-Value="startsAt" Granularity="DateGranularity.Minute" aria-label="开始时间" /&gt;
/// </code>
/// 值是 <see cref="DateOnly"/>、<see cref="DateTime"/> 或 <see cref="DateTimeOffset"/>（可空的空着时为 null）；所有段都填了才写回，
/// 删掉一段写回 null（不可空的类型不变）。年、月、季度、周写回那一段的第一天；只显示日期时，原来的时分秒不丢。
/// DateTimeOffset 按语言包的时区显示与写回。值在 Min–Max 之外、是不可选的日期或时刻时外壳显示出错，值照样写回，由表单校验决定能不能提交。
/// 键盘：数字逐段输入（满了自动跳段），↑↓ 加减，Page Up / Down 大步，Home / End 最小 / 最大，Backspace 删一位（删空回到上一段），←→ 与 Tab 换段。
/// </summary>
/// <typeparam name="TValue">值的类型：DateOnly、DateTime、DateTimeOffset 或它们的可空类型。</typeparam>
public class DateField<TValue> : SegmentedField<TValue>
{
    /// <summary>精确到哪一段，默认到日；到时、分、秒时是日期加时间。</summary>
    [Parameter] public DateGranularity Granularity { get; set; } = DateGranularity.Day;

    /// <summary>最早的日期：DateOnly（那一整天都可选），或带时间的 DateTime、DateTimeOffset（精确到时分秒时同一天里再比时间）。</summary>
    [Parameter] public object? Min { get; set; }

    /// <summary>最晚的日期，写法同 <see cref="Min"/>。</summary>
    [Parameter] public object? Max { get; set; }

    /// <summary>不可选的日期（周末、节假日）：键入时显示出错。</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>带时间时不可选的时刻（午休、非营业时间）；参数是时间与那一天，周末与平日可以不一样。</summary>
    [Parameter] public Func<TimeOnly, DateOnly, bool>? IsTimeUnavailable { get; set; }

    private protected bool WithTime => Granularity is DateGranularity.Hour or DateGranularity.Minute or DateGranularity.Second;

    private protected override DateSegmentType[] Fields => DateSegments.FieldsOf(Granularity);

    private protected override string? CurrentIso => Wall(CurrentValue) is { } wall ? IsoOf(wall) : null;

    // 值 → 语言时区里的日期时间（墙上时间）
    private protected DateTime? Wall(object? value) => value switch
    {
        null => null,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        DateTime { Kind: DateTimeKind.Utc } utc => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone),
        DateTime time => time,
        DateTimeOffset offset => TimeZoneInfo.ConvertTime(offset, Zone).DateTime,
        _ => throw new InvalidOperationException($"DateField 的值要是 DateOnly、DateTime 或 DateTimeOffset，不是 {value.GetType()}。"),
    };

    private protected string IsoOf(DateTime wall) => Granularity switch
    {
        DateGranularity.Hour => wall.ToString("yyyy-MM-ddTHH:00", CultureInfo.InvariantCulture),
        DateGranularity.Minute => wall.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
        DateGranularity.Second => wall.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        _ => DateSegments.PeriodIso(DateOnly.FromDateTime(wall), Granularity),
    };

    // 填好的段 → 那一段的第一天（带时间时加上时分秒）
    DateTime? WallOf(string iso)
    {
        if (DateSegments.FromIso(iso, Fields) is not { } parts || DateSegments.PeriodStart(parts, Granularity) is not { } day) return null;
        var date = day.ToDateTime(TimeOnly.MinValue);
        return WithTime ? date.Add(new TimeSpan(parts.Hour ?? 0, parts.Minute ?? 0, parts.Second ?? 0)) : date;
    }

    private protected override bool TryFromIso(string? iso, out TValue value)
    {
        value = default!;
        if (iso is null) return default(TValue) is null;
        if (WallOf(iso) is not { } wall) return false;
        // 只显示日期时，原来的时分秒不丢（同 vue 的 like.set）
        if (!WithTime && Wall(CurrentValue) is { } previous) wall = wall.Date + previous.TimeOfDay;
        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        object result = type switch
        {
            _ when type == typeof(DateOnly) => DateOnly.FromDateTime(wall),
            _ when type == typeof(DateTimeOffset) => new DateTimeOffset(wall, Zone.GetUtcOffset(wall)),
            _ when CurrentValue is DateTime { Kind: DateTimeKind.Utc } => TimeZoneInfo.ConvertTimeToUtc(wall, Zone),
            _ => wall,
        };
        value = (TValue)result;
        return true;
    }

    private protected override bool IsOutOfRange(string iso)
    {
        if (WallOf(iso) is not { } wall) return false;
        var start = DateOnly.FromDateTime(wall);
        var end = DateSegments.PeriodEnd(start, Granularity);
        if (Wall(Max) is { } max && start > DateOnly.FromDateTime(max)) return true;
        if (Wall(Min) is { } min && end < DateOnly.FromDateTime(min)) return true;
        var byDay = Granularity == DateGranularity.Day || WithTime;
        if (byDay && IsDateUnavailable?.Invoke(start) == true) return true;
        if (!WithTime) return false;
        // min / max 带时间时，同一天里再比时分秒
        if (Min is DateTime or DateTimeOffset && wall < Wall(Min)) return true;
        if (Max is DateTime or DateTimeOffset && wall > Wall(Max)) return true;
        return IsTimeUnavailable?.Invoke(TimeOnly.FromDateTime(wall), start) == true;
    }
}
