using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 建在 DataTable 上：这里测展开成一列、同级排序、展开按钮与懒加载、多选父子联动
public class TreeDataTableTests : BunitContext
{
    public TreeDataTableTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Unit(string Id, string Name, int Headcount, IReadOnlyList<Unit>? Children = null, bool Lazy = false);

    static readonly Unit[] Org =
    [
        new("rd", "研发部", 46, [new("fe", "前端组", 12), new("be", "后端组", 18)]),
        new("mk", "市场部", 21, Lazy: true),
    ];

    IRenderedComponent<TreeDataTable<Unit>> RenderOrg(Action<ComponentParameterCollectionBuilder<TreeDataTable<Unit>>>? configure = null) =>
        Render<TreeDataTable<Unit>>(p =>
        {
            p.Add(x => x.Items, Org).Add(x => x.GetKey, u => u.Id).Add(x => x.GetChildren, u => u.Children)
                .Add(x => x.HasChildren, u => u.Children is { Count: > 0 } || u.Lazy);
            p.AddChildContent<PropertyColumn<Unit, string>>(c => c.Add(x => x.Property, u => u.Name).Add(x => x.Sortable, true));
            p.AddChildContent<PropertyColumn<Unit, int>>(c => c.Add(x => x.Property, u => u.Headcount).Add(x => x.Sortable, true));
            configure?.Invoke(p);
        });

    static string[] Keys(IRenderedComponent<TreeDataTable<Unit>> cut) => [.. cut.FindAll("tbody tr[data-key]").Select(row => row.GetAttribute("data-key")!)];

    [Fact]
    public void 按展开状态展开成一列_同级之间按排序_展开按钮的名称是行名的子项()
    {
        IReadOnlyList<string> expanded = ["rd"];
        var cut = RenderOrg(p => p.Add(x => x.Expanded, expanded).Add(x => x.ExpandedChanged, v => expanded = v)
            .Add(x => x.Sort, new ColumnSort("headcount", SortDirection.Descending)));

        // 同级按人数倒序：市场部在研发部后面（46 > 21），研发部下后端组在前端组前面
        Assert.Equal(["rd", "be", "fe", "mk"], Keys(cut));
        var toggle = cut.Find("button[aria-label='研发部 的子项']");
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));

        toggle.Click();
        Assert.Equal([], expanded);
    }

    [Fact]
    public void 多列排序时同级按几列依次排()
    {
        Unit[] org = [new("a", "甲", 10, [new("a1", "乙", 5), new("a2", "乙", 9), new("a3", "丙", 1)])];
        var cut = Render<TreeDataTable<Unit>>(p =>
        {
            p.Add(x => x.Items, org).Add(x => x.GetKey, u => u.Id).Add(x => x.GetChildren, u => u.Children)
                .Add(x => x.Expanded, ["a"]).Add(x => x.MultiSort, true)
                .Add(x => x.Sorts, [new ColumnSort("name", SortDirection.Descending), new ColumnSort("headcount", SortDirection.Descending)]);
            p.AddChildContent<PropertyColumn<Unit, string>>(c => c.Add(x => x.Property, u => u.Name).Add(x => x.Sortable, true));
            p.AddChildContent<PropertyColumn<Unit, int>>(c => c.Add(x => x.Property, u => u.Headcount).Add(x => x.Sortable, true));
        });

        // 先按名称倒序（乙在丙前），同名的按人数倒序（之前写了 MultiSort 也不排）
        Assert.Equal(["a", "a2", "a1", "a3"], Keys(cut));
    }

    [Fact]
    public async Task 展开还没有子项的行时懒加载()
    {
        var loaded = new TaskCompletionSource();
        Unit? asked = null;
        var cut = RenderOrg(p => p.Add(x => x.LoadChildren, async unit =>
        {
            asked = unit;
            await loaded.Task;
        }));

        var toggle = cut.Find("button[aria-label='市场部 的子项']");
        var click = cut.InvokeAsync(() => toggle.Click());
        Assert.Equal("mk", asked?.Id);
        loaded.SetResult();
        await click;
        Assert.Equal("true", cut.Find("button[aria-label='市场部 的子项']").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void 多选父子联动_勾父行连带子孙_只勾一部分时父行半选()
    {
        IReadOnlyList<string> selected = [];
        var cut = RenderOrg(p => p.Add(x => x.Expanded, ["rd"]).Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Selected, selected).Add(x => x.SelectedChanged, v => selected = v));

        cut.Find("tr[data-key=rd] input[type=checkbox]").Change(true);
        Assert.Equal(["be", "fe", "rd"], selected.Order());

        cut.Render(p => p.Add(x => x.Selected, selected));
        cut.Find("tr[data-key=fe] input[type=checkbox]").Change(false);
        Assert.Equal(["be"], selected);
        cut.Render(p => p.Add(x => x.Selected, selected));
        Assert.Equal("indeterminate", cut.Find("tr[data-key=rd] label").GetAttribute("data-state"));
    }
}
