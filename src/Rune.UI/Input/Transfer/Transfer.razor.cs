using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>穿梭框：两个可多选的列表，勾上之后移到另一边；用法见 Transfer.razor 开头。</summary>
/// <typeparam name="TItem">数据的类型。</typeparam>
public partial class Transfer<TItem>
{
    static readonly Attrs VisuallyHiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };

    // 两边各自勾上的值、搜索词：0 是左边（可选），1 是右边（已选）
    readonly IReadOnlyList<string>[] checkedKeys = [[], []];
    readonly string[] searches = ["", ""];
    string announcement = "";
    int announcements;
    Memo<(IEnumerable<TItem>, Func<TItem, ListboxOption>?), IReadOnlyList<ListEntry<TItem>>> entries;

    /// <summary>全部数据（两边一起）：右边显示值里的那些。</summary>
    [Parameter, EditorRequired] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>数据 → 选项（值、文字、不可用、说明）；Items 是 ListboxOption 或字符串时可以不写。</summary>
    [Parameter] public Func<TItem, ListboxOption>? ToOption { get; set; }

    /// <summary>左右两边的标题，默认取语言包（可选、已选）。</summary>
    [Parameter] public (string Source, string Target)? Titles { get; set; }

    /// <summary>两边带搜索框。</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>搜索时怎样算匹配；默认选项文字包含输入（同 Select，见 UIConfig.Matches）。</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>每一边（section）的部件属性：列表的高度给它的 Sx。</summary>
    [Parameter] public Attrs? PropsPanel { get; set; }

    /// <summary>搜索框（TextField）的部件属性。</summary>
    [Parameter] public Attrs? PropsSearch { get; set; }

    /// <summary>中间两个按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsAction { get; set; }

    /// <summary>两边列表（Listbox）的部件属性。</summary>
    [Parameter] public Attrs? PropsList { get; set; }

    IReadOnlyList<string> Chosen => CurrentValue ?? [];

    IReadOnlyList<ListEntry<TItem>> AllEntries => entries.Get((Items, ToOption), static k =>
        [.. k.Item1.Select(item => new ListEntry<TItem>(ListState.OptionOf(item, k.Item2), item))]);

    string TitleOf(int side) => side == 0
        ? Titles?.Source ?? Config.Lang.TransferSource
        : Titles?.Target ?? Config.Lang.TransferTarget;

    /// <summary>一边的全部项（左边按 Items 的顺序，右边按移过去的先后）。</summary>
    List<ListEntry<TItem>> SideEntries(int side)
    {
        var chosen = Chosen.ToHashSet();
        if (side == 0) return [.. AllEntries.Where(e => !chosen.Contains(e.Option.Value))];
        var byValue = AllEntries.GroupBy(e => e.Option.Value).ToDictionary(g => g.Key, g => g.First());
        return [.. Chosen.Where(byValue.ContainsKey).Select(key => byValue[key])];
    }

    List<ListEntry<TItem>> ShownEntries(int side)
    {
        var text = searches[side].Trim();
        var all = SideEntries(side);
        return text.Length == 0 ? all : [.. all.Where(e => Filter?.Invoke(e.Item, text) ?? Config.Matches(e.Option.Label, text))];
    }

    static Attrs ActionIconLayer(PartLayers<TransferPart> parts) => Attrs.Layer(parts[TransferPart.ActionIcon], null, new Attrs { ["FlipInRtl"] = true });

    // 全选只管看得见的：取消时只去掉看得见的，别的勾选保留
    void CheckAll(int side, List<string> selectable, bool all)
    {
        var others = checkedKeys[side].Where(key => !selectable.Contains(key));
        checkedKeys[side] = all ? [.. others, .. selectable] : [.. others];
    }

    /// <summary>把 keys 从 from 这一边移到另一边：不可用的、已经不在这一边的不动。</summary>
    async Task MoveAsync(int from, IReadOnlyList<string> keys)
    {
        var byValue = AllEntries.GroupBy(e => e.Option.Value).ToDictionary(g => g.Key, g => g.First().Option);
        var chosen = Chosen.ToHashSet();
        var movable = keys.Where(key => byValue.TryGetValue(key, out var option) && !option.Disabled && chosen.Contains(key) == (from == 1)).ToList();
        if (IsDisabled || movable.Count == 0) return;
        var moving = movable.ToHashSet();
        checkedKeys[from] = [.. checkedKeys[from].Where(key => !moving.Contains(key))];
        // 同一句话连着说两次也要念：交替加一个看不见的字符，让文字变一变
        announcement = Config.Lang.TransferMoved(movable.Count, TitleOf(1 - from)) + (++announcements % 2 == 0 ? "​" : "");
        await SetValueAsync(from == 0 ? [.. Chosen, .. movable] : [.. Chosen.Where(key => !moving.Contains(key))]);
    }

    // 双击一项直接移过去：JS 发来 move，Text 是它在这一边看得见的项里的序号
    Task DoubleClickAsync(int side, CommandEventArgs e)
    {
        if (e.Name != "move" || !int.TryParse(e.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)) return Task.CompletedTask;
        var shown = ShownEntries(side);
        return index >= 0 && index < shown.Count ? MoveAsync(side, [shown[index].Option.Value]) : Task.CompletedTask;
    }
}
