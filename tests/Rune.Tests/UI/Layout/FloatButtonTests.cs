using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>浮动按钮：单个固定在角落、组里跟着组排；角标；速拨的展开、收起与 JS 交来的收起；BackTop 是一个 FloatButton。</summary>
public class FloatButtonTests : BunitContext
{
    public FloatButtonTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    IReadOnlyList<string> Rules => Services.GetRequiredService<StyleRegistry>().Rules;

    bool IsFixed(AngleSharp.Dom.IElement element) =>
        Rules.Any(rule => element.ClassList.Any(c => rule.Contains($".{c}{{")) && rule.Contains("position:fixed"));

    [Fact]
    public void 单个按钮固定在角落_组里不固定_带角标时角标那层固定()
    {
        var single = Render<FloatButton>(p => p.Add(x => x.Icon, BuiltInIcons.Plus).Add(x => x.Label, "新建").Add(x => x.ShowTooltip, false));
        Assert.Equal("新建", single.Find("button").GetAttribute("aria-label"));
        Assert.True(IsFixed(single.Find("button")));

        var group = Render<FloatButtonGroup>(p => p.Add(x => x.Label, "帮助")
            .AddChildContent<FloatButton>(b => b.Add(x => x.Icon, BuiltInIcons.Plus).Add(x => x.Label, "新建").Add(x => x.ShowTooltip, false)));
        Assert.Equal("帮助", group.Find("[role=group]").GetAttribute("aria-label"));
        Assert.True(IsFixed(group.Find("[role=group]")));
        Assert.False(IsFixed(group.Find("button")));

        var badged = Render<FloatButton>(p => p.Add(x => x.Icon, BuiltInIcons.Plus).Add(x => x.Label, "客服").Add(x => x.Badge, 2).Add(x => x.ShowTooltip, false));
        Assert.False(IsFixed(badged.Find("button")));
        Assert.Contains("2", badged.Markup);
    }

    [Fact]
    public void 速拨_主按钮展开收起_JS交来的收起()
    {
        var open = false;
        var cut = Render<FloatButtonGroup>(p => p
            .Add(x => x.Icon, BuiltInIcons.Plus)
            .Add(x => x.Label, "新建")
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v))
            .AddChildContent<FloatButton>(b => b.Add(x => x.Icon, BuiltInIcons.Plus).Add(x => x.Label, "新建文档")));
        var trigger = cut.Find("[data-rune-speed-dial-trigger]");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal(trigger.GetAttribute("aria-controls"), cut.Find("[role=group] > div").Id);
        Assert.Equal("closed", cut.Find("[role=group]").GetAttribute("data-rune-speed-dial"));

        trigger.Click();
        Assert.True(open);
        cut.Render(p => p.Add(x => x.Open, open));
        Assert.Equal("true", cut.Find("[data-rune-speed-dial-trigger]").GetAttribute("aria-expanded"));
        Assert.Equal("open", cut.Find("[role=group]").GetAttribute("data-rune-speed-dial"));

        cut.Find("[role=group]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "close" });
        Assert.False(open);
    }

    [Fact]
    public void BackTop是FloatButton_组里不自己固定()
    {
        var single = Render<BackTop>();
        var button = single.Find("button[data-rune-back-top]");
        Assert.Equal("true", button.GetAttribute("aria-hidden"));
        Assert.True(IsFixed(button));

        var grouped = Render<FloatButtonGroup>(p => p.AddChildContent<BackTop>());
        Assert.False(IsFixed(grouped.Find("button[data-rune-back-top]")));
    }
}
