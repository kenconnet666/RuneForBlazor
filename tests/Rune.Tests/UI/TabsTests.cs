using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class TabsTests : BunitContext
{
    public TabsTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static RenderFragment Content(string[] values, bool panels = true, Action<string>? close = null, object? owner = null) => builder =>
    {
        builder.OpenComponent<TabList>(0);
        builder.AddAttribute(1, nameof(TabList.ChildContent), (RenderFragment)(list =>
        {
            foreach (var value in values)
            {
                list.OpenComponent<Tab>(0);
                list.SetKey(value);
                list.AddAttribute(1, nameof(Tab.Value), value);
                if (close is not null) list.AddAttribute(2, nameof(Tab.OnClose), EventCallback.Factory.Create(owner!, () => close(value)));
                list.AddAttribute(3, nameof(Tab.ChildContent), (RenderFragment)(b => b.AddContent(0, value.ToUpperInvariant())));
                list.CloseComponent();
            }
        }));
        builder.CloseComponent();
        if (!panels) return;
        foreach (var value in values)
        {
            builder.OpenComponent<TabPanel>(10);
            builder.SetKey(value);
            builder.AddAttribute(11, nameof(TabPanel.Value), value);
            builder.AddAttribute(12, nameof(TabPanel.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{value} 的内容")));
            builder.CloseComponent();
        }
    };

    [Fact]
    public void 不写值时选中第一个_标签与面板互相引用_其余面板隐藏()
    {
        var cut = Render<Tabs>(p => p.Add(x => x.ChildContent, Content(["a", "b"])));

        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal(["true", "false"], tabs.Select(t => t.GetAttribute("aria-selected")));
        var panel = cut.Find($"#{tabs[0].GetAttribute("aria-controls")}");
        Assert.Equal("tabpanel", panel.GetAttribute("role"));
        Assert.Equal(tabs[0].Id, panel.GetAttribute("aria-labelledby"));
        Assert.False(panel.HasAttribute("hidden"));
        Assert.True(cut.Find($"#{tabs[1].GetAttribute("aria-controls")}").HasAttribute("hidden"));
        Assert.Equal("horizontal", cut.Find("[role=tablist]").GetAttribute("aria-orientation"));
    }

    [Fact]
    public void 点击切换_自动模式聚焦也切换_手动模式聚焦不切换()
    {
        string? value = null;
        var cut = Render<Tabs>(p => p.Add(x => x.ValueChanged, (string? v) => value = v).Add(x => x.ChildContent, Content(["a", "b", "c"])));
        cut.FindAll("[role=tab]")[1].Click();
        Assert.Equal("b", value);
        cut.FindAll("[role=tab]")[2].Focus();
        Assert.Equal("c", value);

        var manual = Render<Tabs>(p => p.Add(x => x.Activation, TabsActivation.Manual).Add(x => x.ChildContent, Content(["a", "b"])));
        Assert.Throws<MissingEventHandlerException>(() => manual.FindAll("[role=tab]")[1].Focus());
    }

    [Fact]
    public void 只当导航的标签没有面板时不写aria_controls()
    {
        var cut = Render<Tabs>(p => p.Add(x => x.ChildContent, Content(["a"], panels: false)));
        Assert.False(cut.Find("[role=tab]").HasAttribute("aria-controls"));
    }

    [Fact]
    public void 关掉当前标签时选后面的一个_关闭按钮只给指针用()
    {
        var cut = Render<Tabs>(p => p.Add(x => x.ChildContent, Content(["a", "b", "c"], close: _ => { }, owner: this)));
        cut.FindAll("[role=tab]")[1].Click();
        Assert.Equal("Delete", cut.Find("[role=tab][aria-selected=true]").GetAttribute("aria-keyshortcuts"));
        var close = cut.Find("[data-rune-tab-close]");
        Assert.Equal(("true", "-1"), (close.GetAttribute("aria-hidden"), close.GetAttribute("tabindex")));

        // 使用方移除了当前标签 b：选它后面的 c
        cut.Render(p => p.Add(x => x.ChildContent, Content(["a", "c"])));
        Assert.Equal("C", cut.Find("[role=tab][aria-selected=true]").TextContent);
    }

    [Fact]
    public void 懒渲染时面板第一次显示才有内容()
    {
        var cut = Render<Tabs>(p => p.Add(x => x.Lazy, true).Add(x => x.ChildContent, Content(["a", "b"])));
        Assert.DoesNotContain("b 的内容", cut.Markup);
        cut.FindAll("[role=tab]")[1].Click();
        Assert.Contains("b 的内容", cut.Markup);
    }
}
