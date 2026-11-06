using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>级联选择触发框里自定义显示的参数：路径上各级的文字与节点（还没加载的节点文字是 key、节点是 default）。</summary>
/// <param name="Labels">各级的文字。</param>
/// <param name="Nodes">各级的节点。</param>
public sealed record CascaderValue<TItem>(IReadOnlyList<string> Labels, IReadOnlyList<TItem?> Nodes);

/// <summary>级联选择：触发框 + 一列一列的选项，选到叶子（或任意一级）；用法见 Cascader.razor 开头。</summary>
/// <typeparam name="TItem">节点的类型。</typeparam>
public partial class Cascader<TItem>
{
    static readonly Attrs ChevronLayer = new() { Sx = InputStyles.Chevron };
    static readonly Attrs ColumnsContent = new() { Sx = ColumnsRow };

    readonly List<string> path = [];
    readonly HashSet<TItem> loading = new(ReferenceEqualityComparer.Instance as IEqualityComparer<TItem>);
    string search = "";
    bool open;

    Memo<(string Id, bool Open, bool Disabled), Attrs> shellManaged;
    Memo<(Attrs Control, bool Open, bool Locked, bool ReadOnly), Attrs> triggerManaged;
    Memo<string, Attrs> refocusTrigger;

    /// <summary>第一级的节点。</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>节点的 key，同级里唯一；第二个参数是它在同级里的位置。</summary>
    [Parameter, EditorRequired] public Func<TItem, int, string> GetKey { get; set; } = default!;

    /// <summary>节点的文字。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetLabel { get; set; } = default!;

    /// <summary>下一级；叶子或还没加载时 null。</summary>
    [Parameter, EditorRequired] public Func<TItem, IReadOnlyList<TItem>?> GetChildren { get; set; } = default!;

    /// <summary>有没有下一级（懒加载时还没取回来也算有）；不写时看 GetChildren 是否非空。</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>进入一个还没有下一级的节点时取回来，放进数据里；期间显示加载中。</summary>
    [Parameter] public Func<TItem, Task>? LoadChildren { get; set; }

    /// <summary>不能选、不能进入的节点。</summary>
    [Parameter] public Func<TItem, bool>? IsNodeDisabled { get; set; }

    /// <summary>每一级都能选；默认只能选到叶子。</summary>
    [Parameter] public bool ChangeOnSelect { get; set; }

    /// <summary>触发框里显示整条路径（默认），false 时只显示最后一级。</summary>
    [Parameter] public bool ShowAllLevels { get; set; } = true;

    /// <summary>路径各级之间的分隔，默认“ / ”。</summary>
    [Parameter] public string Separator { get; set; } = " / ";

    /// <summary>自定义触发框里的内容。</summary>
    [Parameter] public RenderFragment<CascaderValue<TItem>>? RenderValue { get; set; }

    /// <summary>弹出层顶部带搜索框，按整条路径的文字搜索已加载的节点。</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>搜索时怎样算匹配：参数是路径上的节点与输入；默认路径文字包含输入（同 Select，见 UIConfig.Matches）。</summary>
    [Parameter] public Func<IReadOnlyList<TItem>, string, bool>? Filter { get; set; }

    /// <summary>没有选择时显示的文字，默认取语言包（请选择）。</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>弹出位置，默认下方、与起始边对齐。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>打开或关闭后。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>有选择时显示清空按钮。</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>清空按钮的名称，默认取语言包。</summary>
    [Parameter] public string? ClearLabel { get; set; }

    /// <summary>外壳起始边的图标。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>图标的部件属性。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>已选内容那一层的部件属性。</summary>
    [Parameter] public Attrs? PropsValue { get; set; }

    /// <summary>清空按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsClear { get; set; }

    /// <summary>下拉箭头的部件属性。</summary>
    [Parameter] public Attrs? PropsChevron { get; set; }

    /// <summary>弹出层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>搜索框的部件属性（交给 TextField）。</summary>
    [Parameter] public Attrs? PropsSearch { get; set; }

    /// <summary>每一列（视口，role="listbox"）的部件属性。</summary>
    [Parameter] public Attrs? PropsColumn { get; set; }

    bool Locked => IsDisabled || IsReadOnly;

    string ShellId => $"{ControlId}-shell";

    string PopoverId => $"{ControlId}-popover";

    string ColumnId(int level) => $"{ControlId}-column-{level.ToString(CultureInfo.InvariantCulture)}";

    string? AriaLabel => AdditionalAttributes?.TryGetValue("aria-label", out var label) == true ? label as string : null;

    bool Off(TItem node) => IsNodeDisabled?.Invoke(node) ?? false;

    bool Loaded(TItem node) => GetChildren(node) is { Count: > 0 };

    // 有没有下一级（懒加载时还没取回来也算有）
    bool Expandable(TItem node) => HasChildren?.Invoke(node) ?? Loaded(node);

