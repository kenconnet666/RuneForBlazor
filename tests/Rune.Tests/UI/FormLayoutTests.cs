using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Tests.TestComponents;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>字段布局（FieldLayout）的层叠、表单分组、错误汇总与提交没通过时的焦点、离开确认。</summary>
public class FormLayoutTests : BunitContext
{
    public FormLayoutTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 字段布局_里层写了的优先_只读与不可用取并集()
    {
        var outer = new FieldLayout(FieldLabelPlacement.Start, CssLength.Em(6), FieldLabelAlign.End, ReadOnly: true);
        var inner = new FieldLayout(FieldLabelPlacement.Top, Disabled: true);

        var merged = inner.Over(outer);

        Assert.Equal(new FieldLayout(FieldLabelPlacement.Top, CssLength.Em(6), FieldLabelAlign.End, Disabled: true, ReadOnly: true), merged);
        Assert.Same(inner, inner.Over(null));
    }

    [Fact]
    public void 表单的只读交给控件_分组覆盖标签位置并加上不可用()
    {
        var cut = Render<FormLayoutHost>(p => p.Add(x => x.FormReadOnly, true));

        Assert.True(cut.Find("[data-testid=name]").HasAttribute("readonly"));
        Assert.True(cut.Find("form").HasAttribute("data-readonly"));
        var note = cut.Find("[data-testid=note]");
        Assert.True(note.HasAttribute("disabled"));
        Assert.True(note.HasAttribute("readonly"));
        // 表单上的标签在起始边、宽 6em；分组改回顶部
        var fields = cut.FindAll("[data-scope=field][data-part=root]");
        Assert.Equal("--rune-field-label-width:6em", fields[0].GetAttribute("style"));
        Assert.NotEqual(fields[0].ClassName, fields[2].ClassName);
        Assert.Equal(fields[2].ClassName, Render<Field>(p => p.Add(x => x.Label, "意见").AddChildContent<TextField>()).Find("[data-scope=field][data-part=root]").ClassName);
    }

    [Fact]
    public void 分组是以标题为名称的group_可收起时标题是按钮()
    {
        var open = true;
        var cut = Render<FormSection>(p => p
            .Add(x => x.Title, "高级选项")
            .Add(x => x.HeadingLevel, 2)
            .Add(x => x.Collapsible, true)
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v))
            .AddChildContent<Field>(f => f.Add(x => x.Label, "通知").AddChildContent<TextField>()));

        var group = cut.Find("[role=group]");
        var heading = cut.Find("h2");
        var trigger = heading.QuerySelector("button")!;
        var name = cut.Find("#" + group.GetAttribute("aria-labelledby"));
        Assert.Equal("高级选项", name.TextContent.Trim());
        Assert.NotNull(name.Closest("h2"));
        Assert.Equal("true", trigger.GetAttribute("aria-expanded"));
        trigger.Click();
        Assert.False(open);
    }

    [Fact]
    public void 提交没通过_顶上列出错误并让JS把焦点放过去_改对一项少一项()
    {
        var cut = Render<FormLayoutHost>();
        Assert.Empty(cut.FindAll("[data-rune-error-summary]"));
        Assert.False(cut.Find("form").HasAttribute("data-rune-invalid-submit"));

        cut.Find("form").Submit();

        var summary = cut.Find("[data-rune-error-summary]");
        Assert.Equal("-1", summary.GetAttribute("tabindex"));
        Assert.Contains("有 2 项需要修改", summary.TextContent);
        var links = cut.FindAll("[data-rune-error-summary] a");
        Assert.Equal(cut.Find("[data-testid=name]").Id, links[0].GetAttribute("data-rune-focus"));
        Assert.Equal("1", cut.Find("form").GetAttribute("data-rune-invalid-submit"));

        cut.InvokeAsync(cut.Instance.FixName);
        cut.Find("[data-testid=name]").Change("张三");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-rune-error-summary] a")));

        cut.Find("form").Submit();
        Assert.Equal("2", cut.Find("form").GetAttribute("data-rune-invalid-submit"));
    }

    [Fact]
    public void 不要错误汇总时_提交没通过照样标出_交给JS聚焦第一个出错的控件()
    {
        var cut = Render<FormLayoutHost>(p => p.Add(x => x.ErrorSummary, false));

        cut.Find("form").Submit();

        Assert.Empty(cut.FindAll("[data-rune-error-summary]"));
        Assert.Equal("1", cut.Find("form").GetAttribute("data-rune-invalid-submit"));
        Assert.Equal("true", cut.Find("[data-testid=name]").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void 离开确认_有改动时站内跳转先问_确认后才走()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<LeaveConfirmation>(p => p.Add(x => x.When, true));

        cut.InvokeAsync(() => navigation.NavigateTo("/other"));

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("dialog").GetAttribute("data-open")));
        Assert.DoesNotContain("/other", navigation.Uri);
        Assert.Contains("离开这个页面？", cut.Markup);

        cut.FindAll("dialog button").Last().Click();
        cut.WaitForAssertion(() => Assert.EndsWith("/other", navigation.Uri));
    }

    [Fact]
    public void 离开确认_点链接由JS拦下交来命令_有改动时才放拦截标记()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<LeaveConfirmation>(p => p.Add(x => x.When, true));

        cut.Find("[data-rune-leave-guard]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "leave", Text = navigation.ToAbsoluteUri("/orders").ToString() });

        Assert.Equal("true", cut.Find("dialog").GetAttribute("data-open"));
        cut.FindAll("dialog button").Last().Click();
        cut.WaitForAssertion(() => Assert.EndsWith("/orders", navigation.Uri));

        cut.Render(p => p.Add(x => x.When, false));
        Assert.Empty(cut.FindAll("[data-rune-leave-guard]"));
    }

    [Fact]
    public void 离开确认_没有改动或只换查询串时不拦()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var dirty = Render<LeaveConfirmation>(p => p.Add(x => x.When, true));

        dirty.InvokeAsync(() => navigation.NavigateTo("?tab=2"));
        Assert.EndsWith("?tab=2", navigation.Uri);

        dirty.Render(p => p.Add(x => x.When, false));
        dirty.InvokeAsync(() => navigation.NavigateTo("/elsewhere"));
        Assert.EndsWith("/elsewhere", navigation.Uri);
    }
}
