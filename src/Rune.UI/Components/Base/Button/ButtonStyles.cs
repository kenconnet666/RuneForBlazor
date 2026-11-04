using Rune.Styles;

namespace Rune.UI;

/// <summary>按钮的样式（同名就同义，见 vue-tsx-pro 的 variant 词表）。</summary>
public enum ButtonVariant
{
    /// <summary>实底：一组操作里的主要操作，每组最多一个。</summary>
    Solid,

    /// <summary>浅色底：次要但仍需突出的操作。</summary>
    Soft,

    /// <summary>浅色底加描边：表单、设置页里的次要操作。</summary>
    Surface,

    /// <summary>只有描边：次要操作。</summary>
    Outline,

    /// <summary>无底无边，悬停时才有底色：工具栏、行内操作、图标按钮。</summary>
    Ghost,

    /// <summary>像链接（无底、无内边距、悬停下划线）：文字中间的操作。</summary>
    Link,
}

/// <summary>按钮的形状。</summary>
public enum ButtonShape
{
    /// <summary>按内容宽度。</summary>
    Normal,

    /// <summary>占满容器宽度。</summary>
    FullWidth,

    /// <summary>宽高相等（图标按钮）。</summary>
    Square,
}

/// <summary>按钮家族的部件。</summary>
public enum ButtonPart
{
    /// <summary>按钮元素（button 或 a）。</summary>
    Root,

    /// <summary>内容层：包着图标与文字（加载中时隐藏但保留宽度）。</summary>
    Content,

    /// <summary>文字前的图标。</summary>
    Icon,

    /// <summary>文字后的图标。</summary>
    IconEnd,

    /// <summary>加载中叠在内容上的指示层。</summary>
    Spinner,
}

/// <summary>按钮家族（Button、IconButton、LinkButton）的默认值。</summary>
public sealed record ButtonDefaults : IComponentDefaults<ButtonDefaults>
{
    /// <summary>样式。</summary>
    public ButtonVariant? Variant { get; init; }

    /// <summary>色调。</summary>
    public Tone? Tone { get; init; }

