using System.Globalization;
using Rune.Styles;

namespace Rune.UI;

/// <summary>比例条的一段。</summary>
/// <param name="Key">标识。</param>
/// <param name="Label">名称。</param>
/// <param name="Value">数量。</param>
public sealed record ProportionSegment(string Key, string Label, decimal Value)
{
    /// <summary>色调；不写时按顺序取 Accent、Info、Success、Warning、Danger、Neutral。</summary>
    public Tone? Tone { get; init; }

    /// <summary>任意 CSS 颜色（图表色板）；写了时不看 Tone。</summary>
    public CssColor? Color { get; init; }
}

/// <summary>比例条的样式（同 vue-tsx-pro 的 proportionBarStyles）。</summary>
public static class ProportionBarStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    /// <summary>一段的份额（flex-grow）：经它写在段上（类不随数量变化）。</summary>
    static readonly CssVar<CssValue> Share = CssVar.Value("rune-proportion-share");

    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs BarSm = new() { Sx = Sx.Of(BarStyle, SmStyle) };
    internal static readonly Attrs BarMd = new() { Sx = Sx.Of(BarStyle, MdStyle) };
    internal static readonly Attrs Legend = new() { Sx = LegendStyle };
    internal static readonly Attrs LegendItem = new() { Sx = LegendItemStyle };
    internal static readonly Attrs Value = new() { Sx = ValueStyle };

    /// <summary>一段：份额按数量，颜色按色调或任意颜色。</summary>
    internal static Attrs Segment(Tone tone, CssColor? color, decimal value) => new()
    {
        Sx = Sx.Of(SegmentStyle, color is null ? StatusDotStyles.ToneColor(tone) : StatusDotStyles.Custom),
        Vars = color is { } c
            ? [Share.Set(value.ToString(CultureInfo.InvariantCulture)), StatusDotStyles.CustomColor.Set(c)]
            : [Share.Set(value.ToString(CultureInfo.InvariantCulture))],
    };

    static Style RootStyle => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm, MinWidth = 0 });

    static Style BarStyle => Css.Define(static () => new Style { Display = Display.Flex, Gap = CssLength.Rem(0.125), BorderRadius = Radii.Full, Overflow = Overflow.Hidden });

    static Style SmStyle => Css.Define(static () => new Style { Height = CssLength.Rem(0.5) });

    static Style MdStyle => Css.Define(static () => new Style { Height = CssLength.Rem(0.75) });

    static Style SegmentStyle => Css.Define(static () => new Style { FlexGrow = Share, FlexBasis = 0 });

    static Style LegendStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        JustifyContent = JustifyContent.SpaceBetween,
        ColumnGap = Space.Md,
        RowGap = Space.Xs,
        Margin = 0,
        Padding = 0,
        ListStyleType = "none",
        FontSize = FontSizes.Sm,
    });

    static Style LegendItemStyle => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Sm });

    static Style ValueStyle => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold, FontVariantNumeric = FontVariantNumeric.TabularNums });
}
