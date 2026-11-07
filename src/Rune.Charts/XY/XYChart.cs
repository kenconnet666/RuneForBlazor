using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 按数据行画的直角坐标图表（折线图、面积图、柱状图、组合图、散点图）的公共参数：数据、类目、系列、图例与隐藏的系列、下钻。
/// 比例尺、坐标轴、提示框等见 <see cref="XYChartBase{TItem}"/>。
/// </summary>
public abstract class XYChart<TItem> : XYChartBase<TItem>
{
    IReadOnlyList<string> hiddenState = [];

    /// <summary>数据：每一行是类目轴上的一项（一个月、一个项目）。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>类目：类目轴的刻度、提示框的标题、数据表的第一列；标注、区间按它的值找行。</summary>
    [Parameter, EditorRequired] public Func<TItem, object?> Category { get; set; } = null!;

    /// <summary>类目的显示文字；不写时把类目的值转成字符串。</summary>
    [Parameter] public Func<TItem, string>? CategoryText { get; set; }

    /// <summary>系列：每个系列画成一组柱子、一条线或一片面积。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ChartSeries<TItem>> Series { get; set; } = [];

    /// <summary>图例：默认多于一个系列时显示；点图例可以隐藏、恢复系列。</summary>
    [Parameter] public bool? Legend { get; set; }

    /// <summary>隐藏的系列（系列的 Key）：点图例时写回，可以 <c>@bind-HiddenSeries</c>。</summary>
    [Parameter] public IReadOnlyList<string>? HiddenSeries { get; set; }

    /// <summary>点图例隐藏、恢复了系列。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> HiddenSeriesChanged { get; set; }

    /// <summary>
    /// 点了一个类目（下钻，如跳到筛好条件的台账）：点在柱上按那根柱，其余按悬停的类目；写了它时绘图区的指针是手形。
    /// 点图是鼠标的捷径：同样的去处在页面上还要有链接或按钮，键盘与读屏才到得了（WCAG 2.1.1）。
    /// </summary>
    [Parameter] public EventCallback<ChartItemClick<TItem>> OnItemClick { get; set; }

    private protected override IReadOnlyList<TItem> Data => Items;

    private protected override object? CategoryKey(TItem row) => Category(row);

    private protected override string CategoryOf(TItem row) => CategoryText?.Invoke(row) ?? Category(row)?.ToString() ?? "";

    private protected override IReadOnlyList<ChartSeries<TItem>> SeriesSource => Series;

    private protected override bool? ShowLegend => Legend;

    private protected override IReadOnlyList<string> Hidden => HiddenSeries ?? hiddenState;

    private protected override async Task ToggleSeriesAsync(string key)
    {
        IReadOnlyList<string> next = Hidden.Contains(key) ? [.. Hidden.Where(item => item != key)] : [.. Hidden, key];
        hiddenState = next;
        await HiddenSeriesChanged.InvokeAsync(next);
    }

    private protected override bool Clickable => OnItemClick.HasDelegate;

    private protected override Task ActivateRowAsync(int index) => OnItemClick.InvokeAsync(new ChartItemClick<TItem>(Items[index], index));
}
