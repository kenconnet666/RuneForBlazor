using Rune.Styles;

namespace Rune.UI;

/// <summary>进度的部件。</summary>
public enum ProgressPart
{
    /// <summary>有标签时的外层（标签行与进度条；环形是环与下方的标签）。</summary>
    Root,

    /// <summary>条形的标签行（标签与数值）。</summary>
    Header,

    /// <summary>标签。</summary>
    Label,

    /// <summary>数值文字（条形在标签行右侧，环形在中间）。</summary>
    ValueLabel,

    /// <summary>条形的轨道（role="progressbar"）。</summary>
    Track,

    /// <summary>条形的进度（着色部分）。</summary>
    Indicator,

    /// <summary>条形的目标线。</summary>
    Target,

    /// <summary>环形（role="progressbar"）。</summary>
    Circle,

    /// <summary>环形的 SVG。</summary>
    CircleSvg,

    /// <summary>环形的轨道。</summary>
    CircleTrack,

    /// <summary>环形的进度。</summary>
    CircleIndicator,

    /// <summary>环形的目标刻度。</summary>
    CircleTarget,
}

/// <summary>进度的形状。</summary>
public enum ProgressShape
{
    /// <summary>条形（默认）：占满一行。</summary>
    Bar,

    /// <summary>环形：中间显示数值，适合仪表盘、卡片里的完成率；直径随字号缩放。</summary>
    Circle,
}

/// <summary>进度的外观。</summary>
/// <param name="Shape">形状。</param>
/// <param name="Tone">色调。</param>
/// <param name="Indeterminate">不确定进度（没有值）。</param>
public readonly record struct ProgressAppearance(ProgressShape Shape, Tone Tone, bool Indeterminate);

/// <summary>进度的默认值（<see cref="ComponentDefaults"/> 里设置）。</summary>
public sealed record ProgressDefaults : IComponentDefaults<ProgressDefaults>
{
    /// <summary>色调。</summary>
    public Tone? Tone { get; init; }

    /// <summary>字号档位。</summary>
    public Size? Size { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public ProgressDefaults Over(ProgressDefaults outer) => new() { Tone = Tone ?? outer.Tone, Size = Size ?? outer.Size, Radius = Radius ?? outer.Radius };
}

/// <summary>
/// 进度的样式（配方，同 vue-tsx-pro 的 progressRecipe）。条形的进度宽度与目标线的位置是任意值，经 <see cref="Value"/>、<see cref="TargetAt"/>
/// 变量写在元素上。和轨道（fill）之间要有 3:1：中性色用次要文字色，品牌色以外的状态色用各自的文字色；Windows 高对比下用系统色。
/// </summary>
public static class ProgressStyles
{
    /// <summary>条形进度的宽度（百分比）。</summary>
    public static readonly CssVar<CssLength> Value = CssVar.Length("rune-progress-value");

    /// <summary>条形目标线的位置（百分比）。</summary>
    public static readonly CssVar<CssLength> TargetAt = CssVar.Length("rune-progress-target");

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<ProgressPart> Of(ProgressAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("progress", appearance, styles, static (ProgressAppearance a, ProgressPart part) =>
        {
            var circle = a.Shape == ProgressShape.Circle;
            return part switch
            {
                ProgressPart.Root => circle ? CircleRoot : BarRoot,
                ProgressPart.Header => Sx.Of(Header, SharedStyles.RelativeSmall),
                ProgressPart.Label => circle ? SharedStyles.RelativeSmall : Sx.Of(),
                ProgressPart.ValueLabel => circle ? Sx.Of(CircleValue, SharedStyles.RelativeSmall) : BarValue,
                ProgressPart.Track => Track,
                ProgressPart.Indicator => Sx.Of(Indicator, ToneFill(a.Tone, stroke: false), a.Indeterminate ? Sliding : Determinate),
                ProgressPart.Target => Target,
                ProgressPart.Circle => Circle,
                ProgressPart.CircleSvg => CircleSvg,
                ProgressPart.CircleTrack => CircleTrack,
                ProgressPart.CircleIndicator => Sx.Of(CircleIndicator, ToneFill(a.Tone, stroke: true), a.Indeterminate ? Spinning : null),
                _ => CircleTarget,
            };
        });

    static Style ToneFill(Tone tone, bool stroke) => Css.Define((tone, stroke), static k =>
    {
        CssColor color = k.tone switch
        {
            Tone.Neutral => Colors.TextMuted,
            Tone.Accent => Tones.Accent.Solid,
            _ => Tones.Of(k.tone).Text,
        };
        return k.stroke
            ? new Style { Stroke = color, [Media.ForcedColors] = new() { Stroke = CssColors.Highlight } }
            : new Style { BackgroundColor = color, [Media.ForcedColors] = new() { BackgroundColor = CssColors.Highlight } };
    });

    static Style BarRoot => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xs, MinWidth = 0 });

