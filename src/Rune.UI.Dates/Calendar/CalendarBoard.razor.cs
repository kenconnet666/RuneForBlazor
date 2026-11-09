using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>整页月历的一天（DayContent 拿到它）。</summary>
/// <param name="Date">这一天。</param>
/// <param name="Today">今天。</param>
/// <param name="Outside">补齐的上个月、下个月的日子。</param>
/// <param name="Selected">选中的那一天。</param>
/// <param name="Weekend">周六、周日。</param>
/// <param name="Disabled">不可选（范围外、不可选的日期）。</param>
public readonly record struct CalendarBoardDay(DateOnly Date, bool Today, bool Outside, bool Selected, bool Weekend, bool Disabled);

/// <summary>整页的月历：每天一个大格子，放日程、排班、节假日（用法见 CalendarBoard.razor 顶部）。</summary>
/// <typeparam name="TItem">按天放的数据（日程、排班）。</typeparam>
public partial class CalendarBoard<TItem>
{
    static readonly Attrs FlipIcon = new() { ["FlipInRtl"] = true };
    static readonly Attrs HiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };
    static readonly Attrs DayHeadLayer = new() { Sx = CalendarBoardStyles.DayHead };

    readonly string TitleId = UniqueId.Next("calendar-board-title");
    CalendarState state = null!;
    DateOnly? shownMonth;

    /// <summary>选中的那一天（<c>@bind-Value</c>）；没有时 null。</summary>
    [Parameter] public DateOnly? Value { get; set; }

    /// <summary>选中的那一天变了。</summary>
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }

    /// <summary>显示哪个月（那个月的 1 日，<c>@bind-Month</c> 时翻页写回）；不写时从选中的那天（或今天）开始。</summary>
    [Parameter] public DateOnly? Month { get; set; }

    /// <summary>翻到了另一个月。</summary>
    [Parameter] public EventCallback<DateOnly?> MonthChanged { get; set; }

    /// <summary>一周的第一天；不写时取语言包的，再不写按地区惯例。</summary>
    [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>最早可选的日期。</summary>
    [Parameter] public DateOnly? Min { get; set; }

    /// <summary>最晚可选的日期。</summary>
    [Parameter] public DateOnly? Max { get; set; }

    /// <summary>不可选的日期。</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>按天放的数据：日程、排班、提醒。</summary>
    [Parameter] public IEnumerable<TItem> Events { get; set; } = [];

    /// <summary>数据在哪一天。</summary>
    [Parameter] public Func<TItem, DateOnly>? EventDate { get; set; }

    /// <summary>跨天的数据在哪一天结束（含）；不写时只在 EventDate 那一天。</summary>
    [Parameter] public Func<TItem, DateOnly>? EventEnd { get; set; }

    /// <summary>数据的 key；不写时用它在那天的位置。</summary>
    [Parameter] public Func<TItem, string>? EventKey { get; set; }

    /// <summary>默认显示的文字。</summary>
    [Parameter] public Func<TItem, string>? EventLabel { get; set; }

    /// <summary>默认显示的色调，默认 Accent。</summary>
    [Parameter] public Func<TItem, Tone>? EventTone { get; set; }

    /// <summary>自定义一条数据的显示。</summary>
    [Parameter] public RenderFragment<TItem>? EventContent { get; set; }

    /// <summary>每天最多显示几条，其余收成“还有 N 项”，默认 3。</summary>
    [Parameter] public int MaxEvents { get; set; } = 3;

    /// <summary>日期旁边的小字（节假日、“休”“班”、农历）；没有时 null。</summary>
    [Parameter] public Func<DateOnly, string?>? DayNote { get; set; }

    /// <summary>往格子里加别的内容（排班、价格），放在日程下面。</summary>
    [Parameter] public RenderFragment<CalendarBoardDay>? DayContent { get; set; }

    /// <summary>页头右边的内容（筛选、视图切换）。</summary>
    [Parameter] public RenderFragment? Toolbar { get; set; }

    /// <summary>只读：能翻页、移动焦点，不能选。</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>“今天”按钮（Button）的部件属性。</summary>
    [Parameter] public Attrs? PropsToday { get; set; }

    /// <summary>每格（td）的部件属性。</summary>
    [Parameter] public Attrs? PropsCell { get; set; }

    /// <summary>页头（标题、今天、翻页）的部件属性。</summary>
    [Parameter] public Attrs? PropsHeader { get; set; }

    /// <summary>标题（年月）的部件属性。</summary>
    [Parameter] public Attrs? PropsTitle { get; set; }

    /// <summary>上一月按钮的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsPrevious { get; set; }

    /// <summary>下一月按钮的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsNext { get; set; }

    /// <summary>月历表格（table）的部件属性。</summary>
    [Parameter] public Attrs? PropsGrid { get; set; }

    /// <summary>星期标题（th）的部件属性。</summary>
    [Parameter] public Attrs? PropsWeekday { get; set; }

    /// <summary>格里日期按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsDayButton { get; set; }

    /// <summary>日期旁备注（农历、节日）的部件属性。</summary>
    [Parameter] public Attrs? PropsNote { get; set; }

    /// <summary>格里事件列表（ul）的部件属性。</summary>
    [Parameter] public Attrs? PropsEvents { get; set; }

    /// <summary>每个事件的部件属性。</summary>
    [Parameter] public Attrs? PropsEvent { get; set; }

    /// <summary>“还有 n 项”按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsMore { get; set; }

    DayOfWeek FirstDay => FirstDayOfWeek ?? LangFormat.FirstDayOfWeek(Config.Lang);

    DateOnly Today => LocalToday();

    static Attrs ToneLayer(Tone tone) => new() { Sx = SharedStyles.StatusColors(StatusVariant.Soft, tone) };

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var today = Today;
        state ??= new CalendarState(Value ?? Month ?? today, DateUnit.Day, date => Min is { } min && date < min ? min : Max is { } max && date > max ? max : date);
        state.Unit = DateUnit.Day;
        state.Min = Min;
        state.Max = Max;
        state.IsUnavailable = IsDateUnavailable;
        state.ReadOnly = ReadOnly;
        state.Today = today;
        state.Selected = Value is { } day ? [day] : [];
        // 父级改了 Month：翻过去
        if (Month is { } month && Month != shownMonth && (month.Year, month.Month) != (state.Page.Year, state.Page.Month)) state.Focus(month);
        shownMonth = Month;
    }

    /// <summary>按天分好的数据：跨天的每一天都有（最多铺 366 天，数据错了也不会卡住）。</summary>
    Dictionary<DateOnly, List<TItem>> ByDay()
    {
        var map = new Dictionary<DateOnly, List<TItem>>();
        if (EventDate is null) return map;
        foreach (var item in Events)
        {
            var first = EventDate(item);
            var last = EventEnd?.Invoke(item) ?? first;
            for (var (day, count) = (first, 0); count < 366; day = day.AddDays(1), count++)
            {
                if (!map.TryGetValue(day, out var list)) map[day] = list = [];
                list.Add(item);
                if (day >= last) break;
            }
        }
        return map;
    }

    Attrs RootManaged => new()
    {
        Role = "group",
        ["data-rune-calendar"] = true,
        ["data-view"] = "day",
        ["data-focused-date"] = DateValues.Iso(state.Focused),
        ["data-first-day"] = ((int)FirstDay).ToString(CultureInfo.InvariantCulture),
    };

    // 翻页后 Month 写回
    async Task SyncMonthAsync()
    {
        var page = state.Page;
        if (Month is { } month && (month.Year, month.Month) == (page.Year, page.Month)) return;
        shownMonth = page;
        await MonthChanged.InvokeAsync(page);
    }

    async Task TurnAsync(int direction)
    {
        state.TurnPage(direction);
        await SyncMonthAsync();
    }

    async Task TodayAsync()
    {
        state.Focus(Today);
        await SyncMonthAsync();
    }

    // js/calendar.js 交来的：焦点移到某天（focus）、点了日期、格子的空白处、“还有 N 项”（activate）
    async Task OnCommandAsync(CommandEventArgs args)
    {
        if (!DateValues.TryParseIso(args.Text, out var date)) return;
        if (args.Name == "focus") state.Focus(date);
        else if (args.Name == "activate" && state.Activate(date) is [var picked, ..])
        {
            Value = picked;
            await ValueChanged.InvokeAsync(picked);
        }
        await SyncMonthAsync();
    }
}
