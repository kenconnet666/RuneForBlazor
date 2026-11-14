using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

public class BreadcrumbsStepsTests : BunitContext
{
    public BreadcrumbsStepsTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly BreadcrumbItem[] Deep =
    [
        new("首页", "/"), new("项目", "projects"), new("官网改版", "redesign"), new("设计稿", "designs"), new("2026 秋季", "autumn"), new("首页 v3.fig"),
    ];

    [Fact]
    public void 面包屑_最后一项是当前页_其余是链接_分隔符只是装饰()
    {
        var cut = Render<Breadcrumbs>(p => p.Add(x => x.Items, [new("首页", "/"), new("订单", "orders"), new("#10234")]));

        Assert.Equal("面包屑", cut.Find("nav").GetAttribute("aria-label"));
        var items = cut.FindAll("ol > li");
        Assert.Equal(3, items.Count);
        Assert.Equal("orders", items[1].QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("page", items[2].QuerySelector("[aria-current]")!.GetAttribute("aria-current"));
        Assert.Null(items[2].QuerySelector("a"));
        Assert.Equal(2, cut.FindAll("li > span[aria-hidden=true]").Count);
    }

    [Fact]
    public void 面包屑_超过MaxItems时中间几级收进菜单_被折叠的链接一直在DOM里()
    {
        var cut = Render<Breadcrumbs>(p => p.Add(x => x.Items, Deep).Add(x => x.MaxItems, 4));

        var items = cut.FindAll("ol > li");
        Assert.Equal(4, items.Count);
        Assert.Equal("显示被折叠的路径", items[1].QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Equal(["projects", "redesign", "designs"], cut.FindAll("[role=menu] a[role=menuitem]").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void 面包屑_MaxItems比3小时_没有可折的就不折()
    {
        // 第一项与最后两项总是留着：3 项、MaxItems 2 时没有东西可折（之前折出一个空菜单）
        var cut = Render<Breadcrumbs>(p => p.Add(x => x.Items, Deep.Take(3).ToList()).Add(x => x.MaxItems, 2));
        Assert.Equal(3, cut.FindAll("ol > li").Count);
        Assert.Empty(cut.FindAll("[role=menu]"));
    }

    [Fact]
    public void 面包屑_应用内的层级_没有地址的各级是按钮_当前一级是location()
    {
        var picked = -1;
        var cut = Render<Breadcrumbs>(p => p.Add(x => x.Items, [new("全部"), new("研发中心"), new("前端组")]).Add(x => x.OnNavigate, i => picked = i));

        cut.FindAll("ol > li button")[1].Click();
        Assert.Equal(1, picked);
        Assert.Equal("location", cut.Find("[aria-current]").GetAttribute("aria-current"));
    }

    static readonly StepItem[] Checkout = [new("购物车"), new("收货地址"), new("支付"), new("完成")];

    [Fact]
    public void 步骤_按当前步骤推出状态_当前带aria_current_其余有读屏文字()
    {
        var cut = Render<Steps>(p => p.Add(x => x.Items, Checkout).Add(x => x.Current, 1));

        var items = cut.FindAll("ol > li");
        Assert.Equal(["complete", "current", "upcoming", "upcoming"], items.Select(li => li.GetAttribute("data-state")));
        Assert.Equal("step", items[1].GetAttribute("aria-current"));
        Assert.Contains("（已完成）", items[0].TextContent);
        Assert.DoesNotContain("（", items[1].TextContent);
        Assert.NotNull(items[0].QuerySelector("svg[data-icon=check]"));
    }

    [Fact]
    public void 步骤_Navigable时已完成与出错的步骤可以点回去_不可用的不能点()
    {
        var current = 2;
        StepItem[] items = [new("拉取"), new("测试", Status: StepState.Error), new("构建"), new("发布", Disabled: true)];
        var cut = Render<Steps>(p => p.Add(x => x.Items, items).Add(x => x.Current, current).Add(x => x.CurrentChanged, v => current = v).Add(x => x.Navigable, true));

        // 拉取（已完成）、测试（出错）可以点；构建是当前步骤、发布不可用
        var buttons = cut.FindAll("ol > li button");
        Assert.Equal(2, buttons.Count);
        buttons[1].Click();
        Assert.Equal(1, current);
        cut.FindAll("ol > li button")[0].Click();
        Assert.Equal(0, current);
        Assert.Equal("error", cut.FindAll("ol > li")[1].GetAttribute("data-state"));
    }
}
