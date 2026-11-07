namespace Rune.UI;

/// <summary>透视的聚合方式（同 vue-tsx-pro 的 PivotAggregates）。</summary>
public enum PivotAggregate
{
    /// <summary>求和（默认，decimal 精确相加）。</summary>
    Sum,

    /// <summary>明细的条数（不需要数值字段）。</summary>
    Count,

    /// <summary>平均。</summary>
    Average,

    /// <summary>最小。</summary>
    Min,

    /// <summary>最大。</summary>
    Max,
}

/// <summary>透视的一个维度（行或列）：取哪个值、表头、显示与顺序。</summary>
/// <typeparam name="T">明细的类型。</typeparam>
/// <param name="Key">标识。</param>
/// <param name="Header">表头（行维度是这一列的表头；列维度是说明）。</param>
/// <param name="Value">取值。</param>
public sealed record PivotDimension<T>(string Key, string Header, Func<T, object?> Value)
{
    /// <summary>显示的文字；不写时是值本身。</summary>
    public Func<string, string>? Format { get; init; }

    /// <summary>顺序：按这个排，没列出的排在后面；不写时按值排序（数字按大小，文字按语言）。</summary>
    public IReadOnlyList<string>? Order { get; init; }
}

/// <summary>透视聚合的数值。</summary>
/// <typeparam name="T">明细的类型。</typeparam>
/// <param name="Key">标识。</param>
/// <param name="Header">名称：只有行维度时是数值列的表头。</param>
/// <param name="Value">取值。</param>
public sealed record PivotValue<T>(string Key, string Header, Func<T, object?> Value)
{
    /// <summary>值的类型（同表格的列），默认 Number。</summary>
    public ColumnType Type { get; init; } = ColumnType.Number;

    /// <summary>数字的格式（同表格的列）。</summary>
    public NumberFormat? Format { get; init; }
}

/// <summary>透视的结果（<see cref="Pivot.Compute"/>）：行、列与每格的值；要自己画（导出、图表）时直接用。</summary>
/// <param name="RowKeys">各行的维度值（每级一个），按顺序。</param>
/// <param name="ColumnKeys">各列的值，按顺序；没有列维度时是一个空字符串。</param>
/// <param name="Cells">每格的值：键是（行的维度值连起来，列）。</param>
/// <param name="RowTotals">每行的合计（这一行全部明细的聚合）。</param>
/// <param name="ColumnTotals">每列的合计。</param>
/// <param name="GrandTotal">总计。</param>
/// <param name="Extent">各格（不含合计）里最小、最大的值：热力图按它定颜色深浅；没有值时 null。</param>
public sealed record PivotResult(
    IReadOnlyList<IReadOnlyList<string>> RowKeys,
    IReadOnlyList<string> ColumnKeys,
    IReadOnlyDictionary<(string Row, string Column), decimal?> Cells,
    IReadOnlyDictionary<string, decimal?> RowTotals,
    IReadOnlyDictionary<string, decimal?> ColumnTotals,
    decimal? GrandTotal,
    (decimal Low, decimal High)? Extent)
{
    /// <summary>一格的值：没有明细（或明细里都没有数值）时 null。</summary>
    public decimal? Cell(IReadOnlyList<string> row, string column) => Cells.GetValueOrDefault((Pivot.RowId(row), column));

    /// <summary>一行的合计。</summary>
    public decimal? RowTotal(IReadOnlyList<string> row) => RowTotals.GetValueOrDefault(Pivot.RowId(row));
}

/// <summary>
/// 透视（同 vue-tsx-pro 的 pivot）：把明细按行维度（可以多级）与列维度分组，每格聚合一个数值，带行合计、列合计与总计。
/// PivotTable 用它；数值统一按 decimal 计算，平均是和除以个数。
/// </summary>
public static class Pivot
{
    const char Separator = '\u0000';

