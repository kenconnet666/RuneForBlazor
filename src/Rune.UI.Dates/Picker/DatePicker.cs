using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>自定义的快捷选项（一周前、上个月末）。</summary>
/// <param name="Label">按钮上的文字。</param>
/// <param name="Date">这一天；每次打开时按当时算（跟着今天变）。选月份、季度、周时取它所在的那一段。</param>
public sealed record DatePreset(string Label, Func<DateOnly> Date);

/// <summary>
/// 日期选择器（同 vue-tsx-pro 的 DatePicker）：DateField 加一个打开日历的按钮，日历弹在输入框下方；可以直接键入，也可以点选。
/// <code>
/// &lt;Field Label="出发日期"&gt;&lt;DatePicker @bind-Value="date" Min="today" /&gt;&lt;/Field&gt;
/// &lt;DatePicker @bind-Value="month" Granularity="DateGranularity.Month" aria-label="账期" /&gt;
/// &lt;DatePicker @bind-Value="at" Granularity="DateGranularity.Minute" aria-label="开始时间" /&gt;
/// </code>
/// Granularity 决定值的形式（同 DateField）与弹出的选择：Year、Month 选年份、月份，Week 选周（日历按 ISO 周排、行首是周数，点哪天都是选它那一周），
/// Quarter 弹出季度面板（QuarterCalendar），其余选日期。日历下方有“今天”（按粒度是今年、本季度、本月、本周；超出范围时不可用），旁边可以放快捷选项。
/// 选好后关闭，焦点回到按钮；带时间时保留原来的时间（没有时是 00:00）。Esc、点外部关闭；分段里按 Alt + ↓ 打开（同原生 select）。
/// </summary>
/// <typeparam name="TValue">值的类型：DateOnly、DateTime、DateTimeOffset 或它们的可空类型。</typeparam>
public sealed class DatePicker<TValue> : DateField<TValue>
{
    internal readonly string PopoverId = UniqueId.Next("date-picker");
    bool open;

    [Inject] IServiceProvider Services { get; set; } = null!;

    /// <summary>快捷选项：日历旁边的一列按钮。</summary>
    [Parameter] public IReadOnlyList<DatePreset> Presets { get; set; } = [];

    /// <summary>日历下方的“今天”（按粒度是今年、本季度、本月、本周），默认显示。</summary>
    [Parameter] public bool ShowToday { get; set; } = true;

    /// <summary>并排显示几个月，默认 1。</summary>
    [Parameter] public int VisibleMonths { get; set; } = 1;

    /// <summary>弹出位置，默认下方、与起始边对齐；空间不够时翻到另一侧。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>打开按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsButton { get; set; }

    /// <summary>弹层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>弹层里日历（Calendar）的部件属性：可以写它的参数（ShowWeekNumbers、FirstDayOfWeek……）。</summary>
    [Parameter] public Attrs? PropsCalendar { get; set; }

    internal bool IsOpen => open;

    internal Lang Lang => Config.Lang;

    /// <summary>日历的粒度：年、月照搬，其余按日（季度用 QuarterCalendar）。</summary>
    internal DateUnit CalendarUnit => Granularity switch
    {
        DateGranularity.Year => DateUnit.Year,
        DateGranularity.Month => DateUnit.Month,
        _ => DateUnit.Day,
    };

    /// <summary>日历上选中的那一天（当前值所在的那一天）。</summary>
    internal DateOnly? CalendarValue => Wall(CurrentValue) is { } wall ? DateOnly.FromDateTime(wall) : null;

    internal DateOnly Today
    {
        get
        {
            var time = Services.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), Zone).DateTime);
        }
    }

    // 一天 → 它所在的那一段的 ISO（按粒度）；带时间时保留原来的时间
    string IsoOfDay(DateOnly day) => IsoOf(day.ToDateTime(TimeOnly.MinValue) + (WithTime && Wall(CurrentValue) is { } wall ? wall.TimeOfDay : TimeSpan.Zero));

    /// <summary>这一天（所在的那一段）能不能选。</summary>
    internal bool Selectable(DateOnly day) => !IsOutOfRange(IsoOfDay(day));

    /// <summary>当前值就是这一天所在的那一段。</summary>
    internal bool IsCurrent(DateOnly day) => CurrentIso is { } iso && iso == IsoOfDay(day);

    /// <summary>“今天”按钮的文字：按粒度是今年、本季度、本月、本周或今天。</summary>
    internal string TodayLabel => Granularity switch
    {
        DateGranularity.Year => Lang.DateRangePreset["thisYear"],
        DateGranularity.Quarter => Lang.DatePickerThisQuarter,
        DateGranularity.Month => Lang.DateRangePreset["thisMonth"],
        DateGranularity.Week => Lang.DateRangePreset["thisWeek"],
        _ => Lang.DateRangePreset["today"],
    };

    /// <summary>选了一天（日历、今天、快捷选项）：换成粒度的形式写回，关闭弹层。</summary>
    internal async Task PickAsync(DateOnly day)
    {
        if (!Editable) return;
        if (TryFromIso(IsoOfDay(day), out var value)) await WriteAsync(value);
        open = false;
    }

    internal void Toggled(OverlayToggleEventArgs e) => open = e.Open;

    private protected override RenderFragment Trigger => PickerParts.Trigger(BuiltInIcons.CalendarDays, Lang.DatePickerOpen, Editable, open, PopoverId, InputStyles.Of(Appearance, Config.Styles)[InputPart.Button], PropsButton);

    private protected override RenderFragment Popover => builder =>
    {
        builder.OpenComponent<DatePickerPopover<TValue>>(0);
        builder.AddComponentParameter(1, nameof(DatePickerPopover<TValue>.Picker), this);
        builder.CloseComponent();
    };

    internal string AnchorId => ShellId;
}
