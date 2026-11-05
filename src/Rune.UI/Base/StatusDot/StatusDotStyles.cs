using Rune.Styles;

namespace Rune.UI;

/// <summary>状态色点的部件。</summary>
public enum StatusDotPart
{
    /// <summary>色点（span）。</summary>
    Root,
}

/// <summary>色点的大小。</summary>
public enum StatusDotSize
{
    /// <summary>0.5rem（默认）：列表、通知。</summary>
    Sm,

    /// <summary>0.625rem：图例。</summary>
    Md,
}

/// <summary>状态色点的样式（同 vue-tsx-pro 的 statusDotStyles）。</summary>
public static class StatusDotStyles
{
    /// <summary>任意颜色（Color 参数）：经变量写在元素上。</summary>
    public static readonly CssVar<CssColor> CustomColor = CssVar.Color("rune-status-dot-color");

    /// <summary>某个大小、色调（null 时用 <see cref="CustomColor"/>）、某套定制下的默认层。</summary>
    public static PartLayers<StatusDotPart> Of(StatusDotSize size, Tone? tone, ComponentStyles styles) =>
        Recipe.Of("status-dot", (size, tone), styles, static ((StatusDotSize Size, Tone? Tone) a, StatusDotPart _) =>
            Sx.Of(Root, a.Size == StatusDotSize.Md ? Md : Sm, a.Tone is { } tone ? ToneColor(tone) : Custom));

    static Style Root => Css.Define(static () => new Style { Display = Display.InlineBlock, FlexShrink = 0, BorderRadius = Radii.Full });

    static Style Sm => Css.Define(static () => new Style { Width = CssLength.Rem(0.5), Height = CssLength.Rem(0.5) });

    static Style Md => Css.Define(static () => new Style { Width = CssLength.Rem(0.625), Height = CssLength.Rem(0.625) });

    static Style ToneColor(Tone tone) => Css.Define(tone, static tone => new Style
    {
        BackgroundColor = tone == Tone.Neutral ? Colors.TextMuted : Tones.Of(tone).Solid,
    });

    static Style Custom => Css.Define(static () => new Style { BackgroundColor = CustomColor });
}
