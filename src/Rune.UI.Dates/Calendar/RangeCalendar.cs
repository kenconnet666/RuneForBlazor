using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 区间日历（同 vue-tsx-pro 的 RangeCalendar）：先点开始、再点结束，中间悬停时预览；酒店的入住与离店、报表的起止日期。
/// <code>&lt;RangeCalendar @bind-Value="stay" VisibleMonths="2" Min="today" IsDateUnavailable="Booked" /&gt;</code>
/// 值是 <see cref="DateRange"/>（两端都含）；反着点也会排好序。默认区间里不能夹着不可选的日期（订满的那天）：选了开始之后，
/// 越过不可选日期的那一侧都不能再选；允许时写 AllowsUnavailableInRange。选到一半按 Esc 取消。
/// Granularity 是 Month / Year 时按月、按年选，写回的区间是整月、整年：开始那月的 1 日到结束那月的最后一天。键盘与部件同 Calendar。
/// </summary>
public sealed class RangeCalendar : CalendarBase
{
    DateRange? value;

    /// <summary>选中的区间（<c>@bind-Value</c>）；没有时 null。</summary>
    [Parameter] public DateRange? Value { get; set; }

    /// <summary>选中的区间变了。</summary>
    [Parameter] public EventCallback<DateRange?> ValueChanged { get; set; }

    /// <summary>区间里允许夹着不可选的日期。</summary>
    [Parameter] public bool AllowsUnavailableInRange { get; set; }

    /// <summary>选完了一个区间（点了结束）：日期选择器据此关闭弹层。</summary>
    [Parameter] public EventCallback<DateRange> OnPick { get; set; }

    private protected override bool Range => true;

    private protected override bool AllowsUnavailable => AllowsUnavailableInRange;

    private protected override IReadOnlyList<DateOnly> Selected => value is { } range ? [range.Start, range.End] : [];

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        value = Value;
        base.OnParametersSet();
    }

    private protected override async Task OnChangeAsync(IReadOnlyList<DateOnly> dates)
    {
        // 按月、按年选时，结束扩到那一月、那一年的最后一天
        var end = Granularity switch
        {
            DateUnit.Year => new DateOnly(dates[1].Year, 12, 31),
            DateUnit.Month => new DateOnly(dates[1].Year, dates[1].Month, DateTime.DaysInMonth(dates[1].Year, dates[1].Month)),
            _ => dates[1],
        };
        var range = new DateRange(dates[0], end);
        value = range;
        await ValueChanged.InvokeAsync(range);
        await OnPick.InvokeAsync(range);
    }
}
