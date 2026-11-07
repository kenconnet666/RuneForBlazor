namespace Rune.UI;

/// <summary>矩形树图上的一个节点：分组或一条明细（叶子）；布局后带上矩形。</summary>
public sealed class TreemapNode<TData>
{
    /// <summary>分组的值（区域名），叶子是最内层分组的值。</summary>
    public required string Key { get; init; }

    /// <summary>数值：叶子是明细的值，分组是子节点之和。</summary>
    public double Value { get; internal set; }

    /// <summary>层级：根是 0。</summary>
    public required int Depth { get; init; }

    /// <summary>叶子对应的明细。</summary>
    public TData? Data { get; init; }

    /// <summary>父节点（根没有）。</summary>
    public TreemapNode<TData>? Parent { get; init; }

    /// <summary>子节点，按数值从大到小（叶子是 null）。</summary>
    public List<TreemapNode<TData>>? Children { get; init; }

    /// <summary>左。</summary>
    public double X0 { get; internal set; }

    /// <summary>上。</summary>
    public double Y0 { get; internal set; }

    /// <summary>右。</summary>
    public double X1 { get; internal set; }

    /// <summary>下。</summary>
    public double Y1 { get; internal set; }

    /// <summary>从顶层到它的路径（“华东 / 华辰精密”）。</summary>
    public string Path => Parent?.Parent is null ? Key : $"{Parent.Path} / {Key}";

    /// <summary>它所在的顶层分组。</summary>
    public TreemapNode<TData> Top => Parent?.Parent is null ? this : Parent.Top;
}

/// <summary>
/// 矩形树图的布局（同 vue-tsx-pro 的 layout/treemap.ts）：扁平的明细按几个字段逐级分组成树，再按 squarified 算法切块，
/// 块的面积与数值成比例、尽量接近正方形（Bruls、Huizing、van Wijk 的 squarify，写法同 d3-hierarchy 的 treemapSquarify）。
/// <code>var root = TreemapLayout.Group(rows, [r => r.Region, r => r.Supplier], r => r.Amount);
/// TreemapLayout.Arrange(root, 600, 300, padding: 3);   // root.Children[0].X0、Y0、X1、Y1</code>
/// </summary>
public static class TreemapLayout
{
    // 黄金比：squarify 追求的长宽比
    static readonly double Phi = (1 + Math.Sqrt(5)) / 2;

    /// <summary>明细按 keys 逐级分组成树：同一组的明细挂在同一个分组下，数值不是正数的明细不要。</summary>
    public static TreemapNode<TData> Group<TData>(IEnumerable<TData> rows, IReadOnlyList<Func<TData, string>> keys, Func<TData, double> value)
    {
        var root = new TreemapNode<TData> { Key = "", Depth = 0, Children = [] };
        foreach (var row in rows)
        {
            var amount = value(row);
            if (!(amount > 0)) continue;
            var node = root;
            for (var level = 0; level < keys.Count; level++)
            {
                var name = keys[level](row);
                var leaf = level == keys.Count - 1;
                var child = leaf ? null : node.Children!.Find(item => item.Key == name);
                if (child is null)
                {
                    child = new TreemapNode<TData> { Key = name, Depth = level + 1, Parent = node, Data = leaf ? row : default, Children = leaf ? null : [] };
                    node.Children!.Add(child);
                }
                if (leaf) child.Value += amount;
                node = child;
            }
        }
        Sum(root);
        return root;
    }

    static double Sum<TData>(TreemapNode<TData> node)
    {
        if (node.Children is null) return node.Value;
        node.Value = node.Children.Sum(Sum);
        // 稳定排序：数值相同时保持出现的顺序（同 JS 的 Array.sort）
        var sorted = node.Children.OrderByDescending(child => child.Value).ToList();
        node.Children.Clear();
        node.Children.AddRange(sorted);
        return node.Value;
    }

    // 一行（或一列）子节点沿横向依次排开
    static void Dice<TData>(IEnumerable<TreemapNode<TData>> nodes, double total, double x0, double y0, double x1, double y1)
    {
        var k = total != 0 ? (x1 - x0) / total : 0;
        var x = x0;
        foreach (var node in nodes)
        {
            (node.X0, node.Y0, node.Y1) = (x, y0, y1);
            node.X1 = x += node.Value * k;
        }
    }

