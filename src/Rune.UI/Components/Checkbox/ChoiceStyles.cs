using Rune.Styles;

namespace Rune.UI;

/// <summary>勾选框与开关的部件。</summary>
public enum ChoicePart
{
    /// <summary>外层（label）：点文字也能切换。</summary>
    Root,

    /// <summary>控件的盒子：叠放原生 input 与对勾图标。</summary>
    Control,

    /// <summary>原生 input（去掉默认外观后画成方框或轨道）。</summary>
    Input,

    /// <summary>对勾 / 横线图标（勾选框）。</summary>
    Indicator,

    /// <summary>文字。</summary>
    Label,
}

/// <summary>
/// 勾选框（Checkbox）与开关（Switch）的样式（配方）。都是原生 <c>&lt;input type="checkbox"&gt;</c> 去掉默认外观后重画：
/// 键盘、表单提交、读屏都是原生的；状态写在 input 上（:checked、:indeterminate、aria-invalid），样式跟着状态变。
/// </summary>
public static class ChoiceStyles
{
    /// <summary>勾选框的部件默认层。</summary>
    public static PartLayers<ChoicePart> Checkbox(Size? size, ComponentStyles styles) =>
        Recipe.Of("checkbox", size, styles, static (Size? size, ChoicePart part) => part switch
        {
            ChoicePart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            ChoicePart.Control => Control,
            ChoicePart.Input => Sx.Of(Native, Box, SharedStyles.FocusRing, SharedStyles.ReducedMotion),
            ChoicePart.Indicator => Indicator,
            _ => Label,
        });

    /// <summary>开关的部件默认层。</summary>
    public static PartLayers<ChoicePart> Switch(Size? size, ComponentStyles styles) =>
        Recipe.Of("switch", size, styles, static (Size? size, ChoicePart part) => part switch
        {
            ChoicePart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            ChoicePart.Control => Control,
            ChoicePart.Input => Sx.Of(Native, Track, SharedStyles.FocusRing, SharedStyles.ReducedMotion),
            ChoicePart.Indicator => Sx.Of(),
            _ => Label,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.FlexStart,
        Gap = "0.5em",
        LineHeight = LineHeights.Normal,
        Cursor = Cursor.Pointer,
        [When.Has(":disabled")] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    // 盒子与第一行文字居中对齐：行高 1.5em，盒子在其中垂直居中
    static Style Control => Css.Define(static () => new Style
    {
        Display = Display.InlineGrid,
        PlaceItems = "center",
        FlexShrink = 0,
        Height = "1.5em",
    });

    // 共用：去掉原生外观，叠在盒子的同一格里
    static Style Native => Css.Define(static () => new Style
    {
        Appearance = "none",
        GridArea = "1 / 1",
        Margin = 0,
        BorderWidth = 1,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.TextMuted,
        BackgroundColor = Colors.Background,
        Cursor = "inherit",
        Transition = $"background-color {Durations.Fast}, border-color {Durations.Fast}",
        [When.Invalid] = new() { BorderColor = Tones.Danger.Solid },
    });

    static Style Box => Css.Define(static () => new Style
    {
        Width = "1.1em",
        Height = "1.1em",
        BorderRadius = Radii.Sm,
        [When.Checked] = new() { BorderColor = Tones.Accent.Solid, BackgroundColor = Tones.Accent.Solid },
        [When.Indeterminate] = new() { BorderColor = Tones.Accent.Solid, BackgroundColor = Tones.Accent.Solid },
    });

    // 对勾跟在 input 后面：input 勾上（或半选）时显示
    static Style Indicator => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        FontSize = "0.8em",
        Color = Tones.Accent.Contrast,
        PointerEvents = "none",
        Opacity = 0,
        [When.After(When.Checked)] = new() { Opacity = 1 },
        [When.After(When.Indeterminate)] = new() { Opacity = 1 },
    });

    // 开关：轨道是 input 本身，圆点是它的 ::before，勾上时滑到另一端
    static Style Track => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Width = "2em",
        Height = "1.15em",
        BorderRadius = Radii.Full,
        BackgroundColor = Tones.Neutral.Soft,
        [Pseudo.Before] = new()
        {
            Content = "\"\"",
            Position = Position.Absolute,
            InsetBlockStart = "50%",
            InsetInlineStart = "0.1em",
            Width = "0.8em",
            Height = "0.8em",
            BorderRadius = Radii.Full,
            BackgroundColor = Colors.TextMuted,
            Translate = "0 -50%",
            Transition = $"translate {Durations.Fast} {Easings.Standard}, background-color {Durations.Fast}",
        },
        [When.Checked] = new()
        {
            BorderColor = Tones.Accent.Solid,
            BackgroundColor = Tones.Accent.Solid,
            [Pseudo.Before] = new() { BackgroundColor = Tones.Accent.Contrast, Translate = "0.85em -50%" },
        },
    });

    static Style Label => Css.Define(static () => new Style { MinWidth = 0 });
}
