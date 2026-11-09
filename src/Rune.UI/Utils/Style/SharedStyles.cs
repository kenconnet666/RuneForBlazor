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
    /// 状态表面的边框位置（同 vue-tsx-pro 的 statusToneBorderStyles.bordered）：每种样式都留出边框（实底、浅色底时透明），
    /// 切换样式时尺寸不变。排在 <see cref="StatusColors"/> 之前；标记、角标、提示条用，头像不用（同 vue 的 statusToneSx 与 statusToneStyles）。
    /// </summary>
    public static Style StatusBordered => Css.Define(static () => new Style
    {
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = CssColors.Transparent,
    });

    /// <summary>
    /// 状态表面的颜色（同 vue-tsx-pro 的 statusToneStyles）：样式 × 色调 → 文字色、底色与边框色（只有表面、描边样式有边框色）。
    /// 要边框时前面加 <see cref="StatusBordered"/>。
    /// </summary>
    public static Style StatusColors(StatusVariant variant, Tone tone) => Css.Define((variant, tone), static key =>
    {
        var t = Tones.Of(key.tone);
        var (color, background, border) = key.variant switch
        {
            StatusVariant.Solid => (t.Contrast, t.Solid, null),
            StatusVariant.Surface => (t.Text, t.Surface, t.Border),
            StatusVariant.Outline => (t.Text, null, t.Border),
            _ => ((CssColor, CssColor?, CssColor?))(t.Text, t.Soft, null),
        };
        return new Style
        {
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

    /// <summary>
    /// 状态层（同 vue-tsx-pro 的 stateLayerStyles.ghost，Material 的 state layer）：本身透明的按钮悬停 14%、按下 22% 的文字色作底。
    /// 用在有底色的容器（提示条、横幅、彩色标签）里的关闭、移除按钮：幽灵按钮的灰色悬停底与那里的底色不搭。
    /// </summary>
    public static Style StateLayerGhost => Css.Define(static () => new Style
    {
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = "color-mix(in srgb, currentColor 14%, transparent)" },
        [When.Active] = new() { BackgroundColor = "color-mix(in srgb, currentColor 22%, transparent)" },
    });

    /// <summary>
    /// 状态层（同 vue-tsx-pro 的 stateLayerStyles.overlay）：本身有底色的元素（可选中的标签）悬停时在底色上叠 8% 的文字色。
    /// 叠在背景图层，不改底色：各色调、亮暗主题上都协调（亮色里变深、暗色里变亮）。
    /// </summary>
    public static Style StateLayerOverlay => Css.Define(static () => new Style
    {
        BackgroundImage = "none",
        [When.Hover] = new()
        {
            BackgroundImage = "linear-gradient(color-mix(in srgb, currentColor 8%, transparent), color-mix(in srgb, currentColor 8%, transparent))",
        },
    });

    /// <summary>相对所在文字小一档（0.93em）：提示条里的关闭按钮。</summary>
    public static Style RelativeSmall => Css.Define(static () => new Style { FontSize = CssLength.Em(0.93) });

    /// <summary>相对所在文字大两档（1.29em，同 vue-tsx-pro 的 relativeFontSizeStyles.xl）：结果页的标题。</summary>
    public static Style RelativeExtraLarge => Css.Define(static () => new Style { FontSize = CssLength.Em(1.29) });

    /// <summary>色调的实底（勾选框勾上、滑块已选的一段）：取自外层的 <see cref="ToneFill"/>。</summary>
    public static readonly CssVar<CssColor> FillColor = CssVar.Color("rune-tone-fill");

    /// <summary>实底上的前景（对勾、圆点、开关的滑块、刻度点）：取自外层的 <see cref="ToneFill"/>。</summary>
    public static readonly CssVar<CssColor> OnFillColor = CssVar.Color("rune-tone-on-fill");

    /// <summary>
    /// 设置 <see cref="FillColor"/> 与 <see cref="OnFillColor"/>（同 vue-tsx-pro 的 toneFillStyles）。
    /// 状态色用文字色而不是实色：黄、绿的实色在表面上不到 3:1，图形要的是与周围的对比。
    /// </summary>
    public static Style ToneFill(Tone tone) => Css.Define(tone, static tone => tone switch
    {
        Tone.Neutral => new Style { [FillColor.Name] = $"{Colors.InverseSurface}", [OnFillColor.Name] = $"{Colors.InverseText}" },
        Tone.Accent => new Style { [FillColor.Name] = $"{Tones.Accent.Solid}", [OnFillColor.Name] = $"{Tones.Accent.Contrast}" },
        _ => new Style { [FillColor.Name] = $"{Tones.Of(tone).Text}", [OnFillColor.Name] = $"{Colors.Background}" },
    });

    /// <summary>
    /// 按宽度收起的一行里的一项（data-rune-overflow-item，js/layout/overflow.js 量宽度、写 data-overflow-hidden）：放不下的移出排版、藏起来，
    /// 但仍可测量（容器要 position: relative）。SelectedTags 的标签、NavBar 的各项用它。
    /// </summary>
    public static Style OverflowItem => Css.Define(static () => new Style
    {
        [When.Attribute("[data-overflow-hidden]")] = new()
        {
            Position = Position.Absolute,
            InsetInlineStart = 0,
            InsetBlockStart = 0,
            Visibility = Visibility.Hidden,
            PointerEvents = PointerEvents.None,
        },
    });

    /// <summary>键盘聚焦时的焦点环（画在元素外侧）。</summary>
    public static Style FocusRing => Css.Define(static () => new Style
    {
        [When.FocusVisible] = new()
        {
            Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}",
            OutlineOffset = BorderWidths.Sm,
        },
    });

    /// <summary>里面的元素键盘聚焦时，焦点环画在这个元素外侧（同 vue-tsx-pro 的 focusRingStyles.hasVisible）：原生 input 视觉上看不见的拖放区。</summary>
    public static Style FocusRingWithin => Css.Define(static () => new Style
    {
        [When.Has(":focus-visible")] = new()
        {
            Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}",
            OutlineOffset = BorderWidths.Sm,
        },
    });

    /// <summary>键盘聚焦时的焦点环（画在元素内侧）：外侧会被裁掉的元素用（面板里的按钮、表格单元格）。</summary>
    public static Style FocusRingInset => Css.Define(static () => new Style
    {
        [When.FocusVisible] = new()
        {
            Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}",
            OutlineOffset = $"calc(-1 * {BorderWidths.Sm})",
        },
    });

    /// <summary>
    /// 波纹的宿主（带 data-rune-ripple 的按钮、菜单项）：波纹相对它定位，超出圆角的部分裁掉。
    /// 波纹本身由 JS 初始化器在按下时画（纯客户端，Server 模式也没有往返）。
    /// </summary>
    public static Style RippleHost => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Overflow = Overflow.Hidden,
    });

    /// <summary>视觉上隐藏、读屏仍能读到（给图标按钮补文字说明）。</summary>
    // 读屏能读到、视觉上看不见的通行写法：1px 的盒子不随字号变，这里的 px 是有意的
