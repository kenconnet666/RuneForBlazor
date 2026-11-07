using System.Globalization;

namespace Rune.UI;

/// <summary>固定列的偏移（px，按列的 key）与挨着滚动列的那一列（画分隔线）。</summary>
sealed record StickyOffsets(IReadOnlyDictionary<string, double> Start, IReadOnlyDictionary<string, double> End, string? StartEdge, string? EndEdge);

/// <summary>显示中的一列：列菜单的固定、拖动后的宽度盖过列定义；值类型按数据推断好。</summary>
/// <param name="Column">列。</param>
/// <param name="Sticky">固定在哪一侧（列菜单改过的）。</param>
/// <param name="Width">宽度（px，拖动调整过的）；没有时与其他没写宽度的列平分。</param>
/// <param name="Type">值类型。</param>
sealed record ColumnView<TItem>(DataTableColumn<TItem> Column, ColumnSticky? Sticky, double? Width, ColumnType Type)
{
    public string Key => Column.ColumnKey;

    /// <summary>对齐：写了 Align 用它；状态列靠起始边；数字类靠结束边方便对比位数。</summary>
    public TableAlign Align => Column.Align ?? (Column.HasStatuses || !ColumnDisplay.IsNumeric(Type) ? TableAlign.Start : TableAlign.End);

    public double MinWidth => Column.MinWidth ?? ColumnDisplay.FlexMinWidth;
}

/// <summary>
/// 列的计算（同 vue-tsx-pro 的 columns.ts、column-display.tsx）：值类型、默认的格式、合计、排序、筛选、列的排列，固定列的偏移与表格的最小宽度。
/// </summary>
static class ColumnDisplay
{
    /// <summary>没写宽度的列，计算表格最小宽度与固定列偏移时按这么宽算（px）。</summary>
    public const double FlexMinWidth = 120;

    /// <summary>列的值类型：写了 Type 用它；写了数字格式或合计是 Sum 的是数字列；否则看第一个非空值。</summary>
    public static ColumnType TypeOf<TItem>(DataTableColumn<TItem> column, IEnumerable<TItem> items)
    {
        if (column.Type is { } type) return type;
        if (column.Format is not null || column.Summary == ColumnSummary.Sum) return ColumnType.Number;
        foreach (var item in items)
        {
            var value = column.ValueOf(item);
            if (value is null) continue;
            if (IsNumber(value)) return ColumnType.Number;
            return value is DateOnly or DateTime or DateTimeOffset ? ColumnType.Date : ColumnType.Text;
        }
        return ColumnType.Text;
    }

    /// <summary>数字类：默认靠结束边对齐，排序时十进制字符串按数值。</summary>
    public static bool IsNumeric(ColumnType type) => type is ColumnType.Number or ColumnType.Currency or ColumnType.Percent or ColumnType.FileSize;

    /// <summary>
    /// 列的排列（列菜单、列设置）：order 是列 key 的顺序，没列到的按定义的顺序排在后面；pins 盖过列的 Sticky（null 取消固定）。
    /// 固定在起始边的排到最前、结束边的排到最后：固定列要挨着表格的边。隐藏的列不在结果里。
    /// </summary>
    public static IReadOnlyList<ColumnView<TItem>> Arrange<TItem>(
        IReadOnlyList<DataTableColumn<TItem>> columns, IReadOnlyList<TItem> items, IReadOnlyList<string> order,
        IReadOnlyDictionary<string, ColumnSticky?> pins, IReadOnlyDictionary<string, double> widths, IReadOnlyCollection<string> hidden)
    {
        var rank = order.Select((key, index) => (key, index)).ToDictionary(static pair => pair.key, static pair => pair.index);
        var arranged = columns
            .Select((column, index) => (Column: column, Position: rank.TryGetValue(column.ColumnKey, out var at) ? at : rank.Count + index))
            .OrderBy(static entry => entry.Position)
            .Where(entry => !hidden.Contains(entry.Column.ColumnKey))
            .Select(entry => new ColumnView<TItem>(
                entry.Column,
                pins.TryGetValue(entry.Column.ColumnKey, out var pin) ? pin : entry.Column.Sticky,
                widths.TryGetValue(entry.Column.ColumnKey, out var width) ? width : entry.Column.Width,
                TypeOf(entry.Column, items)))
            .ToList();
        return [.. arranged.Where(static view => view.Sticky == ColumnSticky.Start), .. arranged.Where(static view => view.Sticky is null), .. arranged.Where(static view => view.Sticky == ColumnSticky.End)];
    }

    /// <summary>没写内容时单元格的文字：按值类型与语言格式化；值与类型对不上（数字列里的文字）时原样显示。</summary>
    public static string Format(object value, ColumnType type, NumberFormat? format, Lang lang)
    {
        var number = Decimal(value);
        switch (type)
        {
            case ColumnType.Number when number is { } n:
                return LangFormat.Number(n, format ?? NumberFormat.Default, lang);
            case ColumnType.Currency when number is { } n:
                return LangFormat.Number(n, (format ?? new NumberFormat()) with { Style = NumberStyle.Currency }, lang);
            case ColumnType.Percent when number is { } n:
                return LangFormat.Number(n, format ?? new NumberFormat { Style = NumberStyle.Percent, MaximumFractionDigits = 1 }, lang);
            case ColumnType.FileSize when number is { } n:
                return LangFormat.FileSize((long)n, lang.Code);
            case ColumnType.Date or ColumnType.DateTime when value is DateOnly or DateTime or DateTimeOffset:
                return LangFormat.Date(value, lang.Code, type == ColumnType.DateTime);
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }
    }

