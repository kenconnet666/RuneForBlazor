using Rune.Styles;

namespace Rune.UI;

/// <summary>窗口按钮的部件。</summary>
public enum WindowControlsPart
{
    /// <summary>外层（div，role="group"）。</summary>
    Root,

    /// <summary>每个按钮（button）。</summary>
    Button,

    /// <summary>关闭按钮另加的一层（悬停变红）。</summary>
    Close,
}

/// <summary>窗口按钮的样式：同 Windows 11 的标题栏按钮（46px 宽、占满标题栏的高度、悬停变底色、关闭悬停变红）。</summary>
public static class WindowControlsStyles
{
    // Windows 的关闭按钮悬停色：不随主题（系统里所有窗口一样）
    const string CloseHover = "#c42b1c";

    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<WindowControlsPart> Of(ComponentStyles styles) =>
        Recipe.Of("window-controls", styles, static (WindowControlsPart part) => part switch
        {
            WindowControlsPart.Root => Root,
            WindowControlsPart.Button => Sx.Of(SharedStyles.ButtonReset, SharedStyles.FocusRingInset, Button),
            _ => Close,
        });

    // 在标题栏的拖动区域里：按钮本身不可拖（点了是按钮，不是拖窗口）
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignSelf = AlignSelf.Stretch,
        FlexShrink = 0,
        ["app-region"] = "no-drag",
        // 窗口失去焦点：图形变淡（同系统的标题栏）
        [When.Attribute("[data-active=\"false\"]")] = new() { Color = Colors.TextMuted },
    });

    static Style Button => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        // Windows 在 100% 缩放下是 46px：随系统缩放（WebView 的 rem 跟着 DPI），不随页面的字号
        Width = CssLength.Rem(2.875),
        Cursor = Cursor.Default,
        Transition = $"background-color {Durations.Xs} {Easings.Standard}",
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Active] = new() { BackgroundColor = Colors.FillActive },
        [Media.ForcedColors] = new() { Color = CssColors.ButtonText },
    });

    static Style Close => Css.Define(static () => new Style
    {
        [When.Hover] = new() { BackgroundColor = CloseHover, Color = "#fff" },
        [When.Active] = new() { BackgroundColor = $"color-mix(in srgb, {CloseHover} 80%, transparent)", Color = "#fff" },
    });
}
