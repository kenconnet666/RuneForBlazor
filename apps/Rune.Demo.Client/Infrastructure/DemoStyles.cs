using Rune.Styles;
using Rune.UI;

namespace Rune.Demo.Client.Infrastructure;

/// <summary>演示页面自己的样式（不属于组件库）。</summary>
public static class DemoStyles
{
    /// <summary>渲染次数、运行环境这类小字。</summary>
    public static Style RenderInfo => Css.Define(static () => new Style
    {
        Margin = 0,
        FontSize = FontSizes.Xs,
        Color = Colors.TextMuted,
    });

    /// <summary>演示页的标题行：标题 + 渲染模式切换。</summary>
    public static Style PageHeader => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Md,
    });

    /// <summary>渲染模式切换。</summary>
    public static Style ModeSwitch => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        Gap = Space.Xs,
        Padding = Space.Xs,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceMuted,
    });

    /// <summary>渲染模式切换里的一项；当前模式（aria-current）高亮。</summary>
    public static Style ModeLink => Css.Define(static () => new Style
    {
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Sm,
        Color = Colors.Text,
        TextDecoration = TextDecoration.None,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Current] = new() { BackgroundColor = Tones.Accent.Solid, Color = Tones.Accent.Contrast },
    });
}
