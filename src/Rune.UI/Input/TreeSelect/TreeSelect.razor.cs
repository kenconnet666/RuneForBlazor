using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>树形下拉选择：触发框 + 弹出的树，单选或勾选，可搜索。用法见 TreeSelect.razor 顶部的说明。</summary>
/// <typeparam name="TItem">节点的数据类型。</typeparam>
public partial class TreeSelect<TItem>
{
    static readonly Attrs VisuallyHiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };
    static readonly Attrs ChevronLayer = new() { Sx = InputStyles.Chevron };
    // 树的滚动区域占满弹出层剩下的高度（最大高度由定位写在弹出层上）
    static readonly Attrs TreeScrollLayer = new() { Sx = Css.Define(static () => new Style { FlexGrow = 1, MinHeight = 0 }) };

    // 已见过的节点文字：远程搜索换掉了已选的节点，触发框里仍显示它的文字
    readonly Dictionary<string, string> seenLabels = [];
    IReadOnlyList<string> expanded = [];
    EventCallback<string>? removeCallback;
    bool open;

    Memo<IReadOnlyList<TItem>, Dictionary<string, (TItem Node, string? Parent)>> nodeMap;
    Memo<IReadOnlyList<TItem>, TreeIndex> index;
    Memo<(Attrs Control, bool Open, bool Locked, bool ReadOnly, bool Multiple, bool Tagged), Attrs> triggerManaged;
    Memo<(string Id, bool Open, bool Disabled), Attrs> shellManaged;
    Memo<string, Attrs> searchDefaults, refocusTrigger;

    /// <summary>第一层的节点；有 Load 时是第一次加载之前的树（编辑页放上已选节点的那一枝）。</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>节点的 key，整棵树里唯一：也是选中后写回的值。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetKey { get; set; } = null!;

    /// <summary>节点的文字。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetLabel { get; set; } = null!;

    /// <summary>子节点；叶子或还没加载时返回 null 或空列表。</summary>
    [Parameter, EditorRequired] public Func<TItem, IReadOnlyList<TItem>?> GetChildren { get; set; } = null!;

    /// <summary>有没有子节点（懒加载时子节点还没取回来也算有）。</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>展开还没有子节点的节点时取回来（同 Tree 的 LoadChildren）。</summary>
    [Parameter] public EventCallback<TItem> LoadChildren { get; set; }

    /// <summary>不能选的节点（只能展开，如只能选到部门、不能选公司时；同 Cascader 的 IsNodeDisabled）。</summary>
    [Parameter] public Func<TItem, bool>? IsNodeDisabled { get; set; }

    /// <summary>单选（默认，值是 Value）或多选（勾选，父子联动，值是 Values）。</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; }

    /// <summary>多选时勾上的节点（按 <see cref="CheckStrategy"/>，<c>@bind-Values</c>）。</summary>
    [Parameter] public IReadOnlyList<string>? Values { get; set; }

    /// <summary>多选时勾选变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> ValuesChanged { get; set; }

    /// <summary>
    /// 多选时 Values 里放哪些勾上的节点（同 Ant Design 的 showCheckedStrategy）：All 全部（默认）；
    /// Parent 父节点全勾上时只放父节点（提交“整个部门”）；Child 只放叶子（提交“具体的人”）。
    /// </summary>
    [Parameter] public CheckStrategy CheckStrategy { get; set; }

    /// <summary>多选时触发框里最多显示几个标签，其余收进“+N”；<see cref="TagLimit.Responsive"/> 时排成一行、按宽度放。</summary>
    [Parameter] public TagLimit? MaxTagCount { get; set; }

    /// <summary>没有选择时显示的文字；不写时取语言包（请选择）。</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>弹出位置，默认下方、与起始边对齐；空间不够时翻到另一侧。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>打开或关闭后。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>有选择时显示清空按钮。</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>弹出层顶部带搜索框，按节点文字过滤。</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>搜索时怎样算命中（节点、搜索词）；默认节点文字包含输入（忽略大小写与重音，见 <see cref="UIConfig.Matches"/>）。</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>在本地按搜索词过滤，默认 true；自己按 <see cref="Search"/> 请求、由服务端过滤时设为 false（有 Load 时本来就不在本地过滤）。</summary>
    [Parameter] public bool LocalFilter { get; set; } = true;

    /// <summary>搜索框里的文字（<c>@bind-Search</c>）：自己发请求时拿到它；关闭时清空。</summary>
    [Parameter] public string? Search { get; set; }

    /// <summary>搜索框里的文字变了。</summary>
    [Parameter] public EventCallback<string?> SearchChanged { get; set; }

    /// <summary>远程搜索：打开时与搜索词停下 300 ms 后调用，返回命中的节点连同祖先（第一层），结果代替 Items。</summary>
    [Parameter] public OptionsLoader<TItem>? Load { get; set; }

    /// <summary>前置图标。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>清空按钮的名称（读屏读出）；不写时取语言包的“清空”。</summary>
    [Parameter] public string? ClearLabel { get; set; }

    /// <summary>前置图标的部件属性（交给 Icon）。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>已选内容那一层的部件属性。</summary>
    [Parameter] public Attrs? PropsValue { get; set; }

    /// <summary>多选时每个标签的部件属性（交给 Tag）。</summary>
    [Parameter] public Attrs? PropsTag { get; set; }

    /// <summary>多选时那排标签的部件属性（交给 SelectedTags）。</summary>
    [Parameter] public Attrs? PropsTags { get; set; }

    /// <summary>清空按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsClear { get; set; }

    /// <summary>下拉箭头的部件属性（交给 Icon）。</summary>
    [Parameter] public Attrs? PropsChevron { get; set; }

    /// <summary>弹出层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>搜索框的部件属性（交给 TextField）。</summary>
    [Parameter] public Attrs? PropsSearch { get; set; }

    /// <summary>树的部件属性（交给 Tree，可以写它的参数，如 PropsItem）。</summary>
    [Parameter] public Attrs? PropsTree { get; set; }

    // 没给 Values 时用同一个空数组：每次新建的空列表会被当成父级换了值
    Bindable<IReadOnlyList<string>> values { get => field ??= new(this, () => Values ?? Array.Empty<string>(), () => ValuesChanged); }

    Bindable<string?> search { get => field ??= new(this, () => Search, () => SearchChanged); }

    RemoteItems<TItem> remote { get => field ??= new(() => InvokeAsync(StateHasChanged)); }

    bool Multiple => SelectionMode == SelectionMode.Multiple;

    // 不能打开、不能改：不可用，或只读（只读时仍能聚焦、看到已选的内容）
    bool Locked => IsDisabled || IsReadOnly;

    string SearchText => search.Value ?? "";

    string ShellId => $"{ControlId}-shell";

    string PopoverId => $"{ControlId}-popover";

    string TreeId => $"{ControlId}-tree";

    string? AriaLabel => AdditionalAttributes?.TryGetValue("aria-label", out var label) == true ? label as string : null;

    // 树里的节点：远程搜索的结果，第一次回来之前用 Items
    IReadOnlyList<TItem> Nodes => remote.Data ?? Items;

    // 已加载的整棵树：key → 节点与父节点，查已选的文字、找祖先都用它
    Dictionary<string, (TItem Node, string? Parent)> NodeMap => nodeMap.Get(Nodes, this, static (nodes, owner) =>
    {
        var map = new Dictionary<string, (TItem, string?)>();
        Visit(nodes, null);
        return map;

        void Visit(IReadOnlyList<TItem> level, string? parent)
        {
            foreach (var node in level)
            {
                var key = owner.GetKey(node);
                map[key] = (node, parent);
                owner.seenLabels[key] = owner.GetLabel(node);
                Visit(owner.GetChildren(node) ?? [], key);
            }
        }
    });

    // 勾选联动要看到折叠着的节点
    TreeIndex Index => index.Get(Nodes, this, static (nodes, owner) => TreeData.Index(nodes, new TreeAccessors<TItem>(owner.GetKey, owner.GetChildren)));

    IReadOnlyList<string> Selected => Multiple ? values.Value : CurrentValue is { } value ? [value] : [];

    /// <summary>触发框里显示的节点：多选时只列最上层的整枝（父节点也勾上了的不重复列），Child 时列叶子。</summary>
    IReadOnlyList<string> DisplayKeys => Multiple ? ByStrategy(Selected, CheckStrategy == CheckStrategy.Child ? CheckStrategy.Child : CheckStrategy.Parent) : Selected;

    // 本地搜索交给 Tree 的过滤（命中的节点与祖先、展开祖先、命中的加粗）；有 Load 或关掉本地过滤时结果已经是服务端筛好的
    bool LocalSearch => Searchable && SearchText.Trim().Length > 0 && Load is null && LocalFilter;

    string? LocalQuery => LocalSearch && Filter is null ? SearchText.Trim() : null;

    Func<TItem, bool>? LocalMatch => LocalSearch && Filter is { } filter ? node => filter(node, SearchText.Trim()) : null;

    // 服务端搜索的结果全部展开（只有命中的节点与它们的祖先）；清空搜索回到自己展开的状态
    IReadOnlyList<string> ShownExpanded => Load is not null && SearchText.Trim().Length > 0 ? [.. NodeMap.Keys] : expanded;

    string? EmptyText => remote.Failed ? Config.Lang.ListLoadFailed : SearchText.Trim().Length > 0 ? Config.Lang.SelectNoMatch : null;

    EventCallback<string> RemoveCallback => removeCallback ??= EventCallback.Factory.Create<string>(this, RemoveAsync);

    Attrs RefocusTrigger => refocusTrigger.Get(ControlId, static id => new Attrs { ["data-rune-refocus"] = id });

    string LabelOf(string key) => NodeMap.TryGetValue(key, out var found) ? GetLabel(found.Node) : seenLabels.GetValueOrDefault(key) ?? key;

    /// <summary>按取法整理勾上的节点：已加载的按树的顺序与父子关系，还没加载（或远程搜索换掉了）的原样跟在后面。</summary>
    IReadOnlyList<string> ByStrategy(IReadOnlyList<string> keys, CheckStrategy strategy) =>
        [.. TreeData.CheckedByStrategy(Index, keys, strategy), .. keys.Where(key => !Index.Parents.ContainsKey(key))];

    // 外壳：点在空白处时 JS 把焦点交给触发按钮并切换打开（data-rune-control）；箭头按 data-state 转向
    Attrs ShellManaged => shellManaged.Get((ShellId, open, IsDisabled), static k => Attrs.Merge(ShellAttrs, new Attrs
    {
        Id = k.Id,
        ["data-state"] = k.Open ? "open" : "closed",
        ["data-disabled"] = k.Disabled ? true : null,
    }));

    // 触发按钮：combobox 的角色与状态、打开弹层（popovertarget）；↓ 打开由 JS 处理（data-rune-tree-trigger），多选时 Backspace 移除最后一个标签
    Attrs TriggerManaged(bool tagged) => triggerManaged.Get((ControlAttrs, open, Locked, IsReadOnly, Multiple, tagged), this, static (k, owner) => new Attrs
    {
        Sx = k.Tagged ? SelectStyles.TriggerBehindTags : (Sx?)null,
        Id = k.Control.Id,
        Role = "combobox",
        ["type"] = "button",
        ["disabled"] = k.Control["disabled"],
        ["popovertarget"] = k.Locked ? null : owner.PopoverId,
        ["aria-haspopup"] = "tree",
        ["aria-expanded"] = k.Open ? "true" : "false",
        ["aria-controls"] = owner.TreeId,
        ["aria-invalid"] = k.Control["aria-invalid"],
        ["aria-required"] = k.Control["aria-required"],
        ["aria-readonly"] = k.ReadOnly ? "true" : null,
        AriaDescribedBy = k.Control.AriaDescribedBy,
        ["data-rune-tree-trigger"] = k.Locked ? null : true,
        ["data-rune-backspace"] = k.Multiple && !k.Locked ? "remove-last" : null,
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(owner, owner.CommandAsync),
    });

    // 弹出层：贴着外壳、同宽（至少放得下几层缩进）、限高
    Attrs PanelManaged => Attrs.Merge(Floating.Placed(Placement, null), new Attrs
    {
        Sx = PanelWidth,
        Id = PopoverId,
        ["popover"] = "auto",
        ["data-rune-overlay"] = true,
        ["data-anchor"] = ShellId,
        ["data-match-width"] = true,
        ["data-fit-height"] = true,
        ["onrunetoggle"] = EventCallback.Factory.Create<OverlayToggleEventArgs>(this, ToggledAsync),
    });

    // 树：名称取 Field 的标签（或触发按钮上的 aria-label）；没有搜索框时打开后焦点在树上
    Attrs TreeManaged => new()
    {
        Id = TreeId,
        AriaLabelledBy = AriaLabel is null ? Field?.LabelId : null,
        AriaLabel = AriaLabel,
        ["autofocus"] = Searchable ? null : true,
    };

    Attrs SearchDefaults => searchDefaults.Get(Config.Lang.SelectSearch, static label => new Attrs
    {
        Sx = SearchWidth,
        ["placeholder"] = label,
        ["aria-label"] = label,
    });

    // 搜索框：打开后焦点在这里；↓ 进到树里、Tab 关闭（JS，data-rune-tree-search）
    Attrs SearchManaged => new()
    {
        ["autocomplete"] = "off",
        ["autofocus"] = true,
        ["aria-controls"] = TreeId,
        ["data-rune-tree-search"] = true,
    };

    static Style SearchWidth => Css.Define(static () => new Style { Width = CssLength.Percent(100) });

    // 弹出层比触发框窄时也放得下几层缩进
    static Style PanelWidth => Css.Define(static () => new Style { MinWidth = CssLength.Em(16) });

    void SetExpanded(IReadOnlyList<string> keys) => expanded = keys;

    // 单选：Tree 报告新的选择（再点已选的一项时是空的：不取消，同 Select）；关闭弹层由 JS 在选中时做
    Task PickAsync(IReadOnlyList<string> keys) => Locked || keys.Count == 0 ? Task.CompletedTask : SetValueAsync(keys[0]);

    /// <summary>勾选变了（树、移除标签）：按取法写回，还没加载的已选节点保留。</summary>
    Task SetCheckedAsync(IReadOnlyList<string> checkedKeys)
    {
        if (Locked) return Task.CompletedTask;
        var unknown = Selected.Where(key => !Index.Parents.ContainsKey(key));
        return values.SetAsync([.. TreeData.CheckedByStrategy(Index, checkedKeys, CheckStrategy), .. unknown]);
    }

    /// <summary>移除一个标签：取消整枝。</summary>
    Task RemoveAsync(string key) => SetCheckedAsync(Index.Parents.ContainsKey(key) ? TreeData.ToggleChecked(Index, Selected, key) : [.. Selected.Where(k => k != key)]);

    Task ClearAsync() => Multiple ? values.SetAsync([]) : SetValueAsync(null);

    Task CommandAsync(CommandEventArgs e) =>
        e.Name == "remove-last" && Multiple && !Locked && DisplayKeys.Count > 0 ? RemoveAsync(DisplayKeys[^1]) : Task.CompletedTask;

    async Task SetSearchAsync(string? text)
    {
        await search.SetAsync(text);
        if (open && Load is not null) await remote.RequestAsync(Load, text ?? "", debounce: true);
    }

    async Task ToggledAsync(OverlayToggleEventArgs e)
    {
        if (open == e.Open) return;
        open = e.Open;
        if (open)
        {
            // 打开时展开已选节点的祖先：已选的看得见
            var ancestors = expanded.ToHashSet();
            foreach (var key in Selected)
                for (var at = NodeMap.GetValueOrDefault(key).Parent; at is not null; at = NodeMap.GetValueOrDefault(at).Parent)
                    ancestors.Add(at);
            expanded = [.. ancestors];
            await OpenChanged.InvokeAsync(true);
            // 打开就按当前搜索词请求，不等防抖
            if (Load is not null) await remote.RequestAsync(Load, SearchText, debounce: false);
            return;
        }
        remote.Cancel();
        // 关闭时清空搜索词：下次打开是整棵树
        if (SearchText.Length > 0) await search.SetAsync("");
        await OpenChanged.InvokeAsync(false);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        base.OnDispose();
        remote.Dispose();
    }
}
