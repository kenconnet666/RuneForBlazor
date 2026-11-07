using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 散点图（同 vue-tsx-pro 的 ScatterChart）：横轴、纵轴都是数值，每一行在每个系列上画一个点；<see cref="SizeBy"/> 时点的大小随它变（气泡图）。
/// 悬停离指针最近的点，提示框列出这一行的名称、横轴的值与各系列的值。
/// <code>&lt;ScatterChart Items="cities" Category="c => c.Name" X="c => c.Gdp" XLabel="人均 GDP" Series="life" /&gt;</code>
/// 纵轴不强制从 0 起（按数据取）；图例、单位、加载与空状态、给读屏的数据表同其他图表（见 <see cref="XYChartBase{TItem}"/>）。
/// </summary>
public sealed class ScatterChart<TItem> : XYChart<TItem>
{
    // 气泡图的直径范围（px）
    const double BubbleMin = 6;
    const double BubbleMax = 32;

    static readonly Attrs PointLayer = new() { Sx = ScatterStyles.Point };

    /// <summary>横轴的数值。</summary>
    [Parameter, EditorRequired] public Func<TItem, decimal?> X { get; set; } = null!;

    /// <summary>横轴的名字：显示在横轴下方，提示框与数据表里也用；不写时轴下不显示，提示框与数据表写“数值”。</summary>
    [Parameter] public string? XLabel { get; set; }

    /// <summary>横轴数值的格式。</summary>
    [Parameter] public NumberFormat? XFormat { get; set; }

    /// <summary>点的直径（px），默认 9。</summary>
    [Parameter] public double Size { get; set; } = 9;

    /// <summary>气泡图：点的面积按这个值在 6–32px 的直径之间缩放；写了它时 <see cref="Size"/> 不起作用。</summary>
    [Parameter] public Func<TItem, decimal?>? SizeBy { get; set; }

    ContinuousX<TItem>? continuous;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        continuous = new ContinuousX<TItem>(row => (double?)X(row), value => LangFormat.Number((decimal)value, XFormat ?? NumberFormat.Default, Config.Lang),
            XLabel ?? Config.Lang.ChartValue, XLabel);
        base.OnParametersSet();
    }

    private protected override ContinuousX<TItem>? Continuous => continuous;
    private protected override bool ZeroBaseline => false;
    private protected override bool PointTooltip => true;

    // 留出半个最大气泡的边：贴边的点不被裁掉
    private protected override (double Left, double Right, double Top, double Bottom) Padding => (18, 18, 18, 8);

    // 点的半径：固定直径，或按字段的值让面积成比例
    Func<TItem, double> RadiusOf(IReadOnlyList<TItem> rows)
    {
        if (SizeBy is not { } sizeBy) return _ => Size / 2;
        var sizes = rows.Select(row => (double)(sizeBy(row) ?? 0)).ToList();
        var (low, high) = (sizes.DefaultIfEmpty(0).Min(), sizes.DefaultIfEmpty(0).Max());
        return row =>
        {
            var value = (double)(sizeBy(row) ?? 0);
            var ratio = high == low ? 1 : Math.Sqrt((value - low) / (high - low));
            return (BubbleMin + ratio * (BubbleMax - BubbleMin)) / 2;
        };
    }

    private protected override RenderFragment Marks(XYContext<TItem> context) => builder =>
    {
        var radiusOf = RadiusOf(context.Rows);
        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "data-part", "points");
        foreach (var item in context.Series)
        {
            for (var index = 0; index < context.Rows.Count; index++)
            {
                var row = context.Rows[index];
                var cx = context.Category(row, index);
                if (context.ValueOf(item, row) is not { } value || double.IsNaN(cx)) continue;
                builder.OpenElement(2, "circle");
                builder.SetKey($"{item.Key}:{index}");
                builder.AddAttribute(3, "data-part", "point");
                builder.AddAttribute(4, "data-index", index);
                builder.AddAttribute(5, "cx", SvgFormat.Number(cx));
                builder.AddAttribute(6, "cy", SvgFormat.Number(context.Value(value)));
                builder.AddAttribute(7, "r", SvgFormat.Number(radiusOf(row)));
                builder.AddMultipleAttributes(8, Attrs.Merge(PointLayer, item.ColorLayer));
                builder.CloseElement();
            }
        }
        builder.CloseElement();
    };
}

/// <summary>散点图的样式：气泡互相重叠时用表面色描一圈边分开；悬停的那一行（charts.js 写 data-active）描粗一点。</summary>
internal static class ScatterStyles
{
    internal static Style Point => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 1,
        FillOpacity = 0.85,
        TransformBox = TransformBox.FillBox,
        TransformOrigin = TransformOrigin.Center,
        Scale = "1",
        [When.Starting] = new() { Scale = "0" },
        Transition = $"scale {Durations.Lg} {Easings.Standard}, stroke-width {Durations.Sm} {Easings.Standard}",
        PointerEvents = PointerEvents.None,
        [When.Attribute("[data-active]")] = new() { StrokeWidth = 2.5, FillOpacity = 1 },
    });
}
