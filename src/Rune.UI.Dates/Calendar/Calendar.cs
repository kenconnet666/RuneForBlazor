using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 日历（同 vue-tsx-pro 的 Calendar，WAI-ARIA 的日期网格）：选一天或多天，可以并排几个月、显示周数、自定义日期格、切到月份与年份视图。
/// <code>
/// &lt;Calendar @bind-Value="date" Min="today" aria-label="出发日期" /&gt;
/// &lt;Calendar SelectionMode="SelectionMode.Multiple" @bind-Values="days" VisibleMonths="2" /&gt;
/// &lt;Calendar @bind-Value="day" Context="cell"&gt;&lt;span aria-hidden="true"&gt;@cell.Date.Day&lt;/span&gt;&lt;small&gt;¥399&lt;/small&gt;&lt;/Calendar&gt;
/// </code>
/// 月份名、星期名、读屏念的完整日期按当前语言；一周从哪天开始按语言（FirstDayOfWeek 可以覆盖）。选区间用 RangeCalendar；
/// 选月份、年份用 Granularity。键盘：←→ 一天、↑↓ 一周，Page Up / Down 一个月、加 Shift 一年，Home / End 到这周两端，Enter / 空格选中。
/// </summary>
public sealed class Calendar : CalendarBase
{
    DateOnly? value;
    IReadOnlyList<DateOnly> values = [];

    /// <summary>单选（默认，值是 Value）或多选（值是 Values）。</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; }

    /// <summary>选中的日期（<c>@bind-Value</c>）；没有时 null。</summary>
    [Parameter] public DateOnly? Value { get; set; }

    /// <summary>选中的日期变了。</summary>
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }

    /// <summary>多选时选中的日期（<c>@bind-Values</c>）。</summary>
    [Parameter] public IReadOnlyList<DateOnly>? Values { get; set; }

    /// <summary>多选时选择变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<DateOnly>> ValuesChanged { get; set; }

    /// <summary>点选了一天（再点已选的那天也会调用）：日期选择器据此关闭弹层。</summary>
    [Parameter] public EventCallback<DateOnly> OnPick { get; set; }

    private protected override bool Multiple => SelectionMode == SelectionMode.Multiple;

    private protected override IReadOnlyList<DateOnly> Selected => Multiple ? values : value is { } day ? [day] : [];

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        value = Value;
        values = Values ?? [];
        base.OnParametersSet();
    }

    private protected override async Task OnChangeAsync(IReadOnlyList<DateOnly> dates)
    {
        if (Multiple)
        {
            values = dates;
            await ValuesChanged.InvokeAsync(dates);
            return;
        }
        value = dates[0];
        await ValueChanged.InvokeAsync(value);
        await OnPick.InvokeAsync(dates[0]);
    }
}
