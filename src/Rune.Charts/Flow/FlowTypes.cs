using Rune.Styles;

namespace Rune.UI;

/// <summary>流向图（桑基图、弦图）的一个节点（同 vue-tsx-pro 的 FlowNode）。</summary>
/// <param name="Id">节点的标识：流向里用它指节点。</param>
public sealed record FlowNode(string Id)
{
    /// <summary>显示的名字；不写时用 Id。</summary>
    public string? Label { get; init; }

    /// <summary>颜色；不写时按顺序取图表色板。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>一条流向：从 Source 到 Target 的量。</summary>
/// <param name="Source">起点节点的标识。</param>
/// <param name="Target">终点节点的标识。</param>
/// <param name="Value">流量，要大于 0；不是正数、指向不存在节点的流向不画。</param>
public sealed record FlowLink(string Source, string Target, decimal Value);

/// <summary>节点加上实际的名字与颜色（流向图共用，同 vue 的 resolveFlowNodes）。</summary>
internal sealed record ResolvedFlowNode(string Id, string Label, CssColor Color)
{
    /// <summary>把颜色写在元素的 style 上。</summary>
    public Attrs ColorLayer { get; } = ChartFrame.ColorVar(Color);

    /// <summary>节点的名字与颜色定下来；按 Id 查（重复的 Id 取第一个）。</summary>
    public static Dictionary<string, ResolvedFlowNode> Of(IReadOnlyList<FlowNode> nodes)
    {
        var resolved = new Dictionary<string, ResolvedFlowNode>();
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            resolved.TryAdd(node.Id, new ResolvedFlowNode(node.Id, node.Label ?? node.Id, ChartColor.Resolve(node.Color, index)));
        }
        return resolved;
    }

    /// <summary>布局用的流向：两端都在、流量是正数的；Index 是它在输入里的位置。</summary>
    public static List<(FlowLink Link, int Index)> Valid(IReadOnlyDictionary<string, ResolvedFlowNode> nodes, IReadOnlyList<FlowLink> links) =>
        [.. links.Select((link, index) => (link, index)).Where(pair => nodes.ContainsKey(pair.link.Source) && nodes.ContainsKey(pair.link.Target) && pair.link.Value > 0)];
}
