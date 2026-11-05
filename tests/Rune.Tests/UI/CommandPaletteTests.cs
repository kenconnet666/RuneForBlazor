using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class CommandPaletteTests : BunitContext
{
    public CommandPaletteTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly ListboxOption[] items =
    [
        new("orders", "订单", Description: "全部订单、退款与售后"),
        new("reports", "报表", Description: "销售、库存、流量"),
        new("logout", "退出登录", Disabled: true),
    ];

    [Fact]
    public void 触发按钮打开对话框_快捷键写在对话框上_输入框是列表的宿主()
    {
        var cut = Render<CommandPalette<ListboxOption>>(p => p.Add(x => x.Items, items));
        var trigger = cut.Find("button[data-rune-open]");
        var dialog = cut.Find("dialog");
        Assert.Equal(dialog.Id, trigger.GetAttribute("data-rune-open"));
        Assert.Equal("Control+K Meta+K", trigger.GetAttribute("aria-keyshortcuts"));
        Assert.Equal("mod+k", dialog.GetAttribute("data-rune-hotkey"));
        var input = cut.Find("input[role=combobox]");
        Assert.Equal("text first", input.GetAttribute("data-rune-list"));
        Assert.Equal(cut.Find("[role=listbox]").Id, input.GetAttribute("aria-controls"));
    }

    [Fact]
    public void 按文字或说明过滤_执行后关闭_不可用项不执行()
    {
        ListboxOption? done = null;
        var cut = Render<CommandPalette<ListboxOption>>(p => p.Add(x => x.Items, items).Add(x => x.OnAction, (ListboxOption o) => done = o));
        cut.Find("input[role=combobox]").Input("库存");
        Assert.Single(cut.FindAll("[role=option]"));
        Assert.StartsWith("报表", cut.Find("[role=option]").TextContent.Trim());
        cut.Find("[role=option]").Click();
        Assert.Equal("reports", done?.Value);

        done = null;
        cut.Find("input[role=combobox]").Input("退出");
        cut.Find("[role=option]").Click();
        Assert.Null(done);
    }
}