#pragma warning disable RUNE007
    public static Style VisuallyHidden => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Width = 1,
        Height = 1,
        Padding = 0,
        Margin = -1,
        Overflow = Overflow.Hidden,
        Clip = "rect(0, 0, 0, 0)",
        WhiteSpace = WhiteSpace.Nowrap,
        BorderWidth = 0,
    });
#pragma warning restore RUNE007

    /// <summary>
    /// 去掉按钮的默认外观（同 vue-tsx-pro 的 buttonBaseStyles.root）：外观全由组件自己给的按钮用（标签页的标签、展开按钮）。
    /// 文字样式继承，触摸时点击立即响应；不可用时是禁止的指针。
    /// </summary>
    public static Style ButtonReset => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Margin = 0,
        Padding = 0,
        BorderWidth = 0,
        BackgroundColor = CssColors.Transparent,
        Color = CssWide.Inherit,
        Font = CssWide.Inherit,
        TextAlign = CssWide.Inherit,
        TextDecorationLine = TextDecorationLine.None,
        Cursor = Cursor.Pointer,
        UserSelect = UserSelect.None,
        TouchAction = TouchAction.Manipulation,
        WebkitTapHighlightColor = CssColors.Transparent,
        [When.Disabled] = new() { Cursor = Cursor.NotAllowed },
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
