namespace Rune.UI;

/// <summary>日历一格的状态（渲染与 data-* 用）。</summary>
readonly record struct CalendarCell(
    bool Selected, bool Disabled, bool Unavailable, bool Today, bool Focused, bool Outside,
    bool RangeStart, bool RangeEnd, bool InRange, bool Anchor);

/// <summary>
/// 日历的状态（同 vue-tsx-pro 的 calendar-state.ts，参考 React Aria 的 useCalendarState）：焦点日期、显示哪一页、单选 / 区间 / 多选。
/// 三种视图：Day（月份网格）、Month（一年的各月）、Year（一个十年）；Unit 是选择的粒度，在比它粗的视图里点一格是“进到这一格”。
/// 键盘在 JS 里算出要去的日期交给 <see cref="Focus"/>；区间选到一半时的悬停预览也在 JS 里画，这里只记开始（<see cref="Anchor"/>）。
/// </summary>
sealed class CalendarState
{
    public CalendarState(DateOnly initial, DateUnit unit, Func<DateOnly, DateOnly> clamp)
    {
        View = unit;
        Focused = clamp(initial);
        Page = PageOf(Focused, View);
    }

    // ───────── 每次渲染前由组件写入 ─────────

    public bool Range { get; set; }

    public bool Multiple { get; set; }

    public DateUnit Unit { get; set; }

    public int VisibleMonths { get; set; } = 1;

    public DateOnly? Min { get; set; }

    public DateOnly? Max { get; set; }

    public Func<DateOnly, bool>? IsUnavailable { get; set; }

    public bool AllowsUnavailableInRange { get; set; }

    public bool ReadOnly { get; set; }

    public DateOnly Today { get; set; }

    public IReadOnlyList<DateOnly> Selected { get; set; } = [];

    // ───────── 状态 ─────────

    /// <summary>当前视图。</summary>
    public DateUnit View { get; private set; }

    /// <summary>焦点日期：总是精确到天，切到月、年视图再回来时回到原来那天。</summary>
    public DateOnly Focused { get; private set; }

    /// <summary>当前页的第一格：Day 视图是第一个月的 1 日，Month 视图是这年 1 月 1 日，Year 视图是十年的第一年。</summary>
    public DateOnly Page { get; private set; }

    /// <summary>区间选到一半：已经点了的开始。</summary>
    public DateOnly? Anchor { get; private set; }

    /// <summary>Day 视图里并排的各月（每月 1 日）；其他视图只有 Page。</summary>
    public IEnumerable<DateOnly> Months => View == DateUnit.Day ? Enumerable.Range(0, VisibleMonths).Select(Page.AddMonths) : [Page];

    /// <summary>已选的区间（区间模式、两端都有时）。</summary>
    public DateRange<DateOnly>? SelectedRange => Range && Selected is [var start, var end, ..] ? DateRange.Of(start, end) : null;

    /// <summary>已选的日期有不能选的（范围外、不可选，或区间里夹着不可选的日期）。</summary>
    public bool Invalid =>
        Selected.Any(date => IsDisabled(date, Min, Max, Unit))
        || (SelectedRange is { } range && !AllowsUnavailableInRange && IsUnavailable is { } unavailable
            && Enumerable.Range(0, range.Days).Any(offset => unavailable(range.Start.AddDays(offset))));

    // ───────── 页 ─────────

    static DateOnly PageOf(DateOnly date, DateUnit view) => view switch
    {
        DateUnit.Year => new(date.Year / 10 * 10, 1, 1),
        DateUnit.Month => new(date.Year, 1, 1),
        _ => new(date.Year, date.Month, 1),
    };

    DateOnly Move(DateOnly date, int direction) => View switch
    {
        DateUnit.Year => date.AddYears(10 * direction),
        DateUnit.Month => date.AddYears(direction),
        _ => date.AddMonths(VisibleMonths * direction),
    };

    DateOnly PageEnd => Move(Page, 1).AddDays(-1);

    /// <summary>焦点移出当前页时翻页：往前移出时焦点所在的月排在最后，往后移出时排在最前（同 React Aria）。</summary>
    void FollowFocus()
    {
        if (Focused >= Page && Focused <= PageEnd) return;
        Page = Focused < Page && View == DateUnit.Day
            ? new DateOnly(Focused.Year, Focused.Month, 1).AddMonths(-(VisibleMonths - 1))
            : PageOf(Focused, View);
    }

    // ───────── 范围 ─────────

    static DateOnly Truncate(DateOnly date, DateUnit unit) => unit switch
    {
        DateUnit.Year => new(date.Year, 1, 1),
        DateUnit.Month => new(date.Year, date.Month, 1),
        _ => date,
    };

    static int Compare(DateOnly a, DateOnly b, DateUnit unit) => Truncate(a, unit).CompareTo(Truncate(b, unit));

    /// <summary>是不是同一天 / 同一月 / 同一年。</summary>
    public static bool Same(DateOnly? a, DateOnly? b, DateUnit unit) => a is { } x && b is { } y && Compare(x, y, unit) == 0;

    static bool IsOutOfRange(DateOnly date, DateOnly? min, DateOnly? max, DateUnit unit) =>
        (min is { } low && Compare(date, low, unit) < 0) || (max is { } high && Compare(date, high, unit) > 0);

