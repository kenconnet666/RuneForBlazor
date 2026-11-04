using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 各组件共用的样式片段，和组件自己的样式一起放进 Sx：<c>Part.Of(…, [Trigger, SharedStyles.FocusRingInset], …)</c>。
/// </summary>
public static class SharedStyles
{
    /// <summary>色调的文字色（链接、提示条的图标与标题、文字的 Tone）。</summary>
    public static Style ToneText(Tone tone) => Css.Define(tone, static tone => new Style { Color = Tones.Of(tone).Text });

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
