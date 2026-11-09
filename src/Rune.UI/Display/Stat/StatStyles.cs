using Rune.Styles;

namespace Rune.UI;

/// <summary>统计数值的部件（同 vue-tsx-pro 的 statRecipe）。</summary>
public enum StatPart
{
    /// <summary>dl。</summary>
    Root,

    /// <summary>指标名（dt，图标与名称）。</summary>
    Label,

    /// <summary>图标的外层（span）。</summary>
    Icon,

    /// <summary>数值那一行（dd）。</summary>
    Value,

    /// <summary>数字本身（span 或 AnimatedNumber）：按色调着色。</summary>
    Number,

    /// <summary>单位。</summary>
    Unit,

    /// <summary>变化量（dd）。</summary>
    Change,

    /// <summary>变化量后面的说明（“较上月”）。</summary>
    ChangeLabel,

    /// <summary>说明（dd）。</summary>
    Description,

    /// <summary>补充内容（dd，ChildContent 的外层）。</summary>
    Body,

    /// <summary>底栏（dd）。</summary>
    Footer,
}

/// <summary>
/// 统计数值的样式（配方，同 vue-tsx-pro 的 statRecipe）：第一行是图标与指标名、结束边是变化量，第二行是数值，其余各占一整行。
/// 可换行的弹性布局而不是两列网格：卡片窄时变化量换到下一行，指标名不被挤成一字一行；DOM 顺序不变（读屏仍是名称、数值、变化量）。
/// 数值的色调是档位。
/// </summary>
public static class StatStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    /// <summary>StatGroup 每个指标的最小宽度：经它写在外层上（类不随宽度变化）。</summary>
    internal static readonly CssVar<CssLength> MinItemWidth = CssVar.Length("rune-stat-min-item-width", "9rem");

    internal static readonly Attrs NumberSkeleton = new() { Sx = NumberSkeletonStyle };
    internal static readonly Attrs ChangeSkeleton = new() { Sx = ChangeSkeletonStyle };
    internal static readonly Attrs Reveal = new() { Sx = ShimmerStyles.Reveal };
    internal static readonly Attrs GroupRoot = new() { Sx = GroupRootStyle };

    /// <summary>某个色调、在不在 StatGroup 里时各部件的默认层。</summary>
    public static PartLayers<StatPart> Of(TextTone? tone, bool grouped, ComponentStyles styles) =>
        Recipe.Of("stat", (tone, grouped), styles, static ((TextTone? Tone, bool Grouped) a, StatPart part) => part switch
        {
            StatPart.Root => Sx.Of(RootStyle, a.Grouped ? GroupItem : null),
            StatPart.Label => LabelStyle,
            StatPart.Icon => IconStyle,
            StatPart.Value => ValueStyle,
            StatPart.Number => Sx.Of(NumberStyle, a.Tone is { } t ? SharedStyles.TextColor(t) : null),
            StatPart.Unit => UnitStyle,
            StatPart.Change => ChangeStyle,
            StatPart.ChangeLabel => ChangeLabelStyle,
            StatPart.Description => DescriptionStyle,
            StatPart.Body => BodyStyle,
            _ => FooterStyle,
        });

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

    // 每个指标的起始边一条竖线；整组往起始方向挪一条线的位置，每行第一个的竖线藏到外面（只横向裁：里面链接的焦点环上下不被裁）
    static Style GroupRootStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        RowGap = Space.Md,
        OverflowX = Overflow.Clip,
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
