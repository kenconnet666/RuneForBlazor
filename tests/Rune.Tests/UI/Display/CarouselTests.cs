using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>轮播：区域与每一张的读屏名称、页点与箭头、最后一张的序号、JS 换张写回序号、不循环时到头的箭头不可用。</summary>
public class CarouselTests : BunitContext
{
    static readonly string[] Items = ["a", "b", "c", "d"];

    public CarouselTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 区域与每一张的名称_页点_JS换张写回序号()
    {
        var index = 0;
        var cut = Render<Carousel<string>>(p => p
            .Add(x => x.Items, Items)
            .Add(x => x.Index, index)
            .Add(x => x.IndexChanged, (int i) => index = i)
            .Add(x => x.ChildContent, item => builder => builder.AddContent(0, item)));
        var root = cut.Find("section");
        Assert.Equal(("carousel", "轮播"), (root.GetAttribute("aria-roledescription"), root.GetAttribute("aria-label")));
        Assert.Equal("第 2 张，共 4 张", cut.FindAll("[role=group]")[1].GetAttribute("aria-label"));
        Assert.Equal(4, cut.FindAll("[data-carousel-dot]").Count);
        Assert.Equal("true", cut.Find("[data-carousel-dot='0']").GetAttribute("aria-current"));
        Assert.Equal("3", root.GetAttribute("data-last"));

        root.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "carousel-index", Text = "2" });
        Assert.Equal(2, index);
        cut.Render(p => p.Add(x => x.Index, index));
        Assert.Equal("2", cut.Find("section").GetAttribute("data-index"));
        Assert.Equal("true", cut.Find("[data-carousel-dot='2']").GetAttribute("aria-current"));
    }

    [Fact]
    public void 一次看两张_不循环时到头的箭头不可用_自动播放有暂停按钮()
    {
        var cut = Render<Carousel<string>>(p => p
            .Add(x => x.Items, Items)
            .Add(x => x.SlidesPerView, 2)
            .Add(x => x.Loop, false)
            .Add(x => x.Autoplay, TimeSpan.FromSeconds(3)));
        var root = cut.Find("section");
        Assert.Equal(("2", "3000"), (root.GetAttribute("data-last"), root.GetAttribute("data-autoplay")));
        Assert.True(cut.Find("[data-carousel-step='-1']").HasAttribute("disabled"));
        Assert.False(cut.Find("[data-carousel-step='1']").HasAttribute("disabled"));
        Assert.Equal("暂停自动播放", cut.Find("[data-carousel-pause]").GetAttribute("aria-label"));
    }
}
