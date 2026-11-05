using Rune.Styles;

namespace Rune.UI;

/// <summary>加载条的部件。</summary>
public enum LoadingBarPart
{
    /// <summary>外层（div，role="progressbar"）。</summary>
    Root,

    /// <summary>那条线（div）。</summary>
    Bar,
}

/// <summary>加载条的样式（同 vue-tsx-pro 的 loadingBarStyles）：可见、出错由 JS 写在两个元素上（data-visible、data-state）。</summary>
public static class LoadingBarStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<LoadingBarPart> Of(ComponentStyles styles) =>
        Recipe.Of("loading-bar", styles, static (LoadingBarPart part) => part == LoadingBarPart.Root ? Root : Bar);

    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        InsetInline = 0,
        InsetBlockStart = 0,
        ZIndex = 2000,
        Height = BorderWidths.Md,
        PointerEvents = PointerEvents.None,
        Opacity = 0,
        Transition = $"opacity {Durations.Md} {Easings.Standard}",
        [When.Attribute("[data-visible]")] = new() { Opacity = 1 },
    });

    // 进度由 JS 写成 scaleX；起始边对齐，从右到左排版时从右边长出来
    static Style Bar => Css.Define(static () => new Style
    {
        Height = CssLength.Percent(100),
        BackgroundColor = Tones.Accent.Solid,
        BoxShadow = $"0 0 8px {Tones.Accent.Solid}",
        Transform = "scaleX(0)",
        TransformOrigin = "left center",
        Transition = $"transform {Durations.Md} {Easings.Standard}",
        [When.Rtl] = new() { TransformOrigin = "right center" },
        [Media.ForcedColors] = new() { BackgroundColor = CssColors.Highlight },
        [When.Attribute("[data-state=\"error\"]")] = new() { BackgroundColor = Tones.Danger.Solid, BoxShadow = BoxShadow.None },
    });
}
