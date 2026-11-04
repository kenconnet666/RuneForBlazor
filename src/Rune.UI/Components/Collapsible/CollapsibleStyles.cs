using Rune.Styles;

namespace Rune.UI;

/// <summary>折叠面板的外观。</summary>
public enum CollapsibleVariant
{
    /// <summary>无边框（默认）。</summary>
    Plain,

    /// <summary>带边框与圆角。</summary>
    Outline,
}

/// <summary>折叠面板的部件：用于部件标记（data-part）与按部件的样式定制（<see cref="ComponentStyles"/>）。</summary>
public enum CollapsiblePart
{
    /// <summary>外层容器。</summary>
    Root,

    /// <summary>展开收起的按钮。</summary>
    Trigger,

    /// <summary>内容区。</summary>
    Content,
}

/// <summary>折叠面板的默认值（<see cref="ComponentDefaults"/> 里设置）。</summary>
public sealed record CollapsibleDefaults : IComponentDefaults<CollapsibleDefaults>
{
    /// <summary>外观。</summary>
    public CollapsibleVariant? Variant { get; init; }

    /// <inheritdoc/>
    public CollapsibleDefaults Over(CollapsibleDefaults outer) => new() { Variant = Variant ?? outer.Variant };
}

/// <summary>折叠面板的样式（配方）：部件 × 档位 × 定制，每种组合只创建一次。</summary>
public static class CollapsibleStyles
{
    /// <summary>某个外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<CollapsiblePart> Of(CollapsibleVariant variant, ComponentStyles styles) =>
        Recipe.Of("collapsible", variant, styles, static (CollapsibleVariant variant, CollapsiblePart part) => part switch
        {
            CollapsiblePart.Root => variant == CollapsibleVariant.Outline ? [Root, OutlineRoot] : Root,
            CollapsiblePart.Trigger => [Trigger, SharedStyles.FocusRingInset, SharedStyles.ReducedMotion],
            _ => Content,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Color = Colors.Text,
    });

    static Style OutlineRoot => Css.Define(static () => new Style
    {
        BorderWidth = 1,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        Overflow = Overflow.Hidden,
    });

    static Style Trigger => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MinHeight = Sizes.MinTarget,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        BorderWidth = 0,
        BackgroundColor = "transparent",
        Color = "inherit",
        Font = "inherit",
        TextAlign = TextAlign.Start,
        Cursor = Cursor.Pointer,
        TransitionProperty = "background-color",
        TransitionDuration = Durations.Fast,
        TransitionTimingFunction = Easings.Standard,
        [When.Hover] = new() { BackgroundColor = Colors.SurfaceHover },
        [When.Expanded] = new() { FontWeight = FontWeights.Semibold },
    });

    static Style Content => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
    });
}
