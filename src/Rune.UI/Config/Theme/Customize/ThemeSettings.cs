namespace Rune.UI;

/// <summary>圆角档位：默认圆角的倍数。</summary>
public enum ThemeRadius
{
    /// <summary>直角。</summary>
    None,

    /// <summary>小圆角（默认的 0.5 倍）。</summary>
    Small,

    /// <summary>默认。</summary>
    Medium,

    /// <summary>大圆角（默认的 1.75 倍）。</summary>
    Large,
}

/// <summary>字体搭配。</summary>
public enum ThemeFont
{
    /// <summary>正文与标题都是无衬线（默认）。</summary>
    Sans,

    /// <summary>标题用衬线，正文无衬线：书卷感，长文仍好读。</summary>
    SerifHeadings,
}

/// <summary>动效速度。</summary>
public enum ThemeMotion
{
    /// <summary>关掉过渡与动画。</summary>
    None,

    /// <summary>快（默认时长的 0.6 倍）。</summary>
    Fast,

    /// <summary>默认。</summary>
    Normal,

    /// <summary>慢（默认时长的 1.5 倍）。</summary>
    Slow,
}

/// <summary>
/// 主题定制的设置（同 vue-tsx-pro 的 ThemeSettings）：不写的旋钮沿用主题本来的取值。
/// 用 <see cref="ThemeCustomization.Apply"/> 套到一套主题上，ThemeCustomizer 是它的界面。
/// </summary>
public sealed record ThemeSettings
{
    /// <summary>品牌色（Radix 色阶）。</summary>
    public AccentScale? Accent { get; init; }

    /// <summary>中性色（Radix 灰色色阶）。</summary>
    public NeutralScale? Neutral { get; init; }

    /// <summary>圆角。</summary>
    public ThemeRadius? Radius { get; init; }

    /// <summary>缩放：字号的倍数，0.9–1.1；间距、控件高度以 em 计，随字号一起缩放。</summary>
    public double? Scaling { get; init; }

    /// <summary>字体搭配。</summary>
    public ThemeFont? Font { get; init; }

    /// <summary>动效速度。</summary>
    public ThemeMotion? Motion { get; init; }

    /// <summary>一个旋钮都没动：主题保持原样。</summary>
    public bool IsEmpty => Accent is null && Neutral is null && Radius is null && Scaling is null && Font is null && Motion is null;
}
