using Rune.Styles;

namespace Rune.UI;

/// <summary>步骤的状态。</summary>
public enum StepState
{
    /// <summary>已完成：对勾、品牌色。</summary>
    Complete,

    /// <summary>当前：aria-current="step"。</summary>
    Current,

    /// <summary>未开始。</summary>
    Upcoming,

    /// <summary>出错：警示图标、危险色。</summary>
    Error,
}

/// <summary>一个步骤。</summary>
/// <param name="Title">标题。</param>
/// <param name="Description">说明（标题下面的小字）。</param>
/// <param name="Icon">代替序号的图标；已完成时仍显示对勾，出错时仍显示警示图标。</param>
/// <param name="Status">状态；不写时按当前步骤推出。</param>
/// <param name="Href">地址：已完成的这一步渲染成链接，点了跳过去。</param>
/// <param name="Disabled">不可点回（即使已完成、有 Href 或 Navigable）。</param>
/// <param name="Key">唯一的 key；不写时用位置。</param>
public sealed record StepItem(
    string Title,
    string? Description = null,
    IconData? Icon = null,
    StepState? Status = null,
    string? Href = null,
    bool Disabled = false,
    string? Key = null);

/// <summary>步骤条的部件。</summary>
public enum StepsPart
{
    /// <summary>每个步骤（li）。</summary>
    Item,

    /// <summary>可点回的步骤外层（按钮，或有 Href 时的链接）。</summary>
    Trigger,

    /// <summary>序号圆圈。</summary>
    Indicator,

    /// <summary>文字区（标题与说明的容器）。</summary>
    Text,

    /// <summary>标题。</summary>
    Title,

    /// <summary>说明。</summary>
    Description,

    /// <summary>步骤之间的连接线。</summary>
    Connector,
}

/// <summary>一步的外观档位：交给 <see cref="StepsStyles.Of"/>。</summary>
/// <param name="Orientation">方向。</param>
/// <param name="State">状态。</param>
/// <param name="Link">外层是链接（去掉链接的颜色与下划线）。</param>
public readonly record struct StepLook(Orientation Orientation, StepState State, bool Link = false) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Orientation, State];
}

/// <summary>步骤条的样式（配方，同 vue-tsx-pro 的 stepsStyles）。</summary>
public static class StepsStyles
{
    /// <summary>某一步的外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<StepsPart> Of(StepLook look, ComponentStyles styles) =>
        Recipe.Of("steps", look, styles, static (StepLook a, StepsPart part) => part switch
        {
            StepsPart.Item => a.Orientation == Orientation.Vertical ? Sx.Of(Item, ItemVertical) : Item,
            StepsPart.Trigger => a.Link ? Sx.Of(Trigger, Link, SharedStyles.FocusRing) : Trigger,
            StepsPart.Indicator => Sx.Of(Indicator, a.State switch
            {
                StepState.Complete => Complete,
                StepState.Current => Current,
                StepState.Error => Error,
                _ => Upcoming,
            }),
            StepsPart.Text => Text,
            StepsPart.Title => Title,
            StepsPart.Description => Description,
            _ => Sx.Of(Connector, a.Orientation == Orientation.Vertical ? ConnectorVertical : null, a.State == StepState.Complete ? ConnectorDone : null),
        });

    static string Fade(string property) => $"{property} {Durations.Md} {Easings.Standard}";

    static Style Item => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexGrow = 1,
        FlexBasis = 0,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Sm,
        MinWidth = 0,
    });

    // 竖排：高度随内容，连接线相对这一项定位
    static Style ItemVertical => Css.Define(static () => new Style { FlexGrow = 0, FlexBasis = CssLength.Auto, Position = Position.Relative });

    // 可点回的步骤：整块是按钮或链接（按钮的默认外观、焦点环、波纹由 ButtonBase 给出）
    static Style Trigger => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Sm,
        BorderRadius = Radii.Sm,
        TextAlign = TextAlign.Start,
    });

    static Style Link => Css.Define(static () => new Style { Color = CssWide.Inherit, TextDecorationLine = TextDecorationLine.None });

    static Style Indicator => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexShrink = 0,
        Width = CssLength.Em(2),
        Height = CssLength.Em(2),
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderRadius = Radii.Full,
        // 用 em 而不是 Sm（默认字号下 13 / 14 ≈ 0.93）：随 Size 缩放
        FontSize = CssLength.Em(0.93),
        FontWeight = FontWeights.Semibold,
        // 走到下一步时圆圈换色、连接线变色都过渡，不是一下跳过去
        Transition = $"{Fade("background-color")}, {Fade("border-color")}, {Fade("color")}",
    });

    static Style Complete => Css.Define(static () => new Style { BorderColor = Tones.Accent.Solid, BackgroundColor = Tones.Accent.Solid, Color = Tones.Accent.Contrast });

    static Style Current => Css.Define(static () => new Style { BorderColor = Tones.Accent.Solid, Color = Tones.Accent.Text });

    static Style Upcoming => Css.Define(static () => new Style { BorderColor = Colors.Border, Color = Colors.TextMuted });

    static Style Error => Css.Define(static () => new Style { BorderColor = Tones.Danger.Solid, BackgroundColor = Tones.Danger.Soft, Color = Tones.Danger.Text });

    static Style Text => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xs, PaddingBlockStart = CssLength.Em(0.3), MinWidth = 0 });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Medium });

    static Style Description => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = CssLength.Em(0.93) });

    // 步骤之间的连接线：横排在标题右侧延伸
    static Style Connector => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        AlignSelf = AlignSelf.Center,
        MinWidth = Space.Lg,
        Height = BorderWidths.Xs,
        BackgroundColor = Colors.Border,
        Transition = Fade("background-color"),
    });

    // 竖排：从圆圈下面一直连到下一步的圆圈（圆圈宽 2 × 0.93em，中线在 0.93em）
    static Style ConnectorVertical => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInlineStart = CssLength.Em(0.93),
        InsetBlockStart = $"calc(1.86em + {Space.Xs})",
        InsetBlockEnd = $"calc(-1 * {Space.Lg} + {Space.Xs})",
        Width = BorderWidths.Xs,
        Height = CssLength.Auto,
        MinWidth = 0,
        // 两端都定了位置时按 align-self 对齐：横排的 center 会让空的 span 缩成 0 高，这里要拉伸
        AlignSelf = AlignSelf.Stretch,
        MarginInlineStart = $"calc({BorderWidths.Xs} / -2)",
    });

    static Style ConnectorDone => Css.Define(static () => new Style { BackgroundColor = Tones.Accent.Solid });
}
