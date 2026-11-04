using Rune.Styles;

namespace Rune.UI;

/// <summary>原生下拉选择的部件。</summary>
public enum NativeSelectPart
{
    /// <summary>外框（div）。</summary>
    Root,

    /// <summary>select 元素。</summary>
    Select,

    /// <summary>右侧的箭头图标（不接收点击，点在它上面也是点 select）。</summary>
    Chevron,
}

/// <summary>原生下拉选择的样式（配方）：外框与输入元素来自 <see cref="ControlStyles"/>，去掉 select 的默认箭头换成 Lucide 的。</summary>
public static class NativeSelectStyles
{
    /// <summary>某个字号档位、圆角、某套定制下各部件的默认层。</summary>
    public static PartLayers<NativeSelectPart> Of(Size? size, Radius radius, ComponentStyles styles) =>
        Recipe.Of("native-select", (size, radius), styles, static ((Size? Size, Radius Radius) a, NativeSelectPart part) => part switch
        {
            NativeSelectPart.Root => Sx.Of(ControlStyles.Surface, ScaleStyles.Radius(a.Radius), a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            NativeSelectPart.Select => Sx.Of(ControlStyles.Input, Select),
            _ => Sx.Of(ControlStyles.Adornment, Chevron),
        });

    // 箭头叠在 select 的结束边上：select 留出它的位置，再用负外边距把箭头拉回去
    static Style Select => Css.Define(static () => new Style
    {
        Appearance = "none",
        Cursor = Cursor.Pointer,
        PaddingInlineEnd = "1.5em",
        MarginInlineEnd = "-1.5em",
        TextOverflow = TextOverflow.Ellipsis,
    });

    static Style Chevron => Css.Define(static () => new Style { PointerEvents = "none" });
}
