using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>
/// 读进来的表格（CSV、Excel 的一张工作表，同 vue-tsx-pro 的 SheetTable）：表头是各列的标题文字，每行是“标题 → 值”。
/// 导入（DataImport）读它；CSV 由 <see cref="Sheets.ReadCsvAsync"/> 读，不依赖别的库。
/// </summary>
/// <param name="Name">工作表名（CSV 是文件名）。</param>
/// <param name="Headers">各列的标题，按列的顺序。</param>
/// <param name="Rows">数据行：标题 → 值（文字、数字、是否、日期或 null）。</param>
public sealed record SheetTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

/// <summary><see cref="Sheets.FromMatrix"/> 的选项。</summary>
public sealed record SheetMatrixOptions
{
    /// <summary>表头占几行，默认 1：几行时同一列各行的标题用“ / ”连接，上下相同的只留一个。</summary>
    public int HeaderRows { get; init; } = 1;

    /// <summary>这些列（标题）的单元格里有换行时拆成几行（一格里写了几个零件号的报价单）；null 时不拆，<see cref="SplitAllLines"/> 时所有列都拆。</summary>
    public IReadOnlyList<string>? SplitLines { get; init; }

    /// <summary>所有列的单元格里有换行时都拆成几行。</summary>
    public bool SplitAllLines { get; init; }

    /// <summary>没有标题的列叫什么（从 0 起的列号），默认“第 N 列”。</summary>
    public Func<int, string>? Untitled { get; init; }
}

/// <summary>读表格：CSV 解析、行列矩阵转成表（同 vue-tsx-pro 的 sheet.ts）。</summary>
public static class Sheets
{
    static bool IsBlank(object? value) => value is null || (value is string text && string.IsNullOrWhiteSpace(text));

    /// <summary>
    /// 行列矩阵（合并单元格已展开）→ 表：表头可以是几行；全空的行去掉；标题重复时后面的加序号（“备注”“备注 2”）；
    /// 要拆的列里单元格有换行时拆成几行，别的列照抄。
    /// </summary>
    public static SheetTable FromMatrix(string name, IReadOnlyList<IReadOnlyList<object?>> matrix, SheetMatrixOptions? options = null)
    {
        options ??= new();
        var width = matrix.Count == 0 ? 0 : matrix.Max(static row => row.Count);
        var head = matrix.Take(options.HeaderRows).ToList();
        var seen = new Dictionary<string, int>();
        var headers = Enumerable.Range(0, width).Select(column =>
        {
            var parts = new List<string>();
            foreach (var row in head)
            {
                var text = column < row.Count && !IsBlank(row[column]) ? Conditions.ToText(row[column]).Trim() : "";
                // 合并单元格展开后上下两行相同（没分组的列跨两行）：只留一个
                if (text.Length > 0 && (parts.Count == 0 || parts[^1] != text)) parts.Add(text);
            }
            var title = parts.Count > 0 ? string.Join(" / ", parts) : options.Untitled?.Invoke(column) ?? $"第 {column + 1} 列";
            var count = seen.GetValueOrDefault(title) + 1;
            seen[title] = count;
            return count == 1 ? title : $"{title} {count}";
        }).ToList();

        bool Split(string header) => options.SplitAllLines || options.SplitLines?.Contains(header) == true;
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var cells in matrix.Skip(options.HeaderRows))
        {
            if (cells.All(IsBlank)) continue;
            // 拆行：要拆的列里最多有几行，就拆成几行
            var lines = headers.Select((header, column) =>
            {
                var value = column < cells.Count ? cells[column] : null;
                return Split(header) && value is string text && text.Contains('\n')
                    ? text.Split('\n').Select(static line => line.Trim()).Where(static line => line.Length > 0).Cast<object?>().ToList()
                    : [value];
            }).ToList();
            var count = Math.Max(1, lines.Max(static values => values.Count));
            for (var line = 0; line < count; line++)
            {
                var row = new Dictionary<string, object?>();
                for (var column = 0; column < headers.Count; column++)
                {
                    var values = lines[column];
                    row[headers[column]] = values.Count > 1 ? (line < values.Count ? values[line] : null) : values[0];
                }
                rows.Add(row);
            }
        }
        return new(name, headers, rows);
    }

    /// <summary>CSV 文字 → 行列矩阵（RFC 4180：引号里的逗号、换行、两个引号；开头的 BOM 去掉）。</summary>
    public static IReadOnlyList<IReadOnlyList<object?>> ParseCsv(string text)
    {
        var source = text.StartsWith('﻿') ? text[1..] : text;
        var rows = new List<IReadOnlyList<object?>>();
        var row = new List<object?>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < source.Length; index++)
        {
            var c = source[index];
            if (quoted)
            {
                if (c == '"' && index + 1 < source.Length && source[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c is '\n' or '\r')
            {
                if (c == '\r' && index + 1 < source.Length && source[index + 1] == '\n') index++;
                row.Add(field.ToString());
                rows.Add(row);
                row = [];
                field.Clear();
            }
            else field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>行列矩阵 → CSV 文字（RFC 4180：有逗号、引号、换行的格子加引号；数字、日期按不变区域写）。</summary>
    public static string ToCsv(IEnumerable<IReadOnlyList<object?>> matrix) =>
        string.Join("\r\n", matrix.Select(static row => string.Join(",", row.Select(static cell =>
        {
            var text = cell is null ? "" : Convert.ToString(cell, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            return text.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
        }))));

    /// <summary>
    /// 行列矩阵 → CSV 的 data URL（导入模板的下载链接用）：开头加 BOM，Excel 打开时中文才不乱码。
    /// </summary>
    public static string CsvDataUrl(IEnumerable<IReadOnlyList<object?>> matrix) =>
        "data:text/csv;charset=utf-8," + Uri.EscapeDataString("﻿" + ToCsv(matrix));

    /// <summary>读 CSV 文件（UTF-8）：一张表，名字是文件名（去掉扩展名）。</summary>
    /// <param name="file">选到的文件。</param>
    /// <param name="maxSize">最大字节数，默认 10 MB（Blazor 默认只让读 512 KB）。</param>
    /// <param name="options">表头行数、拆行。</param>
    public static async Task<IReadOnlyList<SheetTable>> ReadCsvAsync(IBrowserFile file, long maxSize = 10 * 1024 * 1024, SheetMatrixOptions? options = null)
    {
        await using var stream = file.OpenReadStream(maxSize);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        var name = Path.GetFileNameWithoutExtension(file.Name);
        return [FromMatrix(string.IsNullOrEmpty(name) ? "CSV" : name, ParseCsv(await reader.ReadToEndAsync()), options)];
    }
}
