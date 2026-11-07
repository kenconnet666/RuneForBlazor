using Rune.Styles;

namespace Rune.UI;

/// <summary>滚动加载的列表末尾的状态的样式（同 vue-tsx-pro 的 infiniteListStatusStyles）：居中的一行弱化文字，有内容时才占高度。</summary>
public static class InfiniteListStatusStyles
{
    internal static readonly Attrs Root = new() { Sx = RootStyle };

    internal static readonly Attrs RootShown = new() { Sx = Sx.Of(RootStyle, Shown) };

    static Style RootStyle => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Sm,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Sm,
    });

    static Style Shown => Css.Define(static () => new Style { MinHeight = CssLength.Em(2.75), PaddingBlock = Space.Xs, PaddingInline = Space.Md });
}
