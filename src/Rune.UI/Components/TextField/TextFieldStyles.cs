using Rune.Styles;

namespace Rune.UI;

/// <summary>文本框与多行文本框的部件。</summary>
public enum TextFieldPart
{
    /// <summary>外框（div）：边框、聚焦环画在这里。</summary>
    Root,

    /// <summary>输入元素（input / textarea）。</summary>
    Input,

    /// <summary>前面的图标。</summary>
    Icon,

    /// <summary>后面的内容（单位、按钮）。</summary>
    Suffix,
}

/// <summary>文本框的默认值。</summary>
public sealed record TextFieldDefaults : IComponentDefaults<TextFieldDefaults>
{
    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <summary>每次输入都更新值（默认失去焦点或回车时更新）。</summary>
    public bool? Immediate { get; init; }

    /// <inheritdoc/>
    public TextFieldDefaults Over(TextFieldDefaults outer) => new()
    {
        Radius = Radius ?? outer.Radius,
        Immediate = Immediate ?? outer.Immediate,
    };
}

/// <summary>文本框与多行文本框的样式（配方）：外框与输入元素来自 <see cref="ControlStyles"/>。</summary>
public static class TextFieldStyles
{
    /// <summary>某个字号档位、圆角、是否多行、某套定制下各部件的默认层。</summary>
    public static PartLayers<TextFieldPart> Of(Size? size, Radius radius, bool multiline, ComponentStyles styles) =>
        Recipe.Of(multiline ? "textarea" : "text-field", (size, radius, multiline), styles,
            static ((Size? Size, Radius Radius, bool Multiline) a, TextFieldPart part) => part switch
            {
                TextFieldPart.Root => Sx.Of(
                    ControlStyles.Surface,
                    ScaleStyles.Radius(a.Radius),
                    a.Size is { } size ? ScaleStyles.FontSize(size) : null,
                    a.Multiline ? MultilineRoot : null),
                TextFieldPart.Input => Sx.Of(ControlStyles.Input, a.Multiline ? MultilineInput : null),
                _ => ControlStyles.Adornment,
            });

    // 多行：输入元素撑满外框的高度
    static Style MultilineRoot => Css.Define(static () => new Style { AlignItems = AlignItems.Stretch });

    // 多行：可以竖向拖大；支持 field-sizing 的浏览器里高度跟着内容长（不超过 MaxRows 由使用方的 Sx 限制）
    static Style MultilineInput => Css.Define(static () => new Style
    {
        PaddingBlock = "0.5em",
        Resize = "vertical",
        ["field-sizing"] = "content",
        MinHeight = "3lh",
    });
}
