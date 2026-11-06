using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>穿梭框：两边的项、移动、双击、全选只管看得见的项。</summary>
public class TransferTests : BunitContext
{
    public TransferTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly ListboxOption[] people =
    [
        new("a", "周一航"), new("b", "许悠"), new("c", "林小满"), new("d", "宋知遥", Disabled: true),
    ];

    IRenderedComponent<Transfer<ListboxOption>> RenderTransfer(Func<IReadOnlyList<string>?> read, Action<IReadOnlyList<string>?> write, bool searchable = false) =>
        Render<Transfer<ListboxOption>>(p => p
            .Add(x => x.Items, people)
            .Add(x => x.Value, read())
            .Add(x => x.ValueChanged, EventCallback.Factory.Create(this, write))
            .Add(x => x.Searchable, searchable));

    static IEnumerable<string> Labels(AngleSharp.Dom.IElement list) =>
        list.QuerySelectorAll("[role=option]").Select(o => o.GetAttribute("data-label")!);

    [Fact]
    public void 左边是没选的_右边按值的顺序_勾上后移过去并播报()
    {
        IReadOnlyList<string>? value = ["c"];
        var cut = RenderTransfer(() => value, v => value = v);
        cut.Render(p => p.Add(x => x.Value, value));

        var lists = cut.FindAll("[role=listbox]");
        Assert.Equal(["周一航", "许悠", "宋知遥"], Labels(lists[0]));
        Assert.Equal(["林小满"], Labels(lists[1]));
        // 没有勾上时按钮不可用
        Assert.All(cut.FindAll("button[aria-label^=移到]"), b => Assert.True(b.HasAttribute("disabled")));

        lists[0].QuerySelectorAll("[role=option]")[1].Click();
        cut.Find("button[aria-label=移到已选]").Click();
        Assert.Equal(["c", "b"], value);
        Assert.Equal("已把 1 项移到已选", cut.Find("[aria-live=polite]").TextContent.TrimEnd('​'));
    }

    [Fact]
    public void 双击直接移过去_不可用的移不动()
    {
        IReadOnlyList<string>? value = [];
        var cut = RenderTransfer(() => value, v => value = v);

        cut.FindAll("[role=listbox]")[0].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "move", Text = "3" });
        Assert.Equal([], value);
        cut.FindAll("[role=listbox]")[0].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "move", Text = "0" });
        Assert.Equal(["a"], value);
    }

    [Fact]
    public void 全选只管搜索后看得见的项()
    {
        IReadOnlyList<string>? value = [];
        var cut = RenderTransfer(() => value, v => value = v, searchable: true);

        cut.FindAll("input[type=search]")[0].Input("许");
        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("input[type=search]")[0].Input("");
        Assert.Equal("1 / 4", cut.FindAll("section small")[0].TextContent);
    }
}
