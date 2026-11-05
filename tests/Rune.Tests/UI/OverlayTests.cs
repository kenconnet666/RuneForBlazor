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
    public void 弹出框_触发元素拿到popovertarget_名称取触发元素_定位约定交给JS()
    {
        var cut = Render<Popover>(p => p.Add(x => x.Trigger, TriggerButton("筛选")).Add(x => x.Arrow, true).AddChildContent("内容"));

        var trigger = cut.Find("button");
        var overlay = cut.Find("[popover]");
        Assert.Equal(overlay.Id, trigger.GetAttribute("popovertarget"));
        Assert.Equal(overlay.Id, trigger.GetAttribute("aria-controls"));
        Assert.Equal("dialog", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal(trigger.Id, overlay.GetAttribute("aria-labelledby"));
        Assert.Equal("bottom-start", overlay.GetAttribute("data-rune-floating"));
        Assert.NotNull(overlay.QuerySelector("[data-rune-arrow]"));
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
    public void 提示_触发元素指向提示_手动的popover_不可用时只渲染触发元素()
    {
        var cut = Render<Tooltip>(p => p
            .Add(x => x.Trigger, TriggerButton("复制"))
            .Add(x => x.Delay, 0)
            .AddChildContent("复制链接"));

        var tooltip = cut.Find("[role=tooltip]");
        var trigger = cut.Find("button");
        Assert.Equal("复制链接", tooltip.TextContent);
        Assert.Equal("manual", tooltip.GetAttribute("popover"));
        Assert.Equal("top", tooltip.GetAttribute("data-rune-floating"));
        Assert.Equal("0", tooltip.GetAttribute("data-delay"));
        Assert.Equal(tooltip.Id, trigger.GetAttribute("data-rune-tooltip"));
        Assert.Equal(tooltip.Id, trigger.GetAttribute("aria-describedby"));

        cut.Render(p => p.Add(x => x.Disabled, true));
        Assert.Empty(cut.FindAll("[role=tooltip]"));
        Assert.Null(cut.Find("button").GetAttribute("data-rune-tooltip"));
    }

    [Fact]
    public void 悬停卡片_挂载后才输出面板_第一次打开才渲染内容()
    {
        var opened = new List<bool>();
        var cut = Render<HoverCard>(p => p
            .Add(x => x.Trigger, TriggerButton("@ada"))
            .Add(x => x.OpenChanged, (bool open) => opened.Add(open))
            .AddChildContent("<p>资料</p>"));

        var card = cut.Find("[popover]");
        Assert.Equal("manual", card.GetAttribute("popover"));
        Assert.Equal("card", card.GetAttribute("data-rune-hover-panel"));
        Assert.Equal(("700", "300"), (card.GetAttribute("data-delay"), card.GetAttribute("data-close-delay")));
        Assert.Equal(card.Id, cut.Find("button").GetAttribute("data-rune-hovercard"));
        Assert.Empty(cut.FindAll("[popover] p"));

        card.TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        Assert.Equal("资料", cut.Find("[popover] p").TextContent);
        Assert.Equal([true], opened);
    }

    [Fact]
    public async Task 气泡确认_确认期间加载中_保持打开时不关闭_取消时通知()
    {
        var cancelled = 0;
        var keepOpen = true;
        var cut = Render<Popconfirm>(p => p
            .Add(x => x.Title, "删除？")
            .Add(x => x.Description, "不可恢复")
            .Add(x => x.Trigger, TriggerButton("删除"))
            .Add(x => x.OnConfirm, EventCallback.Factory.Create<ConfirmEventArgs>(this, e => { e.KeepOpen = keepOpen; }))
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => cancelled++)));

        var overlay = cut.Find("[popover]");
        Assert.Equal("alertdialog", overlay.GetAttribute("role"));
        Assert.Equal("top", overlay.GetAttribute("data-rune-floating"));
        Assert.Equal("删除？", cut.Find($"#{overlay.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("不可恢复", cut.Find($"#{overlay.GetAttribute("aria-describedby")}").TextContent);
        await cut.InvokeAsync(() => overlay.TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true }));
        Assert.Equal("true", cut.Find("[popover]").GetAttribute("data-open"));

        // KeepOpen：仍然打开
        cut.FindAll("[popover] button")[1].Click();
        Assert.Equal("true", cut.Find("[popover]").GetAttribute("data-open"));

        // 确认：受控地关上，随后的 toggle 不算取消
        keepOpen = false;
        cut.FindAll("[popover] button")[1].Click();
        Assert.Equal("false", cut.Find("[popover]").GetAttribute("data-open"));
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false });
        Assert.Equal(0, cancelled);

        // 再打开后按 Esc（toggle 关闭）算取消
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        cut.Find("[popover]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false });
        Assert.Equal(1, cancelled);
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
        var cut = Render<Drawer>(p => p.Add(x => x.Trigger, TriggerButton("筛选")).Add(x => x.Side, DrawerSide.Start));

        Assert.Equal(cut.Find("dialog").Id, cut.Find("button[data-rune-open]").GetAttribute("data-rune-open"));
        Assert.Equal("drawer", cut.Find("dialog").GetAttribute("data-scope"));
    }

    [Fact]
    public void 气泡确认_取消自动聚焦并只负责关闭_确认触发回调()
    {
        var confirmed = 0;
        var cut = Render<Popconfirm>(p => p
            .Add(x => x.Title, "删除这条记录？")
            .Add(x => x.OnConfirm, EventCallback.Factory.Create<ConfirmEventArgs>(this, () => confirmed++)));

        var buttons = cut.FindAll("[popover] button");
        Assert.True(buttons[0].HasAttribute("autofocus"));
        Assert.True(buttons[0].HasAttribute("data-rune-close"));
        buttons[1].Click();
        Assert.Equal(1, confirmed);
    }

    [Theory]
    [InlineData(Tone.Danger, "circle-exclamation-point")]
    [InlineData(Tone.Warning, "triangle-exclamation-point")]
    [InlineData(Tone.Success, "circle-check")]
    public void 气泡确认_色调决定图标_HideIcon不显示(Tone tone, string icon)
    {
        var cut = Render<Popconfirm>(p => p.Add(x => x.Title, "撤销发布？").Add(x => x.Tone, tone));
        Assert.Equal(icon, cut.Find("[data-part=icon]").GetAttribute("data-icon"));

        cut.Render(p => p.Add(x => x.HideIcon, true));
        Assert.Empty(cut.FindAll("[data-part=icon]"));
    }
}
