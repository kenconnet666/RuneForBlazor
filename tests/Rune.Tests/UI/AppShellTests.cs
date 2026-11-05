using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class AppShellTests : BunitContext
{
    public AppShellTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    // 侧栏：一个只有图标的导航项（紧凑时文字藏起来）
    static readonly RenderFragment Nav = builder =>
    {
        builder.OpenComponent<NavList>(0);
        builder.AddAttribute(1, "aria-label", "后台");
        builder.AddAttribute(2, nameof(NavList.ChildContent), (RenderFragment)(b =>
        {
            b.OpenComponent<NavItem>(0);
            b.AddAttribute(1, nameof(NavItem.Href), "orders");
            b.AddAttribute(2, nameof(NavItem.Icon), BuiltInIcons.Search);
            b.AddAttribute(3, nameof(NavItem.ChildContent), (RenderFragment)(t => t.AddContent(0, "订单")));
            b.CloseComponent();
        }));
        builder.CloseComponent();
    };

    static readonly RenderFragment Brand = builder =>
    {
        builder.OpenComponent<AppShellBrand>(0);
        builder.AddAttribute(1, nameof(AppShellBrand.Href), "");
        builder.AddAttribute(2, nameof(AppShellBrand.Icon), BuiltInIcons.Star);
        builder.AddAttribute(3, nameof(AppShellBrand.ChildContent), (RenderFragment)(t => t.AddContent(0, "小满商城")));
        builder.CloseComponent();
    };

    [Fact]
    public void 跳到正文指向正文_菜单按钮由JS打开抽屉_抽屉点链接或变宽时关上()
    {
        var cut = Render<AppShell>(p => p.Add(x => x.Sidebar, Nav).Add(x => x.ChildContent, "<p>正文</p>"));
        var main = cut.Find("main");
        Assert.Equal("-1", main.GetAttribute("tabindex"));
        Assert.Equal(main.Id, cut.Find("a[data-rune-focus]").GetAttribute("data-rune-focus"));

        var menu = cut.Find("header button[aria-haspopup=dialog]");
        Assert.Equal("打开导航", menu.GetAttribute("aria-label"));
        var drawer = cut.Find("dialog");
        Assert.Equal(drawer.Id, menu.GetAttribute("data-rune-open"));
        Assert.True(drawer.HasAttribute("data-rune-close-on-link"));
        Assert.Equal(AppShellStyles.WideQuery, drawer.GetAttribute("data-rune-close-when"));
        Assert.Equal("导航", cut.Find($"#{drawer.Id}-title").TextContent);
    }

    [Fact]
    public void 没有侧栏时没有菜单按钮与抽屉_页脚右栏顶栏导航按需渲染()
    {
        var cut = Render<AppShell>(p => p.Add(x => x.ChildContent, "<p>正文</p>"));
        Assert.Empty(cut.FindAll("dialog"));
        Assert.Empty(cut.FindAll("header button[aria-haspopup]"));
        Assert.Empty(cut.FindAll("aside, footer"));

        cut.Render(p => p
            .Add(x => x.Aside, "<p>目录</p>")
            .Add(x => x.Footer, "<small>版本 3.2.0</small>")
            .Add(x => x.Nav, "<a href=\"#a\">模块</a>"));
        Assert.Equal("目录", cut.Find("aside").TextContent.Trim());
        Assert.Contains("版本 3.2.0", cut.Find("footer").TextContent);
        Assert.Equal("模块", cut.Find("header a[href='#a']").TextContent);
    }

    [Fact]
    public void 收起侧栏_导航变紧凑_品牌名只留给读屏_抽屉里仍完整()
    {
        var collapsed = false;
        var cut = Render<AppShell>(p => p
            .Add(x => x.Brand, Brand)
            .Add(x => x.Sidebar, Nav)
            .Add(x => x.SidebarCollapsible, true)
            .Add(x => x.SidebarCollapsed, collapsed)
            .Add(x => x.SidebarCollapsedChanged, (bool v) => collapsed = v));
        var name = cut.Find("header a span");
        var expandedClass = name.ClassName;
        Assert.Null(cut.Find("nav").GetAttribute("data-compact"));

        cut.Find("button[aria-label='收起侧栏']").Click();
        Assert.True(collapsed);
        cut.Render(p => p.Add(x => x.SidebarCollapsed, collapsed));
        // 抽屉的内容在第一次打开时才渲染
        cut.Find("dialog").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });

        var navs = cut.FindAll("nav[aria-label='后台']");
        Assert.Equal(2, navs.Count);
        Assert.True(navs[0].HasAttribute("data-compact"));
        Assert.False(navs[1].HasAttribute("data-compact"));
        Assert.NotEqual(expandedClass, cut.Find("header a span").ClassName);
        Assert.NotNull(cut.Find("button[aria-label='展开侧栏']"));
    }

    [Fact]
    public void 全屏_隐藏顶栏_右上角退出_JS报告离开全屏时退出()
    {
        var fullscreen = false;
        var cut = Render<AppShell>(p => p
            .Add(x => x.Sidebar, Nav)
            .Add(x => x.Fullscreen, fullscreen)
            .Add(x => x.FullscreenChanged, (bool v) => fullscreen = v));
        var root = cut.Find("[data-rune-fullscreen]");
        Assert.Equal("false", root.GetAttribute("data-rune-fullscreen"));
        var headerClass = cut.Find("header").ClassName;

        cut.Find("header button[title='全屏']").Click();
        Assert.True(fullscreen);
        cut.Render(p => p.Add(x => x.Fullscreen, fullscreen));
        Assert.Equal("true", cut.Find("[data-rune-fullscreen]").GetAttribute("data-rune-fullscreen"));
        Assert.NotEqual(headerClass, cut.Find("header").ClassName);
        Assert.NotNull(cut.Find("button[aria-label='退出全屏']"));

        cut.Find("[data-rune-fullscreen]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "fullscreen-exit" });
        Assert.False(fullscreen);
    }

    [Fact]
    public void 静态渲染时没有全屏与收起按钮_不可全屏时不写全屏属性()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<AppShell>(p => p.Add(x => x.Sidebar, Nav).Add(x => x.SidebarCollapsible, true));
        Assert.Empty(cut.FindAll("button[title='全屏'], button[aria-label='收起侧栏']"));
        Assert.NotNull(cut.Find("header button[data-rune-open]"));

        cut.Render(p => p.Add(x => x.Fullscreenable, false));
        Assert.Empty(cut.FindAll("[data-rune-fullscreen]"));
    }

    [Fact]
    public void 对话框可以指定id_外部按钮据此打开()
    {
        var cut = Render<Dialog>(p => p.Add(x => x.Id, "settings").Add(x => x.Title, "设置"));
        var dialog = cut.Find("dialog");
        Assert.Equal("settings", dialog.Id);
        Assert.Equal("settings-title", dialog.GetAttribute("aria-labelledby"));
    }
}
