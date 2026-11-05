using Rune.Styles;

namespace Rune.UI;

/// <summary>加载遮罩的部件。</summary>
public enum LoadingOverlayPart
{
    /// <summary>外层（div，带 aria-busy）：遮罩按它定位。</summary>
    Root,

    /// <summary>遮罩（div）：盖住内容，中间是加载指示。</summary>
    Overlay,

    /// <summary>加载指示（Spinner 组件）。</summary>
    Spinner,
}

/// <summary>加载遮罩的样式（同 vue-tsx-pro 的 loadingOverlayStyles）。</summary>
public static class LoadingOverlayStyles
{
    /// <summary>是否模糊下面的内容、某套定制下的部件默认层。</summary>
    public static PartLayers<LoadingOverlayPart> Of(bool blur, ComponentStyles styles) =>
        Recipe.Of("loading-overlay", blur, styles, static (bool blur, LoadingOverlayPart part) => part switch
        {
            LoadingOverlayPart.Root => Root,
            LoadingOverlayPart.Overlay => Sx.Of(Overlay, blur ? Blur : null),
            _ => Spinner,
        });

    static Style Root => Css.Define(static () => new Style { Position = Position.Relative });

    // 插入或去掉 hidden 时淡入淡出：display 的 allow-discrete 撑住离场，不需要计时器
    static Style Overlay => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        ZIndex = 1,
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        BorderRadius = CssWide.Inherit,
        BackgroundColor = $"color-mix(in srgb, {Colors.Surface} 70%, transparent)",
        Transition = $"opacity {Durations.Md} {Easings.Enter}, display {Durations.Md} allow-discrete",
        [When.Starting] = new() { Opacity = 0 },
        [When.Attribute("[hidden]")] = new()
        {
            Display = Display.None,
            Opacity = 0,
            Transition = $"opacity {Durations.Sm} {Easings.Exit}, display {Durations.Sm} allow-discrete",
        },
    });

    static Style Blur => Css.Define(static () => new Style { BackdropFilter = "blur(2px)" });

    static Style Spinner => Css.Define(static () => new Style { FontSize = FontSizes.Xl, Color = Tones.Accent.Solid });
}
