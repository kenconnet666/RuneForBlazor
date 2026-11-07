using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>正文列表与 ClientOnly：圆点 / 编号 / 图标、逐项覆盖图标、有序列表不沿用外层图标；ClientOnly 按是否交互渲染。</summary>
public class ListTests : BunitContext
{
    public ListTests() => Services.AddRuneUI();

    [Fact]
    public void 带图标的列表_写上role_项里图标在前_逐项覆盖()
    {
        var cut = Render<List>(p => p
            .Add(x => x.Icon, BuiltInIcons.Check)
            .AddChildContent<ListItem>(item => item.AddChildContent("不限项目"))
            .AddChildContent<ListItem>(item => item.Add(x => x.Icon, BuiltInIcons.Minus).AddChildContent("单点登录")));

        Assert.Equal("list", cut.Find("ul").GetAttribute("role"));
        var items = cut.FindAll("li");
        Assert.Equal(["check", "minus"], items.Select(li => li.QuerySelector("svg")!.GetAttribute("data-icon")));
        Assert.Equal("不限项目", items[0].QuerySelector("div")!.TextContent);
    }

    [Fact]
    public void 圆点列表没有role_有序列表里的项不沿用外层图标()
    {
        var cut = Render<List>(p => p
            .Add(x => x.Icon, BuiltInIcons.Check)
            .AddChildContent<OrderedList>(ol => ol.AddChildContent<ListItem>(item => item.AddChildContent("第一步"))));
        Assert.Empty(cut.FindAll("ol svg"));
        Assert.Equal("第一步", cut.Find("ol li").TextContent);

        var plain = Render<List>(p => p.AddChildContent<ListItem>(item => item.AddChildContent("一项")));
        Assert.Null(plain.Find("ul").GetAttribute("role"));
        Assert.Empty(plain.FindAll("svg"));
    }

    [Fact]
    public void ClientOnly_预渲染时画占位_交互渲染时画内容()
    {
        RenderFragment content = b => b.AddContent(0, "地图");
        RenderFragment fallback = b => b.AddContent(0, "骨架");

        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        Assert.Equal("骨架", Render<ClientOnly>(p => p.Add(x => x.ChildContent, content).Add(x => x.Fallback, fallback)).Markup);

        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        Assert.Equal("地图", Render<ClientOnly>(p => p.Add(x => x.ChildContent, content).Add(x => x.Fallback, fallback)).Markup);
    }
}
