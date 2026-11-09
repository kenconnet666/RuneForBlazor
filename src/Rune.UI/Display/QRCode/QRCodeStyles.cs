using Rune.Styles;

namespace Rune.UI;

/// <summary>二维码的状态。</summary>
public enum QRCodeStatus
{
    /// <summary>可以扫（默认）。</summary>
    Active,

    /// <summary>加载中。</summary>
    Loading,

    /// <summary>已过期（写了 OnRefresh 时可以刷新）。</summary>
    Expired,

    /// <summary>已扫描，等手机上确认。</summary>
    Scanned,
}

/// <summary>二维码的部件（同 vue-tsx-pro 的 QRCode）。</summary>
public enum QRCodePart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>二维码（svg，role="img"）。</summary>
    Code,

    /// <summary>中间的 logo 底板。</summary>
    Logo,

    /// <summary>logo 图片（写了 LogoSrc 时）。</summary>
    LogoImage,

    /// <summary>状态层（role="status"）。</summary>
    Overlay,
}

/// <summary>二维码的样式（同 vue-tsx-pro 的 qrCodeStyles）：颜色是“纸面”（<see cref="SharedStyles.Paper"/>），边长、颜色、logo 大小经变量给。</summary>
public static class QRCodeStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssLength> SizeVar = CssVar.Length("rune-qr-code-size", "10em");
    internal static readonly CssVar<CssLength> LogoSizeVar = CssVar.Length("rune-qr-code-logo-size", "22%");
    internal static readonly CssVar<CssColor> ColorVar = CssVar.Color("rune-qr-code-color");
    internal static readonly CssVar<CssColor> BackgroundVar = CssVar.Color("rune-qr-code-background");

    /// <summary>有没有状态要显示时各部件的默认层。</summary>
    public static PartLayers<QRCodePart> Of(bool overlayShown, ComponentStyles styles) =>
        Recipe.Of("q-r-code", overlayShown, styles, static (bool shown, QRCodePart part) => part switch
        {
            QRCodePart.Root => Sx.Of(SharedStyles.Paper, Root),
            QRCodePart.Code => Code,
            QRCodePart.Logo => Logo,
            QRCodePart.LogoImage => LogoImage,
            _ => Sx.Of(Overlay, shown ? OverlayShown : null),
        });

    // 颜色先取变量（Color、BgColor 改了时写在外层上），没有时是纸面
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineBlock,
        FlexShrink = 0,
        Width = SizeVar,
        AspectRatio = "1",
        BorderRadius = Radii.Md,
        VerticalAlign = VerticalAlign.Middle,
        Color = $"var({ColorVar.Name}, {SharedStyles.PaperInk})",
        BackgroundColor = $"var({BackgroundVar.Name}, {SharedStyles.PaperBackground})",
    });

    static Style Code => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        ShapeRendering = ShapeRendering.CrispEdges,
    });

    static Style Logo => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlockStart = CssLength.Percent(50),
        InsetInlineStart = CssLength.Percent(50),
        Translate = "-50% -50%",
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Width = LogoSizeVar,
        AspectRatio = "1",
        Padding = CssLength.Percent(3),
        // 跟着根元素的底色（BgColor 改了也一样）
        BackgroundColor = CssWide.Inherit,
        BorderRadius = Radii.Sm,
        Overflow = Overflow.Hidden,
    });

    static Style LogoImage => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        ObjectFit = ObjectFit.Contain,
    });

    static Style Overlay => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Xs,
        Padding = Space.Sm,
        TextAlign = TextAlign.Center,
        BorderRadius = CssWide.Inherit,
        PointerEvents = PointerEvents.None,
    });

    static Style OverlayShown => Css.Define(static () => new Style
    {
        PointerEvents = PointerEvents.Auto,
        BackgroundColor = $"color-mix(in srgb, {SharedStyles.PaperBackground} 90%, transparent)",
        BackdropFilter = "blur(2px)",
    });
}
