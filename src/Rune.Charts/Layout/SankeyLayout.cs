namespace Rune.UI;

/// <summary>桑基图节点的排法（同 d3-sankey 的 sankeyJustify 等）。</summary>
public enum SankeyAlign
{
    /// <summary>两端对齐：没有流出的节点排到最后一列（默认）。</summary>
    Justify,

    /// <summary>靠左：按离起点的步数。</summary>
    Left,

    /// <summary>靠右：按离终点的步数。</summary>
    Right,

    /// <summary>居中：只有流出的节点排到它的去向的前一列。</summary>
    Center,
}

/// <summary>布局后的桑基图节点。</summary>
public sealed class SankeyNode
{
    /// <summary>节点的标识。</summary>
    public required string Id { get; init; }

    /// <summary>流量：流入、流出里较大的那个。</summary>
    public double Value { get; internal set; }

    /// <summary>所在的列，从 0 起。</summary>
    public int Layer { get; internal set; }

    /// <summary>左。</summary>
    public double X0 { get; internal set; }

    /// <summary>右。</summary>
    public double X1 { get; internal set; }

    /// <summary>上。</summary>
    public double Y0 { get; internal set; }

    /// <summary>下。</summary>
    public double Y1 { get; internal set; }

    /// <summary>从这个节点流出的。</summary>
    public List<SankeyLink> SourceLinks { get; } = [];

    /// <summary>流入这个节点的。</summary>
    public List<SankeyLink> TargetLinks { get; } = [];

    internal int Depth;
    internal int Height;
}

/// <summary>布局后的流向。</summary>
public sealed class SankeyLink
{
    /// <summary>在输入里的位置。</summary>
    public required int Index { get; init; }

    /// <summary>起点。</summary>
    public required SankeyNode Source { get; init; }

    /// <summary>终点。</summary>
    public required SankeyNode Target { get; init; }

    /// <summary>流量。</summary>
    public required double Value { get; init; }

    /// <summary>带子的粗细。</summary>
    public double Width { get; internal set; }

    /// <summary>起点处带子中线的纵坐标。</summary>
    public double Y0 { get; internal set; }

    /// <summary>终点处带子中线的纵坐标。</summary>
    public double Y1 { get; internal set; }
}

/// <summary>布局的输入：一条流向。</summary>
/// <param name="Source">起点节点的标识。</param>
/// <param name="Target">终点节点的标识。</param>
/// <param name="Value">流量，大于 0。</param>
public readonly record struct FlowAmount(string Source, string Target, double Value);

