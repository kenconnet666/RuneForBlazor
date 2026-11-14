using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>页头（同 vue-tsx-pro 0.26.0 的 PageHeader）：面包屑、返回、标题与标记、说明、操作。</summary>
public class PageHeaderTests : BunitContext
{
    public PageHeaderTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 只有标题时_h1在hgroup里_没有返回与操作()
    {
        var cut = Render<PageHeader>(p => p.Add(x => x.Title, "采购单"));

        Assert.Equal("采购单", cut.Find("header > div > hgroup h1").TextContent);
        Assert.Empty(cut.FindAll("nav"));
        Assert.Empty(cut.FindAll("button, a"));
        Assert.Empty(cut.FindAll("""[data-part="actions"], [data-part="meta"], p"""));
        Assert.Equal(("page-header", "root"), (cut.Find("header").GetAttribute("data-scope"), cut.Find("header").GetAttribute("data-part")));
    }

    [Fact]
    public void 面包屑_标记_说明_操作_下面的内容各就各位()
    {
        var cut = Render<PageHeader>(p => p
            .Add(x => x.Title, "PO-2026-0187")
            .Add(x => x.HeadingLevel, 2)
            .Add(x => x.Breadcrumbs, [new BreadcrumbItem("采购单", "/orders"), new BreadcrumbItem("PO-2026-0187")])
            .Add(x => x.Meta, "<span>已到货</span>")
            .Add(x => x.Description, "华辰精密 · 2026-10-20 下单")
            .Add(x => x.Actions, "<button>催单</button>")
            .AddChildContent("<section>摘要</section>"));

        Assert.Equal("PO-2026-0187", cut.Find("hgroup h2").TextContent);
        Assert.NotNull(cut.Find("""header > nav[data-part="breadcrumbs"]"""));
        Assert.Equal("已到货", cut.Find("""hgroup [data-part="titleRow"] > [data-part="meta"]""").TextContent);
        Assert.Equal("华辰精密 · 2026-10-20 下单", cut.Find("""hgroup > p[data-part="description"]""").TextContent);
        Assert.Equal("催单", cut.Find("""[data-part="main"] > [data-part="actions"] > button""").TextContent);
        // 下面的内容在标题行之后
        Assert.Equal("section", cut.Find("header").LastElementChild!.LocalName);
    }

    [Fact]
    public void 返回_给OnBack是按钮_给BackHref是链接_名称取文案()
    {
        var backs = 0;
        var button = Render<PageHeader>(p => p.Add(x => x.Title, "详情").Add(x => x.OnBack, () => backs++));
        var back = button.Find("""button[data-part="back"]""");
        Assert.Equal("返回", back.GetAttribute("aria-label"));
        back.Click();
        Assert.Equal(1, backs);

        var link = Render<PageHeader>(p => p.Add(x => x.Title, "详情").Add(x => x.BackHref, "/orders").Add(x => x.OnBack, () => backs++));
        var anchor = link.Find("""a[data-part="back"]""");
        Assert.Equal(("/orders", "返回"), (anchor.GetAttribute("href"), anchor.GetAttribute("aria-label")));
        // 是链接时点击也调用 OnBack
        anchor.Click();
        Assert.Equal(2, backs);
    }
}