    /// <summary>一列数值的合计（decimal，不经过浮点）：跳过空值与不是数字的值；没有任何数值时为 null。</summary>
    public static decimal? Sum<TItem>(DataTableColumn<TItem> column, IEnumerable<TItem> items)
    {
        decimal? total = null;
        foreach (var item in items)
        {
            if (Decimal(column.ValueOf(item)) is { } value) total = (total ?? 0) + value;
        }
        return total;
    }

    /// <summary>按当前排序排列（返回新的列表）；排序的列不存在时跳过它，都不存在时原样返回。</summary>
    public static IReadOnlyList<TItem> Sort<TItem>(IReadOnlyList<TItem> items, IReadOnlyList<DataTableColumn<TItem>> columns, IReadOnlyList<ColumnSort> sorts, string culture)
    {
        var keys = new List<(Func<TItem, object?>, SortDirection)>();
        foreach (var sort in sorts)
        {
            if (columns.FirstOrDefault(column => column.ColumnKey == sort.Key) is not { } column) continue;
            var numeric = IsNumeric(TypeOf(column, items));
            keys.Add((item => SortValue(column.ValueOf(item), numeric), sort.Direction));
        }
        return Collation.SortBy(items, keys, culture);
    }

    // 数字类的列里，十进制字符串（"2.5"、"2.10"）按数值排：按文字比较时 "2.5" 在 "2.10" 前
    static object? SortValue(object? value, bool numeric) => numeric && value is string text && Decimal(text) is { } number ? number : value;

    /// <summary>按表头筛选留下的行：filters 是各列选中的值（关键字筛选时是 [关键字]），空的不算；没有要筛的时原样返回。</summary>
    public static IReadOnlyList<TItem> Filter<TItem>(IReadOnlyList<TItem> items, IReadOnlyList<DataTableColumn<TItem>> columns,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters, Func<string, string, bool> matches)
    {
        var tests = new List<Func<TItem, bool>>();
        foreach (var column in columns)
        {
            if (column.Filter is not { } kind || !filters.TryGetValue(column.ColumnKey, out var values) || values.Count == 0) continue;
            if (column.FilterBy is { } filterBy) tests.Add(item => filterBy(item, values));
            else if (kind == ColumnFilterKind.Keyword) tests.Add(item => matches(DataTableColumn<TItem>.FilterText(column.ValueOf(item)), values[0]));
            else
            {
                var allowed = values.ToHashSet();
                tests.Add(item => allowed.Contains(DataTableColumn<TItem>.FilterText(column.ValueOf(item))));
            }
        }
        return tests.Count == 0 ? items : [.. items.Where(item => tests.TrueForAll(test => test(item)))];
    }

    /// <summary>固定列的偏移：起始边的从 leading（勾选列的宽度）之后累加，结束边的从末尾往回累加。</summary>
    public static StickyOffsets Offsets<TItem>(IReadOnlyList<ColumnView<TItem>> columns, double leading)
    {
        var start = Accumulate(columns, ColumnSticky.Start, leading);
        var end = Accumulate(columns.Reverse(), ColumnSticky.End, 0);
        return new(start.Offsets, end.Offsets, start.Inner, end.Inner);

        static (Dictionary<string, double> Offsets, string? Inner) Accumulate(IEnumerable<ColumnView<TItem>> ordered, ColumnSticky side, double from)
        {
            var offsets = new Dictionary<string, double>();
            string? inner = null;
            var offset = from;
            foreach (var column in ordered)
            {
                if (column.Sticky != side) continue;
                offsets[column.Key] = offset;
                inner = column.Key;
                offset += column.Width ?? FlexMinWidth;
            }
            return (offsets, inner);
        }
    }

    /// <summary>表格的最小宽度（px）：写了宽度的列按宽度，没写的按 MinWidth 或 120；放不下时横向滚动。</summary>
    public static double MinWidth<TItem>(IReadOnlyList<ColumnView<TItem>> columns, double leading) =>
        columns.Sum(static column => column.Width ?? column.MinWidth) + leading;

    /// <summary>
    /// 没写宽度的列放在 col 上的宽度。table-layout: fixed 里没写宽度的 col 一律平分，各列最小宽度不一样时，写成“自己的最小宽度 + 平分多出来的部分”
    /// （100cqi 是外层的宽度，多出来的是负数时 max 保住最小宽度）；都一样时平分就对，返回 null。
    /// </summary>
    public static string? FlexWidth<TItem>(ColumnView<TItem> column, IReadOnlyList<ColumnView<TItem>> columns, double leading)
    {
        var flexible = columns.Where(static item => item.Width is null).ToList();
        if (flexible.Select(static item => item.MinWidth).Distinct().Count() <= 1) return null;
        var min = Px(column.MinWidth);
        return $"max({min}, calc({min} + (100cqi - {Px(MinWidth(columns, leading))}) / {flexible.Count}))";
    }

    /// <summary>像素值写成 CSS 文字。</summary>
    public static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    static bool IsNumber(object value) => value is decimal or int or long or short or byte or uint or ulong or ushort or sbyte or float or double;

    // 数字与能读成数字的十进制字符串（接口常把金额给成 "19.90"）
    static decimal? Decimal(object? value) => value switch
    {
        null => null,
        decimal number => number,
        double number when double.IsFinite(number) && Math.Abs(number) < 7.9e28 => (decimal)number,
        float number when float.IsFinite(number) && Math.Abs(number) < 7.9e28f => (decimal)number,
        string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ when IsNumber(value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
        _ => null,
    };
}
