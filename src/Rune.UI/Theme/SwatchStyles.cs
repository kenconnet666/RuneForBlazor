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

    /// <summary>色块的内描边。</summary>
    public static Style Ring => Css.Define(static () => new Style
    {
        BoxShadow = $"inset 0 0 0 {BorderWidths.Xs} color-mix(in oklab, {Colors.Text} 14%, transparent)",
    });
}
