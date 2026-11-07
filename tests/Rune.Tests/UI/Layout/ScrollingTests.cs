using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>整页的浮层滚动条与虚拟列表。</summary>
public class ScrollingTests : BunitContext
{
    public ScrollingTests() => Services.AddRuneUI();

    [Fact]
    public void 整页滚动条_隐藏原生滚动条并交给JS_AppShell默认带着_可以关掉()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var shell = Render<AppShell>(p => p.AddChildContent("<p>正文</p>"));
        var plain = Render<AppShell>(p => p.Add(x => x.PageScrollbar, false).AddChildContent("<p>正文</p>"));

        var track = shell.Find("[data-rune-page-scrollbar]");
        Assert.Equal("y", track.GetAttribute("data-axis"));
        Assert.Equal("true", track.GetAttribute("aria-hidden"));
        Assert.Contains("html{scrollbar-width:none}", shell.Find("style").TextContent);
        Assert.Empty(plain.FindAll("[data-rune-page-scrollbar]"));
    }

    [Fact]
    public void 虚拟列表_静态渲染先输出前几项_读屏知道总数与序号()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var items = Enumerable.Range(1, 100).Select(i => $"第 {i} 行").ToList();
        var cut = Render<VirtualList<string>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.InitialCount, 5)
            .Add(x => x.ChildContent, (RenderFragment<string>)(item => b => b.AddContent(0, item)))
            .AddUnmatched("aria-label", "日志"));

        var rows = cut.FindAll("[role=listitem]");
        Assert.Equal(5, rows.Count);
        Assert.Equal("100", rows[0].GetAttribute("aria-setsize"));
        Assert.Equal("5", rows[4].GetAttribute("aria-posinset"));
        Assert.Equal("第 5 行", rows[4].TextContent);
        Assert.Equal("日志", cut.Find("[role=list]").GetAttribute("aria-label"));
    }

    [Fact]
    public void 虚拟列表_没有数据时显示空状态_不是列表()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<VirtualList<string>>(p => p
            .Add(x => x.Items, Array.Empty<string>())
            .Add(x => x.ChildContent, (RenderFragment<string>)(item => b => b.AddContent(0, item)))
            .Add(x => x.Empty, b => b.AddContent(0, "暂无数据")));

        Assert.Empty(cut.FindAll("[role=list]"));
        Assert.Contains("暂无数据", cut.Markup);
    }
}
