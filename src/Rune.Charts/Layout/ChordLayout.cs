namespace Rune.UI;

/// <summary>弦图里一个节点的弧。</summary>
/// <param name="Index">节点的序号。</param>
/// <param name="Start">起始角（12 点钟方向顺时针，弧度）。</param>
/// <param name="End">结束角。</param>
/// <param name="Value">流入加流出的总量。</param>
public readonly record struct ChordGroup(int Index, double Start, double End, double Value);

/// <summary>弦图里的一根弦。</summary>
/// <param name="Index">在输入里的位置。</param>
/// <param name="Source">起点节点上的一段（起始角, 结束角）。</param>
/// <param name="Target">终点节点上的一段。</param>
/// <param name="Value">流量。</param>
public readonly record struct ChordLink(int Index, (double Start, double End) Source, (double Start, double End) Target, double Value);

/// <summary>
/// 弦图的布局（同 vue-tsx-pro 的 layout/chord.ts，即 d3-chord 的 chordDirected）：节点排成一圈，每个节点一段弧（长短按流入加流出的总量），
/// 每条流向一根弦，起点一端在起点节点的弧上、终点一端在终点节点的弧上，宽度都按流量。角度与 <see cref="ChartShapes.Arc"/>、<see cref="ChartShapes.Ribbon"/> 一致。
/// </summary>
public static class ChordLayout
{
    /// <summary>布局：count 是节点数，links 的两端是节点的序号，padAngle 是相邻两段弧之间的空隙（弧度）。</summary>
    public static (IReadOnlyList<ChordGroup> Groups, IReadOnlyList<ChordLink> Chords) Arrange(int count, IReadOnlyList<(int Source, int Target, double Value)> links, double padAngle)
    {
        var totals = new double[count];
        foreach (var (source, target, value) in links)
        {
            totals[source] += value;
            totals[target] += value;
        }
        var sum = totals.Sum();
        var k = sum > 0 ? Math.Max(0, Math.PI * 2 - padAngle * count) / sum : 0;
        var sources = new (double, double)[links.Count];
        var targets = new (double, double)[links.Count];
        var groups = new List<ChordGroup>();
        var angle = 0.0;
        for (var node = 0; node < count; node++)
        {
            var start = angle;
            // 先排流入（按起点从后往前），再排流出（按终点从前往后）：相邻节点之间的弦不交叉
            for (var other = count - 1; other >= 0; other--)
                for (var index = 0; index < links.Count; index++)
                    if (links[index].Target == node && links[index].Source == other) targets[index] = (angle, angle += links[index].Value * k);
            for (var other = 0; other < count; other++)
                for (var index = 0; index < links.Count; index++)
                    if (links[index].Source == node && links[index].Target == other) sources[index] = (angle, angle += links[index].Value * k);
            groups.Add(new ChordGroup(node, start, angle, totals[node]));
            angle += padAngle;
        }
        return (groups, [.. links.Select((link, index) => new ChordLink(index, sources[index], targets[index], link.Value))]);
    }
}
