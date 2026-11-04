using Rune.Styles;

namespace Rune.UI;

/// <summary>提示条的样式（实底不适合大块文字，不提供）。</summary>
public enum AlertVariant
{
    /// <summary>浅色底（默认）。</summary>
    Soft,

    /// <summary>浅色底加描边。</summary>
    Surface,

    /// <summary>只有描边。</summary>
    Outline,
}

/// <summary>读屏怎样播报出现的提示。</summary>
public enum AlertLive
{
    /// <summary>不主动播报（默认）：页面加载时就在的提示。</summary>
    Off,

    /// <summary>礼貌播报（role="status"）：等读屏念完当前内容再念，如“已保存”。</summary>
    Polite,

    /// <summary>立即播报（role="alert"）：打断当前朗读，只用于错误等紧急信息。</summary>
    Assertive,
}

/// <summary>提示条的部件。</summary>
public enum AlertPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>图标（加载中时是加载指示）。</summary>
    Icon,

    /// <summary>文字区：标题与说明的容器。</summary>
    Body,

    /// <summary>标题（加粗的第一行）。</summary>
    Title,

    /// <summary>说明。</summary>
    Description,

    /// <summary>操作区（重试等按钮）。</summary>
    Action,

    /// <summary>关闭按钮。</summary>
    Close,
}

/// <summary>提示条的默认值。</summary>
public sealed record AlertDefaults : IComponentDefaults<AlertDefaults>
{
    /// <summary>样式。</summary>
    public AlertVariant? Variant { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public AlertDefaults Over(AlertDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Radius = Radius ?? outer.Radius,
    };
}

/// <summary>提示条的外观：交给 <see cref="AlertStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Tone">色调。</param>
/// <param name="Size">字号档位；null 时继承上下文。</param>
/// <param name="Radius">圆角。</param>
public readonly record struct AlertAppearance(AlertVariant Variant, Tone Tone, Size? Size, Radius Radius)
{
    /// <summary>外观：组件上写的 &gt; 配置的默认值（<see cref="AlertDefaults"/>）&gt; 浅色底、提示色、中圆角。</summary>
    public static AlertAppearance Resolve(AlertVariant? variant, Tone? tone, Size? size, Radius? radius, UIConfig config)
    {
        var defaults = config.Defaults.Get<AlertDefaults>();
        return new AlertAppearance(
            variant ?? defaults?.Variant ?? AlertVariant.Soft,
            tone ?? UI.Tone.Info,
            size,
            radius ?? defaults?.Radius ?? UI.Radius.Md);
    }
}

/// <summary>提示条的样式（配方）。</summary>
public static class AlertStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<AlertPart> Of(AlertAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("alert", appearance, styles, static (AlertAppearance a, AlertPart part) => part switch
        {
            AlertPart.Root => Sx.Of(
                Root,
                Variant(a.Variant, a.Tone),
                ScaleStyles.Radius(a.Radius),
                a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            AlertPart.Icon => Sx.Of(Icon, SharedStyles.ToneText(a.Tone)),
            AlertPart.Body => Body,
            AlertPart.Title => Sx.Of(Title, SharedStyles.ToneText(a.Tone)),
            AlertPart.Description => Sx.Of(),
            AlertPart.Action => Action,
            _ => Close,
        });

    /// <summary>不写图标时按色调选的图标。</summary>
    public static IconData DefaultIcon(Tone tone) => tone switch
    {
        UI.Tone.Success => BuiltInIcons.CircleCheck,
        UI.Tone.Warning => BuiltInIcons.TriangleExclamationPoint,
        UI.Tone.Danger => BuiltInIcons.CircleExclamationPoint,
        _ => BuiltInIcons.Info,
    };

    static Style Variant(AlertVariant variant, Tone tone) => Css.Define((variant, tone), static key =>
    {
        var t = Tones.Of(key.tone);
        return key.variant switch
        {
            AlertVariant.Surface => new Style { BackgroundColor = t.Soft, BorderColor = t.Border },
            AlertVariant.Outline => new Style { BorderColor = t.Border },
            _ => new Style { BackgroundColor = t.Soft },
        };
    });


    // 内边距用 em：跟着 Size 缩放
    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        Gap = "0.6em",
        PaddingBlock = "0.75em",
        PaddingInline = "1em",
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = "transparent",
        Color = Colors.Text,
        LineHeight = LineHeights.Normal,
    });

    // 图标与第一行文字居中对齐：行高 1.5em，图标 1.15em
    static Style Icon => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        FontSize = "1.15em",
        MarginBlock = "0.1em",
    });

    static Style Body => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Xs,
        FlexGrow = 1,
        MinWidth = 0,
    });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });

    static Style Action => Css.Define(static () => new Style { FlexShrink = 0, AlignSelf = AlignSelf.Center });

    // 关闭按钮贴着右上角：抵消一部分内边距，与标题行对齐
    static Style Close => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        MarginBlock = "-0.25em",
        MarginInlineEnd = "-0.4em",
    });
}
