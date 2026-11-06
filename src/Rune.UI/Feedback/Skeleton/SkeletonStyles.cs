using Rune.Styles;

namespace Rune.UI;

/// <summary>骨架的常用形状。</summary>
public enum SkeletonShape
{
    /// <summary>一行文字：高 1em，占满宽度。</summary>
    Text,

    /// <summary>标题：高 1.5em，四成宽度。</summary>
    Heading,

    /// <summary>圆：头像，边长 2.5em（与同字号的 Avatar 一样大）。</summary>
    Circle,

    /// <summary>按钮：控件高度，宽 6em。</summary>
    Button,

    /// <summary>图片：占满宽度，16:9。</summary>
    Image,
}

/// <summary>骨架的部件。</summary>
public enum SkeletonPart
{
    /// <summary>占位（span，对读屏隐藏）。</summary>
    Root,

    /// <summary>包着的内容：照常排版、撑出大小，但看不见。</summary>
    Content,
}

/// <summary>骨架的样式（同 vue-tsx-pro 的 skeletonStyles）。</summary>
public static class SkeletonStyles
{
    // 底色与光带都是半透明的正文色：叠在任何表面（页面、卡片、浮层）上都比表面深一档（暗色主题里浅一档），
    // 十六套主题不必各调一遍；光带叠在底色上，再深一档
    static readonly string Tint = $"color-mix(in oklab, {Colors.Text} 9%, transparent)";

    /// <summary>各部件的默认层：<paramref name="shape"/> 是形状，<paramref name="wrapping"/> 表示包着内容（字色透明）。</summary>
    public static PartLayers<SkeletonPart> Of(SkeletonShape? shape, bool wrapping, ComponentStyles styles) =>
        Recipe.Of("skeleton", (shape, wrapping), styles, static ((SkeletonShape? Shape, bool Wrapping) k, SkeletonPart part) => part switch
        {
            SkeletonPart.Root => Sx.Of(Root, ShimmerStyles.Wave, k.Shape is { } shape ? ShapeOf(shape) : null, k.Wrapping ? Wrapping : null),
            _ => Hidden,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineBlock,
        BorderRadius = Radii.Sm,
        [ShimmerStyles.HighlightVar] = Tint,
        BackgroundColor = Tint,
        PointerEvents = PointerEvents.None,
        UserSelect = UserSelect.None,
        // Windows 高对比下系统把背景色改掉，占位块就看不见了：用系统的灰色文字色
        [Media.ForcedColors] = new() { BackgroundColor = CssColors.GrayText, ForcedColorAdjust = ForcedColorAdjust.None },
    });

    // 包着内容时：内容决定大小但看不见
    static Style Wrapping => Css.Define(static () => new Style { Color = CssColors.Transparent });

    static Style Hidden => Css.Define(static () => new Style { Visibility = Visibility.Hidden });

    static Style ShapeOf(SkeletonShape shape) => Css.Define(shape, static shape => shape switch
    {
        SkeletonShape.Text => new Style { Display = Display.Block, Width = CssLength.Percent(100), Height = CssLength.Em(1) },
        SkeletonShape.Heading => new Style { Display = Display.Block, Width = CssLength.Percent(40), Height = CssLength.Em(1.5) },
        SkeletonShape.Circle => new Style { Width = CssLength.Em(2.5), Height = CssLength.Em(2.5), FlexShrink = 0, BorderRadius = Radii.Full },
        SkeletonShape.Button => new Style
        {
            Width = CssLength.Em(6),
            Height = $"max({Sizes.Control}, {Sizes.MinTarget})",
            FlexShrink = 0,
            BorderRadius = Radii.Md,
        },
        _ => new Style { Display = Display.Block, Width = CssLength.Percent(100), AspectRatio = "16 / 9", BorderRadius = Radii.Md },
    });
}
