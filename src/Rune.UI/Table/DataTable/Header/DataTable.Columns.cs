using Microsoft.AspNetCore.Components.Web;

namespace Rune.UI;

// DataTable 的列操作：表头点击排序、筛选、列宽、列菜单（排序、固定、移动、隐藏）。
public partial class DataTable<TItem>
{
    // 每列一个不变的点击处理：滚动时表格随可见范围重新渲染，处理函数变了的话浏览器那边会换一个事件编号，
    // 正好在两次渲染之间点下去的那一次（Server 模式）就落到已经作废的编号上、被丢掉
    readonly Dictionary<DataTableColumn<TItem>, Func<MouseEventArgs, Task>> sortClicks = [];

    Func<MouseEventArgs, Task> SortClick(DataTableColumn<TItem> column) =>
        sortClicks.TryGetValue(column, out var click) ? click : sortClicks[column] = e => OnSortClickAsync(column, e);

    Task OnSortClickAsync(DataTableColumn<TItem> column, MouseEventArgs e)
    {
        var key = column.ColumnKey;
        if (!MultiSort) return sort.SetAsync(ColumnSort.Next(sort.Value, key));
        // 不按 Shift：只按这一列（同单列排序的循环）
        if (!e.ShiftKey)
        {
            var own = sorts.Value.Count == 1 ? sorts.Value[0] : (ColumnSort?)null;
            return sorts.SetAsync(ColumnSort.Next(own, key) is { } next ? [next] : []);
        }
        return sorts.SetAsync(ColumnSort.NextMultiple(sorts.Value, key));
    }

    Task SetFilterAsync(string key, IReadOnlyList<string> values) =>
        filters.SetAsync(new Dictionary<string, IReadOnlyList<string>>(filters.Value) { [key] = values });

    Task SetWidthAsync(string key, double width) =>
        columnWidths.SetAsync(new Dictionary<string, double>(columnWidths.Value) { [key] = width });

    Task SetSortAsync(string key, SortDirection? direction)
    {
        if (!MultiSort) return sort.SetAsync(direction is { } d ? new ColumnSort(key, d) : null);
        var rest = sorts.Value.Where(item => item.Key != key);
        return sorts.SetAsync(direction is { } dir ? [new(key, dir), .. rest] : [.. rest]);
    }

    /// <summary>列菜单里选了一项：排序、固定、前后移动（与相邻的可见列交换）、隐藏。</summary>
    Task OnColumnActionAsync(ColumnView<TItem> view, IReadOnlyList<ColumnView<TItem>> shown, ColumnMenuAction action)
    {
        var key = view.Key;
        switch (action)
        {
            case ColumnMenuAction.SortAscending: return SetSortAsync(key, SortDirection.Ascending);
            case ColumnMenuAction.SortDescending: return SetSortAsync(key, SortDirection.Descending);
            case ColumnMenuAction.Unsort: return SetSortAsync(key, null);
            case ColumnMenuAction.Hide: return hiddenColumns.SetAsync([.. hiddenColumns.Value, key]);
            case ColumnMenuAction.MoveBackward or ColumnMenuAction.MoveForward:
                var position = shown.ToList().FindIndex(item => item.Key == key) + (action == ColumnMenuAction.MoveForward ? 1 : -1);
                if (position < 0 || position >= shown.Count) return Task.CompletedTask;
                // 全部列（含隐藏的）按当前的排列，与相邻的可见列交换位置
                var keys = ColumnDisplay.Arrange(columns.All, Rows, columnOrder.Value, columnPins.Value, columnWidths.Value, []).Select(static item => item.Key).ToList();
                var (from, to) = (keys.IndexOf(key), keys.IndexOf(shown[position].Key));
                (keys[from], keys[to]) = (keys[to], keys[from]);
                return columnOrder.SetAsync(keys);
            default:
                var side = action switch { ColumnMenuAction.PinStart => ColumnSticky.Start, ColumnMenuAction.PinEnd => ColumnSticky.End, _ => (ColumnSticky?)null };
                return columnPins.SetAsync(new Dictionary<string, ColumnSticky?>(columnPins.Value) { [key] = side });
        }
    }
}