    bool IsDisabled(DateOnly date, DateOnly? min, DateOnly? max, DateUnit unit) =>
        IsOutOfRange(date, min, max, unit) || (unit == DateUnit.Day && IsUnavailable?.Invoke(date) == true);

    /// <summary>把日期夹到 Min–Max 之间。</summary>
    public DateOnly Clamp(DateOnly date) => Min is { } min && date < min ? min : Max is { } max && date > max ? max : date;

    // 区间选到一半、不允许夹着不可选的日期时：开始两侧最近的不可选日期之内才能选（最多找一年）
    (DateOnly? Min, DateOnly? Max) Limits
    {
        get
        {
            if (Anchor is not { } start || IsUnavailable is not { } unavailable || AllowsUnavailableInRange) return (Min, Max);
            DateOnly? Bound(int step, DateOnly? edge)
            {
                var date = start;
                for (var offset = 0; offset < 366; offset++)
                {
                    var next = date.AddDays(step);
                    if (edge is { } limit && next.CompareTo(limit) * step > 0) return limit;
                    if (unavailable(next)) return date;
                    date = next;
                }
                return edge;
            }
            return (Bound(-1, Min), Bound(1, Max));
        }
    }

    // ───────── 操作 ─────────

    /// <summary>切换视图（不能比选择的粒度更细）。</summary>
    public void SetView(DateUnit view)
    {
        if (view < Unit) return;
        View = view;
        Page = PageOf(Focused, view);
    }

    /// <summary>移动焦点日期（夹到可选范围内），移出当前页时翻页。</summary>
    public void Focus(DateOnly date)
    {
        var (min, max) = Limits;
        Focused = IsOutOfRange(date, min, max, View) ? (min is { } low && date < low ? low : max is { } high && date > high ? high : date) : date;
        FollowFocus();
    }

    /// <summary>这个方向还能不能翻页（整页都在可选范围之外时不能）。</summary>
    public bool CanTurnPage(int direction) => direction < 0
        ? Min is not { } min || Page.AddDays(-1) >= min
        : Max is not { } max || Move(Page, 1) <= max;

    /// <summary>翻页：Day 视图一次翻并排的月数，Month 视图一年，Year 视图十年；焦点跟着走。</summary>
    public void TurnPage(int direction)
    {
        if (!CanTurnPage(direction)) return;
        Page = Move(Page, direction);
        Focused = Clamp(Move(Focused, direction));
        // 夹回范围后可能不在翻到的这页了：焦点落到这页里可选的第一天
        if (Focused < Page || Focused > PageEnd) Focused = Clamp(Page);
    }

    /// <summary>点了一格：当前视图是选择的粒度时选中（返回新的选择，没变时 null），否则进到这一格（年 → 月 → 日）。</summary>
    public IReadOnlyList<DateOnly>? Activate(DateOnly date)
    {
        if (View == Unit)
        {
            Focus(date);
            return Select(date);
        }
        // 进到下一级视图：焦点保留原来的月、日（这个月没有那天时取最后一天）
        var year = date.Year;
        var month = View == DateUnit.Year ? Focused.Month : date.Month;
        var next = new DateOnly(year, month, Math.Min(Focused.Day, DateTime.DaysInMonth(year, month)));
        View = View - 1;
        Focused = Clamp(next);
        Page = PageOf(Focused, View);
        return null;
    }

    IReadOnlyList<DateOnly>? Select(DateOnly date)
    {
        var (min, max) = Limits;
        if (ReadOnly || IsDisabled(date, min, max, Unit)) return null;
        if (Range)
        {
            if (Anchor is not { } start)
            {
                Anchor = date;
                return null;
            }
            Anchor = null;
            var picked = DateRange.Of(start, date);
            return [picked.Start, picked.End];
        }
        if (!Multiple) return [date];
        return Selected.Any(item => Same(item, date, Unit)) ? [.. Selected.Where(item => !Same(item, date, Unit))] : [.. Selected, date];
    }

    /// <summary>取消选到一半的区间（Esc）。</summary>
    public void CancelRange() => Anchor = null;

    /// <summary>父级从外面改了值（快捷按钮、重置）且不在这一页：翻到它那里。</summary>
    public void Reveal(DateOnly? date)
    {
        if (date is { } day && (day < Page || day > PageEnd)) Focus(day);
    }

    /// <summary>一格的状态；month 是这格所在的月份网格（判断是不是补齐的上下月）。</summary>
    public CalendarCell Cell(DateOnly date, DateOnly? month)
    {
        var view = View;
        var (min, max) = Limits;
        var range = Anchor is null ? SelectedRange : null;
        var inRange = range is { } r && Compare(date, r.Start, view) >= 0 && Compare(date, r.End, view) <= 0;
        return new(
            Selected: Range ? inRange : Selected.Any(item => Same(item, date, view)),
            Disabled: IsDisabled(date, min, max, view),
            Unavailable: view == DateUnit.Day && IsUnavailable?.Invoke(date) == true,
            Today: Same(date, Today, view),
            Focused: Same(date, Focused, view),
            Outside: view == DateUnit.Day ? month is { } m && !Same(date, m, DateUnit.Month) : view == DateUnit.Year && date.Year / 10 != Page.Year / 10,
            RangeStart: range is { } start && Same(date, start.Start, view),
            RangeEnd: range is { } end && Same(date, end.End, view),
            InRange: inRange,
            Anchor: Same(date, Anchor, view));
    }
}
