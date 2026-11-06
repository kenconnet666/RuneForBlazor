using Rune.Styles;

namespace Rune.UI;

/// <summary>仪表盘外圈的一段区间：从上一段的终点（第一段从 Min）到 To。</summary>
/// <param name="To">终点。</param>
/// <param name="Tone">色调：落在这一段时数值弧也用它。</param>
public sealed record GaugeBand(decimal To, Tone Tone);

/// <summary>
/// 仪表盘的样式（同 vue-tsx-pro 的 gaugeStyles）：宽 10em，随字号缩放。数值弧与外圈区间用明亮的实色——它们只是辅助，
/// 数值以文字给出（中间与读屏），不需要文字的对比度。
/// </summary>
internal static class GaugeStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    static readonly CssVar<CssColor> Color = CssVar.Color("rune-gauge-color");

    internal static Attrs Root(Tone tone) => Roots[(int)tone];

    internal static Attrs Band(Tone tone) => Bands[(int)tone];

    internal static readonly Attrs Dial = new() { Sx = DialStyle };
    internal static readonly Attrs Svg = new() { Sx = SvgStyle };
    internal static readonly Attrs Track = new() { Sx = TrackStyle };
    internal static readonly Attrs Value = new() { Sx = ValueStyle };
    internal static readonly Attrs Target = new() { Sx = TargetStyle };
    internal static readonly Attrs Center = new() { Sx = CenterStyle };
    internal static readonly Attrs Ends = new() { Sx = EndsStyle };
    internal static readonly Attrs Label = new() { Sx = LabelStyle };

    static readonly Attrs[] Roots = [.. Enum.GetValues<Tone>().Select(tone => new Attrs { Sx = Sx.Of(RootStyle, ToneColor(tone)) })];
    static readonly Attrs[] Bands = [.. Enum.GetValues<Tone>().Select(tone => new Attrs { Sx = Sx.Of(BandStyle, BandColor(tone)) })];

    static CssColor SolidOf(Tone tone) => tone == Tone.Neutral ? Colors.BorderStrong : Tones.Of(tone).Solid;

    static Style ToneColor(Tone tone) => Css.Define(tone, static tone => new Style { [Color.Name] = $"{SolidOf(tone)}" });

    static Style BandColor(Tone tone) => Css.Define(tone, static tone => new Style { Stroke = SolidOf(tone) });

    static Style RootStyle => Css.Define(static () => new Style
    {
        Display = Display.InlineGrid,
        JustifyItems = JustifyItems.Center,
        Gap = Space.Xs,
        Width = CssLength.Em(10),
        MaxWidth = CssLength.Percent(100),
    });

    static Style DialStyle => Css.Define(static () => new Style { Position = Position.Relative, Width = CssLength.Percent(100) });

    static Style SvgStyle => Css.Define(static () => new Style { Display = Display.Block, Width = CssLength.Percent(100), Height = CssLength.Auto, Overflow = Overflow.Visible });

    static Style TrackStyle => Css.Define(static () => new Style { Fill = Fill.None, Stroke = Colors.Fill, StrokeWidth = 14 });

    static Style ValueStyle => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Stroke = Color,
        StrokeWidth = 14,
        [Media.ForcedColors] = new() { ForcedColorAdjust = ForcedColorAdjust.None },
    });

    static Style BandStyle => Css.Define(static () => new Style { Fill = Fill.None, StrokeWidth = 4 });

    static Style TargetStyle => Css.Define(static () => new Style { Stroke = Colors.Text, StrokeWidth = 2.5, StrokeLinecap = StrokeLinecap.Round });

    // 中间的数值：压在半圆的底边上
    static Style CenterStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInline = 0,
        Bottom = 0,
        TextAlign = TextAlign.Center,
        FontSize = CssLength.Em(1.5),
        FontWeight = FontWeights.Semibold,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        LineHeight = 1,
        Color = Colors.Text,
    });

    static Style EndsStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.SpaceBetween,
        Width = CssLength.Percent(100),
        FontSize = CssLength.Em(0.75),
        Color = Colors.TextMuted,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
    });

    static Style LabelStyle => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted, TextAlign = TextAlign.Center });
}
