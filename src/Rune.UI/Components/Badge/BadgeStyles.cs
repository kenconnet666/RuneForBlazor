using Rune.Styles;

namespace Rune.UI;

/// <summary>标记的样式（与按钮同名的样式同义）。</summary>
public enum BadgeVariant
{
    /// <summary>实底：醒目的状态。</summary>
    Solid,

    /// <summary>浅色底（默认）。</summary>
    Soft,

    /// <summary>浅色底加描边。</summary>
    Surface,

    /// <summary>只有描边。</summary>
    Outline,
}

/// <summary>标记与标签的部件。</summary>
public enum BadgePart
{
    /// <summary>外层（span）。</summary>
    Root,

    /// <summary>文字前的图标。</summary>
    Icon,

    /// <summary>标签的移除按钮。</summary>
    Remove,
}

/// <summary>标记与标签（Badge、Tag）的默认值。</summary>
public sealed record BadgeDefaults : IComponentDefaults<BadgeDefaults>
{
    /// <summary>样式。</summary>
    public BadgeVariant? Variant { get; init; }

    /// <summary>色调。</summary>
    public Tone? Tone { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public BadgeDefaults Over(BadgeDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Tone = Tone ?? outer.Tone,
        Radius = Radius ?? outer.Radius,
    };
}

/// <summary>标记的外观：交给 <see cref="BadgeStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Tone">色调。</param>
/// <param name="Size">字号档位；null 时比上下文字号小一号（0.85em）。</param>
/// <param name="Radius">圆角。</param>
public readonly record struct BadgeAppearance(BadgeVariant Variant, Tone Tone, Size? Size, Radius Radius)
{
    /// <summary>外观：组件上写的 &gt; 配置的默认值（<see cref="BadgeDefaults"/>）&gt; 浅色底、中性、胶囊形。</summary>
    public static BadgeAppearance Resolve(BadgeVariant? variant, Tone? tone, Size? size, Radius? radius, UIConfig config)
    {
        var defaults = config.Defaults.Get<BadgeDefaults>();
        return new BadgeAppearance(
            variant ?? defaults?.Variant ?? BadgeVariant.Soft,
            tone ?? defaults?.Tone ?? UI.Tone.Neutral,
            size,
            radius ?? defaults?.Radius ?? UI.Radius.Full);
    }
}

/// <summary>标记与标签的样式（配方）。</summary>
public static class BadgeStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<BadgePart> Of(BadgeAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("badge", appearance, styles, static (BadgeAppearance a, BadgePart part) => part switch
        {
            BadgePart.Root => Sx.Of(
                Root,
                Variant(a.Variant, a.Tone),
                ScaleStyles.Radius(a.Radius),
                a.Size is { } size ? ScaleStyles.FontSize(size) : RelativeSize),
            BadgePart.Remove => Sx.Of(Remove, SharedStyles.FocusRing),
            _ => Sx.Of(),
        });

    static Style Variant(BadgeVariant variant, Tone tone) => Css.Define((variant, tone), static key =>
    {
        var t = Tones.Of(key.tone);
        return key.variant switch
        {
            BadgeVariant.Solid => new Style { Color = t.Contrast, BackgroundColor = t.Solid },
            BadgeVariant.Surface => new Style { Color = t.Text, BackgroundColor = t.Soft, BorderColor = t.Border },
            BadgeVariant.Outline => new Style { Color = t.Text, BorderColor = t.Border },
            _ => new Style { Color = t.Text, BackgroundColor = t.Soft },
        };
    });

    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = "0.3em",
        PaddingBlock = "0.15em",
        PaddingInline = "0.55em",
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = "transparent",
        FontWeight = FontWeights.Semibold,
        LineHeight = LineHeights.Tight,
        WhiteSpace = WhiteSpace.Nowrap,
        VerticalAlign = VerticalAlign.Middle,
    });

    // 没写字号档位时比旁边的文字小一号
    static Style RelativeSize => Css.Define(static () => new Style { FontSize = "0.85em" });

    // 移除按钮：圆形、跟着文字颜色，悬停时加一层当前颜色的浅底
    static Style Remove => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Width = "1.25em",
        Height = "1.25em",
        MarginInlineEnd = "-0.3em",
        Padding = 0,
        BorderWidth = 0,
        BorderRadius = Radii.Full,
        BackgroundColor = "transparent",
        Color = "inherit",
        Opacity = 0.7,
        Cursor = Cursor.Pointer,
        [When.HoverEnabled] = new() { Opacity = 1, BackgroundColor = "color-mix(in srgb, currentColor 15%, transparent)" },
    });
}
