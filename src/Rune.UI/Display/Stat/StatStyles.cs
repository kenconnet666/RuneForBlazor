using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 统计数值的样式（同 vue-tsx-pro 的 statStyles）：第一行是图标与指标名、结束边是变化量，第二行是数值，其余各占一整行。
/// 可换行的弹性布局而不是两列网格：卡片窄时变化量换到下一行，指标名不被挤成一字一行；DOM 顺序不变（读屏仍是名称、数值、变化量）。
/// </summary>
public static class StatStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    /// <summary>StatGroup 每个指标的最小宽度：经它写在外层上（类不随宽度变化）。</summary>
    internal static readonly CssVar<CssLength> MinItemWidth = CssVar.Length("rune-stat-min-item-width", "9rem");

    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs GroupedRoot = new() { Sx = Sx.Of(RootStyle, GroupItem) };
    internal static readonly Attrs Label = new() { Sx = LabelStyle };
    internal static readonly Attrs Icon = new() { Sx = IconStyle, ["aria-hidden"] = "true" };
    internal static readonly Attrs Value = new() { Sx = ValueStyle };
    internal static readonly Attrs Unit = new() { Sx = UnitStyle };
    internal static readonly Attrs Change = new() { Sx = ChangeStyle };
    internal static readonly Attrs ChangeLabel = new() { Sx = ChangeLabelStyle };
    internal static readonly Attrs Description = new() { Sx = DescriptionStyle };
    internal static readonly Attrs Body = new() { Sx = BodyStyle };
    internal static readonly Attrs Footer = new() { Sx = FooterStyle };
    internal static readonly Attrs NumberSkeleton = new() { Sx = NumberSkeletonStyle };
    internal static readonly Attrs ChangeSkeleton = new() { Sx = ChangeSkeletonStyle };
    internal static readonly Attrs Reveal = new() { Sx = ShimmerStyles.Reveal };
    internal static readonly Attrs GroupRoot = new() { Sx = GroupRootStyle };

    /// <summary>数字本身：字号大一档、粗体、等宽数字，按色调着色。</summary>
    internal static Attrs Number(TextTone? tone) => tone is { } t ? TonedNumbers[(int)t] : PlainNumber;

    static readonly Attrs PlainNumber = new() { Sx = NumberStyle };
    static readonly Attrs[] TonedNumbers = [.. Enum.GetValues<TextTone>().Select(tone => new Attrs { Sx = Sx.Of(NumberStyle, SharedStyles.TextColor(tone)) })];

    static Style RootStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        ColumnGap = Space.Sm,
        RowGap = Space.Xs,
        Margin = 0,
        MinWidth = 0,
    });

    // dt 里放图标与指标名：dl 的子元素只能是 dt、dd
    static Style LabelStyle => Css.Define(static () => new Style
    {
        Order = 0,
        FlexGrow = 1,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MinWidth = 0,
        Margin = 0,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });

    static Style IconStyle => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexShrink = 0,
        Width = CssLength.Em(1.75),
        Height = CssLength.Em(1.75),
        BorderRadius = Radii.Md,
        BackgroundColor = Tones.Accent.Soft,
        Color = Tones.Accent.Text,
    });

    static Style ValueStyle => Css.Define(static () => new Style
    {
        Order = 2,
        FlexBasis = CssLength.Percent(100),
        Display = Display.Flex,
        AlignItems = AlignItems.Baseline,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Xs,
        Margin = 0,
    });

    static Style NumberStyle => Css.Define(static () => new Style
    {
        FontSize = FontSizes.Xxl,
        FontWeight = FontWeights.Bold,
        LineHeight = LineHeights.Tight,
        Color = Colors.Text,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
    });

    static Style UnitStyle => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = FontSizes.Xs });

    // 变化量在指标名那一行的结束边
    static Style ChangeStyle => Css.Define(static () => new Style
    {
        Order = 1,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        FontSize = FontSizes.Xs,
        Margin = 0,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style ChangeLabelStyle => Css.Define(static () => new Style { Color = Colors.TextMuted });

    static Style DescriptionStyle => Css.Define(static () => new Style
    {
        Order = 3,
        FlexBasis = CssLength.Percent(100),
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        Margin = 0,
    });

    static Style BodyStyle => Css.Define(static () => new Style { Order = 3, FlexBasis = CssLength.Percent(100), Margin = 0, MinWidth = 0 });

    // 底栏：两项时分别靠两端，多了换行
    static Style FooterStyle => Css.Define(static () => new Style
    {
        Order = 3,
        FlexBasis = CssLength.Percent(100),
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Baseline,
        JustifyContent = JustifyContent.SpaceBetween,
        ColumnGap = Space.Md,
        RowGap = Space.Xs,
        Margin = 0,
        MarginBlockStart = Space.Xs,
        PaddingBlockStart = Space.Sm,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
    });

    // 骨架与数值、变化量同高：加载完换成内容时不跳
    static Style NumberSkeletonStyle => Css.Define(static () => new Style { FontSize = FontSizes.Xxl, Width = CssLength.Em(4), Height = CssLength.Em(1.25) });

    static Style ChangeSkeletonStyle => Css.Define(static () => new Style { Width = CssLength.Em(6), Height = CssLength.Em(1.5) });

    // 每个指标的起始边一条竖线；整组往起始方向挪一条线的位置，每行第一个的竖线藏到外面（overflow 裁掉）
    static Style GroupRootStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        RowGap = Space.Md,
        Overflow = Overflow.Hidden,
        GridTemplateColumns = $"repeat(auto-fit, minmax(min({MinItemWidth}, 100%), 1fr))",
    });

    static Style GroupItem => Css.Define(static () => new Style
    {
        PaddingInline = Space.Md,
        MarginInlineStart = $"calc(-1 * {BorderWidths.Xs})",
        BorderInlineStartWidth = BorderWidths.Xs,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderInlineStartColor = Colors.BorderMuted,
    });
}
