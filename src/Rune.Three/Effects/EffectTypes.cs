using Rune.Styles;

namespace Rune.UI;

/// <summary>线框地形的画法（同 vue-tsx-pro 的 WaveGridTypes）。</summary>
public enum WaveGridType
{
    /// <summary>网格线。</summary>
    Grid,

    /// <summary>只画交点。</summary>
    Dots,
}

/// <summary>彩纸喷出的位置：相对区域的比例，X 从左（0）到右（1）、Y 从上（0）到下（1）。</summary>
/// <param name="X">从左（0）到右（1）。</param>
/// <param name="Y">从上（0）到下（1）。</param>
public readonly record struct ConfettiOrigin(double X, double Y);

/// <summary>彩纸的样式（同 vue-tsx-pro 的 confettiStyles）。</summary>
internal static class ConfettiStyles
{
    /// <summary>铺满最近的定位父元素，指针穿透。</summary>
    internal static readonly Attrs Root = new() { Sx = RootStyle };

    /// <summary>铺满视口、放在顶层（原生 popover，盖得住打开着的对话框）。</summary>
    internal static readonly Attrs Fullscreen = new() { Sx = Sx.Of(RootStyle, FullscreenStyle) };

    static Style RootStyle => Css.Define(static () => new Style { Position = Position.Absolute, Inset = 0, PointerEvents = PointerEvents.None });

    // 原生 popover 的默认样式（居中、边框、底色）改成铺满视口的透明层
    static Style FullscreenStyle => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        Inset = 0,
        Width = CssLength.Vw(100),
        Height = "100vh",
        MaxWidth = CssLength.None,
        MaxHeight = CssLength.None,
        Margin = 0,
        Padding = 0,
        BorderWidth = 0,
        BackgroundColor = CssColors.Transparent,
        Overflow = Overflow.Hidden,
    });
}
