using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.Tests.TestComponents;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>展开收起：Collapse、Collapsible（Disclosure）、Accordion。</summary>
public class AccordionTests : BunitContext
{
    public AccordionTests() => Services.AddRuneUI();

    // 触发器：普通的 button，把 Collapsible 交来的属性展开上去
    static readonly RenderFragment<Attrs> Trigger = attrs => builder =>
    {
        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, attrs);
        builder.AddContent(2, "高级选项");
        builder.CloseElement();
    };

    IRenderedComponent<Collapsible> RenderCollapsible(Action<ComponentParameterCollectionBuilder<Collapsible>>? parameters = null) =>
        Render<Collapsible>(p =>
        {
            p.Add(c => c.Trigger, Trigger).AddChildContent("内容");
            parameters?.Invoke(p);
        });

    [Fact]
    public void Collapsible_触发器拿到aria属性与切换标记_JS切换后写回()
    {
        var cut = RenderCollapsible();
        var button = cut.Find("button");
        Assert.Equal("false", button.GetAttribute("aria-expanded"));
        Assert.Equal(cut.Find("[data-rune-collapse]").Id, button.GetAttribute("aria-controls"));
        // 自动生成的 id 预渲染与 WebAssembly 里不同：不要求接管后恢复
        Assert.Equal("", button.GetAttribute("data-rune-disclose"));

        // 点击由 JS 当场切换，再发 rune-toggle：这里模拟 JS 发的那一次
        Toggle(cut, true);
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.Equal("open", cut.Find("[data-rune-collapse]").GetAttribute("data-state"));

        Toggle(cut, false);
        Assert.Equal("closed", cut.Find("[data-rune-collapse]").GetAttribute("data-state"));

        // 写了 id：接管后恢复（keep）
        var kept = RenderCollapsible(p => p.AddUnmatched("id", "advanced"));
        Assert.Equal(("advanced", "keep"), (kept.Find("button").GetAttribute("aria-controls"), kept.Find("button").GetAttribute("data-rune-disclose")));
    }

    [Fact]
    public void Collapsible_父级传来新值时跟着父级_不可用时点击无效()
    {
        var cut = RenderCollapsible();
        cut.Render(p => p.Add(c => c.Open, true));
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));

        var off = RenderCollapsible(p => p.Add(c => c.Disabled, true));
        Assert.True(off.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Collapsible_双向绑定写回父级_父级不采纳时保持原状()
    {
        var host = Render<BindHost>();
        host.Find("[data-rune-collapse]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        Assert.Equal("True", host.Find("#host-open").TextContent);

        var requested = new List<bool>();
        var cut = RenderCollapsible(p => p.Add(c => c.OpenChanged, open => requested.Add(open)));
        Toggle(cut, true);
        Assert.Equal([true], requested);
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Collapse_收起时仍在页面里且inert_展开时去掉()
    {
        var cut = RenderCollapsible();
        var content = cut.Find("[data-rune-collapse]");
        Assert.Equal(("closed", true), (content.GetAttribute("data-state"), content.HasAttribute("inert")));
        Assert.Contains("内容", content.TextContent);

        Toggle(cut, true);
        content = cut.Find("[data-rune-collapse]");
        Assert.Equal(("open", false), (content.GetAttribute("data-state"), content.HasAttribute("inert")));
    }

    [Fact]
    public void Collapse_页内查找命中时JS发的rune_toggle让它展开_Lazy时第一次展开才渲染()
    {
        var cut = Render<Collapse>(p => p.Add(x => x.Lazy, true).Add(x => x.ChildContent, b => b.AddContent(0, "很重的内容")));
        Assert.DoesNotContain("很重的内容", cut.Markup);

        cut.Find("[data-rune-collapse]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        Assert.Equal("open", cut.Find("[data-rune-collapse]").GetAttribute("data-state"));
        Assert.Contains("很重的内容", cut.Markup);
    }

    static void Toggle(IRenderedComponent<Collapsible> cut, bool open) =>
        cut.Find("[data-rune-collapse]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = open });

    IRenderedComponent<Accordion> RenderAccordion(Action<ComponentParameterCollectionBuilder<Accordion>> parameters) =>
        Render<Accordion>(p =>
        {
            parameters(p);
            p.AddChildContent<AccordionItem>(i => i.Add(x => x.Value, "ship").Add(x => x.Title, "发货").AddChildContent("48 小时内"));
            p.AddChildContent<AccordionItem>(i => i.Add(x => x.Value, "refund").Add(x => x.Title, "退款").AddChildContent("原路退回"));
            p.AddChildContent<AccordionItem>(i => i.Add(x => x.Value, "member").Add(x => x.Title, "会员").Add(x => x.Disabled, true));
        });

    [Fact]
    public void Accordion_标题是heading里的按钮_内容是以标题为名称的region_方向键交给JS()
    {
        var cut = RenderAccordion(p => p.Add(x => x.HeadingLevel, 2));

        var trigger = cut.Find("h2 > button");
        var region = cut.Find("[role=region]");
        Assert.Equal(region.Id, trigger.GetAttribute("aria-controls"));
        Assert.Equal(trigger.Id, region.GetAttribute("aria-labelledby"));
        Assert.Equal("vertical tabbable", cut.Find("[data-scope=accordion][data-part=root]").GetAttribute("data-rune-roving"));
        Assert.True(cut.FindAll("h2 > button")[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Accordion_单开时展开一项收起另一项_再点收起_不可收起时保留唯一的一项()
    {
        string? open = "ship";
        var cut = RenderAccordion(p => p.Add(x => x.Value, open).Add(x => x.ValueChanged, (string? v) => open = v));
        var triggers = cut.FindAll("button");
        Assert.Equal(["true", "false"], triggers.Take(2).Select(t => t.GetAttribute("aria-expanded")));

        cut.FindAll("button")[1].Click();
        Assert.Equal("refund", open);
        cut.FindAll("button")[1].Click();
        Assert.Null(open);

        string? kept = "ship";
        var strict = RenderAccordion(p => p.Add(x => x.Value, kept).Add(x => x.Collapsible, false).Add(x => x.ValueChanged, (string? v) => kept = v));
        strict.FindAll("button")[0].Click();
        Assert.Equal("ship", kept);
    }

    [Fact]
    public void Accordion_多开时各项独立()
    {
        IReadOnlyList<string> open = ["ship"];
        var cut = RenderAccordion(p => p
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Values, open)
            .Add(x => x.ValuesChanged, (IReadOnlyList<string> v) => open = v));

        cut.FindAll("button")[1].Click();

        Assert.Equal(["ship", "refund"], open);
    }
}
