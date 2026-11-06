using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 图片预览的样式（同 vue-tsx-pro 的 lightboxStyles）：占满视口的深色 dialog，不跟主题变（看图片本身的颜色）。
/// 平移、缩放、旋转由 JS 写在内容与图片的行内样式上；拖动、捏合时（视口上的 data-interacting）内容不要过渡。
/// </summary>
public static class LightboxStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    static readonly Condition Interacting = When.Parent(When.Attribute("[data-interacting]"));
    static readonly Condition ImageFailed = When.Parent(When.Has("""img[data-state="error"]"""));

    internal static readonly Attrs Root = new() { Sx = Sx.Of(RootStyle, OverlayTransitions.Fade, OverlayTransitions.Backdrop) };
    internal static readonly Attrs Toolbar = new() { Sx = ToolbarStyle };
    internal static readonly Attrs Counter = new() { Sx = CounterStyle };
    internal static readonly Attrs Zoom = new() { Sx = ZoomStyle };
    internal static readonly Attrs Viewport = new() { Sx = ViewportStyle };
    internal static readonly Attrs Content = new() { Sx = ContentStyle };
    internal static readonly Attrs Image = new() { Sx = ImageStyle };
    internal static readonly Attrs Fallback = new() { Sx = FallbackStyle };
    internal static readonly Attrs Caption = new() { Sx = CaptionStyle };

    // 深色背景上的按钮：文字白色，悬停时浅一点的底
    internal static readonly Attrs Button = new() { Sx = ButtonStyle };
    internal static readonly Attrs Previous = new() { Sx = Sx.Of(ButtonStyle, SideStyle, PreviousStyle), ["Radius"] = Radius.Full, ["PropsIcon"] = new Attrs { ["FlipInRtl"] = true } };
    internal static readonly Attrs Next = new() { Sx = Sx.Of(ButtonStyle, SideStyle, NextStyle), ["Radius"] = Radius.Full, ["PropsIcon"] = new Attrs { ["FlipInRtl"] = true } };

    // 占满视口：原生 dialog 默认有最大宽高与边框
    static Style RootStyle => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Vw(100),
        Height = CssLength.Dvh(100),
        MaxWidth = CssLength.None,
        MaxHeight = CssLength.None,
        Margin = 0,
        Padding = 0,
        BorderWidth = 0,
        Display = Display.None,
        FlexDirection = FlexDirection.Column,
        BackgroundColor = Colors.ScrimStrong,
        Color = Colors.OnScrim,
        ColorScheme = ColorSchemeKeywords.Dark,
        [When.Modal] = new() { Display = Display.Flex },
    });

    static Style ToolbarStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Xs,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        FlexShrink = 0,
    });

    static Style CounterStyle => Css.Define(static () => new Style
    {
        MarginInlineEnd = CssLength.Auto,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        FontSize = FontSizes.Sm,
        Opacity = 0.8,
    });

    static Style ZoomStyle => Css.Define(static () => new Style
    {
        MinWidth = CssLength.Em(3.5),
        TextAlign = TextAlign.Center,
        FontSize = FontSizes.Sm,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
    });

    // 触屏拖动不能被当成页面滚动
    static Style ViewportStyle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        FlexGrow = 1,
        MinHeight = 0,
        Overflow = Overflow.Hidden,
        TouchAction = TouchAction.None,
        Cursor = Cursor.Grab,
        Outline = OutlineStyle.None,
        [When.Active] = new() { Cursor = Cursor.Grabbing },
    });

    // 平移与缩放作用在它上面（transform-origin 左上角）；它的大小是旋转之后图片占的框
    static Style ContentStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = 0,
        Left = 0,
        TransformOrigin = "0 0",
        TransitionProperty = "transform",
        TransitionDuration = Durations.Md,
        TransitionTimingFunction = Easings.Standard,
        [Interacting] = new() { TransitionProperty = TransitionProperty.None },
    });

    // 以框的中心旋转（大小与旋转由 JS 写）
    static Style ImageStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = CssLength.Percent(50),
        Left = CssLength.Percent(50),
        MaxWidth = CssLength.None,
        UserSelect = UserSelect.None,
        PointerEvents = PointerEvents.None,
    });

    // 大图加载失败：视口正中一个图标与原因
    static Style FallbackStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.None,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Sm,
        Opacity = 0.8,
        [ImageFailed] = new() { Display = Display.Flex },
    });

    static Style SideStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = CssLength.Percent(50),
        Translate = "0 -50%",
        ZIndex = 1,
    });

    static Style PreviousStyle => Css.Define(static () => new Style { InsetInlineStart = Space.Md });

    static Style NextStyle => Css.Define(static () => new Style { InsetInlineEnd = Space.Md });

    static Style CaptionStyle => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        PaddingBlock = Space.Md,
        PaddingInline = Space.Xl,
        TextAlign = TextAlign.Center,
        FontSize = FontSizes.Sm,
        Opacity = 0.9,
    });

    static Style ButtonStyle => Css.Define(static () => new Style
    {
        Color = Colors.OnScrim,
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = $"color-mix(in srgb, {Colors.OnScrim} 12%, transparent)" },
    });
}
