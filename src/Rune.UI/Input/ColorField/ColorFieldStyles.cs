using Rune.Styles;

namespace Rune.UI;

/// <summary>颜色输入的部件（外壳与文字输入框同 TextField，见 <see cref="InputPart"/>）。</summary>
public enum ColorFieldPart
{
    /// <summary>色块（按钮，点开弹出层里的 ColorPicker）。</summary>
    Swatch,

    /// <summary>色块里的颜色（span）。</summary>
    Fill,
}

/// <summary>颜色输入的样式（同 vue-tsx-pro 的 colorFieldStyles）。</summary>
public static class ColorFieldStyles
{
    /// <summary>色块里显示的颜色（写在 Fill 上的变量）。</summary>
    public static readonly CssVar<CssColor> Color = CssVar.Color("rune-color-field-fill");

    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<ColorFieldPart> Of(ComponentStyles styles) =>
        Recipe.Of("color-field", styles, static (ColorFieldPart part) => part switch
        {
            ColorFieldPart.Swatch => Sx.Of(SwatchStyles.Checkerboard, SwatchStyles.Interactive, Swatch, SharedStyles.FocusRing),
            _ => Sx.Of(Fill, SwatchStyles.Ring),
        });

    // 色块：棋盘格垫底，颜色铺在上面一层
    static Style Swatch => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        FlexShrink = 0,
        Width = CssLength.Em(1.25),
        Height = CssLength.Em(1.25),
        BorderRadius = Radii.Sm,
        Overflow = Overflow.Hidden,
        // 能点开取色器时才是手形；不可用、只读时随外壳（not-allowed / 文字光标）
        Cursor = Cursor.Pointer,
        [When.Disabled] = new() { Cursor = CssWide.Inherit },
    });

    static Style Fill => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        BackgroundColor = Color,
    });

    /// <summary>文字输入框上叠加的等宽字体：#hex 各位对齐，改一位时不跳动（输入框的部件仍是 InputPart.Input）。</summary>
    internal static Style MonoInput => Css.Define(static () => new Style { FontFamily = Fonts.Mono, FontVariantLigatures = FontVariantLigatures.None });
}
