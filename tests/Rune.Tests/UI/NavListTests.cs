using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class NavListTests : BunitContext
{
    public NavListTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    static RenderFragment Item(string href, string text, NavLinkMatch match = NavLinkMatch.All) => builder =>
    {
        builder.OpenComponent<NavItem>(0);
        builder.AddAttribute(1, nameof(NavItem.Href), href);
        builder.AddAttribute(2, nameof(NavItem.Match), match);
        builder.AddAttribute(3, nameof(NavItem.ChildContent), (RenderFragment)(b => b.AddContent(0, text)));
        builder.CloseComponent();
    };

    [Fact]
    public void 只有锚点的地址接在当前页后面_当前页比较路径_前缀匹配时下级页面也算()
    {
        Navigation.NavigateTo("orders/42?tab=log");
        Assert.Equal("http://localhost/orders/42?tab=log#refund", NavLinks.Resolve(Navigation, "#refund"));
        Assert.True(NavLinks.IsCurrent(Navigation, "orders/42", NavLinkMatch.All));
        Assert.True(NavLinks.IsCurrent(Navigation, "Orders/42/", NavLinkMatch.All));
        Assert.False(NavLinks.IsCurrent(Navigation, "orders", NavLinkMatch.All));
        Assert.True(NavLinks.IsCurrent(Navigation, "orders", NavLinkMatch.Prefix));
        Assert.False(NavLinks.IsCurrent(Navigation, "order", NavLinkMatch.Prefix));
        Assert.False(NavLinks.IsCurrent(Navigation, "#refund", NavLinkMatch.All));
    }

    [Fact]
    public void 当前页带aria_current_含当前页的分组自动展开_换页后跟着变()
    {
        Navigation.NavigateTo("orders/refunds");
        var cut = Render<NavList>(p => p.AddChildContent<NavGroup>(g => g
            .Add(x => x.Label, "订单")
            .AddChildContent(b =>
            {
                Item("orders", "全部订单")(b);
                Item("orders/refunds", "退款")(b);
            })));

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.Equal("退款", cut.Find("a[aria-current=page]").TextContent.Trim());
        Assert.Equal("orders/refunds", cut.Find("a[aria-current=page]").GetAttribute("href"));

        Navigation.NavigateTo("orders");
        cut.WaitForAssertion(() => Assert.Equal("全部订单", cut.Find("a[aria-current=page]").TextContent.Trim()));
    }

    [Fact]
    public void 紧凑时各项只剩图标_文字视觉隐藏_小节标题只剩图标()
    {
        var cut = Render<NavList>(p => p.Add(x => x.Compact, (bool?)true).AddChildContent<NavSection>(s => s
            .Add(x => x.Label, "项目")
            .Add(x => x.Icon, BuiltInIcons.Star)
            .AddChildContent<NavItem>(i => i.Add(x => x.Href, "ledger").Add(x => x.Icon, BuiltInIcons.User).Add(x => x.Badge, 3).AddChildContent("台账"))));

        Assert.True(cut.Find("nav").HasAttribute("data-compact"));
        Assert.NotNull(cut.Find("a[data-rune-tooltip]"));
        Assert.Equal("台账", cut.Find("[role=tooltip]").TextContent.Trim());
        // 角标在紧凑时是图标上的小圆点，读屏念出数目
        Assert.Contains("3", cut.Find("a").TextContent);
        Assert.Equal(cut.Find("ul[aria-labelledby]").GetAttribute("aria-labelledby"), cut.Find("li > span > span[id]").Id);
    }
}
