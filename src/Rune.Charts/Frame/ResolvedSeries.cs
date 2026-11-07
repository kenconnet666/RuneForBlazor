namespace Rune.UI;

/// <summary>系列加上实际使用的颜色。</summary>
internal sealed record ResolvedSeries<TItem>(ChartSeries<TItem> Source, Rune.Styles.CssColor Color)
{
    public string Key => Source.Key;
    public string Label => Source.Label;

    /// <summary>把颜色写在元素的 style 上（--rune-chart-color）：同一个系列的图形共用一份。</summary>
    public Attrs ColorLayer { get; } = ChartFrame.ColorVar(Color);

    public decimal? ValueOf(TItem row) => Source.Value(row);

    /// <summary>系列的颜色定下来：写了的用它，没写的按顺序取色板。</summary>
    public static IReadOnlyList<ResolvedSeries<TItem>> Of(IEnumerable<ChartSeries<TItem>> series) =>
        [.. series.Select((item, index) => new ResolvedSeries<TItem>(item, ChartColor.Resolve(item.Color, index)))];
}
