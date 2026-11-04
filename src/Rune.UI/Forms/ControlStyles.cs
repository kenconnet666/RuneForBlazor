using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 输入类控件（TextField、Textarea，以及之后的 NumberField、Select）共用的样式：
/// 外框（边框、圆角、底色、悬停、聚焦环、校验未通过、不可用）与里面去掉了默认外观的输入元素。
/// 外框按里面 input 的状态变化（<see cref="When.Has"/>），状态写在 input 上（aria-invalid、disabled），类名不变。
/// </summary>
public static class ControlStyles
{
    /// <summary>外框：包住输入元素与前后的图标、文字；高度跟着字号（<see cref="Sizes.Control"/>）。</summary>
    public static Style Surface => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = "0.5em",
        MinHeight = Sizes.Control,
        PaddingInline = "0.75em",
        BorderWidth = 1,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.Background,
        Color = Colors.Text,
        LineHeight = LineHeights.Normal,
        Transition = $"border-color {Durations.Fast}, box-shadow {Durations.Fast}",
        [When.Hover] = new() { BorderColor = Colors.TextMuted },
        [When.FocusWithin] = new()
        {
            BorderColor = Tones.Accent.Solid,
            BoxShadow = $"0 0 0 3px {Colors.FocusRing}",
        },
        [Invalid] = new()
        {
            BorderColor = Tones.Danger.Solid,
            [When.FocusWithin] = new() { BoxShadow = $"0 0 0 3px color-mix(in srgb, {Tones.Danger.Solid} 35%, transparent)" },
        },
        [Disabled] = new()
        {
            Opacity = Opacities.Disabled,
            Cursor = Cursor.NotAllowed,
            [When.Hover] = new() { BorderColor = Colors.Border },
        },
    });

    /// <summary>外框里的输入元素：去掉自己的边框、底色与聚焦环（由外框画），占满剩余宽度。</summary>
    public static Style Input => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Margin = 0,
        PaddingBlock = "0.4em",
        PaddingInline = 0,
        BorderWidth = 0,
        Outline = "none",
        BackgroundColor = "transparent",
        Color = "inherit",
        FontFamily = "inherit",
        FontSize = "inherit",
        LineHeight = "inherit",
        [Pseudo.Placeholder] = new() { Color = Colors.TextMuted, Opacity = 1 },
        [When.Disabled] = new() { Cursor = Cursor.NotAllowed },
    });

    /// <summary>外框里输入元素前后的图标与文字（单位、清除按钮）：次要色、不被压缩。</summary>
    public static Style Adornment => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Color = Colors.TextMuted,
    });

    // 外框里的输入元素校验未通过 / 不可用
    static readonly Condition Invalid = When.Has("[aria-invalid=\"true\"]");
    static readonly Condition Disabled = When.Has(":disabled");
}
