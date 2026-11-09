using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>部件属性落在对应的元素上：表格的各层格子，输入控件的外壳（直接写的属性仍落在输入元素上）。</summary>
public class PartPropsTests : BunitContext
{
    public PartPropsTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Row(string Name, int Count);

    static Attrs Class(string name) => new() { Class = name };

    [Fact]
    public void 表格的部件属性落在各自的格子上()
    {
        var cut = Render<DataTable<Row>>(p => p
            .Add(x => x.Items, [new Row("甲", 1), new Row("乙", 2)])
            .Add(x => x.GetKey, row => row.Name)
            .Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.PropsHead, Class("my-head"))
            .Add(x => x.PropsHeaderCell, Class("my-th"))
            .Add(x => x.PropsSelectHeaderCell, Class("my-select-th"))
            .Add(x => x.PropsCell, Class("my-td"))
            .Add(x => x.PropsSelectCell, Class("my-select-td"))
            .Add(x => x.PropsSortButton, Class("my-sort"))
            .AddChildContent<PropertyColumn<Row, string>>(c => c.Add(x => x.Property, row => row.Name).Add(x => x.Sortable, true))
            .AddChildContent<PropertyColumn<Row, int>>(c => c.Add(x => x.Property, row => row.Count)));

        Assert.NotNull(cut.Find("thead.my-head"));
        Assert.Equal(2, cut.FindAll("th.my-th").Count);
        Assert.Single(cut.FindAll("th.my-select-th"));
        Assert.Equal(4, cut.FindAll("td.my-td").Count);
        Assert.Equal(2, cut.FindAll("td.my-select-td").Count);
        Assert.Single(cut.FindAll("th button.my-sort"));
    }

    [Fact]
    public void 输入控件的PropsRoot落在外壳上_直接写的属性落在输入元素上()
    {
        var cut = Render<TextField>(p => p.Add(x => x.PropsRoot, Class("my-shell")).AddUnmatched("placeholder", "名称"));
        var shell = cut.Find("div.my-shell");
        Assert.Equal("名称", shell.QuerySelector("input")!.GetAttribute("placeholder"));
        Assert.DoesNotContain("my-shell", cut.Find("input").ClassName ?? "");
    }
}
