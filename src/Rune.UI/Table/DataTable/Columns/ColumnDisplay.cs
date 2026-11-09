using System.Globalization;
using Microsoft.AspNetCore.Components;

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

/// <summary>表头第一行的一格：一个分组（跨组里的几列），或没有分组的一列（跨两行）。</summary>
/// <param name="Group">组标题；没有分组的一列时为 null。</param>
/// <param name="Columns">这一格下面的列（没有分组时是一列）。</param>
sealed record HeaderGroup<TItem>(string? Group, IReadOnlyList<ColumnView<TItem>> Columns);

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
            if (Numeric.IsNumber(value)) return ColumnType.Number;
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

    /// <summary>按分组排出表头第一行：相邻、组标题相同的列合成一格；没有任何分组时返回 null（表头只有一行）。</summary>
    public static IReadOnlyList<HeaderGroup<TItem>>? HeaderLayout<TItem>(IReadOnlyList<ColumnView<TItem>> columns)
    {
        if (!columns.Any(static view => view.Column.Group is not null)) return null;
        var cells = new List<(string? Group, List<ColumnView<TItem>> Columns)>();
        foreach (var view in columns)
        {
            var group = view.Column.Group;
            if (group is not null && cells.Count > 0 && cells[^1].Group == group) cells[^1].Columns.Add(view);
            else cells.Add((group, [view]));
        }
        return [.. cells.Select(static cell => new HeaderGroup<TItem>(cell.Group, cell.Columns))];
    }

    /// <summary>没写内容时单元格的文字：按值类型与语言格式化；值与类型对不上（数字列里的文字）时原样显示。</summary>
    public static string Format(object value, ColumnType type, NumberFormat? format, Lang lang)
    {
        var number = Numeric.Of(value, text: true);
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
                return LangFormat.Date(value, lang, type == ColumnType.DateTime);
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }
    }

    /// <summary>
    /// 一格的显示（DataTable、ColumnTable 共用）：列自己写的内容；没有时空值显示列的 EmptyText（默认语言包的 —），
    /// 状态显示成 StatusBadge，其余按类型与格式。
    /// </summary>
    public static RenderFragment Cell<TItem>(DataTableColumn<TItem> column, TItem item, ColumnType type, Lang lang)
    {
        if (column.CellContent(item) is { } content) return content;
        var value = column.ValueOf(item);
        if (value is null || value is string { Length: 0 }) return builder => builder.AddContent(0, column.EmptyText ?? lang.EmptyValue);
        if (column.StatusContent(value) is { } status) return status;
        var text = Format(value, type, column.Format, lang);
        return builder => builder.AddContent(0, text);
    }

    /// <summary>合计行里一列的内容：SummaryContent，或 Sum 精确相加按列的格式显示；第一列没写合计时是“合计”。</summary>
    public static RenderFragment? Summary<TItem>(DataTableColumn<TItem> column, int position, ColumnType type, IReadOnlyList<TItem> items, string? label, Lang lang)
    {
        if (column.SummaryContent is { } content) return content(items);
        if (column.Summary == ColumnSummary.Sum)
        {
            var text = Sum(column, items) is { } total ? Format(total, type, column.Format, lang) : column.EmptyText ?? lang.EmptyValue;
            return builder => builder.AddContent(0, text);
        }
        if (position == 0) return builder => builder.AddContent(0, label ?? lang.TableSummary);
        return null;
    }

    /// <summary>一列数值的合计（decimal，不经过浮点）：跳过空值与不是数字的值；没有任何数值时为 null。</summary>
    public static decimal? Sum<TItem>(DataTableColumn<TItem> column, IEnumerable<TItem> items)
    {
        decimal? total = null;
        foreach (var item in items)
        {
            if (Numeric.Of(column.ValueOf(item), text: true) is { } value) total = (total ?? 0) + value;
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
    static object? SortValue(object? value, bool numeric) => numeric && value is string text && Numeric.Of(text, text: true) is { } number ? number : value;

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
}

/// <summary>不渲染的一段列 [From, To)：用一格 colspan 占住它们的位置（colgroup 里的列都在，宽度不变）。</summary>
/// <param name="From">第一列的下标。</param>
/// <param name="To">最后一列的下一列的下标。</param>
readonly record struct ColumnSkip(int From, int To)
{
    /// <summary>跨几列。</summary>
    public int Span => To - From;
}

/// <summary>列虚拟化（同 vue-tsx-pro 的 columnSkips）：只渲染看得见的列，前后各多几列。</summary>
static class ColumnWindow
{
    /// <summary>没写宽度的列按这个宽度（px）：宽度要确定，才算得出哪些列看得见。</summary>
    public const double DefaultWidth = 150;

    /// <summary>挂载前（服务端、浏览器第一次渲染）按这个视口宽度算，两边一致。</summary>
    public const double InitialViewport = 1280;

    /// <summary>
    /// 要跳过的几段（最多两段）。两端的固定列（开头的 start、末尾的 end）总是渲染。
    /// <paramref name="offset"/> 是视口起始边在表格里的位置（scrollLeft 的绝对值），<paramref name="leading"/> 是数据列前面的宽度（勾选列、详情列）。
    /// </summary>
    public static IReadOnlyList<ColumnSkip> Skips<TItem>(IReadOnlyList<ColumnView<TItem>> columns, double offset, double width, double leading, int overscan = 3)
    {
        var lead = 0;
        while (lead < columns.Count && columns[lead].Sticky == ColumnSticky.Start) lead++;
        var trail = columns.Count;
        while (trail > lead && columns[trail - 1].Sticky == ColumnSticky.End) trail--;
        var position = leading;
        for (var index = 0; index < lead; index++) position += WidthOf(columns[index]);
        int first = -1, last = -1;
        for (var index = lead; index < trail; index++)
        {
            var size = WidthOf(columns[index]);
            if (first < 0 && position + size > offset) first = index;
            if (position < offset + width) last = index;
            position += size;
        }
        if (first < 0) first = trail - 1;
        if (last < first) last = first;
        first = Math.Max(lead, first - overscan);
        last = Math.Min(trail - 1, last + overscan);
        var skips = new List<ColumnSkip>(2);
        if (first > lead) skips.Add(new(lead, first));
        if (last + 1 < trail) skips.Add(new(last + 1, trail));
        return skips;
    }

    static double WidthOf<TItem>(ColumnView<TItem> column) => column.Width ?? DefaultWidth;

    /// <summary>按顺序走一遍列：要渲染的列给出下标，跳过的一段给出那一段（只给一次）。</summary>
    public static IEnumerable<(int Column, ColumnSkip? Skip)> Walk(int count, IReadOnlyList<ColumnSkip> skips)
    {
        var column = 0;
        while (column < count)
        {
            ColumnSkip? skip = null;
            foreach (var candidate in skips)
                if (candidate.From == column && candidate.Span > 0) skip = candidate;
            yield return (column, skip);
            column = skip?.To ?? column + 1;
        }
    }
}

/// <summary>列虚拟化时表头第一行的一格：分组里露出来的几列、没有分组的一列，或跳过的一段（跨两行的占位）。</summary>
/// <param name="Position">在原来的表头布局里是第几格（占位时是它后面那一格的）。</param>
/// <param name="Cell">露出来的组或列；占位时为 null。</param>
/// <param name="Gap">跳过的一段；不是占位时为 null。</param>
sealed record HeaderSlot<TItem>(int Position, HeaderGroup<TItem>? Cell, ColumnSkip? Gap)
{
    /// <summary>
    /// 分组表头按跳过的列段重排第一行：跳过的列合成跨两行的占位，部分露出的组只跨露出的列。
    /// <paramref name="layout"/> 是 <see cref="ColumnDisplay.HeaderLayout{TItem}"/> 的结果，列的顺序与 <paramref name="shown"/> 一致。
    /// </summary>
    public static IReadOnlyList<HeaderSlot<TItem>> Of(IReadOnlyList<HeaderGroup<TItem>> layout, IReadOnlyList<ColumnView<TItem>> shown, IReadOnlyList<ColumnSkip> skips)
    {
        var positionOf = new int[shown.Count];
        var index = 0;
        for (var position = 0; position < layout.Count; position++)
            foreach (var _ in layout[position].Columns) positionOf[index++] = position;

        var slots = new List<HeaderSlot<TItem>>();
        foreach (var (column, gap) in ColumnWindow.Walk(shown.Count, skips))
        {
            if (gap is { } skipped)
            {
                slots.Add(new(skipped.To < shown.Count ? positionOf[skipped.To] : layout.Count, null, skipped));
                continue;
            }
            var position = positionOf[column];
            var cell = layout[position];
            if (cell.Group is not null && slots.Count > 0 && slots[^1] is { Cell.Group: not null } open && open.Position == position)
                slots[^1] = open with { Cell = open.Cell! with { Columns = [.. open.Cell.Columns, shown[column]] } };
            else
                slots.Add(new(position, cell.Group is null ? cell : new HeaderGroup<TItem>(cell.Group, [shown[column]]), null));
        }
        return slots;
    }
}
