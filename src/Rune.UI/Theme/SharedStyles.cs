using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 各组件共用的样式片段，和组件自己的样式一起放进 Sx：<c>Part.Of(…, [Trigger, SharedStyles.FocusRingInset], …)</c>。
/// </summary>
public static class SharedStyles
{
    /// <summary>色调的文字色（链接、提示条的图标与标题、文字的 Tone）。</summary>
    public static Style ToneText(Tone tone) => Css.Define(tone, static tone => new Style { Color = Tones.Of(tone).Text });

    /// <summary>文字的色调（文字组件、链接的 Tone）：Muted 是次要文字色，Inherit 跟随所在的文字颜色。</summary>
    public static Style TextColor(TextTone tone) => tone switch
    {
        TextTone.Neutral => ToneText(Tone.Neutral),
        TextTone.Muted => MutedText,
        TextTone.Accent => ToneText(Tone.Accent),
        TextTone.Info => ToneText(Tone.Info),
        TextTone.Success => ToneText(Tone.Success),
        TextTone.Warning => ToneText(Tone.Warning),
        TextTone.Danger => ToneText(Tone.Danger),
        _ => InheritedText,
    };

    static Style MutedText => Css.Define(static () => new Style { Color = Colors.TextMuted });

    static Style InheritedText => Css.Define(static () => new Style { Color = CssWide.Inherit });

    /// <summary>
    /// 状态表面的颜色（同 vue-tsx-pro 的 statusToneSx）：样式 × 色调 → 文字色、底色与边框。
    /// 每种样式都留出边框的位置（实底、浅色底时透明），切换样式时尺寸不变。标记、标签、提示条共用。
    /// </summary>
    public static Style StatusColors(StatusVariant variant, Tone tone) => Css.Define((variant, tone), static key =>
    {
        var t = Tones.Of(key.tone);
        var (color, background, border) = key.variant switch
        {
            StatusVariant.Solid => (t.Contrast, t.Solid, (CssColor)"transparent"),
            StatusVariant.Surface => (t.Text, t.Surface, t.Border),
            StatusVariant.Outline => (t.Text, (CssColor?)null, t.Border),
            _ => (t.Text, t.Soft, (CssColor)"transparent"),
        };
        return new Style
        {
            BorderWidth = BorderWidths.Xs,
            BorderStyle = BorderStyle.Solid,
            BorderColor = border,
            Color = color,
            BackgroundColor = background,
        };
    });

    /// <summary>
    /// 可关闭的元素的淡入淡出（同 vue-tsx-pro 的 presenceStyles）：关闭时 JS 初始化器加 data-dismissing 淡出，
    /// 离场结束再交给使用方移除；没被移除时淡回来。提示条用它。
    /// </summary>
    public static Style Fade => Css.Define(static () => new Style
    {
        Transition = $"opacity {Durations.Sm} {Easings.Enter}",
        [When.Attribute("[data-dismissing]")] = new() { Opacity = 0, Transition = $"opacity {Durations.Sm} {Easings.Exit}" },
    });

    /// <summary>相对所在文字小一档（0.93em）：提示条里的关闭按钮。</summary>
    public static Style RelativeSmall => Css.Define(static () => new Style { FontSize = "0.93em" });

    /// <summary>键盘聚焦时的焦点环（画在元素外侧）。</summary>
    public static Style FocusRing => Css.Define(static () => new Style
    {
        [When.FocusVisible] = new()
        {
            Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}",
            OutlineOffset = 2,
        },
    });

    /// <summary>键盘聚焦时的焦点环（画在元素内侧）：外侧会被裁掉的元素用（面板里的按钮、表格单元格）。</summary>
    public static Style FocusRingInset => Css.Define(static () => new Style
    {
        [When.FocusVisible] = new()
        {
            Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}",
            OutlineOffset = -2,
        },
    });

    /// <summary>
    /// 波纹的宿主（带 data-rune-ripple 的按钮、菜单项）：波纹相对它定位，超出圆角的部分裁掉。
    /// 波纹本身由 JS 初始化器在按下时画（纯客户端，Server 模式也没有往返）。
    /// </summary>
    public static Style RippleHost => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Overflow = "hidden",
    });

    /// <summary>视觉上隐藏、读屏仍能读到（给图标按钮补文字说明）。</summary>
    public static Style VisuallyHidden => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Width = 1,
        Height = 1,
        Padding = 0,
        Margin = -1,
        Overflow = "hidden",
        Clip = "rect(0, 0, 0, 0)",
        WhiteSpace = WhiteSpace.Nowrap,
        BorderWidth = 0,
    });

    /// <summary>
    /// 交互状态的过渡：悬停、按下、选中、聚焦时背景色、边框色、文字色、阴影的变化（按钮、菜单项、输入框外框、勾选框共用，同 vue-tsx-pro）。
    /// 用户要求减少动态效果时时长 token 为 0，这里不用另写。
    /// </summary>
    public static Style InteractionColors => Css.Define(static () => new Style
    {
        Transition = $"background-color {Durations.Sm} {Easings.Standard}, border-color {Durations.Sm} {Easings.Standard}, " +
                     $"color {Durations.Sm} {Easings.Standard}, box-shadow {Durations.Sm} {Easings.Standard}",
    });
}
