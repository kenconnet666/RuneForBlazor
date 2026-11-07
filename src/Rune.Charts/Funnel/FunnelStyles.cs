using Rune.Styles;

namespace Rune.UI;

/// <summary>漏斗的一个环节。</summary>
/// <param name="Label">名称。</param>
/// <param name="Value">数值。</param>
public sealed record FunnelStage(string Label, decimal Value)
{
    /// <summary>颜色；不写时用图表的 Color。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>漏斗图的样式（同 vue-tsx-pro 的 funnelChartStyles）。</summary>
internal static class FunnelStyles
{
    static readonly CssVar<CssLength> BarWidth = CssVar.Length("rune-funnel-bar-width");

    // 名称 | 居中的横条 | 数值：三列由整个列表定宽，各行用 subgrid 对齐
    internal static Style List => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "max-content minmax(0, 1fr) max-content",
        ColumnGap = Space.Md,
    });

    internal static Style Row => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridColumn = "1 / -1",
        GridTemplateColumns = GridTemplateColumns.Subgrid,
        AlignItems = AlignItems.Center,
        PaddingBlock = CssLength.Rem(0.125),
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Sm,
        // 能下钻：悬停有底色
        [When.Attribute("[data-clickable]")] = new() { [When.Hover] = new() { BackgroundColor = Colors.Fill } },
    });

    internal static Style Label => Css.Define(static () => new Style { Color = Colors.Text, TextAlign = TextAlign.End, WhiteSpace = WhiteSpace.Nowrap });

    internal static Style Track => Css.Define(static () => new Style { Display = Display.Flex, JustifyContent = JustifyContent.Center });

    static Style BarStyle => Css.Define(static () => new Style
    {
        Width = BarWidth,
        Height = CssLength.Rem(1.75),
        MinWidth = CssLength.Rem(0.125),
        BorderRadius = Radii.Sm,
        BackgroundColor = ChartStyles.Color,
        Transition = $"width {Durations.Md} {Easings.Standard}",
    });

    /// <summary>一条横条：宽度（百分比）与颜色。</summary>
    internal static Attrs Bar(double percent, CssColor color) =>
        new() { Sx = BarStyle, Vars = [BarWidth.Set(CssLength.Percent(percent)), ChartStyles.Color.Set(color)] };

    internal static Style Value => Css.Define(static () => new Style { FontVariantNumeric = FontVariantNumeric.TabularNums, FontWeight = FontWeights.Medium, WhiteSpace = WhiteSpace.Nowrap });

    // 环节之间：转化率写在中间那一列，小号的次要文字
    internal static Style Rate => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridColumn = "1 / -1",
        GridTemplateColumns = GridTemplateColumns.Subgrid,
        PaddingInline = Space.Xs,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        LineHeight = LineHeights.Tight,
    });

    internal static Style RateText => Css.Define(static () => new Style { GridColumn = "2", TextAlign = TextAlign.Center });

    internal static Style Overall => Css.Define(static () => new Style { GridColumn = "2 / 4", TextAlign = TextAlign.Center, PaddingBlockStart = Space.Xs });

    internal static Style Skeleton => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, Gap = Space.Sm });

    static Style SkeletonBarStyle => Css.Define(static () => new Style { Height = CssLength.Rem(1.75), BorderRadius = Radii.Sm, Width = BarWidth });

    /// <summary>一条骨架横条：宽度（百分比）。</summary>
    internal static Attrs SkeletonBar(int percent) => new() { Sx = SkeletonBarStyle, Vars = [BarWidth.Set(CssLength.Percent(percent))] };
}
