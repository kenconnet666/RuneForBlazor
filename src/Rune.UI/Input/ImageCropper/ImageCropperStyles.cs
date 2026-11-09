using Rune.Styles;

namespace Rune.UI;

/// <summary>裁剪框的形状。</summary>
public enum ImageCropShape
{
    /// <summary>矩形（默认）。</summary>
    Rect,

    /// <summary>圆形（头像）：输出的图四角透明。</summary>
    Round,
}

/// <summary>导出裁好的图的选项：像素大小（只写一边时按宽高比算另一边；都不写时按图片原本的像素）、类型与质量。</summary>
/// <param name="Width">输出的宽（px）。</param>
/// <param name="Height">输出的高（px）。</param>
/// <param name="ContentType">图片类型，默认 image/png。</param>
/// <param name="Quality">质量（image/jpeg、image/webp），0–1。</param>
public sealed record ImageCropOutput(int? Width = null, int? Height = null, string ContentType = "image/png", double? Quality = null);

/// <summary>裁剪图片的部件（同 vue-tsx-pro 的 ImageCropper）。</summary>
public enum ImageCropperPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>区域（role="group"，能聚焦）。</summary>
    Viewport,

    /// <summary>图片（img）。</summary>
    Image,

    /// <summary>裁剪框与四周的遮罩。</summary>
    Frame,

    /// <summary>工具条（缩小、缩放滑块、放大、还原）。</summary>
    Toolbar,
}

/// <summary>裁剪图片的样式（同 vue-tsx-pro 的 imageCropperStyles）：裁剪框的位置与图片的变换由 JS 写。</summary>
public static class ImageCropperStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    internal static readonly CssVar<CssLength> HeightVar = CssVar.Length("rune-cropper-height", "18rem");

    /// <summary>各部件的默认层。</summary>
    public static PartLayers<ImageCropperPart> Of(ComponentStyles styles) =>
        Recipe.Of("image-cropper", styles, static (ImageCropperPart part) => part switch
        {
            ImageCropperPart.Root => Root,
            ImageCropperPart.Viewport => Sx.Of(Viewport, SharedStyles.FocusRing),
            ImageCropperPart.Image => Image,
            ImageCropperPart.Frame => Frame,
            _ => Toolbar,
        });

    internal static readonly Attrs Slider = new() { Sx = Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0 }) };

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm, MinWidth = 0 });

    static Style Viewport => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Height = HeightVar,
        Overflow = Overflow.Hidden,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceMuted,
        Cursor = Cursor.Grab,
        UserSelect = UserSelect.None,
        TouchAction = TouchAction.None,
        [When.Dragging] = new() { Cursor = Cursor.Grabbing },
    });

    static Style Image => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlockStart = 0,
        Left = 0,
        MaxWidth = CssLength.None,
        TransformOrigin = "0 0",
        PointerEvents = PointerEvents.None,
    });

    // 裁剪框：四周用很大的阴影压暗，框本身留一条细边；圆形时（根元素的 data-shape）是椭圆
    static Style Frame => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        BoxShadow = "0 0 0 9999px rgb(0 0 0 / 0.5)",
        OutlineWidth = BorderWidths.Xs,
        OutlineStyle = OutlineStyle.Solid,
        OutlineColor = "rgb(255 255 255 / 0.8)",
        PointerEvents = PointerEvents.None,
        [When.Inside(When.Attribute("[data-shape=round]"), RootMarker)] = new() { BorderRadius = CssLength.Percent(50) },
    });

    static Style Toolbar => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs });

    /// <summary>根元素的标记：裁剪框按它的 data-shape 画圆。</summary>
    internal static readonly Marker RootMarker = Css.Marker("rune-image-cropper");
}
