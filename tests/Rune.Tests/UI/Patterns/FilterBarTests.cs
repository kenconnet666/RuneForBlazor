using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>查询栏（同 vue-tsx-pro 的 FilterBar）：role="search" 的表单，提交是查询；More 在 JS 量出之前收着；展开、计数、重置。</summary>
public class FilterBarTests : BunitContext
{
    public FilterBarTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public async Task 提交就是查询_重置交给使用方_More收着并在展开旁计数()
    {
        int searched = 0, reset = 0;
        var expanded = false;
        var cut = Render<FilterBar>(p => p
            .Add(x => x.OnSearch, () => searched++)
            .Add(x => x.OnReset, () => reset++)
            .Add(x => x.MoreActive, 2)
            .Add(x => x.ExpandedChanged, v => expanded = v)
            .AddChildContent("<div class=\"common\">名称</div>")
            .Add(x => x.More, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"rare\">年度</div>"))));

        var form = cut.Find("form");
        Assert.Equal(("search", "14em", "false"), (form.GetAttribute("role"), form.GetAttribute("data-rune-filter-bar"), form.GetAttribute("data-expanded")));
        // 常用的与 More 在同一个栅格里，More 外面一层（量出来之前收着）
        Assert.NotNull(cut.Find("""[data-part="fields"] > .common"""));
        Assert.NotNull(cut.Find("""[data-part="fields"] > [data-part="more"] > .rare"""));

        var toggle = cut.Find("""[data-part="toggle"]""");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Contains("2", toggle.TextContent);

        await cut.InvokeAsync(() => form.Submit());
        Assert.Equal(1, searched);
        await cut.InvokeAsync(() => cut.Find("""[data-part="reset"]""").Click());
        Assert.Equal(1, reset);

        await cut.InvokeAsync(() => cut.Find("""[data-part="toggle"]""").Click());
        Assert.True(expanded);
        Assert.Equal("true", cut.Find("form").GetAttribute("data-expanded"));
        Assert.DoesNotContain("2", cut.Find("""[data-part="toggle"]""").TextContent);
    }

    [Fact]
    public void 标签默认在起始边_宽度随文字()
    {
        var cut = Render<FilterBar>(p => p.AddChildContent<Field>(f => f.Add(x => x.Label, "名称").AddChildContent<TextField>()));

        Assert.Equal("start", cut.Find("""[data-scope="field"]""").GetAttribute("data-label-placement"));
    }
}
