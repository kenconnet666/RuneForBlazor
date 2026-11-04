using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 列表里的一行（同 vue-tsx-pro 的 itemStyles）：下拉选项、菜单项、导航项、命令面板的结果共用。
/// 键盘高亮由 JS 初始化器写 <c>data-highlighted</c>（aria-activedescendant 指着的那项），与指针悬停同色。
/// </summary>
public static class ItemStyles
{
    /// <summary>一行：图标、文字、尾部内容横排；按钮、链接、div 都能用（去掉了按钮与链接的默认外观）。</summary>
    public static Style Row => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        Width = "100%",
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        Margin = 0,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderWidth = 0,
        BorderRadius = Radii.Sm,
        BackgroundColor = "transparent",
        Color = Colors.Text,
        FontFamily = CssWide.Inherit,
        FontSize = CssWide.Inherit,
        LineHeight = LineHeights.Normal,
        TextAlign = TextAlign.Start,
        TextDecoration = "none",
        Cursor = Cursor.Pointer,
        UserSelect = UserSelect.None,
        Transition = $"background-color {Durations.Sm} {Easings.Standard}, color {Durations.Sm} {Easings.Standard}",
    });

    /// <summary>指针悬停时浅色底。</summary>
    public static Style Hoverable => Css.Define(static () => new Style { [When.Hover] = new() { BackgroundColor = Colors.FillHover } });

    /// <summary>键盘高亮（data-highlighted，aria-activedescendant 指着的选项）：与悬停同色。</summary>
    public static Style Highlighted => Css.Define(static () => new Style { [When.Highlighted] = new() { BackgroundColor = Colors.FillHover } });

    /// <summary>已选（下拉选项，aria-selected）：品牌色文字。</summary>
    public static Style Selected => Css.Define(static () => new Style { [When.Selected] = new() { Color = Tones.Accent.Text } });

    /// <summary>不可用（aria-disabled）：不响应悬停与高亮。</summary>
    public static Style Disabled => Css.Define(static () => new Style
    {
        [When.Disabled] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed, BackgroundColor = "transparent" },
    });

    /// <summary>文字部分：占满剩余宽度，一行放不下时省略。</summary>
    public static Style Label => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Overflow = "hidden",
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });
}
