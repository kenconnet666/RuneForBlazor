using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class ComboboxTests : BunitContext
{
    public ComboboxTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly string[] cities = ["北京", "上海", "广州", "杭州"];

    [Fact]
    public void 输入框是列表宿主_弹层由data_rune_popover指出_展开按钮切换弹层()
    {
        var cut = Render<Combobox<string>>(p => p.Add(x => x.Items, cities).AddUnmatched("aria-label", "城市"));

        var input = cut.Find("input[role=combobox]");
        var panel = cut.Find("[popover]");
        var list = cut.Find("[role=listbox]");
        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal("off", input.GetAttribute("autocomplete"));
        Assert.Equal(list.Id, input.GetAttribute("aria-controls"));
        Assert.Equal("text", input.GetAttribute("data-rune-list"));
        Assert.Equal(panel.Id, input.GetAttribute("data-rune-popover"));
        Assert.Equal("clear", input.GetAttribute("data-rune-escape"));
        var toggle = cut.Find("button[aria-label=显示选项]");
        Assert.Equal(panel.Id, toggle.GetAttribute("popovertarget"));
        Assert.Equal("-1", toggle.GetAttribute("tabindex"));
        Assert.Null(panel.GetAttribute("data-rune-hide-empty"));
    }

    // 下面几个用例不受控（不传 ValueChanged），值从隐藏的 input（Name）读：bUnit 的 Bind 只登记回调，不会把新值传回组件

    [Fact]
    public void 输入即过滤_选中后填入文字_失焦时文字不是选项就恢复()
    {
        var cut = Render<Combobox<string>>(p => p.Add(x => x.Items, cities).Add(x => x.Name, "city"));

        cut.Find("input").Input("州");
        Assert.Equal(["广州", "杭州"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")));
        Assert.Equal("州", cut.Find("[role=listbox]").GetAttribute("data-rune-query"));

        cut.Find("[role=option][data-label='杭州']").Click();
        Assert.Equal("杭州", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("杭州", cut.Find("input[role=combobox]").GetAttribute("value"));
        // 选中后过滤词清空：再打开是全部选项
        Assert.Equal(4, cut.FindAll("[role=option]").Count);

        cut.Find("input[role=combobox]").Input("不存在");
        cut.Find("input[role=combobox]").Blur();
        Assert.Equal("杭州", cut.Find("input[role=combobox]").GetAttribute("value"));
    }

    [Fact]
    public void 允许自由输入_失焦后保留文字并取消选择_Esc命令清空()
    {
        var cut = Render<Combobox<string>>(p => p
            .Add(x => x.Items, cities)
            .Add(x => x.Value, "北京")
            .Add(x => x.AllowCustomValue, true)
            .Add(x => x.Name, "city"));

        Assert.Equal("北京", cut.Find("input[role=combobox]").GetAttribute("value"));
        cut.Find("input[role=combobox]").Input("架构师");
        cut.Find("input[role=combobox]").Blur();
        Assert.Null(cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("架构师", cut.Find("input[role=combobox]").GetAttribute("value"));

        cut.Find("input[role=combobox]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "clear" });
        Assert.Equal("", cut.Find("input[role=combobox]").GetAttribute("value"));
    }

    [Fact]
    public void 自动完成_值就是文字_没有展开按钮_没有建议时不弹出()
    {
        var cut = Render<Autocomplete<string>>(p => p
            .Add(x => x.Items, ["lin@qq.com", "lin@163.com"])
            .Add(x => x.LocalFilter, false)
            .Add(x => x.Name, "email"));

        Assert.Empty(cut.FindAll("button[aria-label=显示选项]"));
        Assert.NotNull(cut.Find("[popover]").GetAttribute("data-rune-hide-empty"));

        cut.Find("input[role=combobox]").Input("lin@");
        Assert.Equal("lin@", cut.Find("input[type=hidden]").GetAttribute("value"));
        cut.Find("[role=option][data-label='lin@163.com']").Click();
        Assert.Equal("lin@163.com", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("lin@163.com", cut.Find("input[role=combobox]").GetAttribute("value"));
        // 没有“选中的选项”：列表里不打勾
        Assert.Empty(cut.FindAll("[role=option][aria-selected=true]"));
    }
}
