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

/// <summary>折叠面板各部件的默认层。</summary>
/// <param name="Root">外层容器。</param>
/// <param name="Trigger">展开收起的按钮。</param>
/// <param name="Content">内容区。</param>
public sealed record CollapsibleParts(Attrs Root, Attrs Trigger, Attrs Content);

/// <summary>折叠面板的样式（配方）：部件 × 档位 × 定制，每种组合只创建一次。</summary>
public static class CollapsibleStyles
{
    const string Scope = "collapsible";

    /// <summary>某个外观、某套定制下各部件的默认层。</summary>
    public static CollapsibleParts Of(CollapsibleVariant variant, ComponentStyles styles) =>
        Css.Define((variant, styles), static key => new CollapsibleParts(
            Root: Part.Of(Scope, CollapsiblePart.Root, key.variant == CollapsibleVariant.Outline ? [Root, OutlineRoot] : Root, key.styles),
            Trigger: Part.Of(Scope, CollapsiblePart.Trigger, [Trigger, SharedStyles.FocusRingInset, SharedStyles.ReducedMotion], key.styles),
            Content: Part.Of(Scope, CollapsiblePart.Content, Content, key.styles)));

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
