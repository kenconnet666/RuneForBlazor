using Rune.Styles;

namespace Rune.UI;

/// <summary>日期、时间选择器弹层的部件（DatePicker、DateRangePicker、TimePicker 共用）。</summary>
public enum PickerPart
{
    /// <summary>弹层（role="dialog" 的原生 popover）。</summary>
    Popover,

    /// <summary>日历旁边的一列快捷选项。</summary>
    Presets,

    /// <summary>一个快捷选项按钮。</summary>
    Preset,

    /// <summary>日历（或时间列表）与它下面的页脚。</summary>
    Main,

    /// <summary>页脚：“今天”“此刻”。</summary>
    Footer,
}

/// <summary>选择器弹层的样式（同 vue-tsx-pro 的 datePickerStyles、pickerPresetStyles）。</summary>
public static class PickerStyles
{
    /// <summary>某种字号、某套定制下各部件的默认层：弹层渲染在顶层、不继承输入框的字号，按控件的档位设置。</summary>
    public static PartLayers<PickerPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("picker", size, styles, static (Size? size, PickerPart part) => part switch
        {
            PickerPart.Popover => Sx.Of(FloatingStyles.Anchored, FloatingStyles.Surface, size is { } s ? ScaleStyles.FontSize(s) : null, OverlayTransitions.Pop, Popover),
            PickerPart.Presets => Presets,
            PickerPart.Preset => Preset,
            PickerPart.Main => Main,
            _ => Footer,
        });

    static Style Popover => Css.Define(static () => new Style
    {
        Gap = Space.Sm,
        Padding = Space.Sm,
        MaxWidth = "calc(100vw - 2em)",
        // 只在打开时排版：写在外面会盖过浏览器给关闭的 popover 的 display: none
        [When.PopoverOpen] = new() { Display = Display.Flex },
    });

    // 一列，靠日历那边有分隔线
    static Style Presets => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        PaddingInlineEnd = Space.Sm,
        BorderInlineEndWidth = BorderWidths.Xs,
        BorderInlineEndStyle = BorderStyle.Solid,
        BorderInlineEndColor = Colors.BorderMuted,
    });

    static Style Preset => Css.Define(static () => new Style { JustifyContent = JustifyContent.FlexStart });

    static Style Main => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        PaddingBlockStart = Space.Xs,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
    });
}
