using Rune.Styles;

namespace Rune.UI;

/// <summary>列表框的部件（同 vue-tsx-pro 的 Listbox）。</summary>
public enum ListboxPart
{
    /// <summary>列表（div，role="listbox"）。</summary>
    Root,

    /// <summary>分组标题（只是视觉上的一行，组名经选项的 aria-describedby 念出）。</summary>
    GroupLabel,

    /// <summary>选项（div，role="option"）。</summary>
    Option,

    /// <summary>选项的文字与说明。</summary>
    Content,

    /// <summary>选项的说明。</summary>
    Description,

    /// <summary>已选标记（对勾）。</summary>
    Check,

    /// <summary>加载中的占位行（交给 Skeleton）。</summary>
    Skeleton,
}

/// <summary>列表框的样式（同 vue-tsx-pro 的 listboxStyles 与 itemStyles）。</summary>
public static class ListboxStyles
{
    /// <summary>各部件的默认层；<paramref name="ownScroll"/> 时列表自己滚动（单独使用的 Listbox，最高 20em）。</summary>
    public static PartLayers<ListboxPart> Of(bool ownScroll, ComponentStyles styles) =>
        Recipe.Of("listbox", ownScroll, styles, static (bool ownScroll, ListboxPart part) => part switch
        {
            // 自己滚动时它就是滚动区域的视口：焦点环画在里面，不被外框裁掉
            ListboxPart.Root => Sx.Of(Root, ownScroll ? Scroll : null, ownScroll ? SharedStyles.FocusRingInset : SharedStyles.FocusRing),
            ListboxPart.GroupLabel => Sx.Of(GroupLabel, SharedStyles.TextColor(TextTone.Muted)),
            ListboxPart.Option => Sx.Of(ItemStyles.Row, ItemStyles.Highlighted, ItemStyles.Selected, ItemStyles.Disabled),
            ListboxPart.Content => Content,
            ListboxPart.Description => Description,
            ListboxPart.Check => Check,
            _ => Skeleton,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Padding = Space.Xs,
        LineHeight = LineHeights.Normal,
        OutlineStyle = OutlineStyle.None,
    });

    // 自己滚动时（列表是 ScrollArea 的视口）：限制高度，滚动不传给页面
    static Style Scroll => Css.Define(static () => new Style
    {
        MaxHeight = CssLength.Em(20),
        OverscrollBehavior = OverscrollBehavior.Contain,
    });

    static Style GroupLabel => Css.Define(static () => new Style
    {
        Display = Display.Block,
        PaddingTop = Space.Sm,
        PaddingBottom = Space.Xs,
        PaddingInline = Space.Sm,
        FontSize = FontSizes.Sm,
        FontWeight = FontWeights.Medium,
    });

    static Style Content => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1,
        MinWidth = 0,
    });

    static Style Description => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted });

    static Style Skeleton => Css.Define(static () => new Style
    {
        Display = Display.Block,
        Height = CssLength.Em(1),
        MarginBlock = CssLength.Em(0.625),
        MarginInline = Space.Sm,
    });

    /// <summary>选项的标记：对勾按它的 aria-selected 显示（不认外面别的选中状态，如选中的表格行）。</summary>
    internal static readonly Marker OptionMarker = Css.Marker("rune-listbox-option");

    // 对勾只在已选的选项里显示，位置照样占着（文字不跳）
    static Style Check => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Visibility = Visibility.Hidden,
        [When.Inside(When.Selected, OptionMarker)] = new() { Visibility = Visibility.Visible },
    });
}
