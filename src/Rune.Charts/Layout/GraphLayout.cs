namespace Rune.UI;

/// <summary>网络图的布局。</summary>
public enum GraphLayoutKind
{
    /// <summary>力导向：连线像弹簧、节点互相排斥、往中心拉、不重叠（默认）。</summary>
    Force,

    /// <summary>分层：沿方向一层层排开（流程、依赖、组织）。</summary>
    Layered,

    /// <summary>排成一圈。</summary>
    Circular,

    /// <summary>同心圆：连得多的在中间，一圈圈往外。</summary>
    Concentric,

    /// <summary>分列：按组分列，列里按连接排。</summary>
    Parallel,
}

/// <summary>布局的输入：一个节点与它占的地方（节点加名字的外框）。</summary>
/// <param name="Id">标识。</param>
/// <param name="Group">所在的组。</param>
/// <param name="Width">占的宽。</param>
/// <param name="Height">占的高。</param>
public readonly record struct GraphLayoutInput(string Id, string? Group, double Width, double Height);

/// <summary>
/// 网络图的布局（同 vue-tsx-pro 的 layout/graph.ts）：给每个节点算出坐标（px，原点任意，画的时候再整体居中、按绘图区缩放）。
/// <list type="bullet">
/// <item>Force：d3-force 的做法，从固定的初始位置迭代 300 步，结果是确定的；</item>
/// <item>Layered：Sugiyama 的简化版（去环、按最长路径分层、重心法几轮减少交叉、向邻居的平均位置靠拢），vertical 时从上到下；</item>
/// <item>Circular、Concentric、Parallel：一圈、同心圆、按组分列。</item>
/// </list>
/// 有组时 Layered、Circular、Parallel 让同组的节点挨在一起（组的先后按 groups）。
/// </summary>
public static class GraphLayout
{
    /// <summary>布局；links 的两端是节点的标识。</summary>
    public static Dictionary<string, (double X, double Y)> Arrange(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<(string Source, string Target)> links,
        GraphLayoutKind layout, bool vertical = false, IReadOnlyList<string>? groups = null, double linkDistance = 90, double charge = -600)
    {
        if (nodes.Count == 0) return [];
        return layout switch
        {
            GraphLayoutKind.Layered => Layered(nodes, links, vertical, groups),
            GraphLayoutKind.Circular => Circular(nodes, groups),
            GraphLayoutKind.Concentric => Concentric(nodes, links),
            GraphLayoutKind.Parallel => Parallel(nodes, links, vertical, groups),
            _ => Force(nodes, links, linkDistance, charge),
        };
    }

    // JS 的 a || b：a 是 0 或 NaN 时取 b
    static double Or(double value, double fallback) => value == 0 || double.IsNaN(value) ? fallback : value;

