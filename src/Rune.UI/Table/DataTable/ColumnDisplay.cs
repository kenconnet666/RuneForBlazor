using System.Globalization;

namespace Rune.UI;

/// <summary>固定列的偏移（px，按列的 key）与挨着滚动列的那一列（画分隔线）。</summary>
sealed record StickyOffsets(IReadOnlyDictionary<string, double> Start, IReadOnlyDictionary<string, double> End, string? StartEdge, string? EndEdge);

/// <summary>
/// 列的计算（同 vue-tsx-pro 的 columns.ts、column-display.tsx）：值类型、对齐、默认的格式、合计、排序，固定列的偏移与表格的最小宽度。
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

    /// <summary>列的对齐：写了 Align 用它；状态列靠起始边；数字类靠结束边方便对比位数。</summary>
    public static TableAlign AlignOf<TItem>(DataTableColumn<TItem> column, ColumnType type) =>
        column.Align ?? (column.HasStatuses || !IsNumeric(type) ? TableAlign.Start : TableAlign.End);

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
            // 数字类的列里，十进制字符串（"2.5"、"2.10"）按数值排：按文字比较时 "2.5" 在 "2.10" 前
            var numeric = IsNumeric(TypeOf(column, items));
            keys.Add((item => SortValue(column.ValueOf(item), numeric), sort.Direction));
        }
        return Collation.SortBy(items, keys, culture);
    }

    static object? SortValue(object? value, bool numeric) => numeric && value is string text && Decimal(text) is { } number ? number : value;

    /// <summary>固定列的偏移：起始边的从 leading（勾选列的宽度）之后累加，结束边的从末尾往回累加。</summary>
    public static StickyOffsets Offsets<TItem>(IReadOnlyList<DataTableColumn<TItem>> columns, Func<DataTableColumn<TItem>, double?> widthOf, double leading)
    {
        var start = Accumulate(columns, ColumnSticky.Start, leading);
        var end = Accumulate(columns.Reverse(), ColumnSticky.End, 0);
        return new(start.Offsets, end.Offsets, start.Inner, end.Inner);

        (Dictionary<string, double> Offsets, string? Inner) Accumulate(IEnumerable<DataTableColumn<TItem>> ordered, ColumnSticky side, double from)
        {
            var offsets = new Dictionary<string, double>();
            string? inner = null;
            var offset = from;
            foreach (var column in ordered)
            {
                if (column.Sticky != side) continue;
                offsets[column.ColumnKey] = offset;
                inner = column.ColumnKey;
                offset += widthOf(column) ?? FlexMinWidth;
            }
            return (offsets, inner);
        }
    }

    /// <summary>表格的最小宽度（px）：写了宽度的列按宽度，没写的按 MinWidth 或 120；放不下时横向滚动。</summary>
    public static double MinWidth<TItem>(IReadOnlyList<DataTableColumn<TItem>> columns, Func<DataTableColumn<TItem>, double?> widthOf, double leading) =>
        columns.Sum(column => widthOf(column) ?? column.MinWidth ?? FlexMinWidth) + leading;

    /// <summary>
    /// 没写宽度的列放在 col 上的宽度。table-layout: fixed 里没写宽度的 col 一律平分，各列最小宽度不一样时，写成“自己的最小宽度 + 平分多出来的部分”
    /// （100cqi 是外层的宽度，多出来的是负数时 max 保住最小宽度）；都一样时平分就对，返回 null。
    /// </summary>
    public static string? FlexWidth<TItem>(DataTableColumn<TItem> column, IReadOnlyList<DataTableColumn<TItem>> columns, Func<DataTableColumn<TItem>, double?> widthOf, double leading)
    {
        var flexible = columns.Where(item => widthOf(item) is null).ToList();
        if (flexible.Select(static item => item.MinWidth ?? FlexMinWidth).Distinct().Count() <= 1) return null;
        var min = Px(column.MinWidth ?? FlexMinWidth);
        return $"max({min}, calc({min} + (100cqi - {Px(MinWidth(columns, widthOf, leading))}) / {flexible.Count}))";
    }

    /// <summary>像素值写成 CSS 文字。</summary>
    public static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    static bool IsNumber(object value) => value is decimal or int or long or short or byte or uint or ulong or ushort or sbyte or float or double;

    // 数字与能读成数字的十进制字符串（接口常把金额给成 "19.90"）
    static decimal? Decimal(object? value) => value switch
    {
        null => null,
        decimal number => number,
        double number when double.IsFinite(number) => (decimal)number,
        float number when float.IsFinite(number) => (decimal)number,
        string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ when IsNumber(value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
        _ => null,
    };
}