/// <summary>
/// 桑基图的布局（同 vue-tsx-pro 的 layout/sankey.ts，即 d3-sankey 的算法）：节点按离起点的远近分列，列里的节点按流量定高，
/// 再几轮松弛让带子尽量平直、互不重叠。流向不能成环：有环时环上的节点停在发现环的那一列。
/// </summary>
public static class SankeyLayout
{
    /// <summary>布局：节点按 ids 的顺序；值不是正数、指向不存在节点的流向不要。</summary>
    public static (IReadOnlyList<SankeyNode> Nodes, IReadOnlyList<SankeyLink> Links) Arrange(IReadOnlyList<string> ids, IReadOnlyList<FlowAmount> input,
        double width, double height, double nodeWidth, double nodePadding, SankeyAlign align = SankeyAlign.Justify, int iterations = 6)
    {
        var nodes = ids.Select(id => new SankeyNode { Id = id }).ToList();
        var byId = new Dictionary<string, SankeyNode>();
        foreach (var node in nodes) byId.TryAdd(node.Id, node);
        var links = new List<SankeyLink>();
        for (var index = 0; index < input.Count; index++)
        {
            var link = input[index];
            if (!byId.TryGetValue(link.Source, out var source) || !byId.TryGetValue(link.Target, out var target) || !(link.Value > 0)) continue;
            var laid = new SankeyLink { Index = index, Source = source, Target = target, Value = link.Value };
            source.SourceLinks.Add(laid);
            target.TargetLinks.Add(laid);
            links.Add(laid);
        }
        foreach (var node in nodes) node.Value = Math.Max(node.SourceLinks.Sum(link => link.Value), node.TargetLinks.Sum(link => link.Value));
        AssignDepths(nodes, forward: true);
        AssignDepths(nodes, forward: false);

        // 分列：横坐标按列均分
        var columnCount = nodes.Select(node => node.Depth).DefaultIfEmpty(0).Max() + 1;
        var kx = columnCount > 1 ? (width - nodeWidth) / (columnCount - 1) : 0;
        var columns = Enumerable.Range(0, columnCount).Select(_ => new List<SankeyNode>()).ToList();
        foreach (var node in nodes)
        {
            var layer = Math.Max(0, Math.Min(columnCount - 1, (int)Math.Floor(ColumnOf(node, columnCount, align))));
            node.Layer = layer;
            node.X0 = layer * kx;
            node.X1 = node.X0 + nodeWidth;
            columns[layer].Add(node);
        }

        var longest = Math.Max(1, columns.Max(column => column.Count));
        var py = longest > 1 ? Math.Min(nodePadding, height / (longest - 1)) : nodePadding;

        // 初始：按流量定高（各列取能放下的最小比例），列里从上往下排，余下的空均分到间隔里
        var ky = columns.Where(column => column.Count > 0)
            .Select(column => (height - (column.Count - 1) * py) / (column.Sum(node => node.Value) is var sum && sum != 0 ? sum : 1))
            .DefaultIfEmpty(0).Min();
        foreach (var column in columns)
        {
            var y = 0.0;
            foreach (var node in column)
            {
                node.Y0 = y;
                node.Y1 = y + node.Value * ky;
                y = node.Y1 + py;
                foreach (var link in node.SourceLinks) link.Width = link.Value * ky;
            }
            var spread = (height - y + py) / (column.Count + 1);
            for (var i = 0; i < column.Count; i++)
            {
                column[i].Y0 += spread * (i + 1);
                column[i].Y1 += spread * (i + 1);
            }
            column.ForEach(ReorderLinks);
        }

        // 让 source → target 的带子平直时 target 的 y0（同 d3-sankey 的 targetTop）
        double TargetTop(SankeyNode source, SankeyNode target)
        {
            var y = source.Y0 - (source.SourceLinks.Count - 1) * py / 2;
            foreach (var link in source.SourceLinks)
            {
                if (link.Target == target) break;
                y += link.Width + py;
            }
            foreach (var link in target.TargetLinks)
            {
                if (link.Source == source) break;
                y -= link.Width;
            }
            return y;
        }
        // 让 source → target 的带子平直时 source 的 y0
        double SourceTop(SankeyNode source, SankeyNode target)
        {
            var y = target.Y0 - (target.TargetLinks.Count - 1) * py / 2;
            foreach (var link in target.TargetLinks)
            {
                if (link.Source == source) break;
                y += link.Width + py;
            }
            foreach (var link in source.SourceLinks)
            {
                if (link.Target == target) break;
                y -= link.Width;
            }
            return y;
        }

        // 列里的节点互不重叠、不出上下边：从中间的节点往两头推，再从两头往回推
        void ResolveCollisions(List<SankeyNode> column, double alpha)
        {
            void PushDown(int from, double start)
            {
                var y = start;
                for (var i = from; i < column.Count; i++)
                {
                    var dy = (y - column[i].Y0) * alpha;
                    if (dy > 1e-6) Shift(column[i], dy);
                    y = column[i].Y1 + py;
                }
            }
            void PushUp(int from, double start)
            {
                var y = start;
                for (var i = from; i >= 0; i--)
                {
                    var dy = (column[i].Y1 - y) * alpha;
                    if (dy > 1e-6) Shift(column[i], -dy);
                    y = column[i].Y0 - py;
                }
            }
            var middle = column.Count >> 1;
            if (middle >= column.Count) return;
            var subject = column[middle];
            PushUp(middle - 1, subject.Y0 - py);
            PushDown(middle + 1, subject.Y1 + py);
            PushUp(column.Count - 1, height);
            PushDown(0, 0);
        }

        // 一列节点朝着与它相连的另一侧节点挪（按流量加权），再消除重叠
        void Relax(List<SankeyNode> column, double alpha, double beta, bool fromLeft)
        {
            foreach (var node in column)
            {
                var (y, weight) = (0.0, 0.0);
                foreach (var link in fromLeft ? node.TargetLinks : node.SourceLinks)
                {
                    var v = link.Value * (link.Target.Layer - link.Source.Layer);
                    y += (fromLeft ? TargetTop(link.Source, node) : SourceTop(node, link.Target)) * v;
                    weight += v;
                }
                if (!(weight > 0)) continue;
                Shift(node, (y / weight - node.Y0) * alpha);
                ReorderNodeLinks(node);
            }
            StableSort(column, ByBreadth);
            ResolveCollisions(column, beta);
        }
        for (var i = 0; i < iterations; i++)
        {
            var alpha = Math.Pow(0.99, i);
            var beta = Math.Max(1 - alpha, (i + 1.0) / iterations);
            for (var c = columns.Count - 2; c >= 0; c--) Relax(columns[c], alpha, beta, fromLeft: false);
            for (var c = 1; c < columns.Count; c++) Relax(columns[c], alpha, beta, fromLeft: true);
        }

        // 带子在两端节点上的位置：按另一端的上下顺序依次排开
        foreach (var node in nodes)
        {
            var (y0, y1) = (node.Y0, node.Y0);
            foreach (var link in node.SourceLinks)
            {
                link.Y0 = y0 + link.Width / 2;
                y0 += link.Width;
            }
            foreach (var link in node.TargetLinks)
            {
                link.Y1 = y1 + link.Width / 2;
                y1 += link.Width;
            }
        }
        return (nodes, links);
    }

