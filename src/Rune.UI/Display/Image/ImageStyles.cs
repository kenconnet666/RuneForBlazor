using Rune.Styles;

namespace Rune.UI;

/// <summary>一个备选的图片源（picture 里的 source）：浏览器从上到下取第一个符合条件、认得格式的。</summary>
/// <param name="SrcSet">图片地址，可以带宽度、像素比的描述（<c>cover-800.avif 800w, cover-1600.avif 1600w</c>）。</param>
/// <param name="Type">格式：浏览器不认的格式跳过（<c>image/avif</c>、<c>image/webp</c>）。</param>
/// <param name="Media">媒体条件：按屏幕宽度、明暗换一张图（<c>(max-width: 40rem)</c>、<c>(prefers-color-scheme: dark)</c>）。</param>
/// <param name="Sizes">各种宽度下图片显示多宽（配合带 w 描述的 srcset）。</param>
public sealed record ImageSource(string SrcSet, string? Type = null, string? Media = null, string? Sizes = null);

/// <summary>图片在框里怎么放（object-fit）。</summary>
public enum ImageFit
{
    /// <summary>铺满、裁掉多余（默认）：缩略图、头像。</summary>
    Cover,

    /// <summary>整张放进去、留白：商品图、图纸。</summary>
    Contain,

    /// <summary>拉伸铺满。</summary>
    Fill,

    /// <summary>原大，超出的裁掉。</summary>
    None,

    /// <summary>比框大时同 Contain，否则原大。</summary>
    ScaleDown,
}

/// <summary>
/// 图片与图片组的样式（同 vue-tsx-pro 的 imageStyles、imageGroupStyles）。加载状态由 JS 初始化器写在图片的 data-state 上（loaded / error），
/// 不用等往返：加载中图片透明、下面的占位在扫光；加载好了淡入、占位藏起来；失败了图片藏起来，露出失败的占位。
/// </summary>
internal static class ImageStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssLength> WidthVar = CssVar.Length("rune-image-width", "auto");
    internal static readonly CssVar<CssLength> HeightVar = CssVar.Length("rune-image-height", "auto");
    static readonly Condition Loaded = When.Attribute("""[data-state="loaded"]""");
    static readonly Condition Failed = When.Attribute("""[data-state="error"]""");
    static readonly Condition Settled = When.Parent(When.Has("""img[data-state]"""));
    static readonly Condition ImageFailed = When.Parent(When.Has("""img[data-state="error"]"""));

    internal static readonly Attrs Placeholder = new() { Sx = Sx.Of(ShimmerStyles.Wave, PlaceholderStyle), AriaHidden = true };
    internal static readonly Attrs Fallback = new() { Sx = FallbackStyle };
    internal static readonly Attrs Group = new() { Sx = GroupStyle };

    /// <summary>外层：圆角档位，可以预览时去掉按钮外观、指针是放大镜。</summary>
    internal static Attrs Root(Radius radius, bool button) => Roots[(int)radius * 2 + (button ? 1 : 0)];

    /// <summary>图片：object-fit。</summary>
    internal static Attrs Image(ImageFit fit) => Images[(int)fit];

    /// <summary>picture 不参与布局：img 照常按外层的宽高铺满。</summary>
    internal static readonly Attrs Picture = new() { Sx = Css.Define(static () => new Style { Display = Display.Contents }) };

    static readonly Attrs[] Roots = [.. Enum.GetValues<Radius>().SelectMany(radius => new[]
    {
        new Attrs { Sx = Sx.Of(RootStyle, ScaleStyles.Radius(radius)) },
        // 先去掉按钮外观，外层的底色、字色再盖上去
        new Attrs { Sx = Sx.Of(SharedStyles.ButtonReset, RootStyle, ScaleStyles.Radius(radius), ButtonStyle, SharedStyles.FocusRing) },
    })];

    static readonly Attrs[] Images = [.. Enum.GetValues<ImageFit>().Select(fit => new Attrs { Sx = Sx.Of(ImageStyle, FitStyle(fit)) })];

    static Style RootStyle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Overflow = Overflow.Hidden,
        VerticalAlign = VerticalAlign.Middle,
        Width = WidthVar,
        Height = HeightVar,
        BackgroundColor = Colors.Fill,
        Color = Colors.TextMuted,
    });

    // 能看大图的：悬停时略微压暗（同 Ant Design 图片预览的遮罩），配合放大镜指针
    static Style ButtonStyle => Css.Define(static () => new Style
    {
        Cursor = Cursor.ZoomIn,
        Transition = $"filter {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { Filter = "brightness(0.9)" },
    });

    // 加载完之前透明（下面的占位在扫光）：没写尺寸时仍占位，量得出图片大小；加载好了淡入，失败了不占位
    static Style ImageStyle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Block,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Opacity = 0,
        [Loaded] = new() { Opacity = 1, Keyframes = ShimmerStyles.FadeIn, AnimationDuration = Durations.Lg, AnimationTimingFunction = Easings.Enter },
        [Failed] = new() { Display = Display.None },
    });

    static Style FitStyle(ImageFit fit) => Css.Define(fit, static fit => new Style
    {
        ObjectFit = fit switch
        {
            ImageFit.Contain => ObjectFit.Contain,
            ImageFit.Fill => ObjectFit.Fill,
            ImageFit.None => ObjectFit.None,
            ImageFit.ScaleDown => ObjectFit.ScaleDown,
            _ => ObjectFit.Cover,
        },
    });

    // 盖满外层、在图片下面；图片加载好或失败了就不要了
    static Style PlaceholderStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        [Settled] = new() { Display = Display.None },
    });

    static Style FallbackStyle => Css.Define(static () => new Style
    {
        Display = Display.None,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Xs,
        Padding = Space.Sm,
        FontSize = FontSizes.Xs,
        TextAlign = TextAlign.Center,
        [ImageFailed] = new() { Display = Display.Flex },
    });

    static Style GroupStyle => Css.Define(static () => new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap, Gap = Space.Sm });
}
