using Rune.Styles;

namespace Rune.UI;

/// <summary>ThreeCanvas 与 EffectSurface 的样式（同 vue-tsx-pro 的 threeCanvasStyles、effectSurfaceStyles）。</summary>
internal static class ThreeStyles
{
    /// <summary>ThreeCanvas 某套定制下各部件的默认层。</summary>
    internal static PartLayers<ThreeCanvasPart> Canvas(ComponentStyles styles) =>
        Recipe.Of("three-canvas", styles, static (ThreeCanvasPart part) => part switch
        {
            ThreeCanvasPart.Root => CanvasRoot,
            // canvas 由 JS 建：铺满的样式写在根元素上（> canvas），这一层只带部件标记与使用方的定制
            ThreeCanvasPart.Canvas => Sx.Of(),
            _ => Fallback,
        });

    /// <summary>EffectSurface 某套定制下各部件的默认层。</summary>
    internal static PartLayers<EffectSurfacePart> Surface(ComponentStyles styles) =>
        Recipe.Of("effect-surface", styles, static (EffectSurfacePart part) => part switch
        {
            EffectSurfacePart.Root => SurfaceRoot,
            EffectSurfacePart.Canvas => SurfaceCanvas,
            _ => SurfaceContent,
        });

    /// <summary>没给降级内容时的默认降级：主题色的柔和渐变。</summary>
    internal static readonly Attrs FallbackGradient = new() { Sx = FallbackGradientStyle };

    static Style CanvasRoot => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Overflow = Overflow.Hidden,
        [new Condition(" > canvas")] = new()
        {
            Position = Position.Absolute,
            Inset = 0,
            Display = Display.Block,
            Width = CssLength.Percent(100),
            Height = CssLength.Percent(100),
        },
    });

    // 只在根元素的 data-state 是 fallback 时显示（状态由 JS 写）
    static Style Fallback => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.None,
        [When.Parent(When.Attribute("[data-state=\"fallback\"]"))] = new() { Display = Display.Block },
    });

    static Style FallbackGradientStyle => Css.Define(static () => new Style
    {
        BackgroundImage = $"radial-gradient(circle at 20% 25%, color-mix(in oklab, {Tones.Accent.Solid} 28%, transparent), transparent 55%), "
            + $"radial-gradient(circle at 80% 70%, color-mix(in oklab, {ChartColors.Series3} 24%, transparent), transparent 55%), "
            + $"radial-gradient(circle at 60% 15%, color-mix(in oklab, {ChartColors.Series5} 18%, transparent), transparent 50%)",
    });

    static Style SurfaceRoot => Css.Define(static () => new Style { Position = Position.Relative, Isolation = Isolation.Isolate, Overflow = Overflow.Hidden });

    static Style SurfaceCanvas => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        ZIndex = -1,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        PointerEvents = PointerEvents.None,
    });

    static Style SurfaceContent => Css.Define(static () => new Style { Position = Position.Relative });
}
