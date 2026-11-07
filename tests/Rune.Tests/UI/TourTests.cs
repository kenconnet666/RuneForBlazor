using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 遮罩、定位、焦点在 JS 里（js/overlay/tour.js、overlays.js），这里测 C# 的一半：交给 JS 的目标与开关、换步骤、完成与跳过
public class TourTests : BunitContext
{
    public TourTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly TourStep[] Steps = [new("欢迎", "介绍"), new("搜索", "查找订单", "#search", Placement.Left), new("完成", "就这些")];

    [Fact]
    public void 目标与方向交给JS_没有目标时整页遮罩_卡片居中()
    {
        var cut = Render<Tour>(p => p.Add(x => x.Steps, Steps).Add(x => x.Open, true).Add(x => x.Current, 1));

        var dialog = cut.Find("dialog");
        Assert.Equal(("true", "#search", "left", "1"), (dialog.GetAttribute("data-open"), dialog.GetAttribute("data-rune-tour-target"), dialog.GetAttribute("data-placement"), dialog.GetAttribute("data-step")));
        Assert.Equal("搜索", cut.Find("#" + dialog.GetAttribute("aria-labelledby")).TextContent);
        Assert.Null(cut.Find("[data-rune-tour-spotlight]").GetAttribute("data-dimmed"));

        cut.Render(p => p.Add(x => x.Current, 0));
        Assert.NotNull(cut.Find("[data-rune-tour-spotlight]").GetAttribute("data-dimmed"));
        Assert.NotNull(cut.Find("[data-rune-tour-panel]").GetAttribute("data-centered"));
    }

    [Fact]
    public void 下一步与命令换步骤_最后一步完成时关闭并回调_跳过只关闭()
    {
        var open = true;
        var current = 1;
        var finished = 0;
        var cut = Render<Tour>(p => p.Add(x => x.Steps, Steps)
            .Add(x => x.Open, open).Add(x => x.OpenChanged, v => open = v)
            .Add(x => x.Current, current).Add(x => x.CurrentChanged, v => current = v)
            .Add(x => x.OnFinish, () => finished++));

        cut.Find("dialog").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "next" });
        Assert.Equal(2, current);
        cut.Render(p => p.Add(x => x.Current, current));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "完成").Click();
        Assert.False(open);
        Assert.Equal(1, finished);

        open = true;
        cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Current, 0));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "跳过").Click();
        Assert.False(open);
        Assert.Equal(1, finished);
    }
}
