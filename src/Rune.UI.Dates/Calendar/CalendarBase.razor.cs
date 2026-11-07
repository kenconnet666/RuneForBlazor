using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>日历的界面：Calendar、RangeCalendar 的共同部分（页头、网格、月份与年份视图、页脚），不单独使用。</summary>
public abstract partial class CalendarBase
{
    static readonly Attrs FlipIcon = new() { ["FlipInRtl"] = true };
    static readonly Attrs MonthLayer = new() { Sx = CalendarStyles.Month };
    static readonly Attrs SpacerLayer = new() { Sx = CalendarStyles.Spacer };
    static readonly Attrs HiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };

    readonly string HintId = UniqueId.Next("calendar-hint");
    CalendarState state = null!;
    DateOnly? revealed;

    /// <summary>最早可选的日期。</summary>
    [Parameter] public DateOnly? Min { get; set; }

    /// <summary>最晚可选的日期。</summary>
    [Parameter] public DateOnly? Max { get; set; }

    /// <summary>不可选的日期（已订满、节假日）：仍能用方向键经过，读屏能听到，不能选。</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>选择的粒度：Day（默认）、Month（选月份）、Year（选年份）；选中写回那一段的第一天。</summary>
    [Parameter] public DateUnit Granularity { get; set; }

    /// <summary>并排显示几个月，默认 1。</summary>
    [Parameter] public int VisibleMonths { get; set; } = 1;

    /// <summary>没有已选日期时一开始显示哪天所在的月，默认今天。</summary>
    [Parameter] public DateOnly? DefaultFocused { get; set; }

    /// <summary>一周的第一天；不写时取语言包的，再不写按地区惯例（zh-CN 周一，en-US 周日）。</summary>
    [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>只读：能翻页、移动焦点，不能改选择。</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>不可用：不能翻页、不能选。</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>字号档位，格子随之缩放；不写时继承上下文字号。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>在每行前显示一年里的第几周。</summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>总是 6 行（翻页时高度不跳），默认是。</summary>
    [Parameter] public bool FixedWeeks { get; set; } = true;

    /// <summary>自定义日期格的内容（价格、标记点）；不写时是日期数字。读屏先念完整日期再念这里的文字：重复的日期数字可以加 aria-hidden。</summary>
    [Parameter] public RenderFragment<CalendarDay>? ChildContent { get; set; }

    /// <summary>页脚：今天、清除这类按钮。</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>页头的部件属性。</summary>
    [Parameter] public Attrs? PropsHeader { get; set; }

    /// <summary>标题按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsHeading { get; set; }

    /// <summary>上一页按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsPrevious { get; set; }

    /// <summary>下一页按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsNext { get; set; }

    /// <summary>并排月份那一层的部件属性。</summary>
    [Parameter] public Attrs? PropsMonths { get; set; }

    /// <summary>每张网格（table）的部件属性。</summary>
    [Parameter] public Attrs? PropsGrid { get; set; }

    /// <summary>星期表头的部件属性。</summary>
    [Parameter] public Attrs? PropsWeekday { get; set; }

    /// <summary>周数的部件属性。</summary>
    [Parameter] public Attrs? PropsWeekNumber { get; set; }

    /// <summary>每格（td）的部件属性。</summary>
    [Parameter] public Attrs? PropsCell { get; set; }

    /// <summary>每格里按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsCellButton { get; set; }

    /// <summary>页脚的部件属性。</summary>
    [Parameter] public Attrs? PropsFooter { get; set; }

    /// <summary>已选的日期：单选一个、区间是 [开始, 结束]、多选若干。</summary>
    private protected abstract IReadOnlyList<DateOnly> Selected { get; }

    /// <summary>选区间：先点开始、再点结束。</summary>
    private protected virtual bool Range => false;

    /// <summary>多选：每天独立切换。</summary>
    private protected virtual bool Multiple => false;

    /// <summary>区间里允许夹着不可选的日期。</summary>
    private protected virtual bool AllowsUnavailable => false;

    /// <summary>选了日期：单选 [日期]（再点已选的那天也会调用），区间 [开始, 结束]（已排好序），多选是新的列表。</summary>
    private protected abstract Task OnChangeAsync(IReadOnlyList<DateOnly> dates);

    /// <summary>已选的日期有不能选的（范围外、不可选，或区间里夹着不可选的日期）。</summary>
    protected bool Invalid => state.Invalid;

    DayOfWeek FirstDay => FirstDayOfWeek ?? LangFormat.FirstDayOfWeek(Config.Lang);

    // 今天：语言时区里的日期
    DateOnly Today => LocalToday();

    Attrs? SizeLayer => Size is { } size ? new Attrs { Sx = ScaleStyles.FontSize(size) } : null;

    static string ViewName(DateUnit view) => view.ToString().ToLowerInvariant();

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var today = Today;
        var first = Selected.Count > 0 ? Selected[0] : (DateOnly?)null;
        state ??= new CalendarState(first ?? DefaultFocused ?? today, Granularity, date => Min is { } min && date < min ? min : Max is { } max && date > max ? max : date);
        state.Range = Range;
        state.Multiple = Multiple;
        state.Unit = Granularity;
        state.VisibleMonths = Math.Max(1, VisibleMonths);
        state.Min = Min;
        state.Max = Max;
        state.IsUnavailable = IsDateUnavailable;
        state.AllowsUnavailableInRange = AllowsUnavailable;
        state.ReadOnly = ReadOnly || Disabled;
        state.Today = today;
        state.Selected = Selected;
        // 父级从外面改了值（快捷按钮、重置）且不在这一页：翻到它那里
        if (first != revealed)
        {
            if (revealed is not null || first is not null) state.Reveal(first);
            revealed = first;
        }
    }

    // 月份视图是这一年的各月，年份视图是所在的十年前后各补一年（共 12 年，排成 4 × 3）
    IEnumerable<DateOnly> PeriodCells(DateOnly page) => state.View == DateUnit.Month
        ? Enumerable.Range(1, 12).Select(month => new DateOnly(page.Year, month, 1))
        : Enumerable.Range(-1, 12).Select(offset => new DateOnly(page.Year + offset, 1, 1));

    Attrs RootManaged => new()
    {
        Role = "group",
        ["data-rune-calendar"] = true,
        ["data-view"] = ViewName(state.View),
        ["data-focused-date"] = DateValues.Iso(state.Focused),
        ["data-first-day"] = ((int)FirstDay).ToString(CultureInfo.InvariantCulture),
        ["data-anchor"] = state.Anchor is { } anchor ? DateValues.Iso(anchor) : null,
        ["data-disabled"] = Disabled ? true : null,
        ["data-invalid"] = state.Invalid ? true : null,
    };

    // JS 交来的：点了一格（activate）、焦点移到某天（focus）、Esc 取消选到一半的区间（cancel）
    async Task OnCommandAsync(CommandEventArgs args)
    {
        if (Disabled) return;
        if (args.Name == "cancel")
        {
            state.CancelRange();
            return;
        }
        if (!DateValues.TryParseIso(args.Text, out var date)) return;
        if (args.Name == "focus") state.Focus(date);
        else if (args.Name == "activate" && state.Activate(date) is { } picked) await OnChangeAsync(picked);
    }
}
