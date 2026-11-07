using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 时间输入框（同 vue-tsx-pro 的 TimeField）：分段输入时、分（可选秒），12 / 24 小时制跟随语言（zh-CN 24 小时，en-US 12 小时加 AM / PM），
/// 也可以用 HourCycle 指定。要从按步长列出的时间里点选用 TimePicker。
/// <code>
/// &lt;Field Label="提醒时间"&gt;&lt;TimeField @bind-Value="remindAt" /&gt;&lt;/Field&gt;
/// &lt;TimeField @bind-Value="at" Granularity="TimeGranularity.Second" HourCycle="HourCycle.H12" aria-label="打卡时间" /&gt;
/// </code>
/// 值是 <see cref="TimeOnly"/>（可空的空着时为 null），键盘同 DateField；12 小时制时在上午 / 下午那段打 a、p 切换。
/// </summary>
/// <typeparam name="TValue">值的类型：TimeOnly 或 TimeOnly?。</typeparam>
public class TimeField<TValue> : SegmentedField<TValue>
{
    /// <summary>精确到哪一段，默认到分。</summary>
    [Parameter] public TimeGranularity Granularity { get; set; } = TimeGranularity.Minute;

    /// <summary>最早的时间：早于它显示出错。</summary>
    [Parameter] public TimeOnly? Min { get; set; }

    /// <summary>最晚的时间：晚于它显示出错。</summary>
    [Parameter] public TimeOnly? Max { get; set; }

    /// <summary>不可选的时间（午休、已约满的时段）：键入时显示出错。</summary>
    [Parameter] public Func<TimeOnly, bool>? IsTimeUnavailable { get; set; }

    private protected override SegmentType[] Fields => DateSegments.FieldsOf(Granularity);

    private protected override string? CurrentIso => CurrentValue switch
    {
        null => null,
        TimeOnly time => time.ToString(Granularity switch
        {
            TimeGranularity.Hour => "HH:00",
            TimeGranularity.Second => "HH:mm:ss",
            _ => "HH:mm",
        }, CultureInfo.InvariantCulture),
        var other => throw new InvalidOperationException($"TimeField 的值要是 TimeOnly，不是 {other.GetType()}。"),
    };

    static TimeOnly? Parse(string iso, SegmentType[] fields) =>
        DateSegments.FromIso(iso, fields) is { Hour: { } hour } parts ? new TimeOnly(hour, parts.Minute ?? 0, parts.Second ?? 0) : null;

    private protected override bool TryFromIso(string? iso, out TValue value)
    {
        value = default!;
        if (iso is null) return default(TValue) is null;
        if (Parse(iso, Fields) is not { } time) return false;
        value = (TValue)(object)time;
        return true;
    }

    private protected override bool IsOutOfRange(string iso) =>
        Parse(iso, Fields) is { } time && (time < Min || time > Max || IsTimeUnavailable?.Invoke(time) == true);
}
