using Rune.Styles;

namespace Rune.UI;

/// <summary>排列方向。</summary>
public enum Orientation
{
    /// <summary>竖排（默认）。</summary>
    Vertical,

    /// <summary>横排，放不下时换行。</summary>
    Horizontal,
}

/// <summary>勾选框、单选框与开关的部件。</summary>
public enum ChoicePart
{
    /// <summary>外层（label）：点文字也能切换。</summary>
    Root,

    /// <summary>控件的盒子：叠放原生 input 与对勾图标。</summary>
    Control,

    /// <summary>原生 input（去掉默认外观后画成方框或轨道）。</summary>
    Input,

    /// <summary>对勾 / 横线图标（勾选框）、圆点（单选框）。</summary>
    Indicator,

    /// <summary>文字。</summary>
    Label,

    /// <summary>文字下面的说明（Description）。</summary>
    Description,
}

/// <summary>
/// 勾选框（Checkbox）、单选框（Radio）与开关（Switch）的样式（配方）。都是原生 <c>&lt;input type="checkbox"&gt;</c> 去掉默认外观后重画：
/// 键盘、表单提交、读屏都是原生的；状态写在 input 上（:checked、:indeterminate、aria-invalid），样式跟着状态变。
/// </summary>
public static class ChoiceStyles
{
    /// <summary>勾选框的部件默认层（勾上时的颜色按色调）。</summary>
    public static PartLayers<ChoicePart> Checkbox(Size? size, Tone tone, ComponentStyles styles) =>
        Recipe.Of("checkbox", (size, tone), styles, static ((Size? Size, Tone Tone) a, ChoicePart part) => part switch
        {
            ChoicePart.Root => Sx.Of(Root, a.Size is { } s ? ScaleStyles.FontSize(s) : null),
            ChoicePart.Control => Control,
            ChoicePart.Input => Sx.Of(Native, Box, BoxTone(a.Tone), SharedStyles.InteractionColors, SharedStyles.FocusRing),
            ChoicePart.Indicator => Sx.Of(Indicator, IndicatorTone(a.Tone)),
            ChoicePart.Label => Label,
            _ => Description,
        });

    /// <summary>单选框的部件默认层（选中时的颜色按色调）。</summary>
    public static PartLayers<ChoicePart> Radio(Size? size, Tone tone, ComponentStyles styles) =>
        Recipe.Of("radio", (size, tone), styles, static ((Size? Size, Tone Tone) a, ChoicePart part) => part switch
        {
            ChoicePart.Root => Sx.Of(Root, a.Size is { } s ? ScaleStyles.FontSize(s) : null),
            ChoicePart.Control => Control,
            ChoicePart.Input => Sx.Of(Native, Circle, CircleTone(a.Tone), SharedStyles.InteractionColors, SharedStyles.FocusRing),
            ChoicePart.Indicator => Sx.Of(Dot, DotTone(a.Tone)),
            ChoicePart.Label => Label,
            _ => Description,
        });

    /// <summary>单选组的外层（role="radiogroup"）。</summary>
    public static Style Group(Orientation orientation) => Css.Define(orientation, static orientation => new Style
    {
        Display = Display.Flex,
        FlexDirection = orientation == Orientation.Horizontal ? FlexDirection.Row : FlexDirection.Column,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Lg,
        RowGap = Space.Xs,
    });

    /// <summary>开关的部件默认层（打开时的颜色按色调）。</summary>
    public static PartLayers<ChoicePart> Switch(Size? size, Tone tone, ComponentStyles styles) =>
        Recipe.Of("switch", (size, tone), styles, static ((Size? Size, Tone Tone) a, ChoicePart part) => part switch
        {
            ChoicePart.Root => Sx.Of(Root, a.Size is { } s ? ScaleStyles.FontSize(s) : null),
            ChoicePart.Control => Control,
            ChoicePart.Input => Sx.Of(Native, Track, TrackTone(a.Tone), SharedStyles.InteractionColors, SharedStyles.FocusRing),
            ChoicePart.Label => Label,
            _ => Sx.Of(),
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
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.TextMuted,
        BackgroundColor = Colors.Background,
        Cursor = "inherit",
        [When.Invalid] = new() { BorderColor = Tones.Danger.Solid },
    });

    static Style Box => Css.Define(static () => new Style
    {
        Width = "1.1em",
        Height = "1.1em",
        BorderRadius = Radii.Sm,
    });

    static Style BoxTone(Tone tone) => Css.Define(tone, static tone =>
    {
        var t = Tones.Of(tone);
        return new Style
        {
            [When.Checked] = new() { BorderColor = t.Solid, BackgroundColor = t.Solid },
            [When.Indeterminate] = new() { BorderColor = t.Solid, BackgroundColor = t.Solid },
        };
    });

    static Style Circle => Css.Define(static () => new Style
    {
        Width = "1.1em",
        Height = "1.1em",
        BorderRadius = Radii.Full,
    });

    static Style CircleTone(Tone tone) => Css.Define(tone, static tone => new Style { [When.Checked] = new() { BorderColor = Tones.Of(tone).Solid } });

    // 圆点跟在 input 后面：选中时显示
    static Style Dot => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        Width = "0.5em",
        Height = "0.5em",
        BorderRadius = Radii.Full,
        PointerEvents = "none",
        Scale = 0,
        Transition = $"scale {Durations.Sm} {Easings.Standard}",
        [When.After(When.Checked)] = new() { Scale = 1 },
    });

    static Style DotTone(Tone tone) => Css.Define(tone, static tone => new Style { BackgroundColor = Tones.Of(tone).Solid });

    // 对勾跟在 input 后面：input 勾上（或半选）时显示
    static Style Indicator => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        FontSize = "0.8em",
        PointerEvents = "none",
        Opacity = 0,
        [When.After(When.Checked)] = new() { Opacity = 1 },
        [When.After(When.Indeterminate)] = new() { Opacity = 1 },
    });

    static Style IndicatorTone(Tone tone) => Css.Define(tone, static tone => new Style { Color = Tones.Of(tone).Contrast });

    // 开关：轨道是 input 本身，圆点是它的 ::before，勾上时滑到另一端
    static Style Track => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Width = "2em",
        Height = "1.15em",
        BorderRadius = Radii.Full,
        BackgroundColor = Colors.Fill,
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
            Transition = $"translate {Durations.Sm} {Easings.Standard}, background-color {Durations.Sm}",
        },
        [When.Checked] = new() { [Pseudo.Before] = new() { Translate = "0.85em -50%" } },
    });

    static Style TrackTone(Tone tone) => Css.Define(tone, static tone =>
    {
        var t = Tones.Of(tone);
        return new Style
        {
            [When.Checked] = new()
            {
                BorderColor = t.Solid,
                BackgroundColor = t.Solid,
                [Pseudo.Before] = new() { BackgroundColor = t.Contrast },
            },
        };
    });

    static Style Label => Css.Define(static () => new Style { MinWidth = 0 });

    // 说明换到下一行，比文字小一号、次要色
    static Style Description => Css.Define(static () => new Style
    {
        Display = Display.Block,
        FontSize = "0.85em",
        Color = Colors.TextMuted,
    });
}
