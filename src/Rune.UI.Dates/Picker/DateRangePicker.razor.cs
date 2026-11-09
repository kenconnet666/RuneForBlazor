using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>日期区间选择器：一个输入框里开始、结束两组分段，按钮弹出区间日历（用法见 DateRangePicker.razor 顶部）。</summary>
/// <typeparam name="TValue">两端的类型：DateOnly（按天、周、月、季度、年），带时间的粒度用 DateTime 或 DateTimeOffset。</typeparam>
public partial class DateRangePicker<TValue> where TValue : struct, IComparable<TValue>
{
    static readonly Attrs HiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };
    static readonly Attrs SeparatorLayer = new() { Sx = Css.Define(static () => new Style { FlexShrink = 0, PaddingInline = Space.Xs, Color = Colors.TextMuted }) };

    readonly string PopoverId = UniqueId.Next("date-range-picker");
    readonly string StartLabelId = UniqueId.Next("date-range-start");
    readonly string EndLabelId = UniqueId.Next("date-range-end");
    bool open;
    // 带时间时日历里选好了日期：关上后焦点去开始那组的“时”接着输入（同 vue-tsx-pro）
    bool editTime;
    // 两组各自填好的那一端（ISO 文字）：只填了一端时值是 null，但这一端还在
    string? startIso;
    string? endIso;
    DateRange<TValue>? shownValue;

    /// <summary>精确到哪一段：Day（默认）、Week、Month、Quarter、Year；Hour、Minute、Second 是日期时间区间（两端带时间，TValue 用 DateTime 或 DateTimeOffset）。</summary>
    [Parameter] public DateGranularity Granularity { get; set; } = DateGranularity.Day;

    /// <summary>最早可选的日期。</summary>
    [Parameter] public DateOnly? Min { get; set; }

    /// <summary>最晚可选的日期。</summary>
    [Parameter] public DateOnly? Max { get; set; }

    /// <summary>不可选的日期（已订满、节假日）。</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>区间里允许夹着不可选的日期。</summary>
    [Parameter] public bool AllowsUnavailableInRange { get; set; }

    /// <summary>日历旁边的快捷区间：内置的（最近 7 天、本月……按今天与语言的一周第一天算）可以直接写枚举值，也可以是自己的 <see cref="RangePreset"/>。</summary>
    [Parameter] public IReadOnlyList<RangePreset> Presets { get; set; } = [];

    /// <summary>并排显示几个月，默认 2。</summary>
    [Parameter] public int VisibleMonths { get; set; } = 2;

    /// <summary>弹出位置，默认下方、与起始边对齐；空间不够时翻到另一侧。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>有值时显示清空按钮。</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>清空按钮的名称，默认取语言包的“清空”。</summary>
    [Parameter] public string? ClearLabel { get; set; }

    /// <summary>前置图标。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>表单提交时开始日期的 name。</summary>
    [Parameter] public string? StartName { get; set; }

    /// <summary>表单提交时结束日期的 name。</summary>
    [Parameter] public string? EndName { get; set; }

    /// <summary>前置图标的部件属性。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>开始那组分段的部件属性。</summary>
    [Parameter] public Attrs? PropsStart { get; set; }

    /// <summary>结束那组分段的部件属性。</summary>
    [Parameter] public Attrs? PropsEnd { get; set; }

    /// <summary>两组中间的“–”的部件属性。</summary>
    [Parameter] public Attrs? PropsSeparator { get; set; }

    /// <summary>每段的部件属性。</summary>
    [Parameter] public Attrs? PropsSegment { get; set; }

    /// <summary>分隔符的部件属性。</summary>
    [Parameter] public Attrs? PropsLiteral { get; set; }

    /// <summary>清空按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsClear { get; set; }

    /// <summary>打开按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsButton { get; set; }

    /// <summary>弹层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>快捷选项一栏的部件属性。</summary>
    [Parameter] public Attrs? PropsPresets { get; set; }

    /// <summary>每个快捷选项（Button）的部件属性。</summary>
    [Parameter] public Attrs? PropsPreset { get; set; }

    /// <summary>弹层里区间日历（RangeCalendar）的部件属性。</summary>
    [Parameter] public Attrs? PropsCalendar { get; set; }

    DateUnit CalendarUnit => Granularity switch
    {
        DateGranularity.Year => DateUnit.Year,
        DateGranularity.Month => DateUnit.Month,
        _ => DateUnit.Day,
    };

    EventCallback<DateRange<DateOnly>?> QuarterPicked => EventCallback.Factory.Create<DateRange<DateOnly>?>(this, range => range is { } picked ? PickAsync(picked) : Task.CompletedTask);

    TimeZoneInfo Zone => LangFormat.ZoneOf(Config.Lang);

    bool WithTime => DateSegments.HasTime(Granularity);

    DateOnly Today => LocalToday(Zone);

    // 日历上的区间（按天）：两端所在的那一天
    DateRange<DateOnly>? CalendarValue => CurrentValue is { } range && DateValues.Wall(range.Start, Zone) is { } start && DateValues.Wall(range.End, Zone) is { } end
        ? new(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end))
        : null;

    string? IsoOf(TValue value) => DateValues.Wall(value, Zone) is { } wall ? DateSegments.WallIso(wall, Granularity) : null;

    // 墙上时间 → 一端的值；原来的值是 UTC 的 DateTime 时仍写回 UTC
    TValue ValueOf(DateTime wall) => (TValue)DateValues.FromWall(wall, typeof(TValue), Zone, CurrentValue?.Start is DateTime { Kind: DateTimeKind.Utc });

    // 带时间时，结束那天默认到最后一刻（同 vue-tsx-pro）：按小时是 23:00，按分是 23:59，按秒是 23:59:59
    TimeSpan EndOfDay => Granularity switch
    {
        DateGranularity.Hour => new(23, 0, 0),
        DateGranularity.Second => new(23, 59, 59),
        _ => new(23, 59, 0),
    };

    static string? Join(string? a, string b) => a is null ? b : $"{a} {b}";

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // 值从外面变了（或刚写回）：两组跟着它；没变时留着只填了一端的那一组
        if (CurrentValue == shownValue && (startIso is not null || endIso is not null || CurrentValue is null)) return;
        shownValue = CurrentValue;
        startIso = CurrentValue is { } range ? IsoOf(range.Start) : null;
        endIso = CurrentValue is { } r ? IsoOf(r.End) : null;
    }

    // 一组的 ISO 文字 → 一端的值：开始是那一段的第一天（带时间时加上时分秒），结束是最后一天
    TValue? EndOf(string? iso, bool end)
    {
        if (DateSegments.WallOf(iso, Granularity) is not { } wall) return null;
        if (end && !WithTime) wall = DateSegments.PeriodEnd(DateOnly.FromDateTime(wall), Granularity).ToDateTime(TimeOnly.MinValue);
        return ValueOf(wall);
    }

    Task StartChangedAsync(string? iso)
    {
        startIso = iso;
        return WriteAsync();
    }

    Task EndChangedAsync(string? iso)
    {
        endIso = iso;
        return WriteAsync();
    }

    // 两端都填好才写回；删掉了一段写回 null
    async Task WriteAsync()
    {
        DateRange<TValue>? next = EndOf(startIso, false) is { } start && EndOf(endIso, true) is { } end ? new DateRange<TValue>(start, end) : null;
        if (next == CurrentValue) return;
        shownValue = next;
        await SetValueAsync(next);
    }

    /// <summary>
    /// 选了一个区间（日历、快捷选项，按天）：写回，关闭弹层。带时间时两端保留原来的时间（没有时开始是 0 点、结束是那天的最后一刻），
    /// 之后在分段里改时间。
    /// </summary>
    async Task PickAsync(DateRange<DateOnly> days)
    {
        if (IsDisabled || IsReadOnly) return;
        var start = days.Start.ToDateTime(TimeOnly.MinValue);
        var end = days.End.ToDateTime(TimeOnly.MinValue);
        if (WithTime)
        {
            start += CurrentValue is { } before && DateValues.Wall(before.Start, Zone) is { } a ? a.TimeOfDay : TimeSpan.Zero;
            end += CurrentValue is { } previous && DateValues.Wall(previous.End, Zone) is { } b ? b.TimeOfDay : EndOfDay;
        }
        var range = new DateRange<TValue>(ValueOf(start), ValueOf(end));
        startIso = IsoOf(range.Start);
        endIso = IsoOf(range.End);
        shownValue = range;
        await SetValueAsync(range);
        editTime = WithTime;
        open = false;
    }

    /// <summary>区间能不能选：两端在范围内；不允许夹着不可选的日期时中间没有。</summary>
    bool Selectable(DateRange<DateOnly> range) =>
        !(Min is { } min && range.Start < min) && !(Max is { } max && range.End > max)
        && (AllowsUnavailableInRange || IsDateUnavailable is not { } unavailable || !Enumerable.Range(0, range.Days).Any(offset => unavailable(range.Start.AddDays(offset))));

    // 结束早于开始、超出范围、夹着不可选的日期
    bool OutOfRange => CurrentValue is { } range && (range.End.CompareTo(range.Start) < 0 || CalendarValue is { } days && !Selectable(days));

    Attrs ShellManaged(string? labelled, bool invalid) => new()
    {
        Id = ControlId,
        Role = "group",
        AriaLabelledBy = labelled,
        AriaDisabled = IsDisabled ? true : null,
        ["data-rune-control"] = true,
        ["data-invalid"] = invalid ? true : null,
        ["data-disabled"] = IsDisabled ? true : null,
    };

    Attrs PopoverManaged => PickerParts.Popover(PopoverId, Config.Lang.DatePickerOpen, Placement, open, ControlId, false,
        EventCallback.Factory.Create<OverlayToggleEventArgs>(this, (OverlayToggleEventArgs e) => (open, editTime) = (e.Open, e.Open && editTime)),
        editTime ? $"{ControlId}-start-hour" : null);
}
