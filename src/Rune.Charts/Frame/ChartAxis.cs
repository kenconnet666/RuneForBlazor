using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>刻度文字在绘图区的哪一边。</summary>
internal enum AxisSide
{
    Left,
    Right,
    Bottom,
}

/// <summary>坐标轴上的一个刻度：位置（竖轴是纵坐标，横轴是横坐标，px）与文字（空串时只画网格线）。</summary>
internal readonly record struct AxisTick(double Position, string Label);

/// <summary>
/// 坐标轴（同 vue-tsx-pro 的 chartAxis）：刻度文字，可带网格线；直角坐标图表与时间条图共用。
/// 键是位置，更新时文字原地换；网格线落在像素中间（1px 的线不发虚）。
/// </summary>
internal static class ChartAxis
{
    // 刻度文字与绘图区的距离
    internal const double Gap = 8;

    static readonly Attrs GridLayer = new() { Sx = ChartSvgStyles.Grid };
    static readonly Attrs TickLayer = new() { Sx = ChartSvgStyles.Tick };

    static string N(double value) => SvgFormat.Number(value);

    /// <summary>1px 的横竖线落在像素中间（x.5），不糊成两像素宽。</summary>
    internal static string Crisp(double value) => N(Math.Floor(value + 0.5) + 0.5);

    public static RenderFragment Render(AxisSide side, IReadOnlyList<AxisTick> ticks, PlotRect plot, bool grid, double gap = Gap) => builder =>
    {
        builder.OpenElement(0, "g");
        if (grid)
        {
            foreach (var tick in ticks)
            {
                var p = Crisp(tick.Position);
                var bottom = side == AxisSide.Bottom;
                builder.OpenElement(1, "line");
                builder.SetKey($"g{tick.Position}");
                builder.AddAttribute(2, "x1", bottom ? p : N(plot.Left));
                builder.AddAttribute(3, "x2", bottom ? p : N(plot.Right));
                builder.AddAttribute(4, "y1", bottom ? N(plot.Top) : p);
                builder.AddAttribute(5, "y2", bottom ? N(plot.Bottom) : p);
                builder.AddMultipleAttributes(6, Attrs.Merge(GridLayer));
                builder.CloseElement();
            }
        }
        foreach (var tick in ticks)
        {
            if (tick.Label.Length == 0) continue;
            builder.AddContent(7, side == AxisSide.Bottom
                ? ChartSvg.Text(tick.Label, tick.Position, plot.Bottom + gap, TickLayer, "middle", "hanging", $"t{tick.Position}")
                : ChartSvg.Text(tick.Label, side == AxisSide.Left ? plot.Left - gap : plot.Right + gap, tick.Position, TickLayer,
                    side == AxisSide.Left ? "end" : "start", "central", $"t{tick.Position}"));
        }
        builder.CloseElement();
    };
}