    static Style CircleRoot => Css.Define(static () => new Style
    {
        Display = Display.InlineGrid,
        JustifyItems = JustifyItems.Center,
        Gap = Space.Xs,
        TextAlign = TextAlign.Center,
    });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Baseline,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
    });

    // 数值宽度不跳：等宽数字，不被标签挤压
    static Style BarValue => Css.Define(static () => new Style { FlexShrink = 0, FontVariantNumeric = FontVariantNumeric.TabularNums });

    static Style CircleValue => Css.Define(static () => new Style
    {
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        FontWeight = FontWeights.Semibold,
        LineHeight = 1,
    });

    static Style Track => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Overflow = Overflow.Hidden,
        Width = CssLength.Percent(100),
        Height = $"calc({Space.Xs} * 1.5)",
        BackgroundColor = Colors.Fill,
        // Windows 高对比下系统把背景色改掉：轨道画一圈系统的文字色
        [Media.ForcedColors] = new() { OutlineStyle = OutlineStyle.Solid, OutlineWidth = BorderWidths.Xs, OutlineColor = CssColors.CanvasText },
    });

    static Style Indicator => Css.Define(static () => new Style
    {
        Height = CssLength.Percent(100),
        BorderRadius = CssWide.Inherit,
        Transition = $"width {Durations.Md} {Easings.Standard}",
        [Media.ForcedColors] = new() { ForcedColorAdjust = ForcedColorAdjust.None },
    });

    static Style Determinate => Css.Define(static () => new Style { Width = Value });

    // 不确定进度：一段 40% 宽的色块从起始边到结束边循环；它本身就是信息，减少动态效果时变慢而不是停下
    static Style Sliding => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(40),
        Keyframes = Slide,
        AnimationDuration = "1.4s",
        AnimationTimingFunction = Easings.Standard,
        AnimationIterationCount = "infinite",
        [When.Rtl] = new() { Keyframes = SlideRtl },
        [Media.ReducedMotion] = new() { AnimationDuration = "3s" },
    });

    // 竖线压在轨道上：正文色，与进度、轨道都有对比；位置按书写方向
    static Style Target => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetBlock = 0,
        InsetInlineStart = TargetAt,
        Width = BorderWidths.Sm,
        MarginInlineStart = $"calc(-0.5 * {BorderWidths.Sm})",
        BackgroundColor = Colors.Text,
        [Media.ForcedColors] = new() { BackgroundColor = CssColors.CanvasText },
    });

    // 直径随字号缩放：5em 在默认字号下约 70px
    static Style Circle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineGrid,
        PlaceItems = PlaceItems.Center,
        Width = CssLength.Em(5),
        Height = CssLength.Em(5),
        FlexShrink = 0,
    });

    static Style CircleSvg => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Rotate = CssValue.Deg(-90),
    });

    static Style CircleTrack => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Stroke = Colors.Fill,
        [Media.ForcedColors] = new() { Stroke = CssColors.CanvasText },
    });

    static Style CircleIndicator => Css.Define(static () => new Style
    {
        Fill = Fill.None,
        Transition = $"stroke-dashoffset {Durations.Md} {Easings.Standard}",
        [Media.ForcedColors] = new() { ForcedColorAdjust = ForcedColorAdjust.None },
    });

    // 不确定进度：四分之一圈的弧一直转；减少动态效果时变慢
    static Style Spinning => Css.Define(static () => new Style
    {
        TransformOrigin = TransformOrigin.Center,
        Keyframes = Spin,
        AnimationDuration = "1s",
        AnimationTimingFunction = AnimationTimingFunction.Linear,
        AnimationIterationCount = "infinite",
        [Media.ReducedMotion] = new() { AnimationDuration = "3s" },
    });

    static Style CircleTarget => Css.Define(static () => new Style { Stroke = Colors.Text, StrokeLinecap = StrokeLinecap.Round });

    static Keyframes Slide => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "translateX(-100%)" },
        ["to"] = new() { Transform = "translateX(250%)" },
    });

    // 从右到左排版：色块在起始边（右边），从右往左扫
    static Keyframes SlideRtl => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "translateX(100%)" },
        ["to"] = new() { Transform = "translateX(-250%)" },
    });

    static Keyframes Spin => Css.Define(static () => new Keyframes { ["to"] = new() { Rotate = CssValue.Turn(1) } });
}
