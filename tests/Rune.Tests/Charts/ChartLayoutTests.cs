using Rune.UI;

namespace Rune.Tests.Charts;

// 布局算法：矩形树图、桑基图、弦图、网络图；期望值同 vue-tsx-pro 的 layout.test.ts
public class ChartLayoutTests
{
    static readonly string[] Ids = ["a", "b", "c", "d"];
    static readonly FlowAmount[] Flows = [new("a", "b", 30), new("a", "c", 10), new("b", "d", 30), new("c", "d", 10)];

    [Fact]
    public void 桑基图_按远近分列_高度按流量_同列不重叠不出界()
    {
        var (nodes, _) = SankeyLayout.Arrange(Ids, Flows, 300, 200, 10, 10);
        var (a, b, c, d) = (nodes[0], nodes[1], nodes[2], nodes[3]);
        Assert.Equal([0, 1, 1, 2], nodes.Select(node => node.Layer));
        Assert.Equal(0, a.X0);
        Assert.Equal(300, d.X1);
        Assert.Equal(40, a.Value);
        // 高度与流量成比例：b 是 c 的三倍
        Assert.Equal(3, (b.Y1 - b.Y0) / (c.Y1 - c.Y0), 5);
        var (upper, lower) = b.Y0 < c.Y0 ? (b, c) : (c, b);
        Assert.True(lower.Y0 - upper.Y1 >= 10 - 1e-6);
        Assert.All(nodes, node => Assert.True(node.Y0 >= -1e-6 && node.Y1 <= 200 + 1e-6));
    }

    [Fact]
    public void 桑基图_带子粗细按流量_在节点上依次排开()
    {
        var (nodes, links) = SankeyLayout.Arrange(Ids, Flows, 300, 200, 10, 10);
        var a = nodes[0];
        var outgoing = links.Where(link => link.Source == a).ToList();
        Assert.Equal(a.Y1 - a.Y0, outgoing.Sum(link => link.Width), 6);
        Assert.Equal(a.Y0, outgoing.Min(link => link.Y0 - link.Width / 2), 6);
    }

    [Fact]
    public void 桑基图_靠右_值不是正数与指向不存在节点的不画()
    {
        var (nodes, links) = SankeyLayout.Arrange(["a", "b", "c", "x"],
            [new("a", "b", 5), new("b", "c", 5), new("x", "c", 2), new("x", "ghost", 2), new("a", "c", 0)], 100, 100, 10, 5, SankeyAlign.Right);
        Assert.Equal(1, nodes.Single(node => node.Id == "x").Layer);
        Assert.Equal(3, links.Count);
    }

    [Fact]
    public void 弦图_弧长按流入加流出_弧之间留空_弦的两端在两端的弧上()
    {
        var (groups, chords) = ChordLayout.Arrange(3, [(0, 1, 10), (1, 2, 5), (2, 0, 5)], 0.1);
        static double Span(ChordGroup group) => group.End - group.Start;
        Assert.Equal(15.0 / 10, Span(groups[0]) / Span(groups[2]), 6);
        Assert.Equal(0.1, groups[1].Start - groups[0].End, 6);
        Assert.Equal(Math.PI * 2, groups[2].End + 0.1, 6);
        var first = chords[0];
        Assert.True(first.Source.Start >= groups[0].Start && first.Source.End <= groups[0].End + 1e-9);
        Assert.True(first.Target.Start >= groups[1].Start);
        Assert.Equal(first.Source.End - first.Source.Start, first.Target.End - first.Target.Start, 9);
    }

    static readonly GraphLayoutInput[] GraphNodes = [.. new[] { "a", "b", "c", "d", "e" }.Select(id => new GraphLayoutInput(id, null, 40, 40))];

    // 最后一条成环：不影响分层
    static readonly (string, string)[] GraphLinks = [("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"), ("d", "e"), ("e", "a")];

