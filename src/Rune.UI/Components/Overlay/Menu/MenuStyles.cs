using Rune.Styles;

namespace Rune.UI;

/// <summary>菜单的部件。</summary>
public enum MenuPart
{
    /// <summary>浮层（popover 元素，role="menu"）。</summary>
    Root,

    /// <summary>分隔线（MenuSeparator）。</summary>
    Separator,
}

/// <summary>菜单项的部件。</summary>
public enum MenuItemPart
{
    /// <summary>菜单项（button，role="menuitem"）。</summary>
    Root,

    /// <summary>图标。</summary>
    Icon,

    /// <summary>右侧的快捷键说明。</summary>
    Shortcut,
}

/// <summary>菜单的样式（配方）。</summary>
public static class MenuStyles
{
    /// <summary>菜单（某个位置、某套定制）的部件默认层。</summary>
    public static PartLayers<MenuPart> Of(ComponentStyles styles) =>
        Recipe.Of("menu", styles, static (MenuPart part) => part switch
        {
            MenuPart.Root => Sx.Of(FloatingStyles.Anchored, FloatingStyles.Surface, OverlayTransitions.Scale, Root),
            _ => Separator,
        });

    /// <summary>菜单项（某个色调、某套定制）的部件默认层。</summary>
    public static PartLayers<MenuItemPart> Item(Tone tone, ComponentStyles styles) =>
        Recipe.Of("menu-item", tone, styles, static (Tone tone, MenuItemPart part) => part switch
        {
            MenuItemPart.Root => Sx.Of(ItemRoot, SharedStyles.RippleHost, SharedStyles.InteractionColors, SharedStyles.ToneText(tone), SharedStyles.FocusRingInset),
            MenuItemPart.Icon => ItemIcon,
            _ => Shortcut,
        });

    // 菜单项多时在菜单里滚动（最大高度由 JS 按视口剩下的空间写）
    static Style Root => Css.Define(static () => new Style
    {
        MinWidth = "12em",
        MaxWidth = "calc(100vw - 2em)",
        Padding = Space.Xs,
        LineHeight = LineHeights.Normal,
        OverflowY = Overflow.Auto,
    });

    // 键盘焦点与悬停用同一种高亮：菜单里焦点就是“当前项”
    static Style ItemRoot => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        Width = "100%",
        MinHeight = Sizes.Control,
        PaddingInline = Space.Sm,
        BorderWidth = 0,
        BorderRadius = Radii.Sm,
        BackgroundColor = "transparent",
        FontFamily = "inherit",
        FontSize = "inherit",
        TextAlign = TextAlign.Start,
        Cursor = Cursor.Pointer,
        [When.HoverEnabled] = new() { BackgroundColor = Colors.FillHover },
        [When.Focus] = new() { BackgroundColor = Colors.FillHover },
        [When.Disabled] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style ItemIcon => Css.Define(static () => new Style { FlexShrink = 0, Opacity = 0.8 });

    static Style Shortcut => Css.Define(static () => new Style
    {
        MarginInlineStart = "auto",
        PaddingInlineStart = Space.Lg,
        Color = Colors.TextMuted,
        FontSize = "0.85em",
    });

    static Style Separator => Css.Define(static () => new Style
    {
        Height = BorderWidths.Xs,
        MarginBlock = Space.Xs,
        MarginInline = $"calc(-1 * {Space.Xs})",
        BackgroundColor = Colors.Border,
    });
}
