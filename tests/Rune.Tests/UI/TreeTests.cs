using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 键盘的移动与高亮在 JS 里（lists.js、tree.js），这里测 C# 的一半：渲染出的行与 aria、点击与命令（展开、选中、勾选、移动）、懒加载、过滤
public class TreeTests : BunitContext
{
    public TreeTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Node(string Id, string Name, IReadOnlyList<Node>? Children = null, bool Lazy = false, bool Disabled = false);

    static readonly Node[] Regions =
    [
        new("east", "华东", [new("sh", "上海"), new("zj", "浙江", [new("hz", "杭州"), new("nb", "宁波")])]),
        new("south", "华南", [new("gz", "广州")]),
    ];

    IRenderedComponent<Tree<Node>> RenderRegions(IReadOnlyList<Node>? items = null, Action<ComponentParameterCollectionBuilder<Tree<Node>>>? configure = null) =>
        Render<Tree<Node>>(p =>
        {
            p.Add(x => x.Items, items ?? Regions)
                .Add(x => x.GetKey, n => n.Id)
                .Add(x => x.GetLabel, n => n.Name)
                .Add(x => x.GetChildren, n => n.Children)
                .Add(x => x.HasChildren, n => n.Lazy || n.Children is { Count: > 0 })
                .Add(x => x.IsDisabled, n => n.Disabled)
                .AddUnmatched("aria-label", "地区");
            configure?.Invoke(p);
        });

    static string Rows(IRenderedComponent<Tree<Node>> cut) =>
        string.Join(",", cut.FindAll("[role=treeitem]").Select(row => row.TextContent.Trim()));

