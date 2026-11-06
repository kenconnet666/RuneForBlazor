using Rune.Styles;

namespace Rune.UI;

/// <summary>迷你图的画法。</summary>
public enum SparklineVariant
{
    /// <summary>折线（默认）。</summary>
    Line,

    /// <summary>折线下面填一层浅色。</summary>
    Area,

    /// <summary>柱：每个值一根，从 0 起。</summary>
    Bar,
}

/// <summary>迷你图的样式（同 vue-tsx-pro 的 sparklineStyles）：颜色来自外层的 --rune-tone-fill（SharedStyles.ToneFill）。</summary>
public static class SparklineStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssLength> HeightVar = CssVar.Length("rune-sparkline-height", "2em");
    internal static readonly CssVar<CssLength> LastX = CssVar.Length("rune-sparkline-last-x");
    internal static readonly CssVar<CssLength> LastY = CssVar.Length("rune-sparkline-last-y");

    internal static Attrs Root(Tone tone) => Roots[(int)tone];

    internal static readonly Attrs Svg = new() { Sx = SvgStyle };
    internal static readonly Attrs Line = new() { Sx = LineStyle };
    internal static readonly Attrs Area = new() { Sx = AreaStyle };
    internal static readonly Attrs Bar = new() { Sx = BarStyle };
    internal static readonly Attrs Last = new() { Sx = LastStyle };

    static readonly Attrs[] Roots = [.. Enum.GetValues<Tone>().Select(tone => new Attrs { Sx = Sx.Of(RootStyle, SharedStyles.ToneFill(tone)) })];

    static Style RootStyle => Css.Define(static () => new Style { Position = Position.Relative, Display = Display.Block, Width = CssLength.Percent(100), Height = HeightVar });

    static Style SvgStyle => Css.Define(static () => new Style { Display = Display.Block, Width = CssLength.Percent(100), Height = CssLength.Percent(100), Overflow = Overflow.Visible });

    static Style LineStyle => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Stroke = SharedStyles.FillColor,
        StrokeWidth = 1.5,
        StrokeLinejoin = StrokeLinejoin.Round,
        StrokeLinecap = StrokeLinecap.Round,
    });

    static Style AreaStyle => Css.Define(static () => new Style { Fill = SharedStyles.FillColor, FillOpacity = 0.15, Stroke = Stroke.None });

    static Style BarStyle => Css.Define(static () => new Style { Fill = SharedStyles.FillColor, FillOpacity = 0.85 });

    // 一圈表面色把点与线分开
    static Style LastStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Left = LastX,
        Top = LastY,
        Width = CssLength.Em(0.375),
        Height = CssLength.Em(0.375),
        BorderRadius = Radii.Full,
        BackgroundColor = SharedStyles.FillColor,
        BoxShadow = $"0 0 0 {BorderWidths.Sm} {Colors.Surface}",
        Transform = "translate(-50%, -50%)",
        PointerEvents = PointerEvents.None,
    });
}
