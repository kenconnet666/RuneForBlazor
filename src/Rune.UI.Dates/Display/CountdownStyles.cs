using Rune.Styles;

namespace Rune.UI;

/// <summary>倒计时的外观。</summary>
public enum CountdownVariant
{
    /// <summary>一串“时:分:秒”（默认），超过一天时前面加天数。</summary>
    Text,

    /// <summary>一格一格的数字块（限时抢购）：天数单独一格，时分秒之间是冒号；字号随上下文。</summary>
    Blocks,
}

/// <summary>倒计时的部件（同 vue-tsx-pro 的 countdownRecipe）。</summary>
public enum CountdownPart
{
    /// <summary>span（role="timer"）。</summary>
    Root,

    /// <summary>数字块外观的一格。</summary>
    Segment,

    /// <summary>格之间的冒号。</summary>
    Separator,
}

/// <summary>倒计时的默认值（UIConfig.Components 里配置）。</summary>
public sealed record CountdownDefaults : IComponentDefaults<CountdownDefaults>
{
    /// <summary>外观。</summary>
    public CountdownVariant? Variant { get; init; }

    /// <inheritdoc/>
    public CountdownDefaults Over(CountdownDefaults outer) => new() { Variant = Variant ?? outer.Variant };
}

/// <summary>倒计时的样式（配方，同 vue-tsx-pro 的 countdownRecipe）：外观是档位。</summary>
public static class CountdownStyles
{
    /// <summary>某种外观下各部件的默认层。</summary>
    public static PartLayers<CountdownPart> Of(CountdownVariant variant, ComponentStyles styles) =>
        Recipe.Of("countdown", variant, styles, static (CountdownVariant variant, CountdownPart part) => part switch
        {
            CountdownPart.Root => Sx.Of(Root, variant == CountdownVariant.Blocks ? BlocksRoot : null),
            CountdownPart.Segment => Segment,
            _ => Separator,
        });

    // 等宽数字：每秒跳动时宽度不变
    static Style Root => Css.Define(static () => new Style { FontVariantNumeric = FontVariantNumeric.TabularNums, WhiteSpace = WhiteSpace.Nowrap });

    static Style BlocksRoot => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = Space.Xs });

    static Style Segment => Css.Define(static () => new Style
    {
        MinWidth = CssLength.Em(2),
        PaddingBlock = Space.Xxs,
        PaddingInline = Space.Xs,
        BorderRadius = Radii.Sm,
        BackgroundColor = Tones.Accent.Solid,
        Color = Tones.Accent.Contrast,
        FontWeight = FontWeights.Semibold,
        TextAlign = TextAlign.Center,
    });

    static Style Separator => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });
}
