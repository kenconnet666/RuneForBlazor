using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Rune.UI;

namespace Rune.Tests.Dates;

// RelativeTime、Countdown 的“现在”取 DI 里的 TimeProvider：这里给可拨动的，测更新、改写日期、到点。
// 时刻按语言的时区显示：固定成东八区，与跑测试的机器无关
public class TimerComponentTests : BunitContext
{
    static readonly DateTimeOffset Start = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    readonly FakeTimeProvider time = new(Start);

    public TimerComponentTests()
    {
        Services.AddRuneUI(o => o.Lang = Lang.ZhCn with { TimeZone = "Asia/Shanghai" });
        Services.AddSingleton<TimeProvider>(time);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 相对时间随时间更新_悬停是完整的日期时间()
    {
        var cut = Render<RelativeTime>(p => p.Add(x => x.Value, Start.AddSeconds(-30)));
        var element = cut.Find("time");

        Assert.Equal(("30秒钟前", "2026-10-20T11:59:30.0000000+00:00"), (element.TextContent, element.GetAttribute("datetime")));
        Assert.Equal("2026年10月20日星期二 19:59", element.GetAttribute("title"));

        time.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() => Assert.Equal("31秒钟前", cut.Find("time").TextContent));
    }

    [Fact]
    public void 超过Threshold写日期_固定的Now不更新()
    {
        var past = Render<RelativeTime>(p => p.Add(x => x.Value, Start.AddDays(-12)).Add(x => x.Threshold, TimeSpan.FromDays(7)));
        Assert.Equal("2026年10月8日", past.Find("time").TextContent);

        var fixedNow = Render<RelativeTime>(p => p.Add(x => x.Value, Start.AddMinutes(-3)).Add(x => x.Now, Start).Add(x => x.Format, RelativeTimeStyle.Narrow));
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal("3分钟前", fixedNow.Find("time").TextContent);
    }

    [Fact]
    public void 倒计时_超过一天加天数_精度到分时向上取整()
    {
        var seconds = Render<Countdown>(p => p.Add(x => x.To, Start + new TimeSpan(1, 2, 3, 4)));
        var minutes = Render<Countdown>(p => p.Add(x => x.To, Start + new TimeSpan(1, 2, 3, 4)).Add(x => x.Precision, CountdownPrecision.Minute));

        Assert.Equal(("1天 02:03:04", "1天 02:04"), (seconds.Find("[role=timer]").TextContent, minutes.Find("[role=timer]").TextContent));

        time.Advance(TimeSpan.FromSeconds(1));
        seconds.WaitForAssertion(() => Assert.Equal("1天 02:03:03", seconds.Find("[role=timer]").TextContent));
    }

    [Fact]
    public void 倒计时的数字块_天数一格_时分秒各一格_中间是冒号()
    {
        var cut = Render<Countdown>(p => p.Add(x => x.To, Start + new TimeSpan(2, 1, 2, 3)).Add(x => x.Variant, CountdownVariant.Blocks));
        var timer = cut.Find("[role=timer]");
        Assert.Equal("countdown", timer.GetAttribute("data-scope"));
        Assert.Equal(["2天", "01", "02", "03"], timer.QuerySelectorAll("[data-part=segment]").Select(segment => segment.TextContent));
        Assert.Equal(2, timer.QuerySelectorAll("[data-part=separator]").Length);
    }

    [Fact]
    public void 到点时OnEnd调用一次_挂载时已过的不调用()
    {
        var ends = 0;
        var cut = Render<Countdown>(p => p.Add(x => x.To, Start.AddSeconds(2)).Add(x => x.OnEnd, () => ends++));
        Render<Countdown>(p => p.Add(x => x.To, Start.AddSeconds(-5)).Add(x => x.OnEnd, () => ends += 100));

        time.Advance(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() => Assert.Equal("00:00:00", cut.Find("[role=timer]").TextContent));
        time.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(1, ends);
    }

    [Fact]
    public void 自定义显示拿到拆好的天时分秒()
    {
        var cut = Render<Countdown>(p => p
            .Add(x => x.To, Start + new TimeSpan(3, 4, 5, 6))
            .Add(x => x.ChildContent, (RenderFragment<CountdownParts>)(left => builder => builder.AddContent(0, $"{left.Days}/{left.Hours}/{left.Minutes}/{left.Seconds}"))));

        Assert.Equal("3/4/5/6", cut.Find("[role=timer]").TextContent);
    }
}
