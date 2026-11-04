using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class OverlayTests : BunitContext
{
    public OverlayTests()
    {
        Services.AddRuneUI();
        // 浮层按是否交互决定内容何时渲染
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    static RenderFragment<Attrs> TriggerButton(string text) => attrs => builder =>
    {
        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, attrs!);
        builder.AddContent(2, text);
        builder.CloseElement();
    };

    [Fact]
    public void 弹出框_触发元素拿到popovertarget与锚点名()
    {
        var cut = Render<Popover>(p => p.Add(x => x.Trigger, TriggerButton("筛选")).AddChildContent("内容"));

        var trigger = cut.Find("button");
        var overlay = cut.Find("[popover]");
        Assert.Equal(overlay.Id, trigger.GetAttribute("popovertarget"));
        Assert.Equal("dialog", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal($"anchor-name:--{overlay.Id}", trigger.GetAttribute("style"));
        Assert.Equal($"position-anchor:--{overlay.Id}", overlay.GetAttribute("style"));
        Assert.Equal("auto", overlay.GetAttribute("popover"));
        Assert.Equal("dialog", overlay.GetAttribute("role"));
        // 不受控：不写 data-open，交给浏览器
        Assert.Null(overlay.GetAttribute("data-open"));
    }

    [Fact]
    public void 弹出框_第一次打开才渲染内容_之后保留()
    {
        var opened = new List<bool>();
        var cut = Render<Popover>(p => p
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, opened.Add))
            .AddChildContent("<p>内容</p>"));

        Assert.Empty(cut.FindAll("p"));
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        Assert.Single(cut.FindAll("p"));
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false });
        Assert.Single(cut.FindAll("p"));
        Assert.Equal([true, false], opened);
    }

    [Fact]
    public void 弹出框_受控时写data_open()
    {
        var cut = Render<Popover>(p => p.Add(x => x.Open, true).AddChildContent("<p>内容</p>"));

        Assert.Equal("true", cut.Find("[popover]").GetAttribute("data-open"));
        Assert.Single(cut.FindAll("p"));
    }

    [Fact]
    public void 菜单_菜单项不在Tab顺序里_不可用的点击无效()
    {
        var clicked = 0;
        var cut = Render<Menu>(p => p
            .Add(x => x.Trigger, TriggerButton("更多"))
            .AddChildContent<MenuItem>(item => item
                .Add(x => x.Disabled, true)
                .AddUnmatched("onclick", EventCallback.Factory.Create(this, () => clicked++))
                .AddChildContent("删除")));

        Assert.Equal("menu", cut.Find("button[popovertarget]").GetAttribute("aria-haspopup"));
        Assert.Equal("menu", cut.Find("[popover]").GetAttribute("role"));
        var item = cut.Find("[role=menuitem]");
        Assert.Equal("-1", item.GetAttribute("tabindex"));
        Assert.Equal("true", item.GetAttribute("aria-disabled"));
        Assert.Throws<MissingEventHandlerException>(() => item.Click());
        Assert.Equal(0, clicked);
    }

    [Fact]
    public void 提示_外层告诉JS提示的id_提示悬停时显示()
    {
        var cut = Render<Tooltip>(p => p.Add(x => x.Content, "复制链接").AddChildContent("<button>复制</button>"));

        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal("复制链接", tooltip.TextContent);
        Assert.Equal(tooltip.Id, cut.Find("[data-rune-tooltip-root]").GetAttribute("data-rune-tooltip-root"));
        var rule = Assert.Single(Registry.Rules, r => r.Contains($".{tooltip.ClassName}{{"));
        Assert.Contains($".{tooltip.ClassName}:is(:not([data-dismissed]):is(:hover, :has(:focus-visible)) > *){{", rule);
    }

    [Fact]
    public void 对话框_关闭时不渲染内容_打开后由JS同步原生状态()
    {
        var cut = Render<Dialog>(p => p.Add(x => x.Title, "编辑订单").AddChildContent("<p>表单</p>"));

        var dialog = cut.Find("dialog");
        Assert.Equal("false", dialog.GetAttribute("data-open"));
        Assert.Empty(cut.FindAll("p"));

        cut.Render(p => p.Add(x => x.Open, true));

        dialog = cut.Find("dialog");
        Assert.Equal("true", dialog.GetAttribute("data-open"));
        Assert.Equal(cut.Find("h2").Id, dialog.GetAttribute("aria-labelledby"));
        Assert.NotNull(cut.Find("button[data-rune-close]"));
        Assert.Single(cut.FindAll("p"));
    }

    [Fact]
    public void 对话框_Esc或遮罩关闭后通知父级()
    {
        var open = true;
        var cut = Render<Dialog>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, value => open = value)));

        cut.Find("dialog").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false });

        Assert.False(open);
    }

    [Fact]
    public void 对话框_触发元素由JS打开()
    {
        var cut = Render<Drawer>(p => p.Add(x => x.Trigger, TriggerButton("筛选")).Add(x => x.Placement, Placement.Start));

        Assert.Equal(cut.Find("dialog").Id, cut.Find("button[data-rune-open]").GetAttribute("data-rune-open"));
        Assert.Equal("drawer", cut.Find("dialog").GetAttribute("data-scope"));
    }

    [Fact]
    public void 气泡确认_取消自动聚焦_确认触发回调()
    {
        var confirmed = 0;
        var cut = Render<Popconfirm>(p => p
            .Add(x => x.Title, "删除这条记录？")
            .Add(x => x.OnConfirm, EventCallback.Factory.Create(this, () => confirmed++)));

        var buttons = cut.FindAll("[popover] button");
        Assert.True(buttons[0].HasAttribute("autofocus"));
        Assert.Equal("alertdialog", cut.Find("[popover]").GetAttribute("role"));
        buttons[1].Click();
        Assert.Equal(1, confirmed);
    }

    [Fact]
    public void 气泡确认_色调决定图标与确认按钮_取消触发回调()
    {
        var cancelled = 0;
        var cut = Render<Popconfirm>(p => p
            .Add(x => x.Title, "撤销发布？")
            .Add(x => x.Tone, Tone.Danger)
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => cancelled++)));

        Assert.Equal("triangle-exclamation-point", cut.Find("[data-part=icon]").GetAttribute("data-icon"));
        Assert.Equal(cut.Find("[data-part=title]").Id, cut.Find("[popover]").GetAttribute("aria-labelledby"));
        cut.FindAll("[popover] button")[0].Click();
        Assert.Equal(1, cancelled);
    }
}
