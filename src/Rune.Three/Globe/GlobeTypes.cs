using Rune.Styles;

namespace Rune.UI;

/// <summary>地图上的一个点：纬度（北正南负）、经度（东正西负），单位度。</summary>
/// <param name="Lat">纬度：北纬为正，-90 到 90。</param>
/// <param name="Lng">经度：东经为正，-180 到 180。</param>
public readonly record struct GeoPoint(double Lat, double Lng);

/// <summary>地球上标注的地点。</summary>
/// <param name="Lat">纬度：北纬为正。</param>
/// <param name="Lng">经度：东经为正。</param>
public sealed record GlobeMarker(double Lat, double Lng)
{
    /// <summary>大小的倍数，默认 1。</summary>
    public double? Size { get; init; }

    /// <summary>颜色（token 或 CSS 颜色）；不写时用 Globe 的 MarkerColor。</summary>
    public CssColor? Color { get; init; }
}

/// <summary>两地之间的弧线。</summary>
/// <param name="From">起点。</param>
/// <param name="To">终点。</param>
public sealed record GlobeArc(GeoPoint From, GeoPoint To);

/// <summary>点阵地球的样式（同 vue-tsx-pro 的 globeStyles）：默认正方形；能拖动时显示抓手光标，拖动中（data-dragging，JS 写）是抓住的光标。</summary>
internal static class GlobeStyles
{
    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs Draggable = new() { Sx = Sx.Of(RootStyle, DraggableStyle) };

    static Style RootStyle => Css.Define(static () => new Style { AspectRatio = 1, Width = CssLength.Percent(100) });

    static Style DraggableStyle => Css.Define(static () => new Style
    {
        Cursor = Cursors.Grab,
        TouchAction = TouchAction.PanY,
        [When.Attribute("[data-dragging]")] = new() { Cursor = Cursors.Grabbing },
    });
}
