using AngleSharp.Dom;
using Bunit;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>时限标记：按工作日算剩余、到期、超时，颜色随状态；工作日历取自配置（同 vue-tsx-pro 的 DeadlineBadge 测试）。</summary>
public class DeadlineBadgeTests : BunitContext
{
    // 2026-10-21 是周三
    static readonly DateOnly Today = new(2026, 10, 21);

    public DeadlineBadgeTests() => Services.AddRuneUI();

    // 看得见的文字（去掉给读屏的那一段）、状态、悬停说明、读屏念的全部文字
    static (string Text, string? State, string? Title, string Reader) Tag(IElement badge)
    {
        var text = string.Concat(badge.ChildNodes.Where(node => node.NodeType == NodeType.Text).Select(node => node.TextContent)).Trim();
        return (text, badge.GetAttribute("data-state"), badge.GetAttribute("title"), badge.TextContent);
    }

    IElement Badge(Action<ComponentParameterCollectionBuilder<DeadlineBadge>> parameters) => Render<DeadlineBadge>(parameters).Find("[data-state]");

    [Fact]
    public void 还早_快到期_今天到期_超时_悬停与读屏有截止日()
    {
        var pending = Tag(Badge(p => p.Add(b => b.Due, new DateOnly(2026, 10, 30)).Add(b => b.Today, Today)));
        Assert.Equal(("剩 7 个工作日", "pending", "截止 2026-10-30"), (pending.Text, pending.State, pending.Title));

        var soon = Tag(Badge(p => p.Add(b => b.Due, new DateOnly(2026, 10, 23)).Add(b => b.Today, Today)));
        Assert.Equal(("剩 2 个工作日", "soon"), (soon.Text, soon.State));

        var due = Tag(Badge(p => p.Add(b => b.Due, Today).Add(b => b.Today, Today)));
        Assert.Equal(("今天到期", "due"), (due.Text, due.State));

        var late = Tag(Badge(p => p.Add(b => b.Due, new DateOnly(2026, 10, 16)).Add(b => b.Today, Today)));
        Assert.Equal(("超时 3 个工作日", "overdue"), (late.Text, late.State));
        Assert.Contains("截止 2026-10-16", late.Reader);
    }

    [Fact]
    public void 开始日加几个工作日_完成了显示按时或超时完成()
    {
        // 周五开始，加 2 个工作日是下周二
        Assert.Equal("截止 2026-10-20", Tag(Badge(p => p.Add(b => b.Start, new DateOnly(2026, 10, 16)).Add(b => b.Workdays, 2).Add(b => b.Today, Today))).Title);

        var onTime = Tag(Badge(p => p.Add(b => b.Due, new DateOnly(2026, 10, 20)).Add(b => b.Done, new DateOnly(2026, 10, 19)).Add(b => b.Today, Today)));
        Assert.Equal(("按时完成", "done"), (onTime.Text, onTime.State));

        var doneLate = Tag(Badge(p => p.Add(b => b.Due, new DateOnly(2026, 10, 16)).Add(b => b.Done, new DateOnly(2026, 10, 20))));
        Assert.Equal("超时 2 个工作日完成", doneLate.Text);
    }

    [Fact]
    public void 节假日不算工作日_按RuneProvider的工作日历()
    {
        var calendar = new WorkCalendar { Holidays = [.. WorkCalendar.Range(new(2026, 10, 22), new(2026, 10, 23))] };
        var cut = Render<RuneProvider>(p => p
            .Add(r => r.WorkCalendar, calendar)
            .AddChildContent<DeadlineBadge>(b => b.Add(x => x.Due, new DateOnly(2026, 10, 26)).Add(x => x.Today, Today)));
        Assert.Equal("剩 1 个工作日", Tag(cut.Find("[data-state]")).Text);
    }

    [Fact]
    public void 没有截止日时什么都不画()
    {
        var cut = Render<DeadlineBadge>();
        Assert.Empty(cut.FindAll("span"));
    }

    [Fact]
    public void 工作日历_调休补班_往前数_反向相差为负()
    {
        var calendar = new WorkCalendar
        {
            Holidays = [.. WorkCalendar.Range(new(2026, 10, 1), new(2026, 10, 8))],
            Workdays = [new(2026, 9, 27), new(2026, 10, 10)],
        };
        Assert.True(calendar.IsWorkday(new(2026, 9, 27)));
        Assert.False(calendar.IsWorkday(new(2026, 10, 5)));
        // 9 月 30 日（周三）起 2 个工作日：10 月 1–8 日放假，9 日周五上班，10 日周六补班
        Assert.Equal(new DateOnly(2026, 10, 10), calendar.AddWorkdays(new(2026, 9, 30), 2));
        Assert.Equal(new DateOnly(2026, 10, 16), WorkCalendar.Weekdays.AddWorkdays(new(2026, 10, 20), -2));
        Assert.Equal(-1, WorkCalendar.Weekdays.WorkdaysBetween(new(2026, 10, 19), new(2026, 10, 16)));
    }
}