    static void Command(IRenderedComponent<Tree<Node>> cut, string name, string text) =>
        cut.Find("[role=tree]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = name, Text = text });

    [Fact]
    public void 按展开状态平铺渲染_层级与位置交给读屏_行号是id后缀()
    {
        var cut = RenderRegions(configure: p => p.Add(x => x.Expanded, ["east"]).AddUnmatched("id", "regions"));

        var tree = cut.Find("[role=tree]");
        Assert.Equal(("regions", "0", "typeahead"), (tree.Id, tree.GetAttribute("tabindex"), tree.GetAttribute("data-rune-list")));
        Assert.Equal("华东,上海,浙江,华南", Rows(cut));
        var zj = cut.FindAll("[role=treeitem]")[2];
        Assert.Equal(("regions-2", "2", "2", "2", "false"),
            (zj.Id, zj.GetAttribute("aria-level"), zj.GetAttribute("aria-setsize"), zj.GetAttribute("aria-posinset"), zj.GetAttribute("aria-expanded")));
        Assert.Null(cut.FindAll("[role=treeitem]")[1].GetAttribute("aria-expanded"));
        Assert.Contains("\"parents\":[-1,0,0,-1]", tree.GetAttribute("data-rune-tree-rows"));
    }

    [Fact]
    public void 展开收起与Enter_没有选择方式时Enter展开收起()
    {
        var cut = RenderRegions();

        Command(cut, "expand", "0");
        Assert.Equal("华东,上海,浙江,华南", Rows(cut));
        Command(cut, "enter", "2");
        Assert.Equal("华东,上海,浙江,杭州,宁波,华南", Rows(cut));
        Command(cut, "collapse", "0");
        Assert.Equal("华东,华南", Rows(cut));
    }

    [Fact]
    public void 单选_点击选中_不可用的节点不能选中()
    {
        IReadOnlyList<string> selected = [];
        Node[] items = [new("a", "甲"), new("b", "乙", Disabled: true)];
        var cut = RenderRegions(items, p => p
            .Add(x => x.SelectionMode, SelectionMode.Single)
            .Add(x => x.Selected, selected)
            .Add(x => x.SelectedChanged, keys => selected = keys));

        cut.FindAll("[role=treeitem]")[1].Click();
        Assert.Empty(selected);
        cut.FindAll("[role=treeitem]")[0].Click();
        Assert.Equal(["a"], selected);
        Assert.Equal("true", cut.FindAll("[role=treeitem]")[1].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void 勾选父子联动_部分勾上时父节点半选_空格勾选()
    {
        IReadOnlyList<string> checkedKeys = ["hz"];
        var cut = RenderRegions(configure: p => p
            .Add(x => x.Expanded, ["east", "zj"])
            .Add(x => x.Checkable, true)
            .Add(x => x.Checked, checkedKeys)
            .Add(x => x.CheckedChanged, keys => checkedKeys = keys));

        string Checks() => string.Join(",", cut.FindAll("[role=treeitem]").Select(row => row.GetAttribute("aria-checked")));
        Assert.Equal("mixed,false,mixed,true,false,false", Checks());

        Command(cut, "space", "4");
        Assert.Equal(["zj", "hz", "nb"], checkedKeys);
        cut.Render(p => p.Add(x => x.Checked, checkedKeys));
        Assert.Equal("mixed,false,true,true,true,false", Checks());

        // 点父节点勾上全部子孙
        cut.FindAll("[role=treeitem]")[0].Click();
        Assert.Equal(["east", "sh", "zj", "hz", "nb"], checkedKeys);
    }

    [Fact]
    public async Task 懒加载_展开时调用LoadChildren_进行中带aria_busy_失败时收回并报告()
    {
        var pending = new TaskCompletionSource();
        TreeLoadError<Node>? error = null;
        Node[] items = [new("a", "甲", Lazy: true)];
        var cut = RenderRegions(items, p => p
            .Add(x => x.LoadChildren, _ => pending.Task)
            .Add(x => x.OnLoadError, e => error = e));

        Command(cut, "expand", "0");
        Assert.Equal(("true", "true"), (cut.Find("[role=treeitem]").GetAttribute("aria-busy"), cut.Find("[role=treeitem]").GetAttribute("aria-expanded")));

        await cut.InvokeAsync(() => pending.SetException(new InvalidOperationException("网络错误")));
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("[role=treeitem]").GetAttribute("aria-expanded")));
        Assert.Null(cut.Find("[role=treeitem]").GetAttribute("aria-busy"));
        Assert.Equal("网络错误", error?.Exception.Message);
    }

    [Fact]
    public void 过滤留下匹配的节点与上级并展开_没有匹配时显示提示()
    {
        var cut = RenderRegions(configure: p => p.Add(x => x.Filter, "宁"));

        Assert.Equal("华东,浙江,宁波", Rows(cut));
        cut.Render(p => p.Add(x => x.Filter, "没有这个地方"));
        Assert.Empty(cut.FindAll("[role=treeitem]"));
        Assert.Contains("没有匹配的节点", cut.Markup);
        cut.Render(p => p.Add(x => x.Filter, ""));
        Assert.Equal("华东,华南", Rows(cut));
    }

    [Fact]
    public void Alt方向键移动_报告目标_拦下不能放的位置()
    {
        var moves = new List<TreeMove<Node>>();
        var cut = RenderRegions(configure: p => p
            .Add(x => x.Expanded, ["east"])
            .Add(x => x.OnMove, move => moves.Add(move))
            .Add(x => x.CanDrop, (node, target) => target.Parent != "sh"));

        Assert.Equal("move", cut.Find("[role=tree]").GetAttribute("data-rune-tree"));
        Command(cut, "move", "2:up");
        Command(cut, "move", "2:in");
        Command(cut, "move", "1:out");
        Assert.Equal([("zj", new TreeMoveTarget("east", 0)), ("sh", new TreeMoveTarget(null, 1))], moves.Select(m => (m.Key, m.Target)));
        Assert.Contains("上海移到第一层第 2 位", cut.Markup);
    }

    [Fact]
    public void 拖动松开_落点换算成目标_放进自己里面或CanDrop不让放时不报告()
    {
        var moves = new List<TreeMove<Node>>();
        var cut = RenderRegions(configure: p => p
            .Add(x => x.Expanded, ["east"])
            .Add(x => x.OnMove, move => moves.Add(move))
            .Add(x => x.CanDrop, (node, target) => target.Parent != "sh"));

        // 行：0 华东、1 上海、2 浙江、3 华南
        Command(cut, "drop", "1:3:after");
        Command(cut, "drop", "2:0:before");
        Command(cut, "drop", "1:2:before");
        Command(cut, "drop", "0:1:inside");
        Command(cut, "drop", "2:1:inside");
        Assert.Equal([("sh", new TreeMoveTarget(null, 2)), ("zj", new TreeMoveTarget(null, 0)), ("sh", new TreeMoveTarget("east", 0))], moves.Select(m => (m.Key, m.Target)));
    }
}
