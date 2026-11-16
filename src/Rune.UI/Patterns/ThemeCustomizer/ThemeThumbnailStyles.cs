using Rune.Styles;

namespace Rune.UI;

/// <summary>主题缩略图的部件。</summary>
public enum ThemeThumbnailPart
{
    /// <summary>外层 span（页面底色）。</summary>
    Root,

    /// <summary>从右下角探出来的卡片。</summary>
    Card,

    /// <summary>卡片里的标题。</summary>
    Title,

    /// <summary>卡片里的一行正文。</summary>
    Line,

    /// <summary>卡片里的品牌色按钮。</summary>
    Button,
}

/// <summary>主题缩略图的样式（同 vue-tsx-pro 的 themeThumbnailStyles）。</summary>
public static class ThemeThumbnailStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<ThemeThumbnailPart> Of(ComponentStyles styles) =>
        Recipe.Of("theme-thumbnail", styles, static (ThemeThumbnailPart part) => part switch
        {
            ThemeThumbnailPart.Root => Sx.Of(Root, SwatchStyles.Ring),
            ThemeThumbnailPart.Card => Card,
            ThemeThumbnailPart.Title => Title,
            ThemeThumbnailPart.Line => Line,
            _ => Button,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.FlexEnd,
        JustifyContent = JustifyContent.FlexEnd,
        FlexShrink = 0,
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Em(6),
        Height = CssLength.Em(3.75),
        Padding = "0.5em 0 0 0.75em",
        Overflow = Overflow.Hidden,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Background,
    });

    // 卡片从右下角探出来：像页面的一角
    static Style Card => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = CssLength.Em(0.3),
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Padding = CssLength.Em(0.5),
        BorderStartStartRadius = Radii.Sm,
        BorderTopWidth = BorderWidths.Xs,
        BorderInlineStartWidth = BorderWidths.Xs,
        BorderTopStyle = BorderStyle.Solid,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.SurfaceRaised,
    });

    static Style Title => Css.Define(static () => new Style { Width = CssLength.Percent(70), Height = CssLength.Em(0.35), BorderRadius = Radii.Full, BackgroundColor = Colors.Text });

    static Style Line => Css.Define(static () => new Style { Width = CssLength.Percent(90), Height = CssLength.Em(0.25), BorderRadius = Radii.Full, BackgroundColor = Colors.TextMuted });

    static Style Button => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(45),
        Height = CssLength.Em(0.6),
        MarginBlockStart = CssLength.Auto,
        BorderRadius = Radii.Sm,
        BackgroundColor = Tones.Accent.Solid,
    });
}
