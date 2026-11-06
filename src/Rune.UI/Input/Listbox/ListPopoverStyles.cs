using Rune.Styles;

namespace Rune.UI;

/// <summary>选项弹出层的部件（同 vue-tsx-pro 的 ListPopover）。</summary>
public enum ListPopoverPart
{
    /// <summary>浮层（popover 元素）：贴着输入框外壳、与它同宽，最高到视口剩下的空间。</summary>
    Root,

    /// <summary>列表上方不随列表滚动的一层（Select 的搜索框）。</summary>
    Header,

    /// <summary>滚动的那一层：选项列表在里面，选项多时按它虚拟滚动。</summary>
    Body,
}

/// <summary>选项弹出层的样式（同 vue-tsx-pro 的 listPopoverStyles 与 floatingStyles.scrollPanel）。</summary>
public static class ListPopoverStyles
{
    /// <summary>某个字号档位（同输入框外壳）、某套定制下各部件的默认层。</summary>
    public static PartLayers<ListPopoverPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("list-popover", size, styles, static (Size? size, ListPopoverPart part) => part switch
        {
            // 浮层在顶层渲染、不是外壳的后代：字号按同一个档位设置
            ListPopoverPart.Root => Sx.Of(
                FloatingStyles.Anchored,
                FloatingStyles.Surface,
                size is { } s ? ScaleStyles.FontSize(s) : null,
                OverlayTransitions.Pop,
                FloatingStyles.ScrollPanel,
                Panel),
            ListPopoverPart.Header => Header,
            _ => FloatingStyles.ScrollArea,
        });

    // 头部固定，下面的滚动区域撑满（FloatingStyles.ScrollPanel）
    static Style Panel => Css.Define(static () => new Style { MaxWidth = "calc(100vw - 2em)" });

    static Style Header => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Padding = Space.Xs,
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.BorderMuted,
    });

}
