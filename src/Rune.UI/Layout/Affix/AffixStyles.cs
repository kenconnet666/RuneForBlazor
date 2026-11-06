using Rune.Styles;

namespace Rune.UI;

/// <summary>固钉与回到顶部的样式（同 vue-tsx-pro 的 affixStyles、backTopStyles）。</summary>
public static class AffixStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它
    /// <summary>贴住时离顶部（或底部）的距离：经它写在元素上（类不随距离变化）。</summary>
    internal static readonly CssVar<CssLength> Offset = CssVar.Length("rune-affix-offset", "0px");

    static readonly Condition Stuck = When.Attribute("[data-stuck]");
    static readonly Condition Shown = When.Attribute("[data-shown]");

    // 哨兵：贴住的位置上一个零高度的元素，滚过它就是贴住了
    internal static readonly Attrs Sentinel = new() { Sx = SentinelStyle };

    /// <summary>回到顶部的按钮：固定在视口的结束边与底部，滚过阈值（JS 写 data-shown）才淡入。</summary>
    internal static readonly Attrs BackTop = new() { Sx = BackTopStyle };

    static readonly Attrs[] Roots =
    [
        new() { Sx = Sx.Of(RootStyle, TopStyle) },
        new() { Sx = Sx.Of(RootStyle, TopStyle, ShadowStyle) },
        new() { Sx = Sx.Of(RootStyle, BottomStyle) },
        new() { Sx = Sx.Of(RootStyle, BottomStyle, ShadowStyle) },
    ];

    /// <summary>sticky 的那层：贴顶或贴底，贴住时要不要阴影。</summary>
    internal static Attrs Root(bool bottom, bool shadow) => Roots[(bottom ? 2 : 0) + (shadow ? 1 : 0)];

    static Style RootStyle => Css.Define(static () => new Style
    {
        Position = Position.Sticky,
        ZIndex = 5,
        TransitionProperty = "box-shadow",
        TransitionDuration = Durations.Sm,
        TransitionTimingFunction = Easings.Standard,
    });

    static Style TopStyle => Css.Define(static () => new Style { Top = Offset });

    static Style BottomStyle => Css.Define(static () => new Style { Bottom = Offset });

    static Style ShadowStyle => Css.Define(static () => new Style { [Stuck] = new() { BoxShadow = Shadows.Md } });

    static Style SentinelStyle => Css.Define(static () => new Style { Height = 0, PointerEvents = PointerEvents.None, Visibility = Visibility.Hidden });

    static Style BackTopStyle => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        InsetInlineEnd = Space.Lg,
        InsetBlockEnd = Space.Lg,
        ZIndex = 20,
        BoxShadow = Shadows.Md,
        TransitionProperty = "opacity, translate, visibility",
        TransitionDuration = Durations.Md,
        TransitionTimingFunction = Easings.Standard,
        Opacity = 0,
        Translate = "0 0.5em",
        PointerEvents = PointerEvents.None,
        Visibility = Visibility.Hidden,
        [Shown] = new() { Opacity = 1, Translate = "0", PointerEvents = PointerEvents.Auto, Visibility = Visibility.Visible },
    });
}