    [Fact]
    public void 网络图_分层_沿方向一层层排开_同一层不重叠()
    {
        var lr = GraphLayout.Arrange(GraphNodes, GraphLinks, GraphLayoutKind.Layered);
        Assert.True(lr["a"].X < lr["b"].X);
        Assert.Equal(lr["b"].X, lr["c"].X);
        Assert.True(lr["c"].X < lr["d"].X && lr["d"].X < lr["e"].X);
        Assert.True(Math.Abs(lr["b"].Y - lr["c"].Y) >= 40);
        var tb = GraphLayout.Arrange(GraphNodes, GraphLinks, GraphLayoutKind.Layered, vertical: true);
        Assert.True(tb["a"].Y < tb["e"].Y);
    }

    [Fact]
    public void 网络图_力导向结果每次相同且不重叠_环形到圆心距离相同()
    {
        var first = GraphLayout.Arrange(GraphNodes, GraphLinks, GraphLayoutKind.Force);
        Assert.Equal(first.Values, GraphLayout.Arrange(GraphNodes, GraphLinks, GraphLayoutKind.Force).Values);
        var points = first.Values.ToList();
        for (var i = 0; i < points.Count; i++)
            for (var j = i + 1; j < points.Count; j++)
                Assert.True(double.Hypot(points[i].X - points[j].X, points[i].Y - points[j].Y) > 20);
        var radii = GraphLayout.Arrange(GraphNodes, GraphLinks, GraphLayoutKind.Circular).Values.Select(p => double.Hypot(p.X, p.Y)).ToList();
        Assert.All(radii, r => Assert.Equal(radii[0], r, 6));
    }

    [Fact]
    public void 网络图_分列_每组一列_按组的顺序()
    {
        var grouped = GraphNodes.Select((node, index) => node with { Group = index < 2 ? "g2" : "g1" }).ToList();
        var columns = GraphLayout.Arrange(grouped, GraphLinks, GraphLayoutKind.Parallel, groups: ["g1", "g2"]);
        Assert.True(columns["c"].X < columns["a"].X);
        Assert.Equal(columns["a"].X, columns["b"].X);
    }

    sealed record Row(string Region, string Supplier, double Amount);

    static readonly Row[] Rows = [new("华东", "甲", 60), new("华东", "乙", 30), new("华南", "丙", 10), new("华南", "丁", 0)];

    static TreemapNode<Row> Tree() => TreemapLayout.Group(Rows, [row => row.Region, row => row.Supplier], row => row.Amount);

    static List<TreemapNode<Row>> Leaves(TreemapNode<Row> node) => node.Children is null ? [node] : [.. node.Children.SelectMany(Leaves)];

    static double Area(TreemapNode<Row> node) => (node.X1 - node.X0) * (node.Y1 - node.Y0);

    [Fact]
    public void 矩形树图_逐级分组_分组是子节点之和_从大到小_不是正数的不要()
    {
        var root = Tree();
        Assert.Equal(100, root.Value);
        Assert.Equal([("华东", 90.0), ("华南", 10.0)], root.Children!.Select(node => (node.Key, node.Value)));
        Assert.Single(root.Children![1].Children!);
        Assert.Same(Rows[0], root.Children![0].Children![0].Data);
        Assert.Equal("华东 / 甲", root.Children![0].Children![0].Path);
    }

    [Fact]
    public void 矩形树图_面积与数值成比例_铺满区域_留出间距互不重叠()
    {
        var root = Tree();
        TreemapLayout.Arrange(root, 200, 100);
        var leaves = Leaves(root);
        Assert.Equal(200 * 100, leaves.Sum(Area), 6);
        Assert.Equal(2, Area(leaves[0]) / Area(leaves[1]), 6);

        var padded = Tree();
        TreemapLayout.Arrange(padded, 200, 100, padding: 4);
        var boxes = Leaves(padded);
        for (var i = 0; i < boxes.Count; i++)
            for (var j = i + 1; j < boxes.Count; j++)
            {
                var (a, b) = (boxes[i], boxes[j]);
                Assert.True(a.X1 <= b.X0 || b.X1 <= a.X0 || a.Y1 <= b.Y0 || b.Y1 <= a.Y0);
            }
    }
}
