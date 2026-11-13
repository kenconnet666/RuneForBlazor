using Rune.UI;

namespace Rune.Tests.Dates;

// 日历的文字与状态：文字的期望值来自 Node 的 Intl（weekday narrow、month short、year、year+month long、weekInfo.firstDay）
public class CalendarTests
{
    static readonly Lang De = Lang.EnUs with { Code = "de-DE" };
    static readonly Lang Ja = Lang.EnUs with { Code = "ja-JP" };

    [Fact]
    public void 星期名_月份名_标题同Intl()
    {
        Assert.Equal("一二三四五六日", string.Concat(CalendarText.Weekdays(DayOfWeek.Monday, Lang.ZhCn).Select(day => day.Narrow)));
        Assert.Equal("SMTWTFS", string.Concat(CalendarText.Weekdays(DayOfWeek.Sunday, Lang.EnUs).Select(day => day.Narrow)));
        Assert.Equal("日月火水木金土", string.Concat(CalendarText.Weekdays(DayOfWeek.Sunday, Ja).Select(day => day.Narrow)));
        Assert.Equal("Sunday", CalendarText.Weekdays(DayOfWeek.Sunday, Lang.EnUs)[0].Long);
        Assert.Equal(("1月", "Jan"), (CalendarText.MonthShort(1, Lang.ZhCn), CalendarText.MonthShort(1, Lang.EnUs)));
        Assert.Equal(("2026年1月", "January 2026", "Januar 2026"), (CalendarText.YearMonth(new(2026, 1, 5), Lang.ZhCn), CalendarText.YearMonth(new(2026, 1, 5), Lang.EnUs), CalendarText.YearMonth(new(2026, 1, 5), De)));
        Assert.Equal(("2026年", "2026", "2026年"), (CalendarText.Year(2026, Lang.ZhCn), CalendarText.Year(2026, Lang.EnUs), CalendarText.Year(2026, Ja)));
        Assert.Equal("2020年 – 2029年", CalendarText.Title(new(2020, 1, 1), DateUnit.Year, Lang.ZhCn));
    }

    [Fact]
    public void 一周的第一天按地区惯例_语言包可以指定()
    {
        Assert.Equal(DayOfWeek.Monday, LangFormat.FirstDayOfWeek(Lang.ZhCn));
        Assert.Equal(DayOfWeek.Sunday, LangFormat.FirstDayOfWeek(Lang.EnUs));
        Assert.Equal(DayOfWeek.Sunday, LangFormat.FirstDayOfWeek(Ja));
        Assert.Equal(DayOfWeek.Monday, LangFormat.FirstDayOfWeek(Lang.EnUs with { Code = "en-GB" }));
        Assert.Equal(DayOfWeek.Saturday, LangFormat.FirstDayOfWeek(Lang.EnUs with { Code = "ar-EG" }));
        Assert.Equal(DayOfWeek.Sunday, LangFormat.FirstDayOfWeek(Lang.ZhCn with { FirstDayOfWeek = 0 }));
    }

    [Fact]
    public void 月份网格补齐六行_周数周一开始按ISO()
    {
        var weeks = CalendarText.MonthGrid(new(2026, 10, 1), DayOfWeek.Monday, fixedWeeks: true);
        Assert.Equal(6, weeks.Count);
        Assert.Equal(new DateOnly(2026, 9, 28), weeks[0][0]);
        Assert.Equal(5, CalendarText.MonthGrid(new(2026, 10, 1), DayOfWeek.Monday, fixedWeeks: false).Count);
        // 2026-01-01 是周四：ISO 第 1 周；周日开始时含 1 月 1 日的那周是第 1 周
        Assert.Equal(1, CalendarText.WeekOfYear(new(2026, 1, 1), DayOfWeek.Monday));
        Assert.Equal(53, CalendarText.WeekOfYear(new(2020, 12, 31), DayOfWeek.Monday));
        Assert.Equal(1, CalendarText.WeekOfYear(new(2025, 12, 28), DayOfWeek.Sunday));
    }

    static CalendarState State(DateOnly initial, DateOnly? min = null, DateOnly? max = null) =>
        new(initial, DateUnit.Day, date => date) { Min = min, Max = max, Today = initial };

    [Fact]
    public void 焦点移出当前页时翻页_夹到可选范围()
    {
        var state = State(new(2026, 10, 30), max: new(2026, 11, 15));
        state.Focus(new(2026, 11, 2));
        Assert.Equal((new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 1)), (state.Focused, state.Page));
        state.Focus(new(2026, 12, 2));
        Assert.Equal(new DateOnly(2026, 11, 15), state.Focused);
        Assert.False(state.CanTurnPage(1));
    }

    [Fact]
    public void 月份视图点一格进到那个月_焦点保留原来那天()
    {
        var state = State(new(2026, 1, 31));
        state.SetView(DateUnit.Month);
        Assert.Null(state.Activate(new(2026, 2, 1)));
        Assert.Equal((DateUnit.Day, new DateOnly(2026, 2, 28)), (state.View, state.Focused));
    }

    [Fact]
    public void 选的单位换了_视图回到新的单位()
    {
        // 按日改成按月：停在月份网格（比“月”更细）上时再点一格没有下一级可进
        var state = State(new(2026, 5, 20));
        state.Unit = DateUnit.Month;
        Assert.Equal((DateUnit.Month, new DateOnly(2026, 1, 1)), (state.View, state.Page));
        // 单位没变时不动视图（每次渲染前都会写一遍）
        state.SetView(DateUnit.Year);
        state.Unit = DateUnit.Month;
        Assert.Equal(DateUnit.Year, state.View);
    }

    [Fact]
    public void 区间选到一半_越过不可选日期的一侧不能选()
    {
        var booked = new DateOnly(2026, 10, 10);
        var state = State(new(2026, 10, 1));
        state.Range = true;
        state.IsUnavailable = date => date == booked;
        Assert.Null(state.Activate(new(2026, 10, 5)));
        Assert.True(state.Cell(new(2026, 10, 12), new(2026, 10, 1)).Disabled);
        Assert.False(state.Cell(new(2026, 10, 9), new(2026, 10, 1)).Disabled);
        Assert.Equal([new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 5)], state.Activate(new(2026, 10, 2)));
    }
}
