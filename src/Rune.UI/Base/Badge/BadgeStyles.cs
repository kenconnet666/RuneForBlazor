using Rune.Styles;

namespace Rune.UI;

/// <summary>标记还是标签（同一套外观，标签多了过渡与移除）。</summary>
public enum BadgeKind
{
    /// <summary>标记（Badge、StatusBadge）。</summary>
    Badge,

    /// <summary>标签（Tag）。</summary>
    Tag,

    /// <summary>可移除的标签（Tag 给了 OnRemove）。</summary>
    RemovableTag,
}

/// <summary>标记与标签的部件。</summary>
public enum BadgePart
{
    /// <summary>标记本身（span）。</summary>
    Root,

    /// <summary>文字前的图标。</summary>
    Icon,

    /// <summary>文字前的圆点（Dot，没有图标时）。</summary>
    Dot,

    /// <summary>标签（Tag）的移除按钮。</summary>
    Remove,
}

/// <summary>标记与标签（Badge、Tag）的默认值。</summary>
public sealed record BadgeDefaults : IComponentDefaults<BadgeDefaults>
{
    /// <summary>样式。</summary>
    public StatusVariant? Variant { get; init; }

    /// <summary>色调。</summary>
    public Tone? Tone { get; init; }

    /// <summary>字号档位。</summary>
    public Size? Size { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public BadgeDefaults Over(BadgeDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Tone = Tone ?? outer.Tone,
        Size = Size ?? outer.Size,
        Radius = Radius ?? outer.Radius,
    };
}

/// <summary>标记的外观：交给 <see cref="BadgeStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Tone">色调。</param>
/// <param name="Size">字号档位；null 时比上下文字号小一号（0.85em）。</param>
/// <param name="Radius">圆角。</param>
/// <param name="Kind">Badge 还是 Tag（可移除的标签结束边内边距变小、离场时淡出缩小）。</param>
public readonly record struct BadgeAppearance(StatusVariant Variant, Tone Tone, Size Size, Radius Radius, BadgeKind Kind = BadgeKind.Badge) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Variant, Tone, Size, Radius, Kind];

    /// <summary>外观：组件上写的 &gt; 配置的默认值（<see cref="BadgeDefaults"/>）&gt; 浅色底、中性、胶囊形。</summary>
    public static BadgeAppearance Resolve(StatusVariant? variant, Tone? tone, Size? size, Radius? radius, UIConfig config, BadgeKind kind = BadgeKind.Badge)
    {
        var defaults = config.Defaults.Get<BadgeDefaults>();
        return new BadgeAppearance(
            variant ?? defaults?.Variant ?? StatusVariant.Soft,
            tone ?? defaults?.Tone ?? UI.Tone.Neutral,
            size ?? defaults?.Size ?? UI.Size.Xs,
            radius ?? defaults?.Radius ?? UI.Radius.Sm,
            kind);
    }
}

/// <summary>标记与标签的样式（配方）。</summary>
public static class BadgeStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<BadgePart> Of(BadgeAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("badge", appearance, styles, static (BadgeAppearance a, BadgePart part) => part switch
        {
            BadgePart.Root => Sx.Of(Root, SharedStyles.StatusBordered, SharedStyles.StatusColors(a.Variant, a.Tone), ScaleStyles.FontSize(a.Size), ScaleStyles.Radius(a.Radius),
                a.Kind == BadgeKind.Badge ? null : TagRoot,
                a.Kind == BadgeKind.RemovableTag ? Removable : null),
            BadgePart.Dot => Dot,
            BadgePart.Remove => Sx.Of(SharedStyles.StateLayerGhost, Remove),
            _ => Sx.Of(),
        });

    /// <summary>可选中的标签（Tag 的 Selected）：手形指针；颜色随选中切换时过渡。</summary>
    internal static Style Selectable => Css.Define(static () => new Style
    {
        Cursor = Cursor.Pointer,
        Transition = $"background-color {Durations.Sm} {Easings.Standard}, color {Durations.Sm} {Easings.Standard}, border-color {Durations.Sm} {Easings.Standard}",
    });

    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        PaddingInline = Space.Sm,
        // 一行的高度：不随内容的图标、按钮撑高
        MinHeight = CssLength.Em(1.75),
        FontWeight = FontWeights.Medium,
        LineHeight = LineHeights.Tight,
        WhiteSpace = WhiteSpace.Nowrap,
        VerticalAlign = VerticalAlign.Middle,
    });

    // 跟着文字色：浅色底、描边是色调色，实底是反白
    static Style Dot => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Width = CssLength.Em(0.5),
        Height = CssLength.Em(0.5),
        BorderRadius = Radii.Full,
        BackgroundColor = CssColors.CurrentColor,
    });

    // 标签：移除被拒绝（使用方没移除）时从离场状态过渡回来
    static Style TagRoot => Css.Define(static () => new Style
    {
        Transition = $"opacity {Durations.Sm} {Easings.Enter}, scale {Durations.Sm} {Easings.Enter}",
    });

    // 可移除的标签：移除按钮紧贴结束边；离场时淡出缩小（JS 加 data-dismissing）
    static Style Removable => Css.Define(static () => new Style
    {
        PaddingInlineEnd = Space.Xs,
        [When.Attribute("[data-dismissing]")] = new()
        {
            Opacity = 0,
            Scale = "0.8",
            Transition = $"opacity {Durations.Sm} {Easings.Exit}, scale {Durations.Sm} {Easings.Exit}",
        },
    });

    // 移除按钮（CloseButton）比独立的图标按钮紧凑；标签之间留有间距，满足目标尺寸的间距例外。
    // 悬停、按下的底色取标签的文字色（状态层）：彩色标签上不是一块灰
    static Style Remove => Css.Define(static () => new Style
    {
        Width = CssLength.Em(1.5),
        MinHeight = CssLength.Em(1.5),
        MarginBlock = $"calc(-1 * {Space.Xs})",
        Color = CssWide.Inherit,
    });
}
