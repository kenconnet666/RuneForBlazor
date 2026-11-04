using Rune.Styles;

namespace Rune.UI;

/// <summary>弹出框的部件。</summary>
public enum PopoverPart
{
    /// <summary>浮层（popover 元素）。</summary>
    Root,

    /// <summary>标题。</summary>
    Title,

    /// <summary>内容。</summary>
    Body,
}

/// <summary>弹出框的样式（配方）；浮层的表面与进出场动效也给菜单用。</summary>
public static class PopoverStyles
{
    /// <summary>某个位置、某套定制下各部件的默认层。</summary>
    public static PartLayers<PopoverPart> Of(Placement placement, ComponentStyles styles) =>
        Recipe.Of("popover", placement, styles, static (Placement placement, PopoverPart part) => part switch
        {
            PopoverPart.Root => Sx.Of(Surface, OverlayTransitions.Scale, Anchor.Position(placement, alignStart: true), Padded),
            PopoverPart.Title => Title,
            _ => Sx.Of(),
        });

    /// <summary>
    /// 浮层的表面：去掉浏览器给 popover 的默认样式（居中、边框、内边距），换成主题的底色、边框、阴影；
    /// 进出场另用 <see cref="OverlayTransitions"/>。还没渲染内容的空壳（第一次打开时内容在路上）先不显示。
    /// </summary>
    public static Style Surface => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Inset = "auto",
        Margin = 0,
        MaxWidth = "min(24em, calc(100vw - 16px))",
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceRaised,
        Color = Colors.Text,
        BoxShadow = Shadows.Md,
        FontSize = FontSizes.Md,
        LineHeight = LineHeights.Normal,
        Overflow = "auto",
        [When.Empty] = new() { Visibility = "hidden" },
    });

    static Style Padded => Css.Define(static () => new Style { Padding = Space.Md });

    static Style Title => Css.Define(static () => new Style
    {
        MarginBlockEnd = Space.Xs,
        FontWeight = FontWeights.Semibold,
    });
}
