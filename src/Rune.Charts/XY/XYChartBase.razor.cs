using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 直角坐标图表（柱状图、折线图、面积图、散点图、组合图、瀑布图）的公共部分（同 vue-tsx-pro 的 XYChart）：
/// 比例尺与坐标轴、网格、十字线与提示框的数据、目标线、标注与区间、下钻；外层结构交给 <see cref="ChartFrame"/>。
/// 各种图只决定画什么（<see cref="Marks"/>），坐标由 <see cref="XYContext{TItem}"/> 换算。
/// <list type="bullet">
/// <item>类目轴：每行一个类目。柱（<see cref="Band"/>）每个类目占一格、画在格子中间；折线、面积的第一个、最后一个类目贴着两端。
/// 刻度文字放不下时隔几个写一个；横向时类目在纵轴，名字太长截短。</item>
/// <item>数值轴：范围默认从 0 起（有负数时含负数），两端扩到“好看”的刻度上；ValueMin、ValueMax 指定的一端不扩。
/// 百分比堆叠按每个类目里各系列的占比画，数值轴是 0–100%。第二数值轴的刻度与主轴对齐。</item>
/// <item>悬停（十字线、圆点、浅底、提示框）在 charts.js 里按绘图区上的 JSON 画，不往返；点了下钻发 activate。</item>
/// </list>
/// </summary>
public abstract partial class XYChartBase<TItem>
{
    // 刻度文字的字号（px）：主题的 xs
    private protected const double TickFont = 12;
    // 刻度文字与绘图区的距离
    const double TickGap = ChartAxis.Gap;
    // 散点图找最近的点时，离指针多远以内算悬停在点上（px）
    const double PointReach = 24;

    /// <summary>数值轴的网格线，默认显示。</summary>
    [Parameter] public bool Grid { get; set; } = true;

    /// <summary>数值轴的下限；不写时按数据自动取（默认从 0 起，数据有负数时取负数）。</summary>
    [Parameter] public double? ValueMin { get; set; }

    /// <summary>数值轴的上限；不写时按数据自动取，扩到“好看”的刻度上。</summary>
    [Parameter] public double? ValueMax { get; set; }

    /// <summary>缩放：绘图区下面一条区间滑块，选一段类目放大看（几年的月度数据）；读屏的数据表、导出的数据仍是全部。</summary>
    [Parameter] public bool Zoom { get; set; }

    /// <summary>缩放的区间：类目的位置，含两端；null 是全部。可以 <c>@bind-ZoomRange</c>。</summary>
    [Parameter] public NumberRange<int>? ZoomRange { get; set; }

    /// <summary>拖了缩放的滑块、点了“显示全部”。</summary>
    [Parameter] public EventCallback<NumberRange<int>?> ZoomRangeChanged { get; set; }

    /// <summary>目标线、参考线：数值轴上的虚线（横向图是竖线）；数值轴的范围会包含它。</summary>
    [Parameter] public IReadOnlyList<ChartTarget>? Targets { get; set; }

    /// <summary>标注：类目上的事件（竖线），或圈出某个系列的值（横向图只画竖线）。</summary>
    [Parameter] public IReadOnlyList<ChartAnnotation>? Annotations { get; set; }

    /// <summary>区间：类目轴或数值轴上的一段，画成浅色的带（数值轴的范围会包含它）。</summary>
    [Parameter] public IReadOnlyList<ChartBand>? Bands { get; set; }

    // ───────── 数据（XYChart 来自参数，瀑布图由各步算出） ─────────

    /// <summary>画的行：每一行是类目轴上的一项。</summary>
    private protected abstract IReadOnlyList<TItem> Data { get; }

    /// <summary>一行的类目：标注、区间按它的值找行。</summary>
    private protected abstract object? CategoryKey(TItem row);

    /// <summary>一行的类目的显示文字：类目轴的刻度、提示框的标题、数据表的第一列。</summary>
    private protected abstract string CategoryOf(TItem row);

    /// <summary>系列。</summary>
    private protected abstract IReadOnlyList<ChartSeries<TItem>> SeriesSource { get; }

    /// <summary>图例：null 时多于一个系列才显示。</summary>
    private protected virtual bool? ShowLegend => null;

    /// <summary>隐藏的系列（系列的 Key）。</summary>
    private protected virtual IReadOnlyList<string> Hidden => [];

