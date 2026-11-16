using Rune.Styles;

namespace Rune.UI;

/// <summary>服务端给的一张拼图（同 vue-tsx-pro 的 SlideChallenge）。</summary>
/// <param name="Id">这一张的标识，换图时变；松手后连同结果一起交给服务端。</param>
/// <param name="Background">背景图（带缺口）的地址。</param>
/// <param name="Piece">拼图图片的地址：和背景一样高，左边缘对着缺口的左边缘。</param>
/// <param name="Width">背景图的宽（px）。</param>
/// <param name="Height">背景图的高（px）。</param>
/// <param name="PieceWidth">拼图图片的宽度（px）。</param>
public sealed record SlideChallenge(string Id, string Background, string Piece, int Width, int Height, int PieceWidth);

/// <summary>拖动轨迹上的一点。</summary>
/// <param name="T">距按下的毫秒数。</param>
/// <param name="X">当时的位置（px）。</param>
public sealed record SlidePoint(int T, int X);

/// <summary>松手时交给服务端的内容（同 vue-tsx-pro 的 SlideResult）。</summary>
/// <param name="X">拼图左边缘在背景图里的位置（px，整数）。</param>
/// <param name="Track">拖动轨迹：真人拖动有加减速和抖动，服务端据此区分脚本。</param>
/// <param name="Duration">从按下到松手的毫秒数。</param>
public sealed record SlideResult(int X, IReadOnlyList<SlidePoint> Track, int Duration);

/// <summary>验证码的状态。</summary>
public enum SlideStatus
{
    /// <summary>可以拖。</summary>
    Idle,

    /// <summary>等服务端判定：不能拖。</summary>
    Verifying,

    /// <summary>通过：锁定。</summary>
    Success,

    /// <summary>没对准：滑块晃一下。</summary>
    Error,
}

/// <summary>滑块验证的部件。</summary>
public enum SlideCaptchaPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>画面（背景与拼图）。</summary>
    Picture,

    /// <summary>背景图。</summary>
    Image,

    /// <summary>拼图。</summary>
    Piece,

    /// <summary>“换一张”按钮。</summary>
    Refresh,

    /// <summary>盖在图上的结果条。</summary>
    Banner,

    /// <summary>还没拿到图时的加载中。</summary>
    Loading,

    /// <summary>滑块条。</summary>
    Track,

    /// <summary>滑过的一段。</summary>
    Fill,

    /// <summary>滑块条上的提示。</summary>
    Hint,

    /// <summary>滑块（role="slider"）。</summary>
    Handle,
}

/// <summary>滑块验证的样式（同 vue-tsx-pro 的 slideCaptchaStyles）。</summary>
public static class SlideCaptchaStyles
{
    /// <summary>滑块的宽度，也是滑块条的高度（JS 按它换算位置）。</summary>
    const string HandleSize = "2.75rem";

    /// <summary>松手回到起点时的过渡：带一点回弹。</summary>
    const string Settle = "0.28s cubic-bezier(0.34, 1.4, 0.64, 1)";

    /// <summary>某个状态下各部件的默认层。</summary>
    public static PartLayers<SlideCaptchaPart> Of(SlideStatus status, ComponentStyles styles) =>
        Recipe.Of("slide-captcha", status, styles, static (SlideStatus status, SlideCaptchaPart part) => part switch
        {
            SlideCaptchaPart.Root => Root,
            SlideCaptchaPart.Picture => Picture,
            SlideCaptchaPart.Image => Image,
            SlideCaptchaPart.Piece => Piece,
            SlideCaptchaPart.Refresh => Sx.Of(Refresh, SharedStyles.FocusRingInset),
            SlideCaptchaPart.Banner => Sx.Of(Banner, status == SlideStatus.Success ? BannerSuccess : BannerError),
            SlideCaptchaPart.Loading => Loading,
            SlideCaptchaPart.Track => Track,
            SlideCaptchaPart.Fill => Sx.Of(Fill, status switch { SlideStatus.Success => FillSuccess, SlideStatus.Error => FillError, _ => null }),
            SlideCaptchaPart.Hint => Hint,
            SlideCaptchaPart.Handle => Sx.Of(Handle, SharedStyles.FocusRingInset, status switch
            {
                SlideStatus.Success => HandleSuccess,
                SlideStatus.Error => HandleError,
                _ => null,
            }, status == SlideStatus.Idle ? null : Locked),
            _ => Sx.Of((Style?)null),
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        Width = CssLength.Percent(100),
        UserSelect = UserSelect.None,
    });

