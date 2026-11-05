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

    static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

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
    public void 菜单项_说明作为描述_快捷键注册在菜单项上_不关菜单的项带标记()
    {
        var cut = Render<MenuItem>(p => p
            .Add(x => x.Shortcut, "Mod+D")
            .Add(x => x.CloseOnSelect, false)
            .Add(x => x.Description, (RenderFragment)(b => b.AddContent(0, "复制到同一个文件夹")))
            .AddChildContent("创建副本"));

        var item = cut.Find("[role=menuitem]");
        Assert.Equal("创建副本", cut.Find($"#{item.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("复制到同一个文件夹", cut.Find($"#{item.GetAttribute("aria-describedby")}").TextContent);
        Assert.Equal(("Mod+D", "Control+D Meta+D"), (item.GetAttribute("data-rune-hotkey"), item.GetAttribute("aria-keyshortcuts")));
        Assert.True(item.HasAttribute("data-rune-keep-open"));
        // 显示成 Windows 的写法；读屏从 aria-keyshortcuts 念，显示的文字不重复念
        var shortcut = cut.Find("[data-rune-hotkey-label]");
        Assert.Equal(("Ctrl+D", "true"), (shortcut.TextContent, shortcut.GetAttribute("aria-hidden")));

        cut.Render(p => p.Add(x => x.Disabled, true));
        Assert.False(cut.Find("[role=menuitem]").HasAttribute("data-rune-hotkey"));
    }

    [Fact]
    public void 复选菜单项_点击切换勾选_默认不关菜单()
    {
        var checkedValue = false;
        var cut = Render<MenuCheckboxItem>(p => p
            .Add(x => x.Checked, checkedValue)
            .Add(x => x.CheckedChanged, v => checkedValue = v)
            .AddChildContent("显示网格"));

        var item = cut.Find("[role=menuitemcheckbox]");
        Assert.Equal("false", item.GetAttribute("aria-checked"));
        Assert.True(item.HasAttribute("data-rune-keep-open"));
        item.Click();
        Assert.True(checkedValue);
    }

    [Fact]
    public void 子菜单_触发项指向子菜单_子菜单以它为名称_不可用时打不开()
    {
        var cut = Render<MenuSub>(p => p
            .Add(x => x.Label, "移动到")
            .AddChildContent<MenuItem>(item => item.AddChildContent("设计稿")));

        var trigger = cut.Find("button[role=menuitem]");
        var panel = cut.Find("[data-rune-submenu]");
        Assert.Equal((panel.Id, "menu"), (trigger.GetAttribute("popovertarget"), trigger.GetAttribute("aria-haspopup")));
        Assert.Equal(("menu", trigger.Id, "right-start"), (panel.GetAttribute("role"), panel.GetAttribute("aria-labelledby"), panel.GetAttribute("data-rune-floating")));

        cut.Render(p => p.Add(x => x.Disabled, true));
        Assert.False(cut.Find("button[role=menuitem]").HasAttribute("popovertarget"));
    }

    [Fact]
    public void 链接菜单项与右键菜单()
    {
        var link = Render<MenuLinkItem>(p => p.Add(x => x.Href, "settings").Add(x => x.Disabled, true).AddChildContent("设置"));
        Assert.False(link.Find("a[role=menuitem]").HasAttribute("href"));

        var region = Render<ContextMenu>(p => p
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "文件")))
            .Add(x => x.MenuContent, (RenderFragment)(b => b.AddContent(0, "菜单项"))));
        var menu = region.Find("[data-rune-context-menu] > [role=menu]");
        Assert.Equal("更多操作", menu.GetAttribute("aria-label"));
        Assert.False(menu.HasAttribute("aria-labelledby"));
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
    public async Task 对话框_开关报告原因_确认后的原因是Confirm()
    {
        var open = true;
        var changes = new List<OverlayOpenChange>();
        var cut = Render<Dialog>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, value => open = value))
            .Add(x => x.OnOpenChange, EventCallback.Factory.Create<OverlayOpenChange>(this, changes.Add))
            .Add(x => x.OnConfirm, EventCallback.Factory.Create<ConfirmEventArgs>(this, () => { })));

        await cut.InvokeAsync(() => cut.Find("dialog").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false, Reason = "escape" }));
        Assert.Equal([new OverlayOpenChange(false, OverlayChangeReason.Escape)], changes);

        cut.Render(p => p.Add(x => x.Open, true));
        changes.Clear();
        await cut.InvokeAsync(() => cut.FindAll("dialog button").Last().Click());
        Assert.Equal([new OverlayOpenChange(false, OverlayChangeReason.Confirm)], changes);
        Assert.False(open);
    }

    [Fact]
    public void 确认对话框_alertdialog_取消先拿到焦点_没有关闭按钮_点遮罩不关()
    {
        var cut = Render<AlertDialog>(p => p.Add(x => x.Title, "删除这个项目？").Add(x => x.Open, true));

        var dialog = cut.Find("dialog");
        Assert.Equal("alertdialog", dialog.GetAttribute("role"));
        Assert.False(dialog.HasAttribute("data-rune-light-dismiss"));
        // 没写 OnConfirm 也有“取消 / 确认”，取消带 autofocus；标题行没有关闭按钮
        var buttons = cut.FindAll("dialog button");
        Assert.Equal(2, buttons.Count);
        Assert.True(buttons[0].HasAttribute("autofocus"));
    }

    [Fact]
    public async Task 命令式对话框_句柄改标题_关上得到原因()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<DialogService>();
        var changes = new List<OverlayOpenChange>();

        DialogHandle handle = null!;
        await host.InvokeAsync(() => handle = dialogs.Open(new DialogOptions
        {
            Title = "发票 FP-1",
            OnOpenChange = changes.Add,
            Content = h => b => b.AddContent(0, $"正文 {h.Options.Title}"),
        }));
        host.WaitForAssertion(() => Assert.Equal("发票 FP-1", host.Find("dialog h2").TextContent));
        Assert.Equal("true", host.Find("dialog").GetAttribute("data-open"));

        await host.InvokeAsync(() => handle.Update(o => o with { Title = "发票 FP-1（已核对）" }));
        host.WaitForAssertion(() => Assert.Equal("发票 FP-1（已核对）", host.Find("dialog h2").TextContent));

        await host.InvokeAsync(handle.Close);
        Assert.Equal(OverlayChangeReason.Imperative, await handle.Closed.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal([new OverlayOpenChange(true, OverlayChangeReason.Imperative), new OverlayOpenChange(false, OverlayChangeReason.Imperative)], changes);
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("dialog")), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task 命令式确认_必填理由没填不关_填了得到理由()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<DialogService>();

        Task<string?> asking = null!;
        // 语句体：不要让 InvokeAsync 等着确认的结果
        await host.InvokeAsync(() => { asking = dialogs.ConfirmAsync(new() { Title = "驳回？" }, new ConfirmReason("驳回理由") { Required = true }); });
        host.WaitForAssertion(() => Assert.Equal("alertdialog", host.Find("dialog").GetAttribute("role")));

        var confirm = () => host.FindAll("dialog button").Last();
        await host.InvokeAsync(() => confirm().Click());
        host.WaitForAssertion(() => Assert.Contains("必填", host.Markup));
        Assert.False(asking.IsCompleted);

        await host.InvokeAsync(() => host.Find("textarea").Change("缺少发票"));
        await host.InvokeAsync(() => confirm().Click());
        Assert.Equal("缺少发票", await asking.WaitAsync(TimeSpan.FromSeconds(5), Token));
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
