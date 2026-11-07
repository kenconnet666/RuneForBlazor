using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>瀑布图的一步。</summary>
/// <param name="Label">名称：类目轴的刻度。</param>
/// <param name="Value">这一步的变动（正数往上、负数往下）；合计柱写了时是新的累计（作为之后的起点，如“V1 测算”）。</param>
public sealed record WaterfallStep(string Label, decimal? Value = null)
{
    /// <summary>合计柱：从 0 画到当时的累计。</summary>
    public bool Total { get; init; }
}

/// <summary>瀑布图一步的种类：决定颜色。</summary>
public enum WaterfallKind
{
    /// <summary>往上走。</summary>
    Increase,

    /// <summary>往下走。</summary>
    Decrease,

    /// <summary>合计。</summary>
    Total,
}

/// <summary>画图用的一行（<see cref="WaterfallChart.RowsOf"/> 算出）：柱子从 Low 画到 High。</summary>
/// <param name="Label">名称。</param>
/// <param name="Kind">种类。</param>
/// <param name="Change">这一步的变动（原值，提示框与数据表按它格式化）。</param>
/// <param name="Cumulative">这一步之后的累计。</param>
/// <param name="Low">柱子的下沿。</param>
/// <param name="High">柱子的上沿。</param>
public sealed record WaterfallRow(string Label, WaterfallKind Kind, decimal? Change, decimal Cumulative, decimal Low, decimal High);

/// <summary>
/// 瀑布图（同 vue-tsx-pro 的 WaterfallChart）：从起点出发，一步步加减到终点，看总数是怎么变过来的
/// （测算两版之间各参数的影响、预算到实际的差异拆解）。
/// <code>&lt;WaterfallChart Steps="steps" Unit="万元" aria-label="V1 到 V2 的变化" /&gt;
/// steps = [new("V1 测算", 120) { Total = true }, new("材料单价", -18), new("工时", 6.5m), new("V2 测算") { Total = true }]</code>
/// 往上的步用 Success 色、往下的用 Danger 色、合计用色板的第 1 个（成本类的图里减少是好事时对调）。提示框与读屏的数据表列出变动与累计。
/// 坐标轴、目标线、加载与空状态同 BarChart（见 <see cref="XYChartBase{TItem}"/>）。
/// </summary>
public sealed class WaterfallChart : XYChartBase<WaterfallRow>
{
    static readonly Attrs ConnectorLayer = new() { Sx = WaterfallStyles.Connector };

    IReadOnlyList<WaterfallRow> rows = [];
    IReadOnlyList<ChartSeries<WaterfallRow>> series = [];

    /// <summary>各步：按顺序从左到右。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<WaterfallStep> Steps { get; set; } = [];

    /// <summary>往上走的颜色，默认 Success。</summary>
    [Parameter] public ChartColor? IncreaseColor { get; set; }

    /// <summary>往下走的颜色，默认 Danger。</summary>
    [Parameter] public ChartColor? DecreaseColor { get; set; }

    /// <summary>合计柱的颜色，默认色板的第 1 个。</summary>
    [Parameter] public ChartColor? TotalColor { get; set; }

    /// <summary>柱子末端的圆角（px），默认 2。</summary>
    [Parameter] public double Radius { get; set; } = 2;

    /// <summary>
    /// 数值轴从 0 起（默认）；false 时从接近最低的累计处起：合计很大、各步变动很小时变动也看得清（合计柱被截短，读数看轴上的刻度）。
    /// 写了 ValueMin 时以它为准。
    /// </summary>
    [Parameter] public bool FromZero { get; set; } = true;

    /// <summary>点了一步（下钻）：参数是这一步与它的位置；写了它时指针是手形。</summary>
    [Parameter] public EventCallback<ChartItemClick<WaterfallStep>> OnItemClick { get; set; }

    /// <summary>各步算成画图用的行：累计、每根柱子的上下沿。</summary>
    public static IReadOnlyList<WaterfallRow> RowsOf(IEnumerable<WaterfallStep> steps)
    {
        var running = 0m;
        return [.. steps.Select(step =>
        {
            var before = step.Total ? 0 : running;
            if (step.Total) running = step.Value ?? running;
            else running += step.Value ?? 0;
            var (low, high) = before <= running ? (before, running) : (running, before);
            var kind = step.Total ? WaterfallKind.Total : running >= before ? WaterfallKind.Increase : WaterfallKind.Decrease;
            return new WaterfallRow(step.Label, kind, step.Value, running, low, high);
        })];
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        rows = RowsOf(Steps);
        // 系列只用来给读屏的表头与算范围；画法、提示框、数据表由这里决定
        series = [new ChartSeries<WaterfallRow>("cumulative", Config.Lang.ChartChange, row => row.Cumulative)];
        base.OnParametersSet();
    }

    private protected override IReadOnlyList<WaterfallRow> Data => rows;
    private protected override object? CategoryKey(WaterfallRow row) => row.Label;
    private protected override string CategoryOf(WaterfallRow row) => row.Label;
    private protected override IReadOnlyList<ChartSeries<WaterfallRow>> SeriesSource => series;
    private protected override bool? ShowLegend => false;
    private protected override bool Clickable => OnItemClick.HasDelegate;
    private protected override Task ActivateRowAsync(int index) => OnItemClick.InvokeAsync(new ChartItemClick<WaterfallStep>(Steps[index], index));

