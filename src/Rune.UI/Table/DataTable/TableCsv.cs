using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Rune.UI;

/// <summary><see cref="DataTable{TItem}.ToCsv"/> 的选项。</summary>
public sealed record CsvOptions
{
    /// <summary>开头加 BOM（U+FEFF），默认加：Excel 按 UTF-8 读，中文不乱码。</summary>
    public bool Bom { get; init; } = true;

    /// <summary>导出原值，不按列的格式格式化（数值不带千分位、日期是 ISO 8601），便于表格软件再计算。</summary>
    public bool Raw { get; init; }

    /// <summary>只导出这几列（列的 key，按这个顺序）；不写时是显示中的、有值的列（去掉没有 Value 的 TemplateColumn，如操作列）。</summary>
    public IReadOnlyList<string>? Columns { get; init; }
}

/// <summary>
/// 按列把行写成 CSV（RFC 4180，同 vue-tsx-pro 的 tableToCsv）：表头是列的标题（分组表头写成“组标题 / 列标题”），
/// 单元格是表格里看到的文字（状态写名字，数字、金额、日期按列的类型与格式）；文字（含状态名、数字与日期列里的字符串）以 = + - @ 开头时前面加 '，打开时不会被当成公式执行（CSV 注入）。
/// </summary>
static partial class TableCsv
{
    public static string Write<TItem>(IReadOnlyList<TItem> items, IReadOnlyList<ColumnView<TItem>> columns, Lang lang, CsvOptions options)
    {
        var text = new StringBuilder();
        if (options.Bom) text.Append('﻿');
        Line(text, columns.Select(view => GuardFormula(view.Column.Group is { } group ? $"{group} / {view.Column.Label}" : view.Column.Label)));
        foreach (var item in items)
        {
            text.Append("\r\n");
            Line(text, columns.Select(view => Cell(view, item, lang, options.Raw)));
        }
        return text.ToString();
    }

    static string Cell<TItem>(ColumnView<TItem> view, TItem item, Lang lang, bool raw)
    {
        var value = view.Column.ValueOf(item);
        if (value is null) return "";
        // 状态名、没登记的状态值都是文字：同样防公式
        if (view.Column.HasStatuses) return GuardFormula(view.Column.StatusLabel(value) ?? DataTableColumn<TItem>.FilterText(value));
        if (raw)
        {
            return value switch
            {
                DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTime time => time.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset offset => offset.ToString("O", CultureInfo.InvariantCulture),
                IFormattable formattable when value is not Enum => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => GuardFormula(DataTableColumn<TItem>.FilterText(value)),
            };
        }
        var shown = ColumnDisplay.Format(value, view.Type, view.Column.Format, lang);
        // 真正的数值、日期是格式化出来的，不会是公式（负数照常）；数字、日期列里混进来的字符串照样要防
        return view.Type == ColumnType.Text || value is string ? GuardFormula(shown) : shown;
    }

    static void Line(StringBuilder text, IEnumerable<string> fields) => text.AppendJoin(',', fields.Select(Field));

    // 一格：含逗号、引号、换行时加引号（引号写两遍）
    static string Field(string text) => text.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;

    static string GuardFormula(string text) => FormulaStart().IsMatch(text) && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _) ? $"'{text}" : text;

    [GeneratedRegex(@"^[=+\-@\t\r]")]
    private static partial Regex FormulaStart();
}
