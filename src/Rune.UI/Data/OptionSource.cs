namespace Rune.UI;

/// <summary>
/// 弹出列表的选项数据（Select、Combobox、TagsInput 共用）：本地的 Items 或远程加载的结果、按搜索词过滤、已选项的文字。
/// 已选项被搜索过滤掉、或远程搜索换了一批选项之后，仍按最后一次见到它时的文字显示（同 vue-tsx-pro 的 useSelectedOptions）。
/// 组件里一个字段：<c>OptionSource&lt;TItem&gt; options => field ??= new(() => InvokeAsync(StateHasChanged));</c>，销毁时 Dispose。
/// </summary>
/// <param name="changed">远程加载的状态变了：组件重新渲染。</param>
sealed class OptionSource<TItem>(Func<Task> changed) : IDisposable
{
    Memo<(IEnumerable<TItem> Items, Func<TItem, ListboxOption>? ToOption), IReadOnlyList<ListEntry<TItem>>> all;
    Memo<IReadOnlyList<ListEntry<TItem>>, Dictionary<string, ListEntry<TItem>>> byValue;
    Memo<(IReadOnlyList<ListEntry<TItem>> All, string Query, Func<TItem, string, bool>? Filter, UIConfig Config), IReadOnlyList<ListEntry<TItem>>> filtered;
    readonly Dictionary<string, ListEntry<TItem>> seen = [];
    RemoteItems<TItem>? remote;

    /// <summary>正在远程加载。</summary>
    public bool Loading => remote?.Loading == true;

    /// <summary>最后一次远程加载失败了。</summary>
    public bool Failed => remote?.Failed == true;

    /// <summary>全部选项（远程结果回来之后是它，之前是本地的 <paramref name="items"/>），按分组排好；输入不变时是同一个列表。</summary>
    public IReadOnlyList<ListEntry<TItem>> All(IEnumerable<TItem> items, Func<TItem, ListboxOption>? toOption) =>
        all.Get((remote?.Data ?? items, toOption), static k => ListState.Entries(k.Items, k.ToOption));

    /// <summary>
    /// 按搜索词过滤（<paramref name="filter"/>，默认显示文字包含搜索词，见 <see cref="UIConfig.Matches"/>）；搜索词为空时是全部。
    /// 服务端过滤（有远程加载、或组件关掉了本地过滤）时调用方直接用 <see cref="All"/>。
    /// </summary>
    public IReadOnlyList<ListEntry<TItem>> Filter(IReadOnlyList<ListEntry<TItem>> entries, string query, Func<TItem, string, bool>? filter, UIConfig config) =>
        query.Length == 0
            ? entries
            : filtered.Get((entries, query, filter, config), static k =>
                [.. k.All.Where(e => k.Filter?.Invoke(e.Item, k.Query) ?? k.Config.Matches(e.Option.Label, k.Query))]);

    /// <summary>已选的项：文字取自全部选项；找不到时用最后一次见到的；都没有时显示值本身（HasItem 为 false）。</summary>
    public List<ChosenOption<TItem>> Chosen(IReadOnlyList<ListEntry<TItem>> entries, IReadOnlyList<string> values)
    {
        var index = byValue.Get(entries, static entries =>
        {
            var map = new Dictionary<string, ListEntry<TItem>>(entries.Count);
            foreach (var entry in entries) map.TryAdd(entry.Option.Value, entry);
            return map;
        });
        var result = new List<ChosenOption<TItem>>(values.Count);
        foreach (var value in values)
        {
            if (index.TryGetValue(value, out var found)) seen[value] = found;
            else if (!seen.TryGetValue(value, out found))
            {
                result.Add(new(new ListboxOption(value, value), default!, false));
                continue;
            }
            result.Add(new(found.Option, found.Item, true));
        }
        return result;
    }

    /// <summary>远程加载（没有 <paramref name="load"/> 时什么都不做）：刚打开时立即请求，输入中 <paramref name="debounce"/> 等停下再请求。请求自己处理失败，不用等它。</summary>
    public void Request(OptionsLoader<TItem>? load, string query, bool debounce)
    {
        if (load is null) return;
        remote ??= new(changed);
        _ = remote.RequestAsync(load, query, debounce);
    }

    /// <summary>取消还在进行的请求（弹层关上）。</summary>
    public void Cancel() => remote?.Cancel();

    /// <summary>搜索后播报的结果（“12 个选项”“没有匹配的选项”）；没有搜索词时 null。</summary>
    public static string? Results(Lang lang, string query, int count) =>
        query.Length == 0 ? null : count > 0 ? lang.ListResultCount(count) : lang.SelectNoMatch;

    /// <summary>列表空着时的文字：加载失败、搜索没有匹配；其余为 null（用组件的 Empty 或默认文案）。</summary>
    public string? EmptyText(Lang lang, string query) =>
        Failed ? lang.ListLoadFailed : query.Length > 0 ? lang.SelectNoMatch : null;

    /// <inheritdoc/>
    public void Dispose() => remote?.Dispose();
}

/// <summary>一个已选的项：选项信息与它的数据（只知道值、找不到数据时 HasItem 为 false）。</summary>
readonly record struct ChosenOption<TItem>(ListboxOption Option, TItem Item, bool HasItem);
