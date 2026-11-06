using Rune.Styles;

namespace Rune.UI;

/// <summary>菜单的部件。</summary>
public enum MenuPart
{
    /// <summary>浮层（popover 元素，role="menu"）；子菜单（MenuSub）的浮层也用它。</summary>
    Root,

    /// <summary>菜单项的容器（滚动区域的内容层）。</summary>
    Content,

    /// <summary>分组的标题（MenuGroup、MenuRadioGroup 的 Label）。</summary>
    GroupLabel,
}

/// <summary>菜单项的部件。</summary>
public enum MenuItemPart
{
    /// <summary>菜单项（button，role="menuitem"）。</summary>
    Root,

    /// <summary>图标。</summary>
    Icon,

    /// <summary>文字层：占满剩下的宽度；有说明时文字与说明上下排。</summary>
    Label,

    /// <summary>文字下方的一行说明（弱化）。</summary>
    Description,

    /// <summary>右侧的快捷键。</summary>
    Shortcut,

    /// <summary>复选、单选项起始边的标记（勾选时显示，没勾选时占着位置，各项文字对齐）。</summary>
    Indicator,

    /// <summary>子菜单项右侧的箭头。</summary>
    Chevron,
}

/// <summary>菜单的样式（配方）。</summary>
public static class MenuStyles
{
    /// <summary>菜单（某个位置、某套定制）的部件默认层。</summary>
    public static PartLayers<MenuPart> Of(ComponentStyles styles) =>
        Recipe.Of("menu", styles, static (MenuPart part) => part switch
        {
            MenuPart.Root => Sx.Of(FloatingStyles.Anchored, FloatingStyles.Surface, OverlayTransitions.Scale, FloatingStyles.ScrollPanel, Root),
            MenuPart.Content => Content,
            _ => GroupLabel,
        });

    /// <summary>菜单项（某个色调、某套定制）的部件默认层。</summary>
    public static PartLayers<MenuItemPart> Item(Tone tone, ComponentStyles styles) =>
        Recipe.Of("menu-item", tone, styles, static (Tone tone, MenuItemPart part) => part switch
        {
            // 菜单里通常只区分普通与破坏性操作；其余色调只给文字上色
            MenuItemPart.Root => Sx.Of(
                ItemStyles.Row,
                tone == Tone.Danger ? ItemStyles.Danger : ItemStyles.Hoverable,
                tone is Tone.Danger or Tone.Neutral ? null : SharedStyles.ToneText(tone),
                SharedStyles.FocusRingInset,
                SharedStyles.RippleHost,
                ItemStyles.Disabled),
            MenuItemPart.Icon => ItemIcon,
            MenuItemPart.Label => Label,
            MenuItemPart.Description => Description,
            MenuItemPart.Indicator => Indicator,
            _ => Trailing,
        });

    /// <summary>有说明时文字层的追加样式：文字与说明上下排。</summary>
    public static Style StackedLabel => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column });

    // 菜单项多时在里面的滚动区域里滚动（最大高度由 JS 按视口剩下的空间写在浮层上）
    static Style Root => Css.Define(static () => new Style
    {
        MinWidth = CssLength.Em(12),
        MaxWidth = "calc(100vw - 2em)",
        LineHeight = LineHeights.Normal,
    });

    static Style Content => Css.Define(static () => new Style { Padding = Space.Xs });

    static Style ItemIcon => Css.Define(static () => new Style { FlexShrink = 0 });

    static Style Label => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0 });

    static Style Description => Css.Define(static () => new Style { Display = Display.Block, Color = Colors.TextMuted, FontSize = FontSizes.Sm });

    // 快捷键、子菜单的箭头：贴着结束边（文字层占满了中间）
    static Style Trailing => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        MarginInlineStart = Space.Lg,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });

    // 复选、单选的标记占一列，选没选中都对齐；按菜单项的 aria-checked 显示（JS 改了选中也跟着变）
    static Style Indicator => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Width = CssLength.Em(1),
        Visibility = Visibility.Hidden,
        [When.Parent(When.Attribute("[aria-checked=\"true\"]"))] = new() { Visibility = Visibility.Visible },
    });

    static Style GroupLabel => Css.Define(static () => new Style
    {
        Display = Display.Block,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
        FontWeight = FontWeights.Medium,
    });
}

/// <summary>MenuRadioGroup 交给组内单选项的状态（级联值）。</summary>
/// <param name="Value">选中项的值。</param>
/// <param name="Select">选中某一项。</param>
public sealed record MenuRadioGroupState(string? Value, Func<string?, Task> Select);
