using Rune.UI;

namespace Rune.Tests.UI;

// 树的展开成行、勾选的父子联动（半选、懒加载出来的子节点跟随父节点）、勾选结果的取法、路径、搜索过滤、组装与移动（同 vue-tsx-pro 的 tree.test.ts）
public class TreeDataTests
{
    sealed record Node(string Id, IReadOnlyList<Node>? Children = null, bool? Lazy = null);

    static readonly TreeAccessors<Node> Accessors = new(node => node.Id, node => node.Children, node => node.Lazy ?? (node.Children?.Count ?? 0) > 0);

    static readonly Node[] Tree =
    [
        new("a", [new("a1"), new("a2", [new("a2x")])]),
        new("b", Lazy: true),
    ];

    static readonly TreeIndex Index = TreeData.Index(Tree, Accessors);

    // 树的形状：叶子是 key，有子节点的是 "key[子…]"
    static string Shape(IEnumerable<Node> nodes) =>
        string.Join(",", nodes.Select(node => node.Children is { Count: > 0 } children ? $"{node.Id}[{Shape(children)}]" : node.Id));

    [Fact]
    public void 只展开Expanded里的节点_层级与同级总数位置()
    {
        var rows = TreeData.Flatten(Tree, Accessors, new HashSet<string> { "a" });

        Assert.Equal(["a", "a1", "a2", "b"], rows.Select(row => row.Key));
        Assert.Equal((1, "a", 2, 2), (rows[2].Depth, rows[2].Parent, rows[2].SetSize, rows[2].Position));
        Assert.True(rows[3].Expandable);
        Assert.False(rows[1].Expandable);
    }

    [Fact]
    public void 勾上父节点勾上全部子孙_子节点全勾上时父节点也勾上_取消时祖先跟着取消()
    {
        Assert.Equal(["a", "a1", "a2", "a2x"], TreeData.ToggleChecked(Index, [], "a"));
        Assert.Equal(["a", "a1", "a2", "a2x"], TreeData.ToggleChecked(Index, ["a1"], "a2x"));
        Assert.Equal(["a1"], TreeData.ToggleChecked(Index, ["a"], "a2x"));
    }

    [Fact]
    public void 只勾了一部分时父节点半选()
    {
        var states = TreeData.CheckStates(Index, ["a2x"]);

        Assert.Equal(CheckState.Checked, states["a2"]);
        Assert.Equal(CheckState.Indeterminate, states["a"]);
        Assert.Equal(CheckState.Unchecked, states["a1"]);
        Assert.Equal(CheckState.Unchecked, states["b"]);
        // a2 的子节点全勾上，显示为勾上；再点它就是取消
        Assert.Empty(TreeData.ToggleChecked(Index, ["a2x"], "a2"));
    }

    [Fact]
    public void 父节点勾上之后懒加载出来的子节点也算勾上()
    {
        var loaded = TreeData.Index([Tree[0], new Node("b", [new("b1"), new("b2")])], Accessors);

        Assert.Equal(CheckState.Checked, TreeData.CheckStates(loaded, ["b"])["b1"]);
        Assert.Equal(["b2"], TreeData.ToggleChecked(loaded, ["b"], "b1"));
    }

    [Fact]
    public void 勾选结果的取法()
    {
        var some = TreeData.ToggleChecked(Index, [], "a2");
        Assert.Equal(["a2", "a2x"], TreeData.CheckedByStrategy(Index, some, CheckStrategy.All));
        Assert.Equal(["a2"], TreeData.CheckedByStrategy(Index, some, CheckStrategy.Parent));
        Assert.Equal(["a2x"], TreeData.CheckedByStrategy(Index, some, CheckStrategy.Child));

        var all = TreeData.ToggleChecked(Index, [], "a");
        Assert.Equal(["a"], TreeData.CheckedByStrategy(Index, all, CheckStrategy.Parent));
        Assert.Equal(["a1", "a2x"], TreeData.CheckedByStrategy(Index, all, CheckStrategy.Child));
    }

