namespace Rune.UI;

/// <summary>怎样用鼠标开始编辑一格（键盘的 Enter、F2、直接打字都能开始）。</summary>
public enum EditTrigger
{
    /// <summary>双击（默认）：单击只是选中这一格，适合以查看为主、偶尔改一下的表。</summary>
    DoubleClick,

    /// <summary>单击：适合主要用来改数据的表。</summary>
    Click,
}

/// <summary>
/// 改了一格（DataTable 的 OnCellChange 拿到它）：写回数据；不接受时（接口拒绝）调用 <see cref="Reject"/>，编辑器留着并显示原因。
/// </summary>
/// <param name="item">这一行。</param>
/// <param name="key">列的 key。</param>
/// <param name="value">新的值（列的值类型）。</param>
public sealed class CellChange<TItem>(TItem item, string key, object? value)
{
    /// <summary>这一行。</summary>
    public TItem Item { get; } = item;

    /// <summary>列的 key。</summary>
    public string Key { get; } = key;

    /// <summary>新的值（列的值类型）。</summary>
    public object? Value { get; } = value;

    /// <summary>不接受的原因；没有调用 <see cref="Reject"/> 时为 null。</summary>
    public string? Rejection { get; private set; }

    /// <summary>不接受这次修改：编辑器留着，下面显示原因。</summary>
    public void Reject(string reason) => Rejection = reason;
}

/// <summary>自己写的单元格编辑器拿到的（PropertyColumn 的 Editor）。</summary>
/// <typeparam name="TItem">一行的类型。</typeparam>
/// <typeparam name="TValue">这一列的值类型。</typeparam>
/// <param name="Item">这一行。</param>
/// <param name="Value">正在编辑的值（草稿）。</param>
/// <param name="OnChange">改草稿。</param>
/// <param name="Commit">提交（校验通过后交给 OnCellChange）。</param>
/// <param name="Cancel">放弃，回到原来的值。</param>
/// <param name="Error">校验没过的原因：编辑器用它标出错误（Invalid）。</param>
public sealed record CellEditor<TItem, TValue>(TItem Item, TValue? Value, Action<TValue?> OnChange, Func<Task> Commit, Action Cancel, string? Error);

/// <summary>DataTable 交给列的编辑状态：草稿、错误、提交与放弃（列按自己的值类型包装成 <see cref="CellEditor{TItem, TValue}"/>）。</summary>
sealed class CellEditSession(object? draft, string label, Action<object?> change, Func<Task> commit, Action cancel)
{
    public object? Draft { get; } = draft;

    public string? Error { get; init; }

    /// <summary>列的名称：默认编辑器的 aria-label。</summary>
    public string Label { get; } = label;

    public void Change(object? value) => change(value);

    public Task CommitAsync() => commit();

    public void Cancel() => cancel();
}

/// <summary>ColumnTable 里一格合并几行、几列；0 是被别的格合并掉（不渲染）。</summary>
/// <param name="RowSpan">往下合并几行，默认 1。</param>
/// <param name="ColSpan">往后合并几列，默认 1。</param>
public readonly record struct CellSpan(int RowSpan = 1, int ColSpan = 1);
