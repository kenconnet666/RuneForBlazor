namespace Rune.UI;

/// <summary>
/// 一个选项（同 vue-tsx-pro 的 ListboxOption）：Listbox、Select、Combobox、单选组与复选组的 Items 都按它理解。
/// </summary>
/// <param name="Value">值：选中后写回的就是它，同一列表里不能重复。</param>
/// <param name="Label">显示的文字；类型搜索、过滤、触发框里显示的都是它。</param>
/// <param name="Disabled">不可用：方向键可以经过（读屏能听到），不能选中。</param>
/// <param name="Description">文字下方的说明。</param>
/// <param name="Icon">文字前的图标。</param>
/// <param name="Group">分组名：同名的选项排在一组，组名显示在组的第一行。</param>
public sealed record ListboxOption(
    string Value,
    string Label,
    bool Disabled = false,
    string? Description = null,
    IconData? Icon = null,
    string? Group = null);

/// <summary>单选或多选。</summary>
public enum SelectionMode
{
    /// <summary>单选（默认）。</summary>
    Single,

    /// <summary>多选。</summary>
    Multiple,
}

/// <summary>渲染用的一行：分组标题或选项（分组标题是单独的一行，虚拟滚动按行算）。</summary>
/// <param name="Index">选项的序号；分组标题行是组里第一个选项的序号。</param>
/// <param name="IsGroupLabel">是分组标题。</param>
/// <param name="GroupStart">选项所在组的第一个选项的序号（组名的 id 由它拼出）；没有分组时 -1。</param>
public readonly record struct ListRow(int Index, bool IsGroupLabel, int GroupStart);

/// <summary>列表里的一项：选项的显示信息，加上它来自的原始数据。</summary>
/// <param name="Option">显示信息。</param>
/// <param name="Item">Items 里对应的那一项：自定义渲染拿到的就是它。</param>
public readonly record struct ListEntry<TItem>(ListboxOption Option, TItem Item);

/// <summary>
/// 选项列表的数据层（同 vue-tsx-pro 的 list-state）：数据 → 选项、按分组排序、是否已选、切换选择。
/// 键盘高亮不在这里：它由 JS 初始化器在浏览器里处理（aria-activedescendant），不用每按一次方向键往返一次。
/// </summary>
public static class ListState
{
    /// <summary>
    /// 一项数据的选项：给了 <paramref name="toOption"/> 用它；没给时数据本身是 <see cref="ListboxOption"/>，
    /// 或是字符串（值与文字都是它）。别的类型没给 toOption 时报错。
    /// </summary>
    public static ListboxOption OptionOf<TItem>(TItem item, Func<TItem, ListboxOption>? toOption) => toOption is not null
        ? toOption(item)
        : item switch
        {
            ListboxOption option => option,
            string text => new ListboxOption(text, text),
            _ => throw new InvalidOperationException($"选项是 {typeof(TItem).Name} 时要给 ToOption（数据 → ListboxOption）"),
        };

    /// <summary>按分组排好的选项：没有分组的在前，各组按第一次出现的顺序，组内保持原顺序。</summary>
    public static IReadOnlyList<ListEntry<TItem>> Entries<TItem>(IEnumerable<TItem> items, Func<TItem, ListboxOption>? toOption)
    {
        var entries = items.Select(item => new ListEntry<TItem>(OptionOf(item, toOption), item)).ToList();
        if (!entries.Any(e => e.Option.Group is not null)) return entries;
        var groups = new List<string?> { null };
        foreach (var entry in entries)
            if (!groups.Contains(entry.Option.Group)) groups.Add(entry.Option.Group);
        return [.. groups.SelectMany(group => entries.Where(e => e.Option.Group == group))];
    }

    /// <summary>展开成渲染用的行：每组前插一行组名（<paramref name="entries"/> 已按分组排好）。</summary>
    public static ListRow[] Rows<TItem>(IReadOnlyList<ListEntry<TItem>> entries)
    {
        var rows = new List<ListRow>(entries.Count);
        string? group = null;
        var start = -1;
        for (var index = 0; index < entries.Count; index++)
        {
            var current = entries[index].Option.Group;
            if (current is not null && current != group)
            {
                start = index;
                rows.Add(new(index, true, index));
            }
            group = current;
            rows.Add(new(index, false, current is null ? -1 : start));
        }
        return [.. rows];
    }

    /// <summary>
    /// 切换一项的选中：单选时选中它（<paramref name="required"/> 时再点已选项不取消），多选时加入或移出。
    /// 没有变化时返回 null。
    /// </summary>
    public static IReadOnlyList<string>? Toggle(IReadOnlyList<string> selected, string value, SelectionMode mode, bool required)
    {
        var has = selected.Contains(value);
        if (mode == SelectionMode.Single)
        {
            if (has) return required ? null : [];
            return [value];
        }
        if (has) return required && selected.Count == 1 ? null : [.. selected.Where(v => v != value)];
        return [.. selected, value];
    }
}