    [Fact]
    public void 路径从第一层到节点_不在树里时为空()
    {
        Assert.Equal(["a", "a2", "a2x"], TreeData.Path(Index, "a2x"));
        Assert.Equal(["b"], TreeData.Path(Index, "b"));
        Assert.Empty(TreeData.Path(Index, "nope"));
    }

    [Fact]
    public void 过滤留下匹配的节点与祖先并展开祖先_匹配节点的子孙不跟着留下()
    {
        var result = TreeData.Filter(Tree, Accessors, node => node.Id.StartsWith("a2", StringComparison.Ordinal));

        Assert.Equal(["a", "a2", "a2x"], result.Include.Order());
        Assert.Equal(["a", "a2"], result.Expand.Order());
        var rows = TreeData.Flatten(Tree, Accessors, result.Expand, result.Include);
        Assert.Equal(["a", "a2", "a2x"], rows.Select(row => row.Key));
        // 同级的总数与位置按过滤后的算
        Assert.Equal((1, 1), (rows[1].SetSize, rows[1].Position));

        Assert.Equal(["a"], TreeData.Filter(Tree, Accessors, node => node.Id == "a").Include);
    }

    [Fact]
    public void 移动到别的父节点下_不改原数据_没动到的节点原样复用()
    {
        var moved = TreeData.Move(Tree, Accessors, (node, children) => node with { Children = children }, "a2x", new("a", 0))!;

        Assert.Equal("a[a2x,a1,a2],b", Shape(moved));
        Assert.Equal("a[a1,a2[a2x]],b", Shape(Tree));
        Assert.Same(Tree[1], moved[1]);
    }

    [Fact]
    public void 移到第一层与同级换位置_位置按移走之后算()
    {
        Node With(Node node, IReadOnlyList<Node> children) => node with { Children = children };

        Assert.Equal("a[a2[a2x]],b,a1", Shape(TreeData.Move(Tree, Accessors, With, "a1", new(null, 2))!));
        Assert.Equal("b,a[a1,a2[a2x]]", Shape(TreeData.Move(Tree, Accessors, With, "b", new(null, 0))!));
    }

    [Fact]
    public void 移到自己或子孙下面_key不存在时返回null()
    {
        Node With(Node node, IReadOnlyList<Node> children) => node with { Children = children };

        Assert.Null(TreeData.Move(Tree, Accessors, With, "a", new("a2", 0)));
        Assert.Null(TreeData.Move(Tree, Accessors, With, "a", new("a", 0)));
        Assert.Null(TreeData.Move(Tree, Accessors, With, "zz", new(null, 0)));
    }

    sealed record Row(string Id, string? ParentId, string Name, IReadOnlyList<Row>? Kids = null);

    static readonly Row[] Rows =
    [
        new("b1", "b", "小组"),
        new("a", null, "研发"),
        new("b", null, "采购"),
        new("a1", "a", "前端"),
        new("x", "gone", "父行不在这批里"),
    ];

    static IReadOnlyList<Row> Build(IReadOnlyList<Row> rows) =>
        TreeData.Build(rows, row => row.Id, row => row.ParentId, (row, kids) => row with { Kids = kids });

    [Fact]
    public void 平铺的行组装成树_同级保持顺序_父行不在的当作根_组装后能过滤()
    {
        var tree = Build(Rows);
        string RowShape(IEnumerable<Row> nodes) =>
            string.Join(",", nodes.Select(node => node.Kids is { Count: > 0 } kids ? $"{node.Id}[{RowShape(kids)}]" : node.Id));

        Assert.Equal("a[a1],b[b1],x", RowShape(tree));
        Assert.Null(Rows[1].Kids);
        var filter = TreeData.Filter(tree, new TreeAccessors<Row>(row => row.Id, row => row.Kids), row => row.Name == "前端");
        Assert.Equal(["a", "a1"], filter.Expand.Concat(filter.Matched));
    }

    [Fact]
    public void 组装时key重复或父子成环抛异常()
    {
        Assert.Contains("出现了两次", Assert.Throws<ArgumentException>(() => Build([Rows[1], Rows[1]])).Message);
        Assert.Contains("成了环", Assert.Throws<ArgumentException>(() => Build([new("p", "q", ""), new("q", "p", "")])).Message);
    }
}
