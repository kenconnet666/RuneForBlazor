namespace Rune.UI;

/// <summary>时间条图的一段：所在的行与起止（墙上时间，不换时区）。</summary>
/// <param name="Row">所在的行（任务、项目、设备）。</param>
/// <param name="Start">开始。</param>
/// <param name="End">结束。</param>
public sealed record TimelineChartItem(string Row, DateTime Start, DateTime End)
{
    /// <summary>按日期：结束含这一天（到第二天 0 点）。</summary>
    /// <param name="row">所在的行。</param>
    /// <param name="start">开始的那天。</param>
    /// <param name="end">结束的那天（含）。</param>
    public TimelineChartItem(string row, DateOnly start, DateOnly end)
        : this(row, start.ToDateTime(TimeOnly.MinValue), end.AddDays(1).ToDateTime(TimeOnly.MinValue))
    {
    }

    /// <summary>名称：提示框的标题；不写时是行名。</summary>
    public string? Label { get; init; }

    /// <summary>系列的 Key（计划、实际）：决定颜色、粗细与图例。</summary>
    public string? Series { get; init; }

    /// <summary>颜色：覆盖系列的颜色（拖期的一段标红）。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>时间条的系列。</summary>
/// <param name="Key">标识：项的 Series 指向它。</param>
/// <param name="Label">名称：图例、提示框。</param>
public sealed record TimelineChartSeries(string Key, string Label)
{
    /// <summary>颜色；不写时按顺序取色板。</summary>
    public ChartColor? Color { get; init; }

    /// <summary>画成宽而浅的轨道、在下面（计划）；默认是实心的条。</summary>
    public bool Track { get; init; }
}

/// <summary>时间轴上的一条竖线：里程碑、截止日。</summary>
/// <param name="At">时间。</param>
/// <param name="Label">文字。</param>
public sealed record TimelineChartMarker(DateTime At, string Label)
{
    /// <summary>颜色，默认次要色。</summary>
    public ChartColor? Color { get; init; }
}