    /// <summary>点了图例的一项：隐藏或恢复这个系列（下一次渲染时生效）。</summary>
    private protected virtual Task ToggleSeriesAsync(string key) => Task.CompletedTask;

    /// <summary>点了数据里的第 index 行（下钻）。</summary>
    private protected virtual Task ActivateRowAsync(int index) => Task.CompletedTask;

    // 点的是画出来的第 index 行：缩放时加上起点
    private protected sealed override Task ActivateAsync(int index) => index < Rows.Count ? ActivateRowAsync(ZoomStart + index) : Task.CompletedTask;

    // ───────── 各种图的差别（同 vue 的 XYChartKind） ─────────

    /// <summary>画出图形（坐标轴、网格、目标线、标注、十字线由这里画）。</summary>
    private protected abstract RenderFragment Marks(XYContext<TItem> context);

    /// <summary>类目占一格（柱）：图形画在格子中间、两端各留半格；否则第一个、最后一个类目贴着两端（折线、面积）。</summary>
    private protected virtual bool Band => false;

    /// <summary>横向：类目在纵轴、数值在横轴。</summary>
    private protected virtual bool Horizontal => false;

    /// <summary>十字线上给每个系列画圆点（折线图、面积图）；柱状图不画，悬停的类目铺一层浅底。</summary>
    private protected virtual bool CrosshairDots => false;

    /// <summary>加载中的骨架是一排柱（柱状图）；否则是一整块。</summary>
    private protected virtual bool SkeletonBars => false;

    /// <summary>绘图区四周的留白（px）：折线两端不贴着边缘。</summary>
    private protected virtual (double Left, double Right, double Top, double Bottom) Padding => default;

    /// <summary>数值范围按堆叠后的总和算（堆叠的柱子、面积）。</summary>
    private protected virtual bool Stacked => false;

    /// <summary>参与堆叠的系列（组合图只叠柱）。</summary>
    private protected virtual bool StacksSeries(ResolvedSeries<TItem> series) => true;

    /// <summary>百分比堆叠：各系列按它在这个类目里的占比画（加起来 100%），数值轴是百分比，提示框带上占比。</summary>
    private protected virtual bool Percent => false;

    /// <summary>第二数值轴（组合图）。</summary>
    private protected virtual ChartSecondaryAxis? Secondary => null;

    /// <summary>十字线只在这些系列上画圆点（组合图里只给线画）。</summary>
    private protected virtual bool DotsSeries(ResolvedSeries<TItem> series) => true;

    /// <summary>横轴是连续的数值（散点图）；null 时横轴是类目。</summary>
    private protected virtual ContinuousX<TItem>? Continuous => null;

    /// <summary>数值轴默认从 0 起（数据有负数时取负数）；false 时按数据自动取（散点图）。</summary>
    private protected virtual bool ZeroBaseline => true;

    /// <summary>提示框跟着离指针最近的数据点（散点图），没有十字线。</summary>
    private protected virtual bool PointTooltip => false;

    /// <summary>提示框里列的项；null 时每个显示中的系列一行（瀑布图列变动与累计）。</summary>
    private protected virtual IReadOnlyList<ChartTooltipItem>? TooltipItems(TItem row) => null;

    /// <summary>数值轴（主轴）的范围要包含的值；null 时按系列的值（堆叠时按正负各自的总和）。</summary>
    private protected virtual IEnumerable<double>? DomainValues(IReadOnlyList<TItem> rows, IReadOnlyList<ResolvedSeries<TItem>> shown) => null;

    /// <summary>没写 ValueMin 时数值轴的下限；null 时按数据自动取（瀑布图不从 0 起时从接近最低的累计处起）。</summary>
    private protected virtual double? DefaultValueMin => null;

    double? Low => ValueMin ?? DefaultValueMin;

    /// <summary>给读屏的数据表；null 时类目一列加每个系列一列。</summary>
    private protected virtual ChartTable? CustomTable() => null;

    // ───────── 文字 ─────────

    bool OnSecondary(string key) => Secondary?.Keys.Contains(key) ?? false;


    /// <summary>一个系列的值的文字：第二数值轴上的按它的格式与单位。</summary>
    private protected string SeriesText(string key, decimal? value) =>
        OnSecondary(key) ? Format(value, Secondary!.Format, Secondary.Unit) : ValueText(value);

    static readonly NumberFormat PercentTickFormat = new() { Style = NumberStyle.Percent, MaximumFractionDigits = 0 };

