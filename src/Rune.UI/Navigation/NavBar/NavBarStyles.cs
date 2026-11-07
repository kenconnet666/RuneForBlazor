using Rune.Styles;

namespace Rune.UI;

/// <summary>横向导航的部件。</summary>
public enum NavBarPart
{
    /// <summary>列表（ul）：一行，放不下的项移出排版。</summary>
    List,

    /// <summary>每一项（li）。</summary>
    Item,

    /// <summary>链接（a），或有子项时的菜单按钮；“更多”按钮也用它。</summary>
    Link,
}

/// <summary>一项的外观档位：交给 <see cref="NavBarStyles.Of"/>。</summary>
/// <param name="Current">当前页（或子项里有当前页）。</param>
/// <param name="Disabled">不可用。</param>
public readonly record struct NavBarLook(bool Current = false, bool Disabled = false) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [];
}

/// <summary>横向导航的样式（配方，同 vue-tsx-pro 的 navBarStyles）。</summary>
public static class NavBarStyles
{
    /// <summary>某一项的外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<NavBarPart> Of(NavBarLook look, ComponentStyles styles) =>
        Recipe.Of("nav-bar", look, styles, static (NavBarLook a, NavBarPart part) => part switch
        {
            NavBarPart.List => List,
            NavBarPart.Item => Sx.Of(Item, SharedStyles.OverflowItem),
            _ => Sx.Of(Link, SharedStyles.FocusRing, a.Current ? Current : null, a.Disabled ? Disabled : null),
        });

    // 一行：放不下的项由 js/layout/overflow.js 移出排版（容器要 position: relative，间距用 column-gap）
    static Style List => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Flex,
        AlignItems = AlignItems.Stretch,
        ColumnGap = Space.Xs,
        MinWidth = 0,
        Margin = 0,
        Padding = 0,
        ListStyle = ListStyle.None,
    });

    static Style Item => Css.Define(static () => new Style { Display = Display.Flex, FlexShrink = 0 });

    static Style Link => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        PaddingInline = Space.Md,
        BorderRadius = Radii.Md,
        Color = Colors.TextMuted,
        FontWeight = FontWeights.Medium,
        WhiteSpace = WhiteSpace.Nowrap,
        TextDecoration = TextDecoration.None,
        BackgroundColor = CssColors.Transparent,
        // 当前页的下划线画在底边（与 Tabs 的下划线同色）
        BoxShadow = BoxShadow.None,
        Transition = $"color {Durations.Sm} {Easings.Standard}, background-color {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { Color = Colors.Text, BackgroundColor = Colors.FillHover },
    });

    static Style Current => Css.Define(static () => new Style
    {
        Color = Tones.Accent.Text,
        BoxShadow = $"inset 0 calc(-1 * {BorderWidths.Sm}) 0 {Tones.Accent.Solid}",
        BorderEndStartRadius = 0,
        BorderEndEndRadius = 0,
        [When.Hover] = new() { Color = Tones.Accent.Text },
    });

    static Style Disabled => Css.Define(static () => new Style { Opacity = Opacities.Disabled, PointerEvents = PointerEvents.None });
}