    private protected override bool Band => true;
    private protected override bool SkeletonBars => true;

    private protected override IEnumerable<double>? DomainValues(IReadOnlyList<WaterfallRow> list, IReadOnlyList<ResolvedSeries<WaterfallRow>> shown) =>
        list.SelectMany(row => new[] { (double)row.Low, (double)row.High });

    /// <summary>不从 0 起时轴的下沿：最低的累计再往下留出变动幅度的一半（没有变动时留出它的一成）；有负的累计时仍按数据自动取，不截。</summary>
    private protected override double? DefaultValueMin
    {
        get
        {
            if (FromZero || rows.Count == 0) return null;
            var values = rows.SelectMany(row => row.Kind == WaterfallKind.Total ? [row.High] : new[] { row.Low, row.High }).Select(value => (double)value).ToList();
            var low = values.Min();
            if (low <= 0) return null;
            var span = values.Max() - low is var s && s != 0 ? s : low * 0.1;
            return Math.Max(0, low - span * 0.5);
        }
    }

    CssColor ColorOf(WaterfallRow row) => ChartColor.Resolve(row.Kind switch
    {
        WaterfallKind.Total => TotalColor ?? ChartColor.Series(1),
        WaterfallKind.Increase => IncreaseColor ?? Tone.Success,
        _ => DecreaseColor ?? Tone.Danger,
    }, 0);

    // 变动：带正负号；suffix 时数值后面带单位（提示框）
    string ChangeText(WaterfallRow row, bool suffix = false) => row.Change is { } change
        ? LangFormat.Number(change, (ValueFormat ?? NumberFormat.Default) with { SignDisplay = SignDisplay.ExceptZero, Suffix = suffix ? Unit : null }, Config.Lang)
        : Config.Lang.EmptyValue;

    private protected override IReadOnlyList<ChartTooltipItem>? TooltipItems(WaterfallRow row)
    {
        var lang = Config.Lang;
        return row.Kind == WaterfallKind.Total
            ? [new(lang.ChartTotal, ColorOf(row), ValueText(row.Cumulative))]
            : [new(lang.ChartChange, ColorOf(row), ChangeText(row, true)), new(lang.ChartCumulative, null, ValueText(row.Cumulative))];
    }

    private protected override ChartTable? CustomTable()
    {
        var lang = Config.Lang;
        string Column(string name) => Unit is null ? name : $"{name}（{Unit}）";
        return new ChartTable(
            [lang.ChartCategory, Column(lang.ChartChange), Column(lang.ChartCumulative)],
            [.. rows.Select(row => new ChartTableRow(row.Label,
                [row.Kind == WaterfallKind.Total ? lang.EmptyValue : ChangeText(row), LangFormat.Number(row.Cumulative, ValueFormat ?? NumberFormat.Default, lang)]))]);
    }

    private protected override RenderFragment Marks(XYContext<WaterfallRow> context) => builder =>
    {
        var width = Math.Min(context.Step * 0.7, 56);
        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "data-part", "bars");
        for (var index = 0; index < context.Rows.Count; index++)
        {
            var row = context.Rows[index];
            var center = context.Category(row, index);
            builder.OpenElement(2, "g");
            builder.SetKey(index);
            // 连接线：这一步的累计接到下一根柱子
            if (index + 1 < context.Rows.Count)
            {
                var level = SvgFormat.Number(context.Value((double)row.Cumulative));
                var (from, to) = (SvgFormat.Number(center + width / 2), SvgFormat.Number(context.Category(context.Rows[index + 1], index + 1) - width / 2));
                builder.OpenElement(3, "line");
                builder.AddAttribute(4, "x1", context.Horizontal ? level : from);
                builder.AddAttribute(5, "x2", context.Horizontal ? level : to);
                builder.AddAttribute(6, "y1", context.Horizontal ? from : level);
                builder.AddAttribute(7, "y2", context.Horizontal ? to : level);
                builder.AddMultipleAttributes(8, Attrs.Merge(ConnectorLayer));
                builder.CloseElement();
            }
            var (d, grow) = BarShape(context, center - width / 2, width, (double)row.Low, (double)row.High, Radius, true,
                row.Kind == WaterfallKind.Decrease || (row.Kind == WaterfallKind.Total && row.Cumulative < 0));
            builder.OpenElement(9, "path");
            builder.AddAttribute(10, "data-part", "bar");
            builder.AddAttribute(11, "data-index", index);
            builder.AddAttribute(12, "d", d);
            builder.AddMultipleAttributes(13, Attrs.Merge(new Attrs { Sx = grow }, ChartFrame.ColorVar(ColorOf(row))));
            builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
    };
}

/// <summary>瀑布图的样式：连接相邻柱子的虚线。</summary>
internal static class WaterfallStyles
{
    internal static Style Connector => Css.Define(static () => new Style
    {
        Stroke = Colors.BorderStrong,
        StrokeWidth = 1,
        StrokeDasharray = "2 2",
        PointerEvents = PointerEvents.None,
    });
}
