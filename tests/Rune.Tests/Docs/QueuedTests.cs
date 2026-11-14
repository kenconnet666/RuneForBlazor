using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Docs.Site;
using Rune.UI;

namespace Rune.Tests.Docs;

/// <summary>
/// 文档站排队渲染的内容（Queued）：预渲染时直接渲染、两头留标记；交互模式的第一页先是占位（预渲染的内容由 JS 放回去），
/// 轮到时换成真正的内容；站内换页后没有占位。
/// </summary>
public class QueuedTests : BunitContext
{
    public QueuedTests()
    {
        Services.AddRuneUI();
        Services.AddScoped<RenderQueue>();
    }

    IRenderedComponent<Queued> RenderQueued(string? hold = "basic") =>
        Render<Queued>(p => p.Add(x => x.Hold, hold).AddChildContent("<button>示例</button>"));

    [Fact]
    public void 预渲染时直接渲染_内容两头是注释标记()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderQueued();

        Assert.Equal("<!--docs-hold:basic--><button>示例</button><!--/docs-hold:basic-->", cut.Markup.Replace("\n", "").Replace("\r", ""));
        Assert.False(Services.GetRequiredService<RenderQueue>().Busy);
        // 没写名字的不留标记
        Assert.DoesNotContain("docs-hold", RenderQueued(hold: null).Markup);
    }

    [Fact]
    public async Task 第一页交互接管时先是占位_轮到后换成内容_队列排完()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var queue = Services.GetRequiredService<RenderQueue>();
        var busy = new List<bool>();
        queue.BusyChanged += () => busy.Add(queue.Busy);
        // 前面还有一段没渲染完：这一段停在占位上
        var previous = await queue.WaitTurnAsync();

        var cut = RenderQueued();
        // 占位不占排版的格子（display: contents），内容还没渲染
        var slot = cut.Find("[data-docs-hold=basic]");
        Assert.Equal("display:contents", slot.GetAttribute("style"));
        Assert.Empty(cut.FindAll("button"));
        Assert.True(queue.Busy);

        previous();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button")));
        Assert.Empty(cut.FindAll("[data-docs-hold]"));
        cut.WaitForAssertion(() => Assert.False(queue.Busy));
        Assert.Equal([true, false], busy);
    }

    [Fact]
    public void 第一页没写名字的直接渲染_不进队列()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var cut = RenderQueued(hold: null);

        Assert.Single(cut.FindAll("button"));
        Assert.False(Services.GetRequiredService<RenderQueue>().Busy);
    }

    [Fact]
    public void 占位带着名字_几段按顺序一个一个换()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var cut = Render(builder =>
        {
            foreach (var name in (string[])["first", "second"])
            {
                builder.OpenComponent<Queued>(0);
                builder.SetKey(name);
                builder.AddAttribute(1, nameof(Queued.Hold), name);
                builder.AddAttribute(2, nameof(Queued.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, $"<p>{name}</p>")));
                builder.CloseComponent();
            }
        });

        cut.WaitForAssertion(() => Assert.Equal(["first", "second"], cut.FindAll("p").Select(p => p.TextContent)));
        Assert.Empty(cut.FindAll("[data-docs-hold]"));
    }
}
