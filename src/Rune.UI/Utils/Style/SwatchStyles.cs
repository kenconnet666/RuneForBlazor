using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 色块共用的样式（同 vue-tsx-pro 的 swatchStyles）：ColorField、之后的 ColorSwatch、主题编辑器里画一块颜色。
/// 棋盘格垫在下面，半透明的颜色看得出来；内描边按正文色的淡色画出色块边界，亮色、暗色主题里都看得见。
/// </summary>
public static class SwatchStyles
{
    /// <summary>棋盘格底：格子大小用 em，随色块所在的字号缩放。</summary>
    public static Style Checkerboard => Css.Define(static () => new Style
    {
        BackgroundImage = $"repeating-conic-gradient({Colors.Fill} 0 25%, {Colors.Surface} 0 50%)",
        BackgroundSize = "0.5em 0.5em",
    });

    /// <summary>能点的色块（打开取色器的按钮、预设色）：悬停时略微放大。描边画在里层，按钮的悬停改不到它，所以放大整块。</summary>
    public static Style Interactive => Css.Define(static () => new Style
    {
        Scale = "1",
        Transition = $"scale {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { Scale = "1.1" },
    });

    /// <summary>色块的内描边。</summary>
    public static Style Ring => Css.Define(static () => new Style
    {
        BoxShadow = $"inset 0 0 0 {BorderWidths.Xs} color-mix(in oklab, {Colors.Text} 14%, transparent)",
    });
}
