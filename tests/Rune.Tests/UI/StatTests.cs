using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>数字格式、会滚动的数字、统计数值、时间线、主栏与侧栏。</summary>
public class StatTests : BunitContext
{
    static readonly Lang Zh = Lang.ZhCn;

    public StatTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Theory]
    [InlineData(1234.5, "1,234.5")]
    [InlineData(0.1234567, "0.123")]
    [InlineData(-42, "-42")]
    public void 默认格式_千分位_最多三位小数(decimal value, string expected) =>
        Assert.Equal(expected, LangFormat.Number(value, NumberFormat.Default, Zh));

    [Fact]
    public void 百分比_缩放与单位_带符号_金额()
    {
        Assert.Equal("12%", LangFormat.Number(0.123m, NumberFormat.Percent, Zh));
        Assert.Equal("+12.3%", LangFormat.Number(0.123m, Stat.DefaultChangeFormat, Zh));
        Assert.Equal("-4.2%", LangFormat.Number(-0.042m, Stat.DefaultChangeFormat, Zh));
        Assert.Equal("0%", LangFormat.Number(0m, Stat.DefaultChangeFormat, Zh));
        Assert.Equal("15.54 万元", LangFormat.Number(155353.2m, new NumberFormat { Scale = 10_000, MaximumFractionDigits = 2, Suffix = "万元" }, Zh));
        Assert.Equal("1,087.50", LangFormat.Number(10_875_000m, new NumberFormat { Scale = 10_000, MinimumFractionDigits = 2, MaximumFractionDigits = 2 }, Zh));
        Assert.Equal("¥12,880.35", LangFormat.Number(12_880.35m, new NumberFormat { Style = NumberStyle.Currency }, Zh));
        // 只写了最少位数：最多位数跟着它（同 Intl）
        Assert.Equal("4.60", LangFormat.Number(4.6m, new NumberFormat { MinimumFractionDigits = 2 }, Zh));
        // 0.5 远离零舍入（同 Intl）
        Assert.Equal("3", LangFormat.Number(2.5m, new NumberFormat { MaximumFractionDigits = 0 }, Zh));
    }

    [Fact]
    public void 浏览器里逐帧格式化用的选项()
    {
        var cut = Render<AnimatedNumber>(p => p.Add(x => x.Value, 1m).Add(x => x.Format, new NumberFormat { Style = NumberStyle.Currency, Scale = 10_000, Suffix = "万" }));
        Assert.Equal("""{"style":"currency","currency":"CNY","minimumFractionDigits":2,"maximumFractionDigits":2,"signDisplay":"auto","useGrouping":true,"scale":10000,"suffix":"万"}""",
            cut.Find("[data-rune-tween]").GetAttribute("data-format"));
        Assert.Equal("zh-CN", cut.Find("[data-rune-tween]").GetAttribute("data-locale"));
    }

    [Fact]
    public void 会滚动的数字_终值给读屏_滚动的部分对读屏隐藏_按正负着色()
    {
        var cut = Render<AnimatedNumber>(p => p.Add(x => x.Value, -0.05m).Add(x => x.Format, NumberFormat.Percent).Add(x => x.Trend, true).Add(x => x.From, 0m));
        var value = cut.Find("[data-rune-tween]");
        Assert.Equal("true", value.GetAttribute("aria-hidden"));
        Assert.Equal("-0.05", value.GetAttribute("data-value"));
        Assert.Equal("0", value.GetAttribute("data-from"));
        Assert.Equal("lg", value.GetAttribute("data-rune-tween"));
        Assert.Equal("-5%", value.TextContent);
        Assert.Contains("-5%", cut.Find("span > span:last-child").TextContent);
        Assert.Equal("down", cut.Find("span").GetAttribute("data-trend"));
    }

    [Fact]
    public void 统计数值_名称与数值成对_变化量按正负与InvertChange着色()
    {
        var cut = Render<Stat>(p => p
            .Add(x => x.Label, "平均响应时间")
            .Add(x => x.Value, 212m)
            .Add(x => x.Unit, "ms")
            .Add(x => x.Change, -0.18m)
            .Add(x => x.InvertChange, true)
            .Add(x => x.ChangeLabel, "较上周"));

        Assert.Equal("平均响应时间", cut.Find("dl > dt").TextContent.Trim());
        Assert.Contains("212", cut.Find("dl > dd").TextContent);
        var change = cut.Find("dd[data-trend]");
        Assert.Equal("down", change.GetAttribute("data-trend"));
        Assert.Contains("-18%", change.TextContent);
        Assert.Contains("较上周", change.TextContent);
        // 越低越好：下降是成功色
        Assert.Equal(TextTone.Success, cut.FindComponents<AnimatedNumber>()[1].Instance.Tone);
    }

    [Fact]
    public void 加载中_数值换成骨架_带aria_busy_文字值原样显示()
    {
        var loading = Render<Stat>(p => p.Add(x => x.Label, "项目总数").Add(x => x.Value, 6m).Add(x => x.Loading, true));
        Assert.Equal("true", loading.Find("dl").GetAttribute("aria-busy"));
        Assert.Empty(loading.FindComponents<AnimatedNumber>());

        var text = Render<Stat>(p => p.Add(x => x.Label, "测算").Add(x => x.Text, "—"));
        Assert.Equal("—", text.Find("dl > dd > span").TextContent);
    }

    [Fact]
    public void 时间线_倒序_进行中_收起旧的()
    {
        TimelineItem[] items = [new("一"), new("二") { Time = "10-18", DateTime = "2026-10-18" }, new("三"), new("四")];
        var cut = Render<Timeline>(p => p.Add(x => x.Items, items).Add(x => x.Pending, "处理中").Add(x => x.MaxVisible, 2));

        var titles = cut.FindAll("li").Select(li => li.TextContent.Trim()).ToList();
        Assert.StartsWith("展开其余 2 条", titles[0]);
        Assert.Equal(["三", "四", "处理中"], titles.Skip(1).Select(t => t[..Math.Min(t.Length, 3)].Trim()));
        Assert.Equal("pending", cut.FindAll("li")[^1].GetAttribute("data-state"));

        cut.Find("button[aria-expanded=false]").Click();
        Assert.Equal(6, cut.FindAll("li").Count);
        Assert.Equal("2026-10-18", cut.Find("time").GetAttribute("datetime"));

        var reversed = Render<Timeline>(p => p.Add(x => x.Items, items).Add(x => x.Reverse, true));
        Assert.True(reversed.Find("ol").HasAttribute("reversed"));
        Assert.Equal("四", reversed.Find("li").TextContent.Trim());
    }

    [Fact]
    public void 主栏与侧栏_侧栏是带名称的aside_宽度经变量()
    {
        var cut = Render<AsideLayout>(p => p
            .Add(x => x.AsideLabel, "收益")
            .Add(x => x.AsideWidth, CssLength.Rem(18))
            .Add(x => x.Aside, (RenderFragment)(b => b.AddContent(0, "侧栏")))
            .AddChildContent("主栏"));
        Assert.Equal("收益", cut.Find("aside").GetAttribute("aria-label"));
        Assert.Contains("--rune-aside-width:18rem", cut.Markup.Replace(" ", ""));
    }
}
