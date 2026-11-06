using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>左边是一棵树的穿梭框：勾选叶子（父子联动）移到右边；用法见 TreeTransfer.razor 开头。</summary>
/// <typeparam name="TItem">节点的数据类型。</typeparam>
public partial class TreeTransfer<TItem>
{
    static readonly Attrs VisuallyHiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };

    // 两边各自勾上的值、搜索词：0 是左边（树，只放还没移过去的叶子），1 是右边（已选）
    readonly IReadOnlyList<string>[] checkedKeys = [[], []];
    readonly string[] searches = ["", ""];
    IReadOnlyList<string> expanded = [];
    string announcement = "";
    int announcements;
    Memo<IReadOnlyList<TItem>, TreeIndex> index;
    Memo<IReadOnlyList<TItem>, Dictionary<string, TItem>> nodes;

    /// <summary>第一层的节点。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>节点的 key，整棵树里唯一：叶子的 key 就是右边的值。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetKey { get; set; } = null!;

    /// <summary>节点的文字。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetLabel { get; set; } = null!;

    /// <summary>子节点；叶子返回 null 或空列表。</summary>
    [Parameter, EditorRequired] public Func<TItem, IReadOnlyList<TItem>?> GetChildren { get; set; } = null!;

    /// <summary>左右两边的标题，默认取语言包（可选、已选）。</summary>
    [Parameter] public (string Source, string Target)? Titles { get; set; }

    /// <summary>两边带搜索框（左边按节点文字过滤树，右边过滤列表）。</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>每一边（section）的部件属性：列表的高度给它的 Sx。</summary>
    [Parameter] public Attrs? PropsPanel { get; set; }

    /// <summary>搜索框（TextField）的部件属性。</summary>
    [Parameter] public Attrs? PropsSearch { get; set; }

    /// <summary>中间两个按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsAction { get; set; }

    /// <summary>左边的树（Tree）的部件属性。</summary>
    [Parameter] public Attrs? PropsTree { get; set; }

    /// <summary>右边列表（Listbox）的部件属性。</summary>
    [Parameter] public Attrs? PropsList { get; set; }

    IReadOnlyList<string> Chosen => CurrentValue ?? [];

    TreeIndex Index => index.Get(Items, this, static (items, owner) => TreeData.Index(items, new TreeAccessors<TItem>(owner.GetKey, owner.GetChildren)));

    // key → 节点：右边的文字、左边“不可用”的判断都用它
    Dictionary<string, TItem> Nodes => nodes.Get(Items, this, static (items, owner) =>
    {
        var map = new Dictionary<string, TItem>();
        Visit(items);
        return map;

        void Visit(IReadOnlyList<TItem> level)
        {
            foreach (var node in level)
            {
                map[owner.GetKey(node)] = node;
                Visit(owner.GetChildren(node) ?? []);
            }
        }
    });

    // 左边能勾的叶子：还没移过去的
    List<string> SourceLeaves
    {
        get
        {
            var chosen = Chosen.ToHashSet();
            return [.. Index.Order.Where(key => (Index.Children.GetValueOrDefault(key)?.Count ?? 0) == 0 && !chosen.Contains(key))];
        }
    }

    // 右边：值里能在树里找到的，按移过去的先后
    List<ListboxOption> Targets => [.. Chosen.Where(Nodes.ContainsKey).Select(key => new ListboxOption(key, GetLabel(Nodes[key])))];

    string TitleOf(int side) => side == 0
        ? Titles?.Source ?? Config.Lang.TransferSource
        : Titles?.Target ?? Config.Lang.TransferTarget;

    string TitleId(int side) => $"{ControlId}-title-{side}";

    // 已经移过去的叶子勾上且不可用；整个穿梭框不可用时都不能勾
    bool Off(TItem node) => IsDisabled || Chosen.Contains(GetKey(node));

    bool Matches(string key, int side)
    {
        var text = searches[side].Trim();
        return text.Length == 0 || (Nodes.TryGetValue(key, out var node) && Config.Matches(GetLabel(node), text));
    }

    // 树报告勾上的全部（含已经移过去的、父节点）：左边只记还没移过去的叶子
    void SetSourceChecked(IReadOnlyList<string> keys)
    {
        var chosen = Chosen.ToHashSet();
        checkedKeys[0] = [.. TreeData.CheckedByStrategy(Index, keys, CheckStrategy.Child).Where(key => !chosen.Contains(key))];
    }

    /// <summary>把 keys 从 from 这一边移到另一边：已经不在这一边的不动。</summary>
    async Task MoveAsync(int from, IReadOnlyList<string> keys)
    {
        var chosen = Chosen.ToHashSet();
        var movable = keys.Where(key => Nodes.ContainsKey(key) && chosen.Contains(key) == (from == 1)).ToList();
        if (IsDisabled || movable.Count == 0) return;
        var moving = movable.ToHashSet();
        checkedKeys[from] = [.. checkedKeys[from].Where(key => !moving.Contains(key))];
        // 同一句话连着说两次也要念：交替加一个看不见的字符，让文字变一变
        announcement = Config.Lang.TransferMoved(movable.Count, TitleOf(1 - from)) + (++announcements % 2 == 0 ? "​" : "");
        await SetValueAsync(from == 0 ? [.. Chosen, .. movable] : [.. Chosen.Where(key => !moving.Contains(key))]);
    }

    // 右边双击一项直接移回去：JS 发来 move，Text 是它在看得见的项里的序号
    Task DoubleClickAsync(CommandEventArgs e)
    {
        if (e.Name != "move" || !int.TryParse(e.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var at)) return Task.CompletedTask;
        var shown = Targets.Where(option => Matches(option.Value, 1)).ToList();
        return at >= 0 && at < shown.Count ? MoveAsync(1, [shown[at].Value]) : Task.CompletedTask;
    }
}