    // 节点的邻居：按标识查
    static Dictionary<string, List<string>> Neighbors(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<(string Source, string Target)> links)
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var node in nodes) map.TryAdd(node.Id, []);
        foreach (var (source, target) in links)
        {
            if (source == target) continue;
            if (map.TryGetValue(source, out var a)) a.Add(target);
            if (map.TryGetValue(target, out var b)) b.Add(source);
        }
        return map;
    }

    // 按组排先后：组在 groups 里的位置，没有组的排最后；同组按原来的顺序
    static Func<GraphLayoutInput, int> GroupRank(IReadOnlyList<string>? groups) => node =>
    {
        var index = node.Group is null || groups is null ? -1 : IndexOf(groups, node.Group);
        return index < 0 ? groups?.Count ?? 0 : index;
    };

    static int IndexOf(IReadOnlyList<string> list, string value)
    {
        for (var i = 0; i < list.Count; i++)
            if (list[i] == value) return i;
        return -1;
    }

    sealed class Point
    {
        public double X, Y, Vx, Vy;
    }

    static Dictionary<string, (double X, double Y)> Force(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<(string Source, string Target)> links, double distance, double charge)
    {
        var index = new Dictionary<string, int>();
        for (var i = 0; i < nodes.Count; i++) index[nodes[i].Id] = i;
        // 初始位置：向日葵式的螺旋（同 d3-force），节点均匀铺开，结果每次相同
        var points = nodes.Select((_, i) =>
        {
            var radius = 10 * Math.Sqrt(0.5 + i);
            var angle = i * Math.PI * (3 - Math.Sqrt(5));
            return new Point { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) };
        }).ToArray();
        var edges = links
            .Select(link => (index.TryGetValue(link.Source, out var s) ? s : -1, index.TryGetValue(link.Target, out var t) ? t : -1))
            .Where(pair => pair.Item1 >= 0 && pair.Item2 >= 0 && pair.Item1 != pair.Item2)
            .ToList();
        var count = new double[points.Length];
        foreach (var (s, t) in edges)
        {
            count[s] += 1;
            count[t] += 1;
        }
        var radii = nodes.Select(node => double.Hypot(node.Width, node.Height) / 2).ToArray();
        // 两点重合时给一点确定的偏移，免得除以 0
        static double Jiggle(int i) => (i + 1) * 1e-6;
        var decay = 1 - Math.Pow(0.001, 1.0 / 300);
        var alpha = 1.0;
        for (var tick = 0; tick < 300; tick++)
        {
            alpha += (0 - alpha) * decay;
            foreach (var (s, t) in edges)
            {
                var (a, b) = (points[s], points[t]);
                var dx = Or(b.X + b.Vx - a.X - a.Vx, Jiggle(s));
                var dy = Or(b.Y + b.Vy - a.Y - a.Vy, Jiggle(t));
                var length = double.Hypot(dx, dy);
                var strength = 1 / Math.Min(count[s], count[t]);
                var k = (length - distance) / length * alpha * strength;
                var bias = count[s] / (count[s] + count[t]);
                dx *= k;
                dy *= k;
                b.Vx -= dx * bias;
                b.Vy -= dy * bias;
                a.Vx += dx * (1 - bias);
                a.Vy += dy * (1 - bias);
            }
            for (var i = 0; i < points.Length; i++)
                for (var j = 0; j < points.Length; j++)
                {
                    if (i == j) continue;
                    var dx = Or(points[j].X - points[i].X, Jiggle(i));
                    var dy = Or(points[j].Y - points[i].Y, Jiggle(j));
                    var squared = Math.Max(dx * dx + dy * dy, 1);
                    var w = charge * alpha / squared;
                    points[i].Vx += dx * w;
                    points[i].Vy += dy * w;
                }
            foreach (var point in points)
            {
                point.Vx *= 0.6;
                point.Vy *= 0.6;
                point.X += point.Vx;
                point.Y += point.Vy;
            }
            // 不重叠：两两推开到外框的半径之和
            for (var i = 0; i < points.Length; i++)
                for (var j = i + 1; j < points.Length; j++)
                {
                    var (a, b) = (points[i], points[j]);
                    var dx = Or(b.X - a.X, Jiggle(i));
                    var dy = Or(b.Y - a.Y, Jiggle(j));
                    var length = double.Hypot(dx, dy);
                    var room = (radii[i] + radii[j]) * 0.6;
                    if (length >= room) continue;
                    var push = (room - length) / length * 0.5;
                    a.X -= dx * push;
                    a.Y -= dy * push;
                    b.X += dx * push;
                    b.Y += dy * push;
                }
        }
        var positions = new Dictionary<string, (double, double)>();
        for (var i = 0; i < nodes.Count; i++) positions[nodes[i].Id] = (points[i].X, points[i].Y);
        return positions;
    }

    // 分层：每层里节点按顺序排，层与层之间 rankGap、层内按各自的外框留空；几轮向邻居的平均位置靠拢（保持顺序、不重叠），连线更直
    static Dictionary<string, (double X, double Y)> PlaceLayers(List<List<GraphLayoutInput>> layers, Dictionary<string, List<string>> around, bool vertical, double rankGap)
    {
        double Breadth(GraphLayoutInput node) => (vertical ? node.Width : node.Height) + 16;
        var along = new Dictionary<string, double>();
        // 每层先居中排开
        foreach (var layer in layers)
        {
            var position = -layer.Sum(Breadth) / 2;
            foreach (var node in layer)
            {
                along[node.Id] = position + Breadth(node) / 2;
                position += Breadth(node);
            }
        }
        // 一层节点挪到邻居的平均位置，再从中间往两头推开、不重叠
        void Relax(List<GraphLayoutInput> layer)
        {
            var wanted = layer.Select(node =>
            {
                var others = around.GetValueOrDefault(node.Id, []).Where(along.ContainsKey).ToList();
                return others.Count > 0 ? others.Sum(id => along[id]) / others.Count : along[node.Id];
            }).ToList();
            for (var i = 0; i < layer.Count; i++) along[layer[i].Id] = wanted[i];
            var middle = layer.Count >> 1;
            for (var i = middle + 1; i < layer.Count; i++)
            {
                var min = along[layer[i - 1].Id] + (Breadth(layer[i - 1]) + Breadth(layer[i])) / 2;
                along[layer[i].Id] = Math.Max(along[layer[i].Id], min);
            }
            for (var i = middle - 1; i >= 0; i--)
            {
                var max = along[layer[i + 1].Id] - (Breadth(layer[i + 1]) + Breadth(layer[i])) / 2;
                along[layer[i].Id] = Math.Min(along[layer[i].Id], max);
            }
        }
        for (var round = 0; round < 4; round++)
        {
            foreach (var layer in layers.Skip(1)) Relax(layer);
            foreach (var layer in layers.Take(layers.Count - 1).Reverse()) Relax(layer);
        }
        var positions = new Dictionary<string, (double, double)>();
        for (var rank = 0; rank < layers.Count; rank++)
            foreach (var node in layers[rank])
            {
                var across = rank * rankGap;
                var value = along[node.Id];
                positions[node.Id] = vertical ? (value, across) : (across, value);
            }
        return positions;
    }

    // 重心法排每层的顺序：几轮上下来回，按相邻层里邻居的平均位置排；有组时组优先
    static void OrderLayers(List<List<GraphLayoutInput>> layers, Dictionary<string, List<string>> around, Func<GraphLayoutInput, int> rankOf)
    {
        var position = new Dictionary<string, double>();
        foreach (var layer in layers)
            for (var i = 0; i < layer.Count; i++) position[layer[i].Id] = i;
        void Sweep(List<GraphLayoutInput> layer, List<GraphLayoutInput> neighborLayer)
        {
            var inNeighbor = neighborLayer.Select(node => node.Id).ToHashSet();
            double Center(GraphLayoutInput node)
            {
                var others = around.GetValueOrDefault(node.Id, []).Where(inNeighbor.Contains).ToList();
                return others.Count > 0 ? others.Sum(id => position[id]) / others.Count : position[node.Id];
            }
            var keys = layer.ToDictionary(node => node.Id, Center);
            SankeyLayout.StableSort(layer, (a, b) => rankOf(a) - rankOf(b) is var byRank && byRank != 0 ? byRank : keys[a.Id].CompareTo(keys[b.Id]));
            for (var i = 0; i < layer.Count; i++) position[layer[i].Id] = i;
        }
        for (var round = 0; round < 6; round++)
        {
            for (var i = 1; i < layers.Count; i++) Sweep(layers[i], layers[i - 1]);
            for (var i = layers.Count - 2; i >= 0; i--) Sweep(layers[i], layers[i + 1]);
        }
    }

    // 分层：去环后按最长路径分层
    static Dictionary<string, (double X, double Y)> Layered(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<(string Source, string Target)> links, bool vertical, IReadOnlyList<string>? groups)
    {
        var ids = nodes.Select(node => node.Id).ToHashSet();
        var outgoing = new Dictionary<string, List<string>>();
        foreach (var node in nodes) outgoing.TryAdd(node.Id, []);
        // 去环：深度优先，指回栈里节点的边不算（等于把它反过来）
        var state = new Dictionary<string, bool>();   // false：在栈里，true：完成
        void Visit(string id)
        {
            state[id] = false;
            foreach (var (source, target) in links)
            {
                if (source != id || !ids.Contains(target) || target == id) continue;
                var seen = state.TryGetValue(target, out var done);
                if (seen && !done) continue;
                outgoing[id].Add(target);
                if (!seen) Visit(target);
            }
            state[id] = true;
        }
        foreach (var node in nodes)
            if (!state.ContainsKey(node.Id)) Visit(node.Id);
        // 最长路径：按拓扑序，节点的层 = 前驱的层 + 1 的最大值
        var indegree = nodes.Select(node => node.Id).Distinct().ToDictionary(id => id, _ => 0);
        foreach (var targets in outgoing.Values)
            foreach (var target in targets.Distinct()) indegree[target]++;
        var rank = nodes.Select(node => node.Id).Distinct().ToDictionary(id => id, _ => 0);
        var queue = new Queue<string>(nodes.Where(node => indegree[node.Id] == 0).Select(node => node.Id));
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var target in outgoing[id].Distinct())
            {
                rank[target] = Math.Max(rank[target], rank[id] + 1);
                if (--indegree[target] == 0) queue.Enqueue(target);
            }
        }
        var byRank = GroupRank(groups);
        var layers = nodes.OrderBy(byRank).GroupBy(node => rank[node.Id]).OrderBy(layer => layer.Key).Select(layer => layer.ToList()).ToList();
        var around = Neighbors(nodes, links);
        OrderLayers(layers, around, byRank);
        // 层间距：横向时要放得下名字（外框的宽），纵向时放得下节点加名字的高
        var extent = nodes.Max(node => vertical ? node.Height : node.Width);
        return PlaceLayers(layers, around, vertical, extent + (groups is { Count: > 0 } ? 72 : 48));
    }

    // 环形：按组、按原来的顺序排成一圈，圈的大小放得下所有外框
    static Dictionary<string, (double X, double Y)> Circular(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<string>? groups)
    {
        var sorted = nodes.OrderBy(GroupRank(groups)).ToList();
        var perimeter = sorted.Sum(node => Math.Max(node.Width, node.Height) + 12);
        var radius = Math.Max(perimeter / (Math.PI * 2), 60);
        var positions = new Dictionary<string, (double, double)>();
        for (var i = 0; i < sorted.Count; i++)
        {
            var angle = (double)i / sorted.Count * Math.PI * 2 - Math.PI / 2;
            positions[sorted[i].Id] = (radius * Math.Cos(angle), radius * Math.Sin(angle));
        }
        return positions;
    }

    // 同心圆：连接数最多的在圆心，往外每圈放得下多少放多少
    static Dictionary<string, (double X, double Y)> Concentric(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<(string Source, string Target)> links)
    {
        var around = Neighbors(nodes, links);
        var sorted = nodes.OrderByDescending(node => around.GetValueOrDefault(node.Id)?.Count ?? 0).ToList();
        var footprint = nodes.Max(node => Math.Max(node.Width, node.Height)) + 12;
        var positions = new Dictionary<string, (double, double)>();
        var (ring, placed) = (0, 0);
        while (placed < sorted.Count)
        {
            var radius = ring * footprint;
            var capacity = ring == 0 ? 1 : Math.Max(1, (int)Math.Floor(Math.PI * 2 * radius / footprint));
            var members = sorted.Skip(placed).Take(capacity).ToList();
            for (var i = 0; i < members.Count; i++)
            {
                var angle = (double)i / members.Count * Math.PI * 2 - Math.PI / 2;
                positions[members[i].Id] = (radius * Math.Cos(angle), radius * Math.Sin(angle));
            }
            placed += members.Count;
            ring++;
        }
        return positions;
    }

    // 分列：每组一列（没有组的单独一列），列里按重心法排
    static Dictionary<string, (double X, double Y)> Parallel(IReadOnlyList<GraphLayoutInput> nodes, IReadOnlyList<(string Source, string Target)> links, bool vertical, IReadOnlyList<string>? groups)
    {
        var byRank = GroupRank(groups);
        var columns = nodes.GroupBy(byRank).OrderBy(column => column.Key).Select(column => column.ToList()).ToList();
        var around = Neighbors(nodes, links);
        OrderLayers(columns, around, _ => 0);
        var extent = nodes.Max(node => vertical ? node.Height : node.Width);
        return PlaceLayers(columns, around, vertical, extent + 96);
    }
}
