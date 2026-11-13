using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>用户按钮：头像、名字、箭头；有说明时两行、双箭头；只剩头像时名字是按钮的名称；未声明的属性落到按钮上。</summary>
public class UserButtonTests : BunitContext
{
    public UserButtonTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 头像名字箭头_属性落到按钮上()
    {
        var cut = Render<UserButton>(p => p.Add(x => x.Name, "林小满").AddUnmatched("aria-haspopup", "menu"));
        var button = cut.Find("button");
        Assert.Equal(("user-button", "root"), (button.GetAttribute("data-scope"), button.GetAttribute("data-part")));
        Assert.Equal("menu", button.GetAttribute("aria-haspopup"));
        Assert.Null(button.GetAttribute("aria-label"));
        Assert.Equal("林小满", cut.Find("[data-part=name]").TextContent);
        Assert.Contains("chevron-down", cut.Find("[data-part=indicator]").OuterHtml);
    }

    [Fact]
    public void 有说明时两行双箭头_只剩头像时名字是按钮的名称()
    {
        var described = Render<UserButton>(p => p.Add(x => x.Name, "林小满").Add(x => x.Description, "采购部"));
        Assert.Equal("采购部", described.Find("[data-part=description]").TextContent);
        Assert.Contains("chevrons-up-down", described.Find("[data-part=indicator]").OuterHtml);

        // 名称只取旁边的文字（名字与说明）：头像也以名字为名称，算进去会念两遍
        var button = described.Find("button");
        Assert.Equal(["林小满", "采购部"], button.GetAttribute("aria-labelledby")!.Split(' ').Select(id => described.Find($"#{id}").TextContent));
        // 自己写了 aria-label 时用它
        var named = Render<UserButton>(p => p.Add(x => x.Name, "林小满").AddUnmatched("aria-label", "账号菜单"));
        Assert.Equal(("账号菜单", null), (named.Find("button").GetAttribute("aria-label"), named.Find("button").GetAttribute("aria-labelledby")));

        var compact = Render<UserButton>(p => p.Add(x => x.Name, "林小满").Add(x => x.Compact, true));
        Assert.Null(compact.Find("button").GetAttribute("aria-labelledby"));
        Assert.Equal("林小满", compact.Find("button").GetAttribute("aria-label"));
        Assert.Empty(compact.FindAll("[data-part=name], [data-part=indicator]"));
    }
}
