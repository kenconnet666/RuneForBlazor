using Rune.UI;

namespace Rune.Tests.Dates;

// 期望值来自 Node 的 Intl.DateTimeFormat（dateStyle、timeStyle），vue-tsx-pro 的 format.date 就是它
public class DateFormatTests
{
    static readonly DateTime At = new(2026, 10, 20, 9, 5, 7);

    [Theory]
    [InlineData("zh-CN", DateStyle.Full, null, "2026年10月20日星期二")]
    [InlineData("zh-CN", DateStyle.Long, null, "2026年10月20日")]
    [InlineData("zh-CN", DateStyle.Medium, null, "2026年10月20日")]
    [InlineData("zh-CN", DateStyle.Short, null, "2026/10/20")]
    [InlineData("zh-CN", null, TimeStyle.Short, "09:05")]
    [InlineData("zh-CN", null, TimeStyle.Medium, "09:05:07")]
    [InlineData("zh-CN", DateStyle.Medium, TimeStyle.Short, "2026年10月20日 09:05")]
    [InlineData("zh-CN", DateStyle.Full, TimeStyle.Short, "2026年10月20日星期二 09:05")]
    [InlineData("en-US", DateStyle.Full, null, "Tuesday, October 20, 2026")]
    [InlineData("en-US", DateStyle.Long, null, "October 20, 2026")]
    [InlineData("en-US", DateStyle.Medium, null, "Oct 20, 2026")]
    [InlineData("en-US", DateStyle.Short, null, "10/20/26")]
    [InlineData("en-US", null, TimeStyle.Short, "9:05 AM")]
    [InlineData("en-US", null, TimeStyle.Medium, "9:05:07 AM")]
    [InlineData("en-US", DateStyle.Medium, TimeStyle.Short, "Oct 20, 2026, 9:05 AM")]
    [InlineData("en-US", DateStyle.Full, TimeStyle.Short, "Tuesday, October 20, 2026 at 9:05 AM")]
    public void 与Intl的写法一致(string culture, DateStyle? date, TimeStyle? time, string expected) =>
        Assert.Equal(expected, LangFormat.Date(At, culture == "zh-CN" ? Lang.ZhCn : Lang.EnUs, date, time));

    [Fact]
    public void 时刻按语言的时区显示_不带时区的原样()
    {
        var tokyo = Lang.ZhCn with { TimeZone = "Asia/Tokyo" };

        Assert.Equal("2026年10月20日 18:05", LangFormat.Date(new DateTimeOffset(At, TimeSpan.Zero), tokyo, DateStyle.Medium, TimeStyle.Short));
        Assert.Equal("2026年10月20日 18:05", LangFormat.Date(DateTime.SpecifyKind(At, DateTimeKind.Utc), tokyo, DateStyle.Medium, TimeStyle.Short));
        Assert.Equal("2026年10月20日 09:05", LangFormat.Date(At, tokyo, DateStyle.Medium, TimeStyle.Short));
    }

    [Fact]
    public void 语言包的小时制优先于地区惯例()
    {
        Assert.Equal("上午9:05", LangFormat.Date(At, Lang.ZhCn with { HourCycle = "h12" }, null, TimeStyle.Short));
        Assert.Equal("09:05", LangFormat.Date(At, Lang.EnUs with { HourCycle = "h23" }, null, TimeStyle.Short));
    }
}
