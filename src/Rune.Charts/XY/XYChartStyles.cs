using Rune.Styles;

namespace Rune.UI;

/// <summary>直角坐标图表的样式：加载中的骨架（同 vue-tsx-pro 的 xyChartStyles；外层结构见 <see cref="ChartStyles"/>）。</summary>
internal static class XYChartStyles
{
    static readonly CssVar<CssLength> BarSize = CssVar.Length("rune-chart-skeleton-bar");

    // 缩放：滑块占满，“显示全部”在结束边
    internal static Style Zoom => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Md, PaddingInline = Space.Sm });

    internal static Style ZoomSlider => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0 });

    internal static Style SkeletonBars => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexEnd,
        JustifyContent = JustifyContent.SpaceAround,
        Gap = Space.Md,
        Height = CssLength.Percent(100),
    });

    internal static Style SkeletonBarsHorizontal => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column, AlignItems = AlignItems.FlexStart });

    static Style Bar => Css.Define(static () => new Style { Display = Display.Block, FlexGrow = 1, FlexBasis = 0, BorderRadius = Radii.Sm, Height = BarSize });

    static Style BarHorizontal => Css.Define(static () => new Style { Display = Display.Block, FlexGrow = 1, FlexBasis = 0, BorderRadius = Radii.Sm, Width = BarSize });

    /// <summary>一根骨架柱：竖向时 percent 是高度，横向时是宽度。</summary>
    internal static Attrs SkeletonBar(int percent, bool horizontal) => new() { Sx = horizontal ? BarHorizontal : Bar, Vars = [BarSize.Set(CssLength.Percent(percent))] };
}
