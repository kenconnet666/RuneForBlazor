namespace Rune.UI;

// DataTable 的格内编辑：开始、提交（列的 Validate 与 OnCellChange）、取消，提交后去哪一格。
public partial class DataTable<TItem>
{
    TItem? ItemOf(string key)
    {
        foreach (var item in Rows)
        {
            if (GetKey(item) == key) return item;
        }
        return default;
    }

    DataTableColumn<TItem>? ColumnOf(string key) => columns.All.FirstOrDefault(column => column.ColumnKey == key);

    /// <summary>开始编辑一格；直接打字时用打的字替换原值（数字列只认数字）。</summary>
    void StartEdit(string rowKey, string columnKey, string? typed = null)
    {
        if (ItemOf(rowKey) is not { } item || ColumnOf(columnKey) is not { } column || !column.CanEdit(item)) return;
        var draft = typed is not null && column.TryDraftFromText(typed, out var fromText) ? fromText : column.ValueOf(item);
        editing = (rowKey, columnKey, draft, null, false);
        StateHasChanged();
    }

    void CancelEdit()
    {
        if (editing is not { } current) return;
        editing = null;
        FocusCell(current.Row, current.Column);
    }

    void FocusCell(string row, string column)
    {
        focusRequest = (row, column, focusRequest.Sequence + 1);
        StateHasChanged();
    }

    /// <summary>提交：列的 Validate、OnCellChange 都接受了才收起；step 是提交后去哪一格（Tab 往后、Shift + Tab 往前）。</summary>
    async Task CommitEditAsync(int step)
    {
        if (editing is not { Saving: false } current || ItemOf(current.Row) is not { } item || ColumnOf(current.Column) is not { } column) return;
        if (column.ValidateDraft(current.Draft, item) is { } invalid)
        {
            editing = current with { Error = invalid };
            StateHasChanged();
            return;
        }
        editing = current with { Saving = true };
        var change = new CellChange<TItem>(item, current.Column, current.Draft);
        await OnCellChange.InvokeAsync(change);
        if (change.Rejection is { } rejected)
        {
            editing = current with { Error = rejected, Saving = false };
            StateHasChanged();
            return;
        }
        // 保存的时候已经点开了别的格（点别处提交时常这样）：不动它
        if (editing is { } now && (now.Row != current.Row || now.Column != current.Column)) return;
        editing = null;
        if (step != 0 && NeighbourEditable(item, current.Column, step) is { } next) StartEdit(current.Row, next);
        else FocusCell(current.Row, current.Column);
    }

    /// <summary>这一行里往后（往前）的下一个可编辑的格。</summary>
    string? NeighbourEditable(TItem item, string columnKey, int step)
    {
        var visible = Shown;
        var from = visible.ToList().FindIndex(view => view.Key == columnKey);
        for (var index = from + step; index >= 0 && index < visible.Count; index += step)
        {
            if (visible[index].Column.CanEdit(item)) return visible[index].Key;
        }
        return null;
    }

    /// <summary>编辑中的格交给列的编辑状态：草稿改了只更新草稿与错误。</summary>
    CellEditSession EditSession(DataTableColumn<TItem> column) => new(
        editing?.Draft,
        column.Label,
        value =>
        {
            if (editing is { } current) editing = current with { Draft = value, Error = null };
            StateHasChanged();
        },
        () => CommitEditAsync(0),
        CancelEdit)
    {
        Error = editing?.Error,
    };
}
