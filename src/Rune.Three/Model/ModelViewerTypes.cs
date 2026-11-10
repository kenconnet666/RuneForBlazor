using Rune.Styles;

namespace Rune.UI;

/// <summary>模型的加载状态（同 vue-tsx-pro 的 ModelViewerStatuses），写在根元素的 data-status 上。</summary>
public enum ModelViewerStatus
{
    /// <summary>加载中。</summary>
    Loading,

    /// <summary>已显示。</summary>
    Loaded,

    /// <summary>加载失败。</summary>
    Error,
}

/// <summary>模型查看器一开始的角度（度）。</summary>
/// <param name="Yaw">绕竖轴转多少度。</param>
/// <param name="Pitch">从上往下看为正。</param>
public readonly record struct ModelViewerOrbit(double Yaw, double Pitch);

/// <summary>
/// 模型查看器的样式（同 vue-tsx-pro 的 modelViewerStyles）：默认正方形；能拖动时显示抓手光标，拖动中（data-dragging，JS 写）是抓住的光标；
/// 加载中、出错的提示盖在画布中间，按根元素的 data-status（JS 写）显示。
/// </summary>
internal static class ModelViewerStyles
{
    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs Interactive = new() { Sx = Sx.Of(RootStyle, InteractiveStyle) };
    internal static readonly Attrs Content = new() { Sx = ContentStyle };
    internal static readonly Attrs Loading = new() { Sx = Sx.Of(Overlay, LoadingOverlay) };
    internal static readonly Attrs Error = new() { Sx = Sx.Of(Overlay, ErrorOverlay) };

    static Style RootStyle => Css.Define(static () => new Style
    {
        AspectRatio = 1,
        Width = CssLength.Percent(100),
        OutlineStyle = OutlineStyle.None,
        [When.FocusVisible] = new() { Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}", OutlineOffset = BorderWidths.Sm },
    });

    static Style InteractiveStyle => Css.Define(static () => new Style
    {
        Cursor = Cursors.Grab,
        TouchAction = TouchAction.PanY,
        [When.Attribute("[data-dragging]")] = new() { Cursor = Cursors.Grabbing },
    });

    // 内容层不定位：提示按外层（整块）铺满、居中
    static Style ContentStyle => Css.Define(static () => new Style { Position = Position.Static });

    static Style Overlay => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.None,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        PointerEvents = PointerEvents.None,
    });

    // 提示在 根元素 > 内容层 > 提示：按根元素的 data-status 显示
    static Style LoadingOverlay => Css.Define(static () => new Style { [new Condition(":is([data-status=\"loading\"] > * > *)")] = new() { Display = Display.Flex } });

    static Style ErrorOverlay => Css.Define(static () => new Style { [new Condition(":is([data-status=\"error\"] > * > *)")] = new() { Display = Display.Flex } });
}
