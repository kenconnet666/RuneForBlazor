using Rune.Styles;

namespace Rune.UI;

/// <summary>滑块的部件。</summary>
public enum SliderPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>input type="range"。</summary>
    Input,

    /// <summary>显示的当前值（output）。</summary>
    Value,
}

/// <summary>滑块的样式（配方）：原生 range 用 accent-color 上色，高度与控件一致，方便与输入框并排。</summary>
public static class SliderStyles
{
    /// <summary>某个字号档位、某套定制下各部件的默认层。</summary>
    public static PartLayers<SliderPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("slider", size, styles, static (Size? size, SliderPart part) => part switch
        {
            SliderPart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            SliderPart.Input => Sx.Of(Input, SharedStyles.FocusRing),
            _ => Value,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MinHeight = Sizes.Control,
    });

    static Style Input => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = "6em",
        Margin = 0,
        AccentColor = Tones.Accent.Solid,
        Cursor = Cursor.Pointer,
        [When.Disabled] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style Value => Css.Define(static () => new Style
    {
        MinWidth = "3em",
        TextAlign = TextAlign.End,
        FontVariantNumeric = "tabular-nums",
        Color = Colors.TextMuted,
    });
}
