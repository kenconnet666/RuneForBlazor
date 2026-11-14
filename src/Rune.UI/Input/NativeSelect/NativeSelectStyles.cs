using Rune.Styles;

namespace Rune.UI;

/// <summary>原生下拉选择自己的部件；外壳与箭头是输入框的部件（<see cref="InputPart"/>，外观同 Select 的 <see cref="InputAppearance.Picker"/>）。</summary>
public enum NativeSelectPart
{
    /// <summary>select 元素。</summary>
    Select,
}

/// <summary>原生下拉选择的样式：select 去掉默认箭头，换成外壳里 Lucide 的箭头。</summary>
public static class NativeSelectStyles
{
    /// <summary>某套定制下的部件默认层。</summary>
    public static PartLayers<NativeSelectPart> Of(ComponentStyles styles) =>
        Recipe.Of("native-select", styles, static (NativeSelectPart _) => Sx.Of(SharedStyles.TouchInputFont, InputStyles.InputElement, Select));

    /// <summary>箭头叠在 select 上，不接收点击（点在它上面也是点 select）。</summary>
    internal static Style Chevron => Css.Define(static () => new Style { PointerEvents = PointerEvents.None });

    // 箭头叠在 select 的结束边上：select 留出它的位置，再用负外边距把箭头拉回去
    static Style Select => Css.Define(static () => new Style
    {
        Appearance = Appearance.None,
        Cursor = Cursor.Pointer,
        PaddingInlineEnd = CssLength.Em(1.5),
        MarginInlineEnd = CssLength.Em(-1.5),
        TextOverflow = TextOverflow.Ellipsis,
        // 没选时显示的占位项用占位色（浏览器不支持给 select 写 ::placeholder）
        [When.Has("> option[value=\"\"]:checked")] = new() { Color = Colors.TextPlaceholder },
    });
}
