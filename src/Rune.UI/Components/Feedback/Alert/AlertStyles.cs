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

    /// <summary>说明（标题下面的正文）。</summary>
    Description,

    /// <summary>额外内容（同时给了 Description 与 ChildContent 时的 ChildContent）。</summary>
    Content,

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
            // 色调排在前面：按部件定制能盖过它
            AlertPart.Root => Sx.Of(
                SharedStyles.StatusColors(StatusVariantOf(a.Variant), a.Tone),
                Root,
                SharedStyles.Fade,
                ScaleStyles.Radius(a.Radius),
                a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            AlertPart.Icon => Icon,
            AlertPart.Body => Body,
            AlertPart.Title => Title,
            AlertPart.Description => Description,
            AlertPart.Content => Content,
            AlertPart.Action => Action,
            _ => Sx.Of(Close, SharedStyles.RelativeSmall),
        });

    static StatusVariant StatusVariantOf(AlertVariant variant) => variant switch
    {
        AlertVariant.Surface => StatusVariant.Surface,
        AlertVariant.Outline => StatusVariant.Outline,
        _ => StatusVariant.Soft,
    };


    // 图标、内边距用 em 与间距 token（也是 em）：跟着 Size 缩放
    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Sm,
        Padding = Space.Md,
        LineHeight = LineHeights.Normal,
    });

    // 图标与第一行文字对齐：行高 1.5em 里居中 1em 的图标
    static Style Icon => Css.Define(static () => new Style { FlexShrink = 0, MarginBlock = CssLength.Em(0.25) });

    static Style Body => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Display = Display.Grid,
        Gap = Space.Xs,
    });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });

    // 正文用正文色（标题、图标跟着色调）
    static Style Description => Css.Define(static () => new Style { Color = Colors.Text });

    static Style Content => Css.Define(static () => new Style { MinWidth = 0 });

    static Style Action => Css.Define(static () => new Style { FlexShrink = 0, AlignSelf = AlignSelf.Center });

    // 关闭按钮：抵消一部分上下内边距，与标题行对齐；颜色跟着色调
    static Style Close => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        MarginBlock = $"calc(-1 * {Space.Xs})",
        Color = CssWide.Inherit,
    });
}
