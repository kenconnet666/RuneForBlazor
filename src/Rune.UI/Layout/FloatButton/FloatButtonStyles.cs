using Rune.Styles;

namespace Rune.UI;

/// <summary>浮动按钮的样式（同 vue-tsx-pro 的 floatButtonStyles）：固定在视口结束边的底角、阴影、组与速拨展开的那一列。</summary>
public static class FloatButtonStyles
{
    /// <summary>单个按钮（没有角标）：固定在角落、带阴影。</summary>
    internal static readonly Attrs CornerButton = new() { Sx = Sx.Of(Corner, Button) };

    /// <summary>组里的按钮、带角标的按钮（角标那层固定在角落）：只带阴影。</summary>
    internal static readonly Attrs PlainButton = new() { Sx = Button };

    /// <summary>带角标时固定在角落的那层（Indicator）。</summary>
    internal static readonly Attrs CornerBadge = new() { Sx = Corner };

    /// <summary>一组：竖排固定在角落。</summary>
    internal static readonly Attrs Group = new() { Sx = Sx.Of(Corner, GroupStyle) };

    /// <summary>速拨展开的那一列：展开、收起时淡入淡出。</summary>
    internal static readonly Attrs Actions = new() { Sx = ActionsStyle };

    /// <summary>速拨收起时的那一列。</summary>
    internal static readonly Attrs ActionsHidden = new() { Sx = Sx.Of(ActionsStyle, Hidden) };

    // 固定在视口的结束边与底部，留出刘海屏的安全区
    static Style Corner => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        InsetInlineEnd = $"calc({Space.Lg} + env(safe-area-inset-right, 0px))",
        InsetBlockEnd = $"calc({Space.Lg} + env(safe-area-inset-bottom, 0px))",
        ZIndex = 20,
    });

    static Style Button => Css.Define(static () => new Style { BoxShadow = Shadows.Md });

    static Style GroupStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
    });

    static Style ActionsStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        Transition = $"opacity {Durations.Md} {Easings.Standard}, translate {Durations.Md} {Easings.Standard}",
    });

    static Style Hidden => Css.Define(static () => new Style
    {
        Opacity = 0,
        Translate = "0 0.75em",
        PointerEvents = PointerEvents.None,
        Visibility = Visibility.Hidden,
    });
}
