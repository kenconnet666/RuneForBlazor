namespace Rune.UI;

/// <summary>地图上一个区域的数据。</summary>
/// <param name="Id">地图要素的 id（中国省份是 "CN-JS" 这样的写法，世界各国是 ISO 两位代码）。</param>
/// <param name="Label">显示的名称：提示框、区域名、数据表。</param>
public sealed record MapArea(string Id, string Label)
{
    /// <summary>数值：决定颜色。</summary>
    public decimal? Value { get; init; }

    /// <summary>指定颜色：不按数值着色（分类着色，如所属大区）。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>地点标记的形状。</summary>
public enum MapPointShape
{
    /// <summary>圆（默认）。</summary>
    Circle,

    /// <summary>方块。</summary>
    Square,

    /// <summary>三角。</summary>
    Triangle,
}

/// <summary>地图上的一个地点。</summary>
/// <param name="Id">标识：流向的 From、To 指向它。</param>
/// <param name="Label">名称：写在点旁边。</param>
/// <param name="Longitude">经度。</param>
/// <param name="Latitude">纬度。</param>
public sealed record MapPoint(string Id, string Label, double Longitude, double Latitude)
{
    /// <summary>数值：决定点的大小（面积按比例），也显示在提示框。</summary>
    public decimal? Value { get; init; }

    /// <summary>颜色，默认 Danger。</summary>
    public ChartColor? Color { get; init; }

    /// <summary>形状，默认圆。</summary>
    public MapPointShape Shape { get; init; }
}

/// <summary>两个地点之间的流向。</summary>
/// <param name="From">起点（地点的 Id）。</param>
/// <param name="To">终点（地点的 Id）。</param>
public sealed record MapLink(string From, string To)
{
    /// <summary>数值：决定线的粗细，也显示在数据表。</summary>
    public decimal? Value { get; init; }

    /// <summary>颜色，默认次要色。</summary>
    public ChartColor? Color { get; init; }
}