    /// <summary>按行维度、列维度分组聚合；columns 为 null 时只按行分组（一列）；value 在 Count 时可以为 null。</summary>
    public static PivotResult Compute<T>(IReadOnlyList<T> data, IReadOnlyList<PivotDimension<T>> rows, PivotDimension<T>? columns, PivotValue<T>? value,
        PivotAggregate aggregate, string culture)
    {
        var groups = new Dictionary<(string, string), List<T>>();
        var byRow = new Dictionary<string, List<T>>();
        var byColumn = new Dictionary<string, List<T>>();
        foreach (var item in data)
        {
            var rowId = RowId([.. rows.Select(dimension => Text(dimension.Value(item)))]);
            var column = columns is null ? "" : Text(columns.Value(item));
            Add(groups, (rowId, column), item);
            Add(byRow, rowId, item);
            Add(byColumn, column, item);
        }
        var compare = Collation.Comparer(culture);
        Comparison<string> byValue = (a, b) => compare(Numeric(a) ?? (object)a, Numeric(b) ?? (object)b);
        // 行：逐级排序（同一上级下的下级按 Order 或按值）
        var rowKeys = byRow.Keys.Select(static key => (IReadOnlyList<string>)key.Split(Separator)).ToList();
        rowKeys.Sort((a, b) =>
        {
            for (var level = 0; level < rows.Count; level++)
            {
                if (a[level] == b[level]) continue;
                return Ordered(a[level], b[level], rows[level].Order, byValue);
            }
            return 0;
        });
        var columnKeys = byColumn.Keys.ToList();
        columnKeys.Sort((a, b) => Ordered(a, b, columns?.Order, byValue));

        var cells = new Dictionary<(string, string), decimal?>();
        var values = new List<decimal>();
        foreach (var row in rowKeys)
        {
            var rowId = RowId(row);
            foreach (var column in columnKeys)
            {
                var result = Aggregate(groups.GetValueOrDefault((rowId, column)) ?? [], value, aggregate);
                cells[(rowId, column)] = result;
                if (result is { } number) values.Add(number);
            }
        }
        return new(
            rowKeys,
            columnKeys,
            cells,
            byRow.ToDictionary(static pair => pair.Key, pair => Aggregate(pair.Value, value, aggregate)),
            byColumn.ToDictionary(static pair => pair.Key, pair => Aggregate(pair.Value, value, aggregate)),
            Aggregate(data, value, aggregate),
            values.Count == 0 ? null : (values.Min(), values.Max()));
    }

    internal static string RowId(IReadOnlyList<string> row) => string.Join(Separator, row);

    static void Add<TKey, TItem>(Dictionary<TKey, List<TItem>> map, TKey key, TItem item) where TKey : notnull
    {
        if (map.TryGetValue(key, out var list)) list.Add(item);
        else map[key] = [item];
    }

    // 按 order 排，没列出的按值排在后面
    static int Ordered(string a, string b, IReadOnlyList<string>? order, Comparison<string> byValue)
    {
        var ra = order is null ? -1 : IndexOf(order, a);
        var rb = order is null ? -1 : IndexOf(order, b);
        if (ra >= 0 || rb >= 0) return (ra < 0 ? int.MaxValue : ra).CompareTo(rb < 0 ? int.MaxValue : rb);
        return byValue(a, b);
    }

    static int IndexOf(IReadOnlyList<string> list, string value)
    {
        for (var at = 0; at < list.Count; at++)
        {
            if (list[at] == value) return at;
        }
        return -1;
    }

    // 一组明细的聚合；没有数值时为 null
    static decimal? Aggregate<T>(IReadOnlyList<T> items, PivotValue<T>? value, PivotAggregate aggregate)
    {
        if (aggregate == PivotAggregate.Count) return items.Count;
        if (value is null || items.Count == 0) return null;
        var numbers = items.Select(item => Numeric(value.Value(item))).OfType<decimal>().ToList();
        if (numbers.Count == 0) return null;
        return aggregate switch
        {
            PivotAggregate.Average => numbers.Sum() / numbers.Count,
            PivotAggregate.Min => numbers.Min(),
            PivotAggregate.Max => numbers.Max(),
            _ => numbers.Sum(),
        };
    }

    // 维度值的文字：空值是空字符串
    static string Text(object? value) => value is null ? "" : DataTableColumn<object>.FilterText(value);

    static decimal? Numeric(object? value) => value switch
    {
        null => null,
        decimal number => number,
        string text => decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null,
        IConvertible convertible when value is int or long or short or byte or double or float => convertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture),
        _ => null,
    };
}

/// <summary>透视表的一行（交给 ColumnTable）：行维度的值。</summary>
/// <param name="Key">各级维度值连起来的标识。</param>
/// <param name="Dimensions">各级行维度的值。</param>
public sealed record PivotRow(string Key, IReadOnlyList<string> Dimensions);