    // 宽高比与拼图的宽度（--rune-slide-piece）由组件按图的尺寸写在 style 上
    static Style Picture => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BoxSizing = BoxSizing.BorderBox,
        Width = CssLength.Percent(100),
        Overflow = Overflow.Hidden,
        BorderRadius = Radii.Md,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.Fill,
    });

    static Style Image => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        ObjectFit = ObjectFit.Cover,
        PointerEvents = PointerEvents.None,
        UserSelect = UserSelect.None,
    });

    // 位置（left）由 JS 写；拖动中跟手，不加过渡
    static Style Piece => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = 0,
        Left = 0,
        Width = "var(--rune-slide-piece)",
        Height = CssLength.Percent(100),
        PointerEvents = PointerEvents.None,
        Filter = "drop-shadow(0 2px 6px rgb(0 0 0 / 0.45))",
        Transition = $"left {Settle}",
        [When.Attribute("[data-dragging]")] = new() { Transition = Transition.None },
    });

    static Style Refresh => Css.Define(static () => new Style { Position = Position.Absolute, Top = Space.Xs, InsetInlineEnd = Space.Xs });

    // 盖在图上的结果条：主题的状态色（实色）略透一点，字用它的对比色，在任何背景图上都读得清
    static Style Banner => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInline = 0,
        Bottom = 0,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Md,
        FontSize = FontSizes.Sm,
        TextAlign = TextAlign.Center,
    });

    static Style BannerSuccess => Css.Define(static () => new Style { BackgroundColor = $"color-mix(in srgb, {Tones.Success.Solid} 88%, transparent)", Color = Tones.Success.Contrast });

    static Style BannerError => Css.Define(static () => new Style { BackgroundColor = $"color-mix(in srgb, {Tones.Danger.Solid} 88%, transparent)", Color = Tones.Danger.Contrast });

    static Style Loading => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Sm,
        Height = CssLength.Percent(100),
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });

    static Style Track => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BoxSizing = BoxSizing.BorderBox,
        Height = HandleSize,
        Overflow = Overflow.Hidden,
        BorderRadius = Radii.Md,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.Fill,
    });

    // 滑块按画面的左右走（缺口在图上的位置是物理的），滑过的一段也从左边起；宽度由 JS 写
    static Style Fill => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlock = 0,
        Left = 0,
        Width = HandleSize,
        BackgroundColor = Tones.Accent.Soft,
        Transition = $"left {Settle}, width {Settle}",
        [When.Attribute("[data-dragging]")] = new() { Transition = Transition.None },
    });

    static Style FillSuccess => Css.Define(static () => new Style { BackgroundColor = Tones.Success.Soft });

    static Style FillError => Css.Define(static () => new Style { BackgroundColor = Tones.Danger.Soft });

    static Style Hint => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        PaddingInline = HandleSize,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
        PointerEvents = PointerEvents.None,
        [When.Attribute("[data-hidden]")] = new() { Visibility = Visibility.Hidden },
    });

    static Style Handle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlock = 0,
        Left = 0,
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Width = HandleSize,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
        BoxShadow = Shadows.Sm,
        Color = Tones.Accent.Text,
        Cursor = Cursor.Grab,
        TouchAction = TouchAction.None,
        Transition = $"left {Settle}, width {Settle}",
        [When.Attribute("[data-dragging]")] = new() { Cursor = Cursor.Grabbing, BoxShadow = Shadows.Md, Transition = Transition.None },
    });

    static Style HandleSuccess => Css.Define(static () => new Style { Color = Tones.Success.Text });

    static Style HandleError => Css.Define(static () => new Style { Color = Tones.Danger.Text });

    static Style Locked => Css.Define(static () => new Style { Cursor = Cursor.Default });
}
