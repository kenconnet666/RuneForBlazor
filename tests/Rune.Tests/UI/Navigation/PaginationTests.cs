using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>分页：页码序列、Pagination、PaginationBar、PageJump。</summary>
public class PaginationTests : BunitContext
{
    public PaginationTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    // 省略号写成 0（前）、-1（后），便于对照 vue-tsx-pro 的用例
    static int[] Sequence(int page, int pageCount, int siblings = 1, int boundaries = 1) =>
        [.. PaginationItems.Of(page, pageCount, siblings, boundaries).Select(i => i.Page ?? (i.Key == "start-ellipsis" ? 0 : -1))];

    [Theory]
    [InlineData(1, 1, new[] { 1 })]
    [InlineData(1, 5, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(4, 7, new[] { 1, 2, 3, 4, 5, 6, 7 })]
    [InlineData(1, 20, new[] { 1, 2, 3, 4, 5, -1, 20 })]
    [InlineData(4, 20, new[] { 1, 2, 3, 4, 5, -1, 20 })]
    [InlineData(6, 20, new[] { 1, 0, 5, 6, 7, -1, 20 })]
    [InlineData(17, 20, new[] { 1, 0, 16, 17, 18, 19, 20 })]
    [InlineData(20, 20, new[] { 1, 0, 16, 17, 18, 19, 20 })]
    public void 页码序列(int page, int pageCount, int[] expected) => Assert.Equal(expected, Sequence(page, pageCount));

    [Fact]
    public void 页数够多时项数固定_翻页时按钮不跳动()
    {
        Assert.Equal([7], Enumerable.Range(1, 50).Select(page => Sequence(page, 50).Length).Distinct());
        Assert.Equal([1, 2, 0, 8, 9, 10, 11, 12, -1, 19, 20], Sequence(10, 20, siblings: 2, boundaries: 2));
    }

    [Fact]
    public void 分页_当前页带aria_current_点页码与下一页写回_到头不可用()
    {
        var page = 1;
        var cut = Render<Pagination>(p => p
            .Add(x => x.Page, page)
            .Add(x => x.PageChanged, EventCallback.Factory.Create<int>(this, v => page = v))
            .Add(x => x.Total, 320)
            .Add(x => x.PageSize, 20)
            .Add(x => x.ShowTotal, true));

        Assert.Equal("分页", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("1", cut.Find("[aria-current=page]").TextContent.Trim());
        Assert.True(cut.Find("button[aria-label=上一页]").HasAttribute("disabled"));
        Assert.Equal("共 320 条", cut.Find("nav > span").TextContent);

        cut.Find("button[aria-label=下一页]").Click();
        Assert.Equal(2, page);
        cut.Find("button[aria-label='第 16 页']").Click();
        Assert.Equal(16, page);
    }

    [Fact]
    public void 分页_给了Href时页码是链接_上一页下一页带rel()
    {
        var cut = Render<Pagination>(p => p.Add(x => x.Page, 3).Add(x => x.PageCount, 12).Add(x => x.Href, n => $"articles?page={n}"));

        Assert.Equal("articles?page=4", cut.Find("a[aria-label='第 4 页']").GetAttribute("href"));
        Assert.Equal("articles?page=2", cut.Find("a[rel=prev]").GetAttribute("href"));
        Assert.Equal("articles?page=4", cut.Find("a[rel=next]").GetAttribute("href"));
    }

    [Fact]
    public void 分页栏_显示范围_只有一页时没有跳页()
    {
        var cut = Render<PaginationBar>(p => p.Add(x => x.Page, 2).Add(x => x.PageSize, 20).Add(x => x.Total, 342));
        Assert.Contains("第 21–40 条，共 342 条", cut.Markup);
        Assert.NotEmpty(cut.FindAll("input[aria-label=跳到第几页]"));

        cut.Render(p => p.Add(x => x.Total, 8));
        Assert.Empty(cut.FindAll("input[aria-label=跳到第几页]"));
    }

    [Fact]
    public void 跳页_回车后夹到范围内写回()
    {
        var page = 1;
        var cut = Render<PageJump>(p => p
            .Add(x => x.Page, page)
            .Add(x => x.PageChanged, EventCallback.Factory.Create<int>(this, v => page = v))
            .Add(x => x.PageCount, 32));

        cut.Find("input").Change("99");
        Assert.Equal(32, page);
    }
}
