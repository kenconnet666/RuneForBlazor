using Rune.Styles;

namespace Rune.UI;

/// <summary>横幅的样式。</summary>
public enum BannerVariant
{
    /// <summary>浅色底（默认）。</summary>
    Soft,

    /// <summary>实底：最醒目，必须先看到的消息。</summary>
    Solid,
}

/// <summary>横幅的部件。</summary>
public enum BannerPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>图标（Icon 组件）。</summary>
    Icon,

    /// <summary>文字区（div）：标题与正文。</summary>
    Content,

    /// <summary>加粗的开头（strong）。</summary>
    Title,

    /// <summary>操作区（div）。</summary>
    Action,

    /// <summary>关闭按钮（CloseButton 组件，保留按钮自己的部件标记，同 vue-tsx-pro）。</summary>
    Close,
}

/// <summary>横幅的样式（同 vue-tsx-pro 的 bannerStyles）：内容居中、一行说完，窄屏时换行；关闭按钮贴在结束边，不参与居中。</summary>
public static class BannerStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<BannerPart> Of(BannerVariant variant, Tone tone, Size? size, bool closable, ComponentStyles styles) =>
        Recipe.Of("banner", (variant, tone, size, closable), styles, static ((BannerVariant Variant, Tone Tone, Size? Size, bool Closable) a, BannerPart part) => part switch
        {
            // 色调排在前面：按部件定制能盖过它
            BannerPart.Root => Sx.Of(
                SharedStyles.StatusBordered,
                SharedStyles.StatusColors(a.Variant == BannerVariant.Solid ? StatusVariant.Solid : StatusVariant.Soft, a.Tone),
                Root,
                a.Closable ? null : Balanced,
                SharedStyles.Fade,
                a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            BannerPart.Icon => Icon,
            BannerPart.Content => Content,
            BannerPart.Title => Title,
            BannerPart.Action => Action,
            _ => Sx.Of(Close, SharedStyles.RelativeSmall),
        });

    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Position = Position.Relative,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Sm,
        RowGap = Space.Xs,
        Width = CssLength.Percent(100),
        PaddingBlock = Space.Sm,
        PaddingInlineStart = Space.Lg,
        // 给贴在结束边的关闭按钮留位置
        PaddingInlineEnd = $"calc({Space.Lg} + 2em)",
        LineHeight = LineHeights.Normal,
        TextAlign = TextAlign.Center,
    });

    // 没有关闭按钮时两边留白一样
    static Style Balanced => Css.Define(static () => new Style { PaddingInlineEnd = Space.Lg });

    static Style Icon => Css.Define(static () => new Style { FlexShrink = 0 });

    static Style Content => Css.Define(static () => new Style { MinWidth = 0 });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold, MarginInlineEnd = Space.Sm });

    static Style Action => Css.Define(static () => new Style { FlexShrink = 0, FontWeight = FontWeights.Medium });

    static Style Close => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInlineEnd = Space.Sm,
        InsetBlockStart = CssLength.Percent(50),
        Translate = "0 -50%",
        Color = CssWide.Inherit,
    });
}
