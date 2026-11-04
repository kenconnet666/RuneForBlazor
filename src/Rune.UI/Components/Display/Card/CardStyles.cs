using Rune.Styles;

namespace Rune.UI;

/// <summary>卡片的样式。</summary>
public enum CardVariant
{
    /// <summary>表面色加细边框（默认）。</summary>
    Surface,

    /// <summary>只有边框，底色透明。</summary>
    Outline,

    /// <summary>页面底色加阴影，浮在页面上。</summary>
    Elevated,

    /// <summary>没有底色与边框，只保留内边距与分区。</summary>
    Ghost,
}

/// <summary>卡片的部件。</summary>
public enum CardPart
{
    /// <summary>卡片本身（div）。</summary>
    Root,

    /// <summary>页头：与正文之间有分隔线；有 Extra 时和它排成一行、两端对齐。</summary>
    Header,

    /// <summary>正文。</summary>
    Body,

    /// <summary>页脚：操作按钮，靠结束边排列，与正文之间有分隔线。</summary>
    Footer,
}

/// <summary>卡片的默认值。</summary>
public sealed record CardDefaults : IComponentDefaults<CardDefaults>
{
    /// <summary>样式。</summary>
    public CardVariant? Variant { get; init; }

    /// <summary>内边距。</summary>
    public SpaceScale? Padding { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public CardDefaults Over(CardDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Padding = Padding ?? outer.Padding,
        Radius = Radius ?? outer.Radius,
    };
}

/// <summary>卡片的外观：交给 <see cref="CardStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Padding">页头、正文、页脚的内边距。</param>
/// <param name="Radius">圆角。</param>
/// <param name="HasExtra">有页头结束边的内容：页头排成一行、两端对齐。</param>
public readonly record struct CardAppearance(CardVariant Variant, SpaceScale Padding, Radius Radius, bool HasExtra = false)
{
    /// <summary>外观：组件上写的 &gt; 配置的默认值（<see cref="CardDefaults"/>）&gt; 表面色、Lg 内边距、大圆角。</summary>
    public static CardAppearance Resolve(CardVariant? variant, SpaceScale? padding, Radius? radius, bool hasExtra, UIConfig config)
    {
        var defaults = config.Defaults.Get<CardDefaults>();
        return new CardAppearance(
            variant ?? defaults?.Variant ?? CardVariant.Surface,
            padding ?? defaults?.Padding ?? SpaceScale.Lg,
            radius ?? defaults?.Radius ?? UI.Radius.Lg,
            hasExtra);
    }
}

/// <summary>卡片的样式（配方）。</summary>
public static class CardStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<CardPart> Of(CardAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("card", appearance, styles, static (CardAppearance a, CardPart part) => part switch
        {
            CardPart.Root => Sx.Of(Root, Variant(a.Variant), ScaleStyles.Radius(a.Radius)),
            CardPart.Header => Sx.Of(Header, a.HasExtra ? HeaderWithExtra : null, ScaleStyles.Padding(a.Padding)),
            CardPart.Body => ScaleStyles.Padding(a.Padding),
            _ => Sx.Of(Footer, ScaleStyles.Padding(a.Padding)),
        });

    static Style Variant(CardVariant variant) => Css.Define(variant, static variant => variant switch
    {
        CardVariant.Outline => new Style { BorderColor = Colors.Border },
        CardVariant.Elevated => new Style { BackgroundColor = Colors.SurfaceRaised, BoxShadow = Shadows.Md },
        CardVariant.Ghost => new Style(),
        _ => new Style { BackgroundColor = Colors.Surface, BorderColor = Colors.Border },
    });

    // 圆角裁掉子元素（页头的底色、正文里的图片）；边框先透明，样式需要时再上色，各样式的尺寸一致
    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        MinWidth = 0,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = "transparent",
        Color = Colors.Text,
        Overflow = "hidden",
    });

    static Style Header => Css.Define(static () => new Style
    {
        BorderBottomWidth = BorderWidths.Xs,
        BorderBottomStyle = BorderBottomStyle.Solid,
        BorderBottomColor = Colors.BorderMuted,
    });

    // 有 Extra 时：页头内容在起始边、Extra 在结束边，放不下时换行
    static Style HeaderWithExtra => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
    });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Sm,
        BorderTopWidth = BorderWidths.Xs,
        BorderTopStyle = BorderTopStyle.Solid,
        BorderTopColor = Colors.BorderMuted,
    });
}