    int IndexOf(IReadOnlyList<TItem> items, string key)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (GetKey(items[index], index) == key) return index;
        }
        return -1;
    }

    /// <summary>沿着 keys 找到各级的节点（找不到的地方停下：还没加载，或数据变了）。</summary>
    List<TItem> NodesAlong(IReadOnlyList<string> keys)
    {
        var found = new List<TItem>();
        var level = Items;
        foreach (var key in keys)
        {
            var index = IndexOf(level, key);
            if (index < 0) break;
            found.Add(level[index]);
            level = GetChildren(level[index]) ?? [];
        }
        return found;
    }

    /// <summary>要显示的各列：第一级，以及路径上每个已加载（或正在加载）下一级的节点的下一级。</summary>
    List<(TItem? Parent, IReadOnlyList<TItem> Items)> Columns
    {
        get
        {
            var list = new List<(TItem?, IReadOnlyList<TItem>)> { (default, Items) };
            foreach (var node in NodesAlong(path))
            {
                if (!Expandable(node) || !(Loaded(node) || loading.Contains(node))) break;
                list.Add((node, GetChildren(node) ?? []));
            }
            return list;
        }
    }

    /// <summary>搜索结果的一项：整条路径。</summary>
    sealed record PathHit(IReadOnlyList<string> Keys, IReadOnlyList<TItem> Nodes, string Label, bool Disabled);

    // 搜索：匹配的完整路径，只列能选的（默认是叶子）；没有搜索词时 null（显示各列）
    List<PathHit>? Hits
    {
        get
        {
            var text = search.Trim();
            if (!Searchable || text.Length == 0) return null;
            var found = new List<PathHit>();
            Visit(Items, [], [], false);
            return found;

            void Visit(IReadOnlyList<TItem> level, List<string> keys, List<TItem> nodes, bool off)
            {
                for (var index = 0; index < level.Count; index++)
                {
                    var node = level[index];
                    List<string> nextKeys = [.. keys, GetKey(node, index)];
                    List<TItem> nextNodes = [.. nodes, node];
                    var nextOff = off || Off(node);
                    var label = string.Join(Separator, nextNodes.Select(GetLabel));
                    var selectable = ChangeOnSelect || !Expandable(node);
                    if (selectable && (Filter?.Invoke(nextNodes, text) ?? Config.Matches(label, text))) found.Add(new(nextKeys, nextNodes, label, nextOff));
                    Visit(GetChildren(node) ?? [], nextKeys, nextNodes, nextOff);
                }
            }
        }
    }

    Attrs RefocusTrigger => refocusTrigger.Get(ControlId, static id => new Attrs { ["data-rune-refocus"] = id });

    // 外壳：点在空白处时 JS 把焦点交给触发按钮并切换打开（data-rune-control）；箭头按 data-state 转向
    Attrs ShellManaged => shellManaged.Get((ShellId, open, IsDisabled), static k => Attrs.Merge(ShellAttrs, new Attrs
    {
        Id = k.Id,
        ["data-state"] = k.Open ? "open" : "closed",
        ["data-disabled"] = k.Disabled ? true : null,
    }));

    // 触发按钮：combobox 的角色与状态、打开弹层（popovertarget）；↓ 打开由 JS 处理（data-rune-cascader-trigger）
    Attrs TriggerManaged => triggerManaged.Get((ControlAttrs, open, Locked, IsReadOnly), this, static (k, owner) => new Attrs
    {
        Id = k.Control.Id,
        Role = "combobox",
        ["type"] = "button",
        ["disabled"] = k.Control["disabled"],
        ["popovertarget"] = k.Locked ? null : owner.PopoverId,
        ["aria-haspopup"] = "listbox",
        ["aria-expanded"] = k.Open ? "true" : "false",
        ["aria-controls"] = owner.ColumnId(0),
        ["aria-invalid"] = k.Control["aria-invalid"],
        ["aria-required"] = k.Control["aria-required"],
        ["aria-readonly"] = k.ReadOnly ? "true" : null,
        AriaDescribedBy = k.Control.AriaDescribedBy,
        ["data-rune-cascader-trigger"] = k.Locked ? null : true,
    });

    // 弹出层：贴着外壳、同宽、限高；data-rune-cascader 让 JS 处理列之间的键盘
    Attrs PanelManaged => Attrs.Merge(Floating.Placed(Placement, null), new Attrs
    {
        Id = PopoverId,
        ["popover"] = "auto",
        ["data-rune-overlay"] = true,
        ["data-rune-cascader"] = true,
        ["data-anchor"] = ShellId,
        ["data-match-width"] = true,
        ["data-fit-height"] = true,
        ["onrunetoggle"] = EventCallback.Factory.Create<OverlayToggleEventArgs>(this, ToggledAsync),
    });

    Attrs SearchDefaults => new()
    {
        Sx = SearchWidth,
        ["placeholder"] = Config.Lang.SelectSearch,
        ["aria-label"] = Config.Lang.SelectSearch,
    };

    // 搜索框：有搜索词时是结果列的键盘宿主（↑↓、Enter），没有时 ↓ 进入各列（JS）
    Attrs SearchManaged(bool searching) => new()
    {
        ["aria-controls"] = searching ? ColumnId(0) : null,
        ["autocomplete"] = "off",
        ["data-rune-list"] = searching ? "text" : null,
        ["data-rune-cascader-search"] = true,
    };

    // 列：自己是列表的键盘宿主（data-rune-list），高亮按路径写在 data-rune-active 上；JS 移动高亮、进入下一级时发命令
    Attrs ColumnViewport(PartLayers<CascaderPart> parts, int level, TItem? parent, int active, bool busy, bool initial) => Attrs.Layer(parts[CascaderPart.ColumnViewport], PropsColumn, new Attrs
    {
        Id = ColumnId(level),
        Role = "listbox",
        TabIndex = -1,
        AriaLabel = parent is not null ? GetLabel(parent) : Field?.LabelId is null ? AriaLabel : null,
        AriaLabelledBy = parent is null ? Field?.LabelId : null,
        ["aria-activedescendant"] = active >= 0 ? $"{ColumnId(level)}-{active.ToString(CultureInfo.InvariantCulture)}" : null,
        ["aria-busy"] = busy ? "true" : null,
        ["autofocus"] = initial && !Searchable ? true : null,
        ["data-rune-list"] = "typeahead",
        ["data-rune-active"] = active >= 0 ? active.ToString(CultureInfo.InvariantCulture) : null,
        ["data-rune-cascader-column"] = level.ToString(CultureInfo.InvariantCulture),
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(this, e => ColumnCommandAsync(level, e)),
    });

    Attrs ResultsViewport(PartLayers<CascaderPart> parts) => Attrs.Layer(parts[CascaderPart.ColumnViewport], PropsColumn, new Attrs
    {
        Id = ColumnId(0),
        Role = "listbox",
        TabIndex = -1,
        AriaLabel = Config.Lang.SelectSearch,
        ["data-rune-query"] = search,
    });

    static Style ColumnsRow => Css.Define(static () => new Style { Display = Display.Flex, Height = CssLength.Percent(100) });

    static Style SearchWidth => Css.Define(static () => new Style { Width = CssLength.Percent(100) });

    /// <summary>高亮第 level 列的一项：它后面的路径换成它（有已加载的下一级时显示出来）。</summary>
    void Activate(int level, string key)
    {
        if (level > path.Count) return;
        path.RemoveRange(level, path.Count - level);
        path.Add(key);
    }

    /// <summary>取回节点的下一级；有了（或取回来了）返回 true。</summary>
    async Task<bool> EnsureChildrenAsync(TItem node)
    {
        if (Loaded(node)) return true;
        if (LoadChildren is null || loading.Contains(node)) return false;
        loading.Add(node);
        try
        {
            StateHasChanged();
            await LoadChildren(node);
        }
        finally
        {
            loading.Remove(node);
        }
        return Loaded(node);
    }

    /// <summary>点一项：有下一级的展开（ChangeOnSelect 时同时选中），叶子选中（JS 随后关闭弹层）。</summary>
    async Task ClickAsync(int level, TItem node, string key)
    {
        if (Off(node)) return;
        List<string> keys = [.. path.Take(level), key];
        if (!Expandable(node))
        {
            await SetValueAsync(keys);
            return;
        }
        if (ChangeOnSelect) await SetValueAsync(keys);
        Activate(level, key);
        await EnsureChildrenAsync(node);
    }

    // JS 交来的命令：highlight（↑↓ 等移动了高亮）、enter（→ 进入下一级），Text 是选项的序号
    async Task ColumnCommandAsync(int level, CommandEventArgs e)
    {
        var columns = Columns;
        if (level >= columns.Count || !int.TryParse(e.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)) return;
        var items = columns[level].Items;
        if (index < 0 || index >= items.Count) return;
        var node = items[index];
        Activate(level, GetKey(node, index));
        if (e.Name == "enter" && Expandable(node)) await EnsureChildrenAsync(node);
    }

    Task PickHitAsync(PathHit hit) => hit.Disabled ? Task.CompletedTask : SetValueAsync([.. hit.Keys]);

    void SetSearch(string? text) => search = text ?? "";

    async Task ToggledAsync(OverlayToggleEventArgs e)
    {
        if (open == e.Open) return;
        open = e.Open;
        if (open)
        {
            search = "";
            path.Clear();
            path.AddRange(CurrentValue ?? []);
            await OpenChanged.InvokeAsync(true);
            // 沿着已选的路径逐级取回来（懒加载），已选的那一列看得见
            foreach (var node in NodesAlong(path).SkipLast(1))
            {
                if (!await EnsureChildrenAsync(node)) break;
            }
            return;
        }
        await OpenChanged.InvokeAsync(false);
    }
}