    string XText(TItem row) => Continuous!.Value(row) is { } x ? Continuous.Format(x) : Config.Lang.EmptyValue;

    // ───────── 系列 ─────────

    private protected IReadOnlyList<ResolvedSeries<TItem>> Resolved { get; private set; } = [];

    private protected IReadOnlyList<ResolvedSeries<TItem>> Visible { get; private set; } = [];

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        Resolved = ResolvedSeries<TItem>.Of(SeriesSource);
        var hidden = Hidden;
        Visible = [.. Resolved.Where(item => !hidden.Contains(item.Key))];
    }

    private protected override async Task ToggleLegendAsync(string key)
    {
        await ToggleSeriesAsync(key);
        OnParametersSet();
    }

    private protected override IReadOnlyCollection<string> HiddenKeys => Hidden;

    private protected override bool IsEmpty => Data.Count == 0;

    private protected override ChartTable BuildTable() => CustomTable() ?? DefaultTable();

    // 百分比堆叠时数值轴是占比：轴上方不写单位（提示框、数据表里仍带）
    private protected override string? FrameUnit => Percent ? null : Unit;

    private protected override string? FrameUnitEnd => Secondary?.Unit;

    private protected override RenderFragment? FooterContent => Zoom && Continuous is null && Data.Count > 2 ? ZoomBar : null;

    private protected override (RenderFragment Plot, string? Data)? Draw()
    {
        if (Rows.Count == 0) return null;
        var layout = ComputeLayout();
        return (PlotFragment(layout), HoverData(layout));
    }

    // 一行里显示中的系列的总和（百分比堆叠的分母；负数不计）
    double RowTotal(TItem row) => Visible.Sum(item => Math.Max((double)(item.ValueOf(row) ?? 0), 0));


    // ───────── 缩放 ─────────

    NumberRange<int>? zoomState;

    // 缩放的区间（散点图的横轴是连续的数值，不按类目缩放）
    NumberRange<int>? Range => Zoom && Continuous is null ? ZoomRange ?? zoomState : null;

    int ZoomStart => Range?.Low ?? 0;

    /// <summary>画的行：缩放时是选中的一段。</summary>
    IReadOnlyList<TItem> Rows => Range is { } range && Data.Count > 0
        ? [.. Data.Skip(Math.Clamp(range.Low, 0, Data.Count - 1)).Take(Math.Max(1, range.High - range.Low + 1))]
        : Data;

    async Task ZoomAsync(NumberRange<int>? range)
    {
        // 选满了就是全部
        if (range is { } r && r.Low <= 0 && r.High >= Data.Count - 1) range = null;
        zoomState = range;
        await ZoomRangeChanged.InvokeAsync(range);
    }

    // ───────── 布局 ─────────


    // 数值轴上约几格：每格不少于 45px（横着的轴 90px），2–6 格
    static int TickCount(double length, bool horizontal) => (int)Math.Max(2, Math.Min(6, Math.Round(length / (horizontal ? 90 : 45), MidpointRounding.AwayFromZero)));

    static double Widest(IEnumerable<string> labels) => labels.Select(label => ChartText.Width(label, TickFont)).DefaultIfEmpty(0).Max();

    sealed record Layout(
        XYContext<TItem> Context,
        IReadOnlyList<AxisTick> CategoryTicks,
        IReadOnlyList<AxisTick> ValueTicks,
        IReadOnlyList<AxisTick> SecondaryTicks);

    /// <summary>画图要的一切：比例尺、刻度、边距；随数据、属性、绘图区大小变。</summary>
    Layout ComputeLayout()
    {
        var rows = Rows;
        var horizontal = Horizontal;
        var shown = Visible;
        var primary = shown.Where(item => !OnSecondary(item.Key)).ToList();
        var secondary = shown.Where(item => OnSecondary(item.Key)).ToList();
        var lang = Config.Lang;

        // 数值轴的范围要包含的值：系列（堆叠时按正负各自的总和）、目标线、数值轴上的区间。
        // 不叠的系列里空值不算：散点图里一行通常只落在一个系列上，其余系列是空的
        double? NumberOf(ResolvedSeries<TItem> item, TItem row) => (double?)item.ValueOf(row);
        var seriesValues = DomainValues(rows, primary) ?? (Stacked
            ? rows.SelectMany(row =>
            {
                var values = primary.Select(item => NumberOf(item, row) ?? 0).ToList();
                return new[] { values.Sum(value => Math.Max(value, 0)), values.Sum(value => Math.Min(value, 0)) };
            })
            : rows.SelectMany(row => primary.Select(item => NumberOf(item, row)).OfType<double>()));
        var values = seriesValues
            .Concat((Targets ?? []).Select(target => (double)target.Value))
            .Concat((Bands ?? []).Where(band => band.Axis == ChartBandAxis.Value).SelectMany(band => new[] { ToDouble(band.From), ToDouble(band.To) }))
            .Where(double.IsFinite)
            .ToList();
        var aligned = secondary.Count > 0
            ? AxisTicks.Aligned(
                values.Concat(new[] { Low, ValueMax }.OfType<double>()),
                rows.SelectMany(row => secondary.Select(item => NumberOf(item, row) ?? 0)))
            : null;

        var valueLength = horizontal ? PlotWidth : PlotHeight;
        (double Start, double End) domain;
        IReadOnlyList<double> ticks;
        if (Percent)
        {
            domain = (0, 1);
            ticks = ChartScale.Ticks(0, 1, TickCount(valueLength, horizontal));
        }
        else if (aligned is not null)
        {
            domain = aligned.Primary.Domain;
            ticks = aligned.Primary.Ticks;
        }
        else
        {
            IEnumerable<double> withZero = ZeroBaseline ? [.. values, 0] : values;
            var low = values.Count > 0 ? withZero.Min() : 0;
            var high = values.Count > 0 ? withZero.Max() : 1;
            var (autoLow, autoHigh) = low == high ? (low - 1, high + 1) : (low, high);
            var nice = ChartScale.Nice((Low ?? autoLow, ValueMax ?? autoHigh), TickCount(valueLength, horizontal));
            domain = (Low ?? nice.Start, ValueMax ?? nice.End);
            ticks = ChartScale.Ticks(domain.Start, domain.End, TickCount(valueLength, horizontal));
        }
        var valueLabels = ticks.Select(tick => LangFormat.Number((decimal)tick, Percent ? PercentTickFormat : ValueFormat ?? NumberFormat.Default, lang)).ToList();
        var secondaryLabels = aligned is null ? [] : aligned.Secondary.Ticks.Select(tick => LangFormat.Number((decimal)tick, Secondary!.Format ?? NumberFormat.Default, lang)).ToList();

        // 连续的横轴（散点图）：按数据取范围与刻度
        var continuous = Continuous;
        var xValues = continuous is null ? [] : rows.Select(continuous.Value).OfType<double>().ToList();
        var xDomain = ChartScale.Nice(xValues.Count > 0 ? (xValues.Min(), xValues.Max()) : (0, 1), TickCount(PlotWidth, true));
        var xTicks = continuous is null ? [] : ChartScale.Ticks(xDomain.Start, xDomain.End, TickCount(PlotWidth, true));

        // 边距：放得下刻度文字
        var categoryLabels = rows.Select(CategoryOf).ToList();
        var padding = Padding;
        var bottomLabels = horizontal ? valueLabels : continuous is not null ? xTicks.Select(continuous.Format).ToList() : categoryLabels;
        double left, right, top, bottom;
        if (horizontal)
        {
            left = Math.Min(Widest(categoryLabels), PlotWidth * 0.3) + TickGap;
            right = Math.Max(8, Widest(valueLabels.TakeLast(1)) / 2);
            top = 4;
            bottom = TickFont + TickGap + 2;
        }
        else
        {
            left = Widest(valueLabels) + TickGap;
            right = aligned is not null ? Widest(secondaryLabels) + TickGap : 8;
            top = 8;
            bottom = TickFont + TickGap + 2 + (continuous?.Title is not null ? TickFont + 8 : 0);
        }
        // 横轴两端的文字居中在端点上：别伸出绘图区外
        if (!horizontal && bottomLabels.Count > 0 && !Band)
        {
            left = Math.Max(left, Widest(bottomLabels.Take(1)) / 2 - padding.Left);
            right = Math.Max(right, Widest(bottomLabels.TakeLast(1)) / 2 - padding.Right);
        }
        var plot = new PlotRect(left, top, Math.Max(0, PlotWidth - left - right), Math.Max(0, PlotHeight - top - bottom));

        // 类目轴：带（柱）每个类目一格；点（折线）首尾贴边；散点图是连续的数值
        var (cStart, cEnd) = horizontal
            ? (plot.Top + padding.Top, plot.Bottom - padding.Bottom)
            : (plot.Left + padding.Left, plot.Right - padding.Right);
        var count = rows.Count;
        var step = Band ? (cEnd - cStart) / Math.Max(count, 1) : count > 1 ? (cEnd - cStart) / (count - 1) : cEnd - cStart;
        var xScale = new LinearScale(xDomain, (cStart, cEnd));
        Func<TItem, int, double> category = continuous is not null
            ? (row, _) => xScale.Map(continuous.Value(row) ?? double.NaN)
            : Band
                ? (_, position) => cStart + step * (position + 0.5)
                : count > 1 ? (_, position) => cStart + step * position : (_, _) => (cStart + cEnd) / 2;
        var valueScale = horizontal
            ? new LinearScale(domain, (plot.Left + padding.Left, plot.Right - padding.Right))
            : new LinearScale(domain, (plot.Bottom - padding.Bottom, plot.Top + padding.Top));

        double? ValueOf(ResolvedSeries<TItem> item, TItem row)
        {
            if (NumberOf(item, row) is not { } value) return null;
            if (Percent) return RowTotal(row) is var total && total == 0 ? null : value / total;
            return aligned is not null && OnSecondary(item.Key) ? aligned.ToPrimary(value) : value;
        }
        var stacked = Stacked ? shown.Where(StacksSeries).ToList() : [];
        var segments = ChartStack.Of([.. stacked.Select(item => (IReadOnlyList<double?>)[.. rows.Select(row => ValueOf(item, row))])]);
        var stacks = stacked.Select((item, s) => (item.Key, Segments: segments[s])).ToDictionary(pair => pair.Key, pair => pair.Segments);

        // 类目刻度：放不下时隔几个写一个（横着排时按文字宽度，竖着排时按行高）
        var every = continuous is not null ? 1 : (int)Math.Max(1, Math.Ceiling((horizontal ? TickFont + 4 : Widest(categoryLabels) + 12) / Math.Max(step, 1)));
        IReadOnlyList<AxisTick> categoryTicks = continuous is not null
            ? [.. xTicks.Select(tick => new AxisTick(xScale.Map(tick), continuous.Format(tick)))]
            : [.. rows.Select((row, position) => new AxisTick(category(row, position),
                position % every != 0 ? "" : horizontal ? ChartText.Truncate(categoryLabels[position], PlotWidth * 0.3, TickFont) : categoryLabels[position]))];
        IReadOnlyList<AxisTick> valueTicks = [.. ticks.Select((tick, position) => new AxisTick(valueScale.Map(tick), valueLabels[position]))];
        IReadOnlyList<AxisTick> secondaryTicks = aligned is null ? [] : [.. aligned.Primary.Ticks.Select((tick, position) => new AxisTick(valueScale.Map(tick), secondaryLabels[position]))];

        var context = new XYContext<TItem>
        {
            Series = shown,
            Rows = rows,
            Horizontal = horizontal,
            Plot = plot,
            Step = step,
            Domain = domain,
            Category = category,
            ValueScale = valueScale,
            ValueOf = ValueOf,
            Stacks = stacks,
        };
        return new Layout(context, categoryTicks, valueTicks, secondaryTicks);
    }

    static double ToDouble(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);

    /// <summary>一个类目在类目轴上的位置（标注、区间用）：散点图是横轴的数值，其余是第几行；找不到时 null。</summary>
    private protected (int Index, double Position)? PositionOf(XYContext<TItem> context, object at)
    {
        var index = Continuous is { } continuous
            ? FindIndex(context.Rows, row => continuous.Value(row) == ToDouble(at))
            : FindIndex(context.Rows, row => Equals(CategoryKey(row), at));
        return index < 0 ? null : (index, context.Category(context.Rows[index], index));
    }

    static int FindIndex(IReadOnlyList<TItem> rows, Func<TItem, bool> match)
    {
        for (var i = 0; i < rows.Count; i++)
            if (match(rows[i])) return i;
        return -1;
    }

    // ───────── 提示框与读屏的数据 ─────────

    IReadOnlyList<ChartTooltipItem> TipItems(TItem row)
    {
        if (TooltipItems(row) is { } custom) return custom;
        var items = new List<ChartTooltipItem>();
        if (Continuous is { } continuous) items.Add(new(continuous.Label, null, XText(row)));
        // 跟着数据点的提示框只列这一行有值的系列（散点图里一行通常只落在一个系列上）
        foreach (var item in Visible)
        {
            var value = item.ValueOf(row);
            if (PointTooltip && value is null) continue;
            var total = Percent ? RowTotal(row) : 0;
            var text = SeriesText(item.Key, value);
            items.Add(new(item.Label, item.Color, Percent && value is { } v && total > 0 ? $"{text}（{ShareText((double)v, total)}）" : text));
        }
        return items;
    }

    /// <summary>
    /// 悬停要用的数据（写在绘图区的 data-chart 上，charts.js 读）：各行的类目坐标、各系列圆点的坐标、每行提示框的文字。
    /// 坐标保留三位小数。
    /// </summary>
    string HoverData(Layout layout)
    {
        var context = layout.Context;
        return HoverJson(json =>
        {
            json.WriteString("mode", PointTooltip ? "point" : CrosshairDots && !Band ? "line" : "band");
            json.WriteBoolean("horizontal", context.Horizontal);
            WriteNumbers(json, "plot", [context.Plot.Left, context.Plot.Top, context.Plot.Width, context.Plot.Height]);
            json.WriteNumber("half", SvgFormat.Round(Math.Max(context.Step / 2, 1)));
            json.WriteNumber("reach", SvgFormat.Round(Math.Max(context.Step, 8)));
            if (PointTooltip)
            {
                json.WriteNumber("pointReach", PointReach);
                json.WriteStartArray("points");
                for (var index = 0; index < context.Rows.Count; index++)
                    foreach (var item in context.Series)
                        if (context.ValueOf(item, context.Rows[index]) is { } value)
                        {
                            json.WriteStartArray();
                            json.WriteNumberValue(SvgFormat.Round(context.Category(context.Rows[index], index)));
                            json.WriteNumberValue(SvgFormat.Round(context.Value(value)));
                            json.WriteNumberValue(index);
                            json.WriteEndArray();
                        }
                json.WriteEndArray();
            }
            else
            {
                WriteNumbers(json, "centers", context.Rows.Select((row, index) => context.Category(row, index)));
                if (CrosshairDots)
                {
                    var dotted = context.Series.Where(DotsSeries).ToList();
                    json.WriteStartArray("dots");
                    for (var index = 0; index < context.Rows.Count; index++)
                    {
                        json.WriteStartArray();
                        foreach (var item in dotted)
                        {
                            if (context.Position(item, context.Rows[index], index) is { } value) json.WriteNumberValue(SvgFormat.Round(context.Value(value)));
                            else json.WriteNullValue();
                        }
                        json.WriteEndArray();
                    }
                    json.WriteEndArray();
                }
            }
            if (Tooltip) WriteTips(json, context.Rows.Select(row => (CategoryOf(row), TipItems(row))));
        });
    }

    static void WriteNumbers(Utf8JsonWriter json, string name, IEnumerable<double> values)
    {
        json.WriteStartArray(name);
        foreach (var value in values) json.WriteNumberValue(SvgFormat.Round(value));
        json.WriteEndArray();
    }

    ChartTable DefaultTable()
    {
        var lang = Config.Lang;
        var continuous = Continuous;
        List<string> columns = [lang.ChartCategory];
        if (continuous is not null) columns.Add(continuous.Label);
        foreach (var item in Resolved)
        {
            var unit = OnSecondary(item.Key) ? Secondary!.Unit : Unit;
            columns.Add(unit is null ? item.Label : $"{item.Label}（{unit}）");
        }
        var rows = Data.Select(row =>
        {
            var cells = new List<string>();
            if (continuous is not null) cells.Add(XText(row));
            foreach (var item in Resolved)
                cells.Add(item.ValueOf(row) is { } value ? LangFormat.Number(value, (OnSecondary(item.Key) ? Secondary!.Format : ValueFormat) ?? NumberFormat.Default, lang) : lang.EmptyValue);
            return new ChartTableRow(CategoryOf(row), cells);
        }).ToList();
        return new ChartTable(columns, rows);
    }

    private protected override IReadOnlyList<ChartLegendItem> LegendItems => ShowLegend ?? Resolved.Count > 1
        ? [.. Resolved.Select(item => new ChartLegendItem(item.Key, item.Label, item.Color))]
        : [];
}