    // 沿纵向依次排开
    static void Slice<TData>(IEnumerable<TreemapNode<TData>> nodes, double total, double x0, double y0, double x1, double y1)
    {
        var k = total != 0 ? (y1 - y0) / total : 0;
        var y = y0;
        foreach (var node in nodes)
        {
            (node.X0, node.Y0, node.X1) = (x0, y, x1);
            node.Y1 = y += node.Value * k;
        }
    }

    // squarify：一行行地放，一行里不断加节点，直到再加会让最长的长宽比变差
    static void Squarify<TData>(TreemapNode<TData> parent, double x0, double y0, double x1, double y1)
    {
        var nodes = parent.Children ?? [];
        var value = parent.Value;
        var i0 = 0;
        while (i0 < nodes.Count)
        {
            var (dx, dy) = (x1 - x0, y1 - y0);
            var i1 = i0;
            double sumValue;
            // 跳过数值为 0 的节点
            do sumValue = nodes[i1++].Value;
            while (sumValue == 0 && i1 < nodes.Count);
            var (minValue, maxValue) = (sumValue, sumValue);
            var alpha = Math.Max(dy / dx, dx / dy) / (value * Phi);
            var beta = sumValue * sumValue * alpha;
            var minRatio = Math.Max(maxValue / beta, beta / minValue);
            for (; i1 < nodes.Count; i1++)
            {
                var nodeValue = nodes[i1].Value;
                sumValue += nodeValue;
                minValue = Math.Min(minValue, nodeValue);
                maxValue = Math.Max(maxValue, nodeValue);
                beta = sumValue * sumValue * alpha;
                var ratio = Math.Max(maxValue / beta, beta / minValue);
                if (ratio > minRatio)
                {
                    sumValue -= nodeValue;
                    break;
                }
                minRatio = ratio;
            }
            var row = nodes.GetRange(i0, i1 - i0);
            // 宽比高小：这一行横着排在顶上；否则竖着排在左边
            if (dx < dy)
            {
                var y = dy != 0 && value != 0 ? y0 + dy * sumValue / value : y1;
                Dice(row, sumValue, x0, y0, x1, y);
                y0 = y;
            }
            else
            {
                var x = dx != 0 && value != 0 ? x0 + dx * sumValue / value : x1;
                Slice(row, sumValue, x0, y0, x, y1);
                x0 = x;
            }
            value -= sumValue;
            i0 = i1;
        }
    }

    /// <summary>给树上每个节点算出矩形（就地写入）：分组四周与块之间留 padding，分组顶边留 paddingTop（不写时同 padding）。</summary>
    public static void Arrange<TData>(TreemapNode<TData> root, double width, double height, double padding = 0, double? paddingTop = null)
    {
        (root.X0, root.Y0, root.X1, root.Y1) = (0, 0, width, height);
        var half = padding / 2;
        var top = paddingTop ?? padding;
        void Position(TreemapNode<TData> node)
        {
            if (node.Children is not { Count: > 0 } children) return;
            // 子节点先在外扩半个间距的区域里切块，各自再缩回半个间距：相邻两块之间正好一个 padding（同 d3 的 paddingInner）
            var (topInset, inset) = node == root ? (0, 0) : (top, padding);
            var (x0, y0, x1, y1) = (node.X0 + inset - half, node.Y0 + topInset - half, node.X1 - inset + half, node.Y1 - inset + half);
            if (x1 < x0) x0 = x1 = (x0 + x1) / 2;
            if (y1 < y0) y0 = y1 = (y0 + y1) / 2;
            Squarify(node, x0, y0, x1, y1);
            foreach (var child in children)
            {
                child.X0 += half;
                child.Y0 += half;
                child.X1 = Math.Max(child.X0, child.X1 - half);
                child.Y1 = Math.Max(child.Y0, child.Y1 - half);
                Position(child);
            }
        }
        Position(root);
    }
}
