using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 图表里一个颜色（同 vue-tsx-pro 的 ChartColor）：色板里的一个（<see cref="Series"/>）、语义色（<c>Tone.Danger</c>，瀑布图的增减、目标线），
/// 或任意 CSS 颜色（<c>"#e5484d"</c>）。色板与语义色是 CSS 变量，切换主题、主题编辑器改品牌色都不用重画。
/// 不写时按位置取色板（<see cref="Resolve"/>）。
/// </summary>
public readonly record struct ChartColor
{
    static readonly CssColor[] Palette =
    [
        ChartColors.Series1, ChartColors.Series2, ChartColors.Series3, ChartColors.Series4,
        ChartColors.Series5, ChartColors.Series6, ChartColors.Series7, ChartColors.Series8,
    ];

    ChartColor(CssColor css) => Css = css;

    /// <summary>实际的 CSS 颜色。</summary>
    public CssColor Css { get; }

    /// <summary>色板里的第 <paramref name="number"/> 个颜色（1–8；第 1 个跟随品牌色）。</summary>
    public static ChartColor Series(int number) => number is >= 1 and <= 8
        ? new(Palette[number - 1])
        : throw new ArgumentOutOfRangeException(nameof(number), number, "图表色板只有 1–8。");

    /// <summary>语义色：中性是弱化的文字色，其余是色调的实色。</summary>
    public static implicit operator ChartColor(Tone tone) => new(tone switch
    {
        Tone.Neutral => Colors.TextMuted,
        Tone.Accent => Tones.Accent.Solid,
        Tone.Info => Tones.Info.Solid,
        Tone.Success => Tones.Success.Solid,
        Tone.Warning => Tones.Warning.Solid,
        _ => Tones.Danger.Solid,
    });

    /// <summary>任意 CSS 颜色。</summary>
    public static implicit operator ChartColor(CssColor css) => new(css);

    /// <summary>任意 CSS 颜色（<c>"#e5484d"</c>）。</summary>
    public static implicit operator ChartColor(string css) => new(css);

    /// <summary>写了就用它，没写时按位置取色板，多于 8 个时循环使用（系列、环形图的扇区、网络图的组共用）。</summary>
    public static CssColor Resolve(ChartColor? color, int index) => color?.Css ?? Palette[index % Palette.Length];
}
