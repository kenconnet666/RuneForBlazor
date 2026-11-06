using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class SkeletonTests : BunitContext
{
    public SkeletonTests()
    {
        Services.AddRuneUI();
        // Listbox 按渲染模式决定是否虚拟滚动
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 骨架_包着内容时撑出大小_对读屏隐藏且内容不可聚焦_加载完只剩内容()
    {
        var cut = Render<Skeleton>(p => p
            .Add(x => x.Delay, ShimmerStyles.Stagger)
            .AddChildContent("<a href=\"/ada\">Ada</a>"));

        var root = cut.Find("span[aria-hidden=true]");
        Assert.True(root.HasAttribute("inert"));
        Assert.Equal("animation-delay:120ms", root.GetAttribute("style"));
        Assert.Equal("Ada", root.QuerySelector("a")!.TextContent);

        cut.Render(p => p.Add(x => x.Loading, false));
        Assert.Empty(cut.FindAll("span[aria-hidden]"));
        Assert.Equal("Ada", cut.Find("a").TextContent);
    }

    [Fact]
    public void 骨架_不包内容时没有inert()
    {
        var cut = Render<Skeleton>(p => p.Add(x => x.Shape, SkeletonShape.Circle));
        Assert.False(cut.Find("span").HasAttribute("inert"));
        Assert.Null(cut.Find("span").GetAttribute("style"));
    }

    [Fact]
    public void 多行骨架_行数_光带依次错开()
    {
        var cut = Render<SkeletonText>(p => p.Add(x => x.Lines, 3).Add(x => x.Delay, 100));
        var lines = cut.FindAll("div[aria-hidden=true] > span");
        Assert.Equal(3, lines.Count);
        Assert.Equal(["animation-delay:100ms", "animation-delay:220ms", "animation-delay:340ms"], lines.Select(l => l.GetAttribute("style")));
    }

    [Fact]
    public async Task 延迟显示_一开始就在加载时立即显示_之后很快完成的不显示_显示了至少保持一段时间()
    {
        var renders = 0;
        using var initially = new DelayedLoading(() => { renders++; return Task.CompletedTask; });
        initially.Update(true);
        Assert.True(initially.Shown);

        using var quick = new DelayedLoading(() => { renders++; return Task.CompletedTask; });
        quick.Update(false);
        quick.Update(true);
        Assert.False(quick.Shown);
        quick.Update(false);
        await Task.Delay(DelayedLoading.Delay + TimeSpan.FromMilliseconds(100), Xunit.TestContext.Current.CancellationToken);
        Assert.False(quick.Shown);
        Assert.Equal(0, renders);

        // 显示后马上完成：至少保持 MinDuration
        initially.Update(false);
        Assert.True(initially.Shown);
        await Task.Delay(DelayedLoading.MinDuration + TimeSpan.FromMilliseconds(200), Xunit.TestContext.Current.CancellationToken);
        Assert.False(initially.Shown);
        Assert.Equal(1, renders);
    }

    [Fact]
    public void 卡片加载中_正文换成骨架_带aria_busy()
    {
        var cut = Render<Card>(p => p
            .Add(x => x.Loading, true)
            .Add(x => x.Header, "本周摘要")
            .AddChildContent("<p>正文</p>"));

        var root = cut.Find("[data-scope=card][data-part=root]");
        Assert.Equal("true", root.GetAttribute("aria-busy"));
        Assert.Contains("本周摘要", root.TextContent);
        Assert.Empty(cut.FindAll("p"));
        Assert.Equal(3, cut.FindAll("[data-part=body] div[aria-hidden=true] > span").Count);
    }

    [Fact]
    public void 列表框加载中_选项换成骨架行_带aria_busy()
    {
        var cut = Render<Listbox<string>>(p => p.Add(x => x.Items, ["a", "b"]).Add(x => x.Loading, true));
        var list = cut.Find("[role=listbox]");
        Assert.Equal("true", list.GetAttribute("aria-busy"));
        Assert.Empty(cut.FindAll("[role=option]"));
        Assert.Equal(4, cut.FindAll("[data-part=skeleton]").Count);
    }
}
