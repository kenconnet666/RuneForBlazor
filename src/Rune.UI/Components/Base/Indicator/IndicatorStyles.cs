using Rune.Styles;

namespace Rune.UI;

/// <summary>角标的部件。</summary>
public enum IndicatorPart
{
    /// <summary>外层（包住被标记的元素）。</summary>
    Root,

    /// <summary>角标。</summary>
    Badge,

    /// <summary>角标里的计数文字。</summary>
    Count,
}

/// <summary>角标在被标记元素的哪个角（按书写方向）。</summary>
public enum IndicatorPlacement
{
    /// <summary>行尾的上角（默认；从右到左排版时左上）。</summary>
    TopEnd,

    /// <summary>行尾的下角：头像的在线状态。</summary>
    BottomEnd,

    /// <summary>行首的上角。</summary>
    TopStart,

    /// <summary>行首的下角。</summary>
    BottomStart,
}

/// <summary>被标记元素的形状：决定角标往里收多少。</summary>
public enum IndicatorOverlap
{
    /// <summary>方形（默认）：角标中心在角上。</summary>
    Rectangular,

    /// <summary>圆形（圆形头像）：角标收到圆的边上。</summary>
    Circular,
}

/// <summary>角标的外观。</summary>
/// <param name="Dot">只是小圆点。</param>
/// <param name="Placement">位置。</param>
/// <param name="Overlap">被标记元素的形状。</param>
/// <param name="Tone">色调。</param>
public readonly record struct IndicatorAppearance(bool Dot, IndicatorPlacement Placement, IndicatorOverlap Overlap, Tone Tone);

/// <summary>角标的默认值（<see cref="ComponentDefaults"/> 里设置）。</summary>
public sealed record IndicatorDefaults : IComponentDefaults<IndicatorDefaults>
{
    /// <summary>色调。</summary>
    public Tone? Tone { get; init; }

    /// <inheritdoc/>
    public IndicatorDefaults Over(IndicatorDefaults outer) => new() { Tone = Tone ?? outer.Tone };
}

/// <summary>角标的样式（配方，同 vue-tsx-pro 的 indicatorRecipe）。</summary>
public static class IndicatorStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<IndicatorPart> Of(IndicatorAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("indicator", appearance, styles, static (IndicatorAppearance a, IndicatorPart part) => part switch
        {
            IndicatorPart.Root => Root,
            IndicatorPart.Badge => Sx.Of(
                SharedStyles.StatusColors(StatusVariant.Solid, a.Tone),
                Badge,
                a.Dot ? Dot : Counter,
                PlacementOf(a.Placement),
                a.Overlap == IndicatorOverlap.Circular ? Circular : null),
            _ => Sx.Of(),
        });

    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineFlex,
        VerticalAlign = VerticalAlign.Middle,
        FlexShrink = 0,
    });

    static Style Badge => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        ZIndex = 1,
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        BorderRadius = Radii.Full,
        // 一圈页面底色，和被标记的元素分开
        BoxShadow = $"0 0 0 {BorderWidths.Sm} {Colors.Background}",
        FontWeight = FontWeights.Semibold,
        LineHeight = 1,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        WhiteSpace = WhiteSpace.Nowrap,
        PointerEvents = PointerEvents.None,
        // 圆点只靠颜色表示“有新消息”：Windows 高对比下保留色调，不被系统改成背景色
        ForcedColorAdjust = ForcedColorAdjust.None,
    });

    static Style Dot => Css.Define(static () => new Style { Width = CssLength.Em(0.625), Height = CssLength.Em(0.625) });

    static Style Counter => Css.Define(static () => new Style
    {
        MinWidth = CssLength.Em(1.6),
        Height = CssLength.Em(1.6),
        PaddingInline = CssLength.Em(0.4),
        FontSize = CssLength.Em(0.75),
    });

    // 角标的中心对准角：按书写方向的位移
    static Style PlacementOf(IndicatorPlacement placement) => Css.Define(placement, static placement => placement switch
    {
        IndicatorPlacement.BottomEnd => new Style { Bottom = 0, InsetInlineEnd = 0, Translate = "50% 50%", [When.Rtl] = new() { Translate = "-50% 50%" } },
        IndicatorPlacement.TopStart => new Style { Top = 0, InsetInlineStart = 0, Translate = "-50% -50%", [When.Rtl] = new() { Translate = "50% -50%" } },
        IndicatorPlacement.BottomStart => new Style { Bottom = 0, InsetInlineStart = 0, Translate = "-50% 50%", [When.Rtl] = new() { Translate = "50% 50%" } },
        _ => new Style { Top = 0, InsetInlineEnd = 0, Translate = "50% -50%", [When.Rtl] = new() { Translate = "-50% -50%" } },
    });

    // 圆形：角落到圆边的距离约是边长的 14.6%（1 - 1/√2 的一半），角标收进去这么多
    static Style Circular => Css.Define(static () => new Style { Margin = CssLength.Percent(14.6) });
}
