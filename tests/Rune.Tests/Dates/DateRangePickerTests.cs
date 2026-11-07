using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.Dates;

/// <summary>日期区间选择器：两端的类型（DateOnly、DateTime）、按粒度的分段与写回。</summary>
public class DateRangePickerTests : BunitContext
{
    public DateRangePickerTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static string[] Values<T>(IRenderedComponent<T> cut) where T : IComponent =>
        [.. cut.FindAll("[data-rune-date-segments]").Select(group => group.GetAttribute("data-value") ?? "")];

    [Fact]
    public void 按天_两组是日期_改了结束那组写回()
    {
        DateRange<DateOnly>? value = new(new(2026, 10, 1), new(2026, 10, 7));
        var cut = Render<DateRangePicker<DateOnly>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, (DateRange<DateOnly>? next) => value = next));
        Assert.Equal(["2026-10-01", "2026-10-07"], Values(cut));

        cut.FindAll("[data-rune-date-segments]")[1].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "change", Text = "2026-10-09" });
        Assert.Equal(new DateRange<DateOnly>(new(2026, 10, 1), new(2026, 10, 9)), value);
    }

    [Fact]
    public void 按月_结束是那个月的最后一天()
    {
        DateRange<DateOnly>? value = null;
        var cut = Render<DateRangePicker<DateOnly>>(p => p
            .Add(x => x.Granularity, DateGranularity.Month)
            .Add(x => x.ValueChanged, (DateRange<DateOnly>? next) => value = next));
        var groups = cut.FindAll("[data-rune-date-segments]");
        groups[0].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "change", Text = "2026-01" });
        cut.FindAll("[data-rune-date-segments]")[1].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "change", Text = "2026-06" });
        Assert.Equal(new DateRange<DateOnly>(new(2026, 1, 1), new(2026, 6, 30)), value);
    }

    [Fact]
    public void 按分钟_两端带时间_写回DateTime()
    {
        DateRange<DateTime>? value = new(new(2026, 10, 24, 22, 0, 0), new(2026, 10, 25, 6, 0, 0));
        var cut = Render<DateRangePicker<DateTime>>(p => p
            .Add(x => x.Granularity, DateGranularity.Minute)
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, (DateRange<DateTime>? next) => value = next));
        Assert.Equal(["2026-10-24T22:00", "2026-10-25T06:00"], Values(cut));
        Assert.Contains("minute", cut.Find("[data-rune-date-segments]").GetAttribute("data-fields"));

        cut.FindAll("[data-rune-date-segments]")[1].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "change", Text = "2026-10-25T07:30" });
        Assert.Equal(new DateRange<DateTime>(new(2026, 10, 24, 22, 0, 0), new(2026, 10, 25, 7, 30, 0)), value);
    }

    [Fact]
    public void 区间的辅助_排序_天数()
    {
        var range = DateRange.Of(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 1));
        Assert.Equal(new DateOnly(2026, 10, 1), range.Start);
        Assert.Equal(7, range.Days);
        Assert.True(range.Contains(new(2026, 10, 3)));
        Assert.Equal(new DateRange<DateTime>(new(2026, 1, 1), new(2026, 1, 2)), DateRange.Of(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1)));
    }
}
