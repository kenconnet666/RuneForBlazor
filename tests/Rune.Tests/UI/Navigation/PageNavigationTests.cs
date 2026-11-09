using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 本页目录、翻页、跳到正文、顶部加载条
public class PageNavigationTests : BunitContext
{
    public PageNavigationTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void 目录的锚点按当前页解析_标题是导航的名称_按级别缩进()
    {
        Navigation.NavigateTo("docs/button");
        var cut = Render<TableOfContents>(p => p.Add(x => x.Items, [new HeadingEntry("install", "安装"), new HeadingEntry("npm", "用 npm", 3)]).Add(x => x.ScrollRoot, "article"));

        var nav = cut.Find("nav");
        Assert.Equal("本页目录", cut.Find($"#{nav.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("article", nav.GetAttribute("data-scroll-root"));
        var links = cut.FindAll("a");
        Assert.Equal(["http://localhost/docs/button#install", "http://localhost/docs/button#npm"], links.Select(a => a.GetAttribute("href")));
        Assert.NotEqual(links[0].ClassName, links[1].ClassName);
        Assert.Equal("[aria-current=\"location\"]", cut.Find("[data-rune-indicator]").GetAttribute("data-rune-indicator"));
    }

    [Fact]
    public void 翻页链接带rel_只有下一篇时只有一张()
    {
        var cut = Render<Pager>(p => p.Add(x => x.Next, new PagerLink("components/dialog", "Dialog 对话框")));
        var link = cut.Find("a");
        Assert.Equal(("next", "components/dialog"), (link.GetAttribute("rel"), link.GetAttribute("href")));
        Assert.Contains("下一篇", link.TextContent);
        Assert.Contains("Dialog 对话框", link.TextContent);
        Assert.Equal("翻页", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void 跳到正文由JS聚焦目标()
    {
        var cut = Render<SkipLink>(p => p.Add(x => x.Target, "main"));
        var link = cut.Find("a");
        Assert.Equal(("#main", "main", "start"), (link.GetAttribute("href"), link.GetAttribute("data-rune-focus"), link.GetAttribute("data-focus-block")));
        Assert.Equal("跳到正文", link.TextContent);
    }

    [Fact]
    public void 加载条按控制器写状态_再开始时次数变()
    {
        var bar = new LoadingBarController();
        var cut = Render<LoadingBar>(p => p.Add(x => x.Controller, bar));
        Assert.Equal("idle", cut.Find("[data-rune-loading-bar]").GetAttribute("data-status"));

        cut.InvokeAsync(bar.Start);
        cut.WaitForAssertion(() => Assert.Equal(("loading", "1"), (cut.Find("[data-rune-loading-bar]").GetAttribute("data-status"), cut.Find("[data-rune-loading-bar]").GetAttribute("data-run"))));
        cut.InvokeAsync(bar.Error);
        cut.WaitForAssertion(() => Assert.Equal("failed", cut.Find("[data-rune-loading-bar]").GetAttribute("data-status")));
        // 没在加载时完成不改状态
        cut.InvokeAsync(bar.Finish);
        Assert.Equal(LoadingBarStatus.Failed, bar.Status);
    }

    [Fact]
    public void 启动加载条_预渲染时在走_交互模式接管后不再加载()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var prerendered = Render<LoadingBar>(p => p.Add(x => x.Startup, true));
        Assert.Equal("loading", prerendered.Find("[data-rune-loading-bar]").GetAttribute("data-status"));

        // 接管后重新渲染出来的：按控制器（没给时不加载）
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var interactive = Render<LoadingBar>(p => p.Add(x => x.Startup, true));
        Assert.Equal("idle", interactive.Find("[data-rune-loading-bar]").GetAttribute("data-status"));
    }
}
