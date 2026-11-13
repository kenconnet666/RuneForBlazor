using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 不受控（不传 ValueChanged），值从隐藏的 input（Name）读：bUnit 的 Bind 只登记回调，不会把新值传回组件
public class TagsInputTests : BunitContext
{
    public TagsInputTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    IRenderedComponent<TagsInput<string>> RenderTags(Action<ComponentParameterCollectionBuilder<TagsInput<string>>>? more = null) =>
        Render<TagsInput<string>>(p =>
        {
            p.Add(x => x.Value, ["a"]).Add(x => x.Name, "tags");
            more?.Invoke(p);
        });

    static string[] Values(IRenderedComponent<TagsInput<string>> cut) =>
        [.. cut.FindAll("input[type=hidden]").Select(i => i.GetAttribute("value")!)];

    static AngleSharp.Dom.IElement Input(IRenderedComponent<TagsInput<string>> cut) => cut.Find("input:not([type=hidden])");

    [Fact]
    public void 分隔符前的部分加成标签_重复的不加_上限之外不加()
    {
        var cut = RenderTags(p => p.Add(x => x.Max, 3));

        Input(cut).Input("b，a；c,d");
        Assert.Equal(["a", "b", "c"], Values(cut));
        // 最后一段留在输入框里
        Assert.Equal("d", Input(cut).GetAttribute("value"));
    }

    [Fact]
    public void Enter_退格_粘贴命令()
    {
        var cut = RenderTags();
        var input = Input(cut);
        Assert.Equal("add", input.GetAttribute("data-rune-enter"));
        Assert.Equal("remove-last", input.GetAttribute("data-rune-backspace"));
        Assert.Equal(",，;；", input.GetAttribute("data-rune-delimiters"));

        Input(cut).Input("b");
        Input(cut).TriggerEvent("onrunecommand", new CommandEventArgs { Name = "add" });
        Assert.Equal(["a", "b"], Values(cut));
        Assert.Equal("", Input(cut).GetAttribute("value"));

        Input(cut).TriggerEvent("onrunecommand", new CommandEventArgs { Name = "paste", Text = "c\nd,b" });
        Assert.Equal(["a", "b", "c", "d"], Values(cut));

        Input(cut).TriggerEvent("onrunecommand", new CommandEventArgs { Name = "remove-last" });
        Assert.Equal(["a", "b", "c"], Values(cut));

        // 只读时哪条路都不加：粘贴、输入分隔符
        cut.Render(p => p.Add(x => x.ReadOnly, true));
        Input(cut).TriggerEvent("onrunecommand", new CommandEventArgs { Name = "paste", Text = "x,y" });
        Input(cut).Input("z,");
        Assert.Equal(["a", "b", "c"], Values(cut));
    }

    [Fact]
    public void 只允许建议时_文字或值对上建议才加_标签显示建议的文字()
    {
        ListboxOption[] team = [new("cm@x", "陈默"), new("xy@x", "许悠")];
        var cut = Render<TagsInput<ListboxOption>>(p => p.Add(x => x.Items, team).Add(x => x.AllowCustomValue, false).Add(x => x.Name, "cc"));
        var input = cut.Find("input[role=combobox]");

        input.Input("陈默");
        cut.Find("input[role=combobox]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "add" });
        cut.Find("input[role=combobox]").Input("不在名单");
        cut.Find("input[role=combobox]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "add" });

        Assert.Equal(["cm@x"], cut.FindAll("input[type=hidden]").Select(i => i.GetAttribute("value")));
        Assert.Equal("陈默", cut.Find("[data-scope=badge]").TextContent.Trim());
        Assert.Equal("不在名单", cut.Find("input[role=combobox]").GetAttribute("value"));
    }

    [Fact]
    public void 没有建议时不是combobox_只读时标签不能移除()
    {
        var cut = RenderTags(p => p.Add(x => x.ReadOnly, true));
        var input = Input(cut);
        Assert.Null(input.GetAttribute("role"));
        Assert.Null(input.GetAttribute("data-rune-enter"));
        Assert.Empty(cut.FindAll("[data-scope=badge] button"));
        Assert.Empty(cut.FindAll("[popover]"));
    }
}
