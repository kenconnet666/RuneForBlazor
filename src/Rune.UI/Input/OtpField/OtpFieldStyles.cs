using Rune.Styles;

namespace Rune.UI;

/// <summary>验证码输入框的部件。</summary>
public enum OtpFieldPart
{
    /// <summary>格子的容器（div）。</summary>
    Root,

    /// <summary>真正的输入框（透明，盖在格子上）。</summary>
    Input,

    /// <summary>一格（div，字画在 ::before 里）。</summary>
    Slot,

    /// <summary>分组之间的短横线。</summary>
    Separator,
}

/// <summary>能输入的字符。</summary>
public enum OtpCharset
{
    /// <summary>只有数字（默认），手机弹出数字键盘。</summary>
    Numeric,

    /// <summary>字母与数字，字母转成大写。</summary>
    Alphanumeric,
}

/// <summary>
/// 验证码输入框的样式（配方，同 vue-tsx-pro 的 otpFieldStyles；格子的外壳来自 <see cref="InputStyles"/>）。格子里的字是 <c>::before</c> 的 <c>content: attr(data-char)</c>：
/// 输入时 JS 按输入框的当前值改 data-char 与当前格的 data-active，不等往返；空的当前格里闪烁的光标是 <c>::after</c>。
/// </summary>
public static class OtpFieldStyles
{
    /// <summary>外层的标记：校验未通过、不可用时格子跟着变。</summary>
    internal static readonly Marker RootMarker = Css.Marker("rune-otp");

    /// <summary>某个字号档位、某套定制下各部件的默认层。</summary>
    public static PartLayers<OtpFieldPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("otp-field", size, styles, static (Size? size, OtpFieldPart part) => part switch
        {
            OtpFieldPart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            OtpFieldPart.Input => Input,
            OtpFieldPart.Slot => Slot,
            _ => Separator,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
    });

    // 真正的输入框盖在格子上面：透明、光标与选中都不显示，点哪一格都是点它
    static Style Input => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Margin = 0,
        Padding = 0,
        BorderWidth = 0,
        OutlineStyle = OutlineStyle.None,
        BackgroundColor = CssColors.Transparent,
        Appearance = Appearance.None,
        Color = CssColors.Transparent,
        CaretColor = CssColors.Transparent,
        FontSize = CssLength.Em(1),
        LetterSpacing = CssLength.Em(-0.5),
        Cursor = Cursor.Text,
        [Pseudo.Selection] = new() { BackgroundColor = CssColors.Transparent },
    });

    // 格子的大小与字：外壳的描边、底色、圆角、悬停与焦点环来自输入框的配方（排在前面，这里盖过它的排版与内边距）
    static Style Slot => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Width = CssLength.Em(2.5),
        Height = $"max(2.75em, {Sizes.MinTarget})",
        PaddingInline = 0,
        FontSize = CssLength.Em(1.15),
        FontWeight = FontWeights.Semibold,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        // 字与光标叠在同一格里
        [Pseudo.Before] = new() { Content = "attr(data-char)", GridArea = "1 / 1" },
        // 校验未通过、不可用：输入框的配方按外壳里的 input 判断，格子里没有 input，按容器上的状态
        [When.Inside(When.Attribute("[data-invalid]"), RootMarker)] = new()
        {
            BorderColor = Tones.Danger.Solid,
            OutlineColor = Tones.Danger.Solid,
            [When.Hover] = new() { BorderColor = Tones.Danger.Solid },
        },
        [When.Inside(When.Attribute("[data-disabled]"), RootMarker)] = new() { Opacity = Opacities.Disabled },
        // 当前格：画出输入框聚焦时的焦点环（焦点在盖着的 input 上，格子自己没有 :focus-within）
        [When.Attribute("[data-active]")] = new() { OutlineStyle = OutlineStyle.Solid },
        // 空的当前格里闪烁的光标；减少动态效果时常亮
        [When.Attribute("""[data-active][data-char=""]""")] = new()
        {
            [Pseudo.After] = new()
            {
                Content = "\"\"",
                GridArea = "1 / 1",
                Width = BorderWidths.Xs,
                Height = CssLength.Em(1.1),
                BackgroundColor = Colors.Text,
                Keyframes = Blink,
                AnimationDuration = "1s",
                AnimationIterationCount = "infinite",
                AnimationTimingFunction = "steps(1)",
            },
        },
        [Media.ReducedMotion] = new() { [Pseudo.After] = new() { AnimationName = "none" } },
    });

    static Style Separator => Css.Define(static () => new Style
    {
        Width = CssLength.Em(0.6),
        Height = BorderWidths.Sm,
        BackgroundColor = Colors.BorderStrong,
    });

    static Keyframes Blink => Css.Define(static () => new Keyframes { ["50%"] = new() { Opacity = 0 } });
}