    /// <summary>字号档位。</summary>
    public Size? Size { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public ButtonDefaults Over(ButtonDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Tone = Tone ?? outer.Tone,
        Size = Size ?? outer.Size,
        Radius = Radius ?? outer.Radius,
    };
}

/// <summary>按钮的外观：交给 <see cref="ButtonStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Tone">色调。</param>
/// <param name="Size">字号档位；null 时继承上下文字号。</param>
/// <param name="Radius">圆角。</param>
/// <param name="Shape">形状。</param>
public readonly record struct ButtonAppearance(ButtonVariant Variant, Tone Tone, Size? Size, Radius Radius, ButtonShape Shape);

/// <summary>按钮家族的样式（配方）。每种样式写成以色调为参数的函数，只写一遍。</summary>
public static class ButtonStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<ButtonPart> Of(ButtonAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("button", appearance, styles, static (ButtonAppearance a, ButtonPart part) => part switch
        {
            ButtonPart.Root => Sx.Of(
                Root,
                SharedStyles.RippleHost,
                SharedStyles.InteractionColors,
                Variant(a.Variant, a.Tone),
                ScaleStyles.Radius(a.Radius),
                a.Size is { } size ? ScaleStyles.FontSize(size) : null,
                a.Shape switch
                {
                    ButtonShape.FullWidth => FullWidth,
                    ButtonShape.Square => Square,
                    _ => null,
                },
                SharedStyles.FocusRing),
            ButtonPart.Content => Content,
            ButtonPart.Spinner => SpinnerOverlay,
            _ => Sx.Of(),
        });

    /// <summary>
    /// 某种样式 × 色调：颜色全部取色调的 token（<see cref="ToneColors"/>）。
    /// 悬停用 <see cref="When.HoverEnabled"/>：不可用的按钮悬停时不变色。
    /// </summary>
    static Style Variant(ButtonVariant variant, Tone tone) => Css.Define((variant, tone), static key =>
    {
        var t = Tones.Of(key.tone);
        return key.variant switch
        {
            ButtonVariant.Solid => new Style
            {
                Color = t.Contrast,
                BackgroundColor = t.Solid,
                [When.HoverEnabled] = new() { BackgroundColor = t.SolidHover },
            },
            ButtonVariant.Soft => new Style
            {
                Color = t.Text,
                BackgroundColor = t.Soft,
                [When.HoverEnabled] = new() { BackgroundColor = t.SoftHover },
            },
            ButtonVariant.Surface => new Style
            {
                Color = t.Text,
                BorderColor = t.Border,
                BackgroundColor = t.Surface,
                [When.HoverEnabled] = new() { BackgroundColor = t.SoftHover },
            },
            ButtonVariant.Outline => new Style
            {
                Color = t.Text,
                BorderColor = t.Border,
                [When.HoverEnabled] = new() { BackgroundColor = t.QuietHover },
            },
            ButtonVariant.Ghost => new Style
            {
                Color = t.Text,
                [When.HoverEnabled] = new() { BackgroundColor = t.QuietHover },
            },
            // 像链接：不要最小高度与左右内边距，上下内边距、边框与圆角照旧（焦点环的形状与按钮一致）
            _ => new Style
            {
                Color = t.Text,
                MinHeight = 0,
                PaddingInline = 0,
                FontWeight = FontWeights.Medium,
                TextUnderlineOffset = CssLength.Em(0.2),
                [When.HoverEnabled] = new() { TextDecorationLine = TextDecorationLine.Underline },
            },
        };
    });

    /// <summary>
    /// 按钮的基础：去掉默认外观、继承文字样式；内部尺寸用 em，跟着字号档位整体缩放；
    /// 可点击区域不小于 24px（WCAG 2.5.8）。
    /// </summary>
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Sm,
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        Margin = 0,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Lg,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = CssColors.Transparent,
        BackgroundColor = CssColors.Transparent,
        Font = CssWide.Inherit,
        TextAlign = CssWide.Inherit,
        FontWeight = FontWeights.Semibold,
        LineHeight = LineHeights.Tight,
        WhiteSpace = WhiteSpace.Nowrap,
        TextDecorationLine = TextDecorationLine.None,
        Cursor = Cursor.Pointer,
        UserSelect = UserSelect.None,
        // 触摸时不等双击缩放的判定，点击立即响应
        TouchAction = TouchAction.Manipulation,
        WebkitTapHighlightColor = CssColors.Transparent,
        [When.Disabled] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style FullWidth => Css.Define(static () => new Style { Display = Display.Flex, Width = CssLength.Percent(100) });

    static Style Square => Css.Define(static () => new Style
    {
        PaddingInline = 0,
        Width = $"max({Sizes.Control}, {Sizes.MinTarget})",
        FlexShrink = 0,
    });

    /// <summary>显示密码按钮平时的图标（睁眼）：按下后（aria-pressed）隐藏。</summary>
    internal static Style RevealIcon => Css.Define(static () => new Style
    {
        [When.Inside(When.Pressed)] = new() { Display = Display.None },
    });

    /// <summary>显示密码按钮按下后的图标（闭眼）。</summary>
    internal static Style ConcealIcon => Css.Define(static () => new Style
    {
        Display = Display.None,
        [When.Inside(When.Pressed)] = new() { Display = Display.Block },
    });

    /// <summary>复制按钮平时的图标：复制后（JS 在按钮上写 data-state="copied"）隐藏。</summary>
    internal static Style CopyIdleIcon => Css.Define(static () => new Style
    {
        [When.Inside(When.Attribute("[data-state=copied]"))] = new() { Display = Display.None },
    });

    /// <summary>复制按钮复制后的对勾：平时隐藏，复制后弹出（减少动态效果时时长为 0）。</summary>
    internal static Style CopiedIcon => Css.Define(static () => new Style
    {
        Display = Display.None,
        [When.Inside(When.Attribute("[data-state=copied]"))] = new()
        {
            Display = Display.Block,
            Keyframes = Pop,
            AnimationDuration = Durations.Md,
            AnimationTimingFunction = Easings.Enter,
        },
    });

    static Keyframes Pop => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "scale(0.5)", Opacity = 0 },
    });

    /// <summary>
    /// 加载状态推迟这么久才换成加载指示（同 vue-tsx-pro 的 useDelayedLoading）：很快完成的操作按钮不闪一下。
    /// 用 CSS 的过渡延迟实现，Server 模式没有往返；点击拦截、aria-busy 仍立即生效。
    /// </summary>
    const string LoadingDelay = "200ms";

    // 加载中隐藏内容但保留占位与读屏名称（不用 visibility: hidden，那样读屏也读不到）；
    // 过渡写在隐藏状态里：进入时推迟，恢复时立即
    static Style Content => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        [When.Attribute("[data-hidden]")] = new() { Opacity = 0, Transition = $"opacity 0s linear {LoadingDelay}" },
    });

    // 指示层插入时从透明开始，推迟后才出现
    static Style SpinnerOverlay => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Transition = $"opacity 0s linear {LoadingDelay}",
        [When.Starting] = new() { Opacity = 0 },
    });
}
