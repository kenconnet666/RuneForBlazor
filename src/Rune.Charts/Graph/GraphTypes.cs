using Rune.Styles;

namespace Rune.UI;

/// <summary>网络图节点的形状。</summary>
public enum GraphNodeShape
{
    /// <summary>圆（默认）。</summary>
    Circle,

    /// <summary>方块。</summary>
    Square,

    /// <summary>六边形。</summary>
    Hexagon,

    /// <summary>三角。</summary>
    Triangle,
}

/// <summary>分层、分列布局的方向。</summary>
public enum GraphDirection
{
    /// <summary>从左到右（默认）。</summary>
    LeftToRight,

    /// <summary>从上到下。</summary>
    TopToBottom,
}

/// <summary>网络图的一个节点。</summary>
/// <param name="Id">标识：连线里用它指节点。</param>
public sealed record GraphNode(string Id)
{
    /// <summary>名称，写在节点下面；不写时用 Id。</summary>
    public string? Label { get; init; }

    /// <summary>名称下面的小字。</summary>
    public string? Sublabel { get; init; }

    /// <summary>所在的组（<see cref="GraphGroup.Id"/>）：同组的框在一起、颜色相同。</summary>
    public string? Group { get; init; }

    /// <summary>颜色；不写时按组取色板，没有组时是色板的第 1 个。</summary>
    public ChartColor? Color { get; init; }

    /// <summary>形状，默认圆。</summary>
    public GraphNodeShape Shape { get; init; }

    /// <summary>大小（px），默认 28。</summary>
    public double? Size { get; init; }
}

/// <summary>网络图的一条连线。</summary>
/// <param name="Source">起点节点的 Id。</param>
/// <param name="Target">终点节点的 Id。</param>
public sealed record GraphLink(string Source, string Target)
{
    /// <summary>强度：写了时按大小加粗（1–6px）；提示框、数据表里显示。</summary>
    public decimal? Value { get; init; }

    /// <summary>线上的短标签（几个字以内）。</summary>
    public string? Label { get; init; }

    /// <summary>虚线：弱关系、待定的关系。</summary>
    public bool Dashed { get; init; }

    /// <summary>颜色；默认是边框色。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>网络图的一组节点。</summary>
/// <param name="Id">组的标识：节点的 Group 指它。</param>
/// <param name="Label">组名：写在框上。</param>
public sealed record GraphGroup(string Id, string Label)
{
    /// <summary>颜色；不写时按组的顺序取色板。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>画图用的节点：带上名称、颜色、大小与位置。</summary>
internal sealed record PlacedGraphNode(GraphNode Source, int Index, string Label, CssColor Fill, double Size, double X, double Y)
{
    public string Id => Source.Id;
}
