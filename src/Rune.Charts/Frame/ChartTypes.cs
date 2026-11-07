using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 一个系列（同 vue-tsx-pro 的 ChartSeries）：数据行里的一个数值画成一组柱子、一条线或一片面积。
/// <code>new ChartSeries&lt;Month&gt;("actual", "实际销售", m => m.Actual) { Color = Tone.Success }</code>
/// </summary>
/// <param name="Key">标识：隐藏的系列、标注圈出的系列按它找。</param>
/// <param name="Label">名称：图例、提示框、读屏的数据表里显示。</param>
/// <param name="Value">取值；null 是空值（线在这里断开，不当成 0）。金额用 decimal 不经过浮点，提示框、数据表按原值格式化。</param>
public record ChartSeries<TItem>(string Key, string Label, Func<TItem, decimal?> Value)
{
    /// <summary>颜色；不写时按系列的顺序取色板（第 1 个系列跟随品牌色）。</summary>
    public ChartColor? Color { get; init; }
}

/// <summary>图例里的一项。</summary>
/// <param name="Key">系列（或扇区）的标识。</param>
/// <param name="Label">显示的名字。</param>
/// <param name="Color">CSS 颜色。</param>
public sealed record ChartLegendItem(string Key, string Label, CssColor Color);

/// <summary>给读屏的数据表（也是导出 CSV 的内容）：列名与每一行。</summary>
/// <param name="Columns">列名：第一列是行标题的列。</param>
/// <param name="Rows">每一行：行标题加其余各列的文字。</param>
public sealed record ChartTable(IReadOnlyList<string> Columns, IReadOnlyList<ChartTableRow> Rows);

/// <summary>数据表的一行。</summary>
/// <param name="Header">行标题。</param>
/// <param name="Cells">其余各列的文字。</param>
public sealed record ChartTableRow(string Header, IReadOnlyList<string> Cells);

/// <summary>提示框的一行。</summary>
/// <param name="Label">系列或数值的名字。</param>
/// <param name="Color">色块的 CSS 颜色；null 时不画色块。</param>
/// <param name="Value">格式化好的数值。</param>
public sealed record ChartTooltipItem(string Label, CssColor? Color, string Value);

/// <summary>点了图里的一项（下钻）：这一行与它在数据里的位置。</summary>
/// <param name="Item">数据行。</param>
/// <param name="Index">在数据里的位置。</param>
public readonly record struct ChartItemClick<TItem>(TItem Item, int Index);

/// <summary>绘图区里画图形的矩形（SVG 坐标，px）。</summary>
/// <param name="Left">左。</param>
/// <param name="Top">上。</param>
/// <param name="Width">宽。</param>
/// <param name="Height">高。</param>
public readonly record struct PlotRect(double Left, double Top, double Width, double Height)
{
    /// <summary>右沿。</summary>
    public double Right => Left + Width;

    /// <summary>下沿。</summary>
    public double Bottom => Top + Height;
}