    // 节点该排在第几列（共 columns 列）
    static double ColumnOf(SankeyNode node, int columns, SankeyAlign align) => align switch
    {
        SankeyAlign.Left => node.Depth,
        SankeyAlign.Right => columns - 1 - node.Height,
        SankeyAlign.Center => node.TargetLinks.Count > 0 ? node.Depth
            : node.SourceLinks.Count > 0 ? node.SourceLinks.Min(link => link.Target.Depth) - 1 : 0,
        _ => node.SourceLinks.Count > 0 ? node.Depth : columns - 1,
    };

    // 从起点（或终点）一层层往外数：Depth 是离起点的步数，Height 是离终点的步数
    static void AssignDepths(List<SankeyNode> nodes, bool forward)
    {
        var current = new List<SankeyNode>(nodes);
        for (var step = 0; current.Count > 0 && step <= nodes.Count; step++)
        {
            var next = new List<SankeyNode>();
            var seen = new HashSet<SankeyNode>();
            foreach (var node in current)
            {
                if (forward) node.Depth = step;
                else node.Height = step;
                foreach (var link in forward ? node.SourceLinks : node.TargetLinks)
                    if (seen.Add(forward ? link.Target : link.Source)) next.Add(forward ? link.Target : link.Source);
            }
            current = next;
        }
    }

    static int ByBreadth(SankeyNode a, SankeyNode b) => a.Y0.CompareTo(b.Y0);

    static int BySourceBreadth(SankeyLink a, SankeyLink b) => ByBreadth(a.Source, b.Source) is var c && c != 0 ? c : a.Index - b.Index;

    static int ByTargetBreadth(SankeyLink a, SankeyLink b) => ByBreadth(a.Target, b.Target) is var c && c != 0 ? c : a.Index - b.Index;

    static void ReorderLinks(SankeyNode node)
    {
        StableSort(node.SourceLinks, ByTargetBreadth);
        StableSort(node.TargetLinks, BySourceBreadth);
    }

    // 节点挪动之后，与它相连的节点上的带子顺序跟着变
    static void ReorderNodeLinks(SankeyNode node)
    {
        foreach (var link in node.TargetLinks) StableSort(link.Source.SourceLinks, ByTargetBreadth);
        foreach (var link in node.SourceLinks) StableSort(link.Target.TargetLinks, BySourceBreadth);
    }

    static void Shift(SankeyNode node, double dy)
    {
        node.Y0 += dy;
        node.Y1 += dy;
    }

    // 稳定排序（同 JS 的 Array.sort）：List.Sort 不稳定，相等的元素可能换位，布局就和 vue 版不同了
    internal static void StableSort<T>(List<T> list, Comparison<T> comparison)
    {
        var sorted = list.Select((item, index) => (item, index)).OrderBy(pair => pair.item, Comparer<T>.Create(comparison)).ThenBy(pair => pair.index).Select(pair => pair.item).ToList();
        list.Clear();
        list.AddRange(sorted);
    }
}
