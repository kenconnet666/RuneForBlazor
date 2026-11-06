using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>一项的渲染状态（自定义内容 ChildContent、按节点计算的部件属性拿到它）。</summary>
/// <param name="Node">节点的原始数据。</param>
/// <param name="Key">节点的 key。</param>
/// <param name="Depth">层级，第一层为 0。</param>
/// <param name="Expanded">展开着。</param>
/// <param name="Selected">已选。</param>
/// <param name="Checked">勾选状态（Checkable 时）。</param>
public sealed record TreeItemState<TItem>(TItem Node, string Key, int Depth, bool Expanded, bool Selected, CheckState Checked);

/// <summary>一次移动：把 Key 移到 Target（交给 <see cref="TreeData.Move{T}"/> 生成新数据）。</summary>
/// <param name="Key">移动的节点的 key。</param>
/// <param name="Node">移动的节点。</param>
/// <param name="Target">放到哪个节点下的第几位（按移走之后的同级算）。</param>
public sealed record TreeMove<TItem>(string Key, TItem Node, TreeMoveTarget Target);

/// <summary>子节点加载失败（LoadChildren 抛出了异常）：节点已经收回去，再展开会重试。</summary>
/// <param name="Exception">异常。</param>
/// <param name="Node">展开的节点。</param>
public sealed record TreeLoadError<TItem>(Exception Exception, TItem Node);

/// <summary>树：可展开的层级列表，单选 / 多选、父子联动的勾选、懒加载、搜索过滤、方向键导航、节点多时虚拟滚动（用法见 Tree.razor 顶部）。</summary>
/// <typeparam name="TItem">节点的数据类型。</typeparam>
public partial class Tree<TItem>
{
    static readonly Attrs VisuallyHiddenLayer = new() { Sx = SharedStyles.VisuallyHidden };
    static readonly Attrs IndentLayer = new() { Sx = TreeStyles.IndentSpacer };
    static readonly Rune.Styles.Sx CollapsedChevron = Rune.Styles.Sx.Of(DisclosureStyles.Icon);
    static readonly Rune.Styles.Sx ExpandedChevron = Rune.Styles.Sx.Of(DisclosureStyles.Icon, DisclosureStyles.Turn);

    /// <summary>行数超过这么多（且在交互模式下）时虚拟滚动；不多时全部渲染：DOM 小，滚动、页内查找都是原生的。</summary>
    public const int VirtualThreshold = 100;

    // 加载中显示几行骨架，一行比一行短
    const int SkeletonRows = 4;
    static readonly Attrs[] SkeletonWidths = [.. Enumerable.Range(0, SkeletonRows).Select(row => new Attrs { InlineStyle = $"width:{85 - row * 15}%" })];

    readonly string generatedId = UniqueId.Next("tree");
    readonly HashSet<string> loadingKeys = [];
    // 过滤时的展开状态单独记：一开始展开匹配项的祖先，可以再收起；清空过滤后回到原来的 Expanded
    IReadOnlyList<string> filterExpanded = [];
    TreeFilter? lastFilter;
    string? announcement;

    // 过滤结果、勾选索引与状态可能是 null（不过滤、不联动），不用 Memo（它只缓存引用类型的非空结果）
    (IReadOnlyList<TItem> Items, string? Query, Func<TItem, bool>? Match, UIConfig Config)? filterInput;
    TreeFilter? filterResult;
    (IReadOnlyList<TItem> Items, bool Linked)? indexInput;
    TreeIndex? indexResult;
    (TreeIndex? Index, IReadOnlyList<string> Checked)? statesInput;
    IReadOnlyDictionary<string, CheckState>? statesResult;
    Memo<(IReadOnlyList<TItem> Items, IReadOnlyList<string> Expanded, TreeFilter? Filter), IReadOnlyList<FlatTreeNode<TItem>>> rows;
    Memo<IReadOnlyList<string>, HashSet<string>> selectedSet, checkedSet, expandedSet;
    Memo<int, int[]> rowIndexes;
    Memo<IReadOnlyList<FlatTreeNode<TItem>>, TreeTables> tables;

    // 交给 JS 的两张表：选项表（文字与不可用，同列表）、结构表（父行、能否展开、是否展开）
    sealed record TreeTables(string Options, string Structure);

    Bindable<IReadOnlyList<string>> expanded { get => field ??= new(this, () => Expanded ?? [], () => ExpandedChanged); }
    Bindable<IReadOnlyList<string>> selected { get => field ??= new(this, () => Selected ?? [], () => SelectedChanged); }
    Bindable<IReadOnlyList<string>> checkedKeys { get => field ??= new(this, () => Checked ?? [], () => CheckedChanged); }

    DelayedLoading? delayedLoading;
    DelayedLoading indicator => delayedLoading ??= new(() => InvokeAsync(StateHasChanged));

    RenderFragment? defaultEmpty;
    RenderFragment DefaultEmpty => defaultEmpty ??= builder =>
    {
        builder.OpenComponent<Empty>(0);
        builder.AddComponentParameter(1, nameof(UI.Empty.Size), Size.Sm);
        builder.AddComponentParameter(2, nameof(UI.Empty.HideIcon), true);
        builder.AddComponentParameter(3, nameof(UI.Empty.Description), (RenderFragment)(b => b.AddContent(0, Filtering is null ? Config.Lang.TreeEmpty : Config.Lang.TreeNoMatch)));
        builder.AddMultipleAttributes(4, Attrs.MergeProps(PropsEmpty));
        builder.CloseComponent();
    };

    /// <summary>第一层的节点。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>节点的 key，整棵树里唯一。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetKey { get; set; } = null!;

    /// <summary>节点的文字：默认显示它，类型搜索、过滤也按它。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetLabel { get; set; } = null!;

    /// <summary>子节点；叶子或还没加载时返回 null 或空列表。</summary>
    [Parameter, EditorRequired] public Func<TItem, IReadOnlyList<TItem>?> GetChildren { get; set; } = null!;

    /// <summary>有没有子节点（懒加载时子节点还没取回来也要能展开）；不写时看 GetChildren 是否非空。</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>
    /// 展开一个还没有子节点的节点时调用：取回子节点并放进数据里（把新的 Items 交回来）；进行中显示加载指示。
    /// 是 EventCallback：完成后使用方跟着重新渲染，新数据才交得回来。
    /// </summary>
    [Parameter] public EventCallback<TItem> LoadChildren { get; set; }

    /// <summary>子节点加载失败：节点收回去，再展开会重试；不写时异常交给外层的 ErrorBoundary。</summary>
    [Parameter] public EventCallback<TreeLoadError<TItem>> OnLoadError { get; set; }

    /// <summary>节点不可用：可以经过、展开，不能选中、勾选。</summary>
    [Parameter] public Func<TItem, bool>? IsDisabled { get; set; }

    /// <summary>文字前的图标，可以按展开状态换（文件夹开 / 合）。</summary>
    [Parameter] public Func<TItem, bool, IconData?>? GetIcon { get; set; }

    /// <summary>展开的节点。</summary>
    [Parameter] public IReadOnlyList<string>? Expanded { get; set; }

    /// <summary>展开的节点变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> ExpandedChanged { get; set; }

    /// <summary>选择方式；不写时不能选中。</summary>
    [Parameter] public SelectionMode? SelectionMode { get; set; }

    /// <summary>选中的节点。</summary>
    [Parameter] public IReadOnlyList<string>? Selected { get; set; }

    /// <summary>选中的节点变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> SelectedChanged { get; set; }

    /// <summary>每项前显示勾选框，默认父子联动。</summary>
    [Parameter] public bool Checkable { get; set; }

    /// <summary>勾选父子不联动：每个节点各自勾选（权限树里“只给这一级”）。</summary>
    [Parameter] public bool CheckStrictly { get; set; }

    /// <summary>勾上的节点（联动时含勾上的父节点与子孙）。</summary>
    [Parameter] public IReadOnlyList<string>? Checked { get; set; }

    /// <summary>勾上的节点变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> CheckedChanged { get; set; }

    /// <summary>正在取节点：还没有节点时显示骨架行（很快返回时不显示），树带 aria-busy。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>没有节点时显示的内容；不写时是 Empty（文案取语言包）。</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>每一层缩进多少（em），默认 1.25。</summary>
    [Parameter] public double Indent { get; set; } = 1.25;

    /// <summary>每项的估计高度（px），虚拟滚动据此算占位，渲染后以实际为准；默认 32。</summary>
    [Parameter] public float EstimateSize { get; set; } = 32;

    /// <summary>
    /// 搜索过滤：按节点文字匹配（忽略大小写与重音，或 UIConfig.TextMatcher 的拼音）。留下匹配的节点与它们的祖先并展开祖先，
    /// 匹配的节点文字加粗；只看已加载的节点。清空后回到原来的展开状态。
    /// </summary>
    [Parameter] public string? Filter { get; set; }

    /// <summary>自己判断的搜索过滤（写了时代替 <see cref="Filter"/>）。</summary>
    [Parameter] public Func<TItem, bool>? FilterBy { get; set; }

    /// <summary>
    /// 移动节点：拖动（放在一项的上沿、下沿是放到它前后，中间是放进它里面，停在收起的节点上会展开它；只认鼠标、笔，触摸时照常滚动）
    /// 或 Alt + 方向键。报告 key 与目标，用 <see cref="TreeData.Move{T}"/> 生成新数据交回 Items；不写时不能移动。
    /// </summary>
    [Parameter] public EventCallback<TreeMove<TItem>> OnMove { get; set; }

    /// <summary>能不能放到这里（如文件不能放进文件里）；不写时都能放（放进自己和自己的子孙里除外）。</summary>
    [Parameter] public Func<TItem, TreeMoveTarget, bool>? CanDrop { get; set; }

    /// <summary>自定义文字部分的内容。</summary>
    [Parameter] public RenderFragment<TreeItemState<TItem>>? ChildContent { get; set; }

    /// <summary>每一项的部件属性。</summary>
    [Parameter] public Attrs? PropsItem { get; set; }

    /// <summary>按节点计算的每一项的部件属性（写了时代替 <see cref="PropsItem"/>）。</summary>
    [Parameter] public Func<TreeItemState<TItem>, Attrs?>? PropsItemFor { get; set; }

    /// <summary>展开箭头的部件属性。</summary>
    [Parameter] public Attrs? PropsToggle { get; set; }

    /// <summary>勾选框的部件属性。</summary>
    [Parameter] public Attrs? PropsCheckbox { get; set; }

    /// <summary>文字的部件属性。</summary>
    [Parameter] public Attrs? PropsLabel { get; set; }

    /// <summary>默认空状态（Empty）的部件属性。</summary>
    [Parameter] public Attrs? PropsEmpty { get; set; }

    /// <summary>加载中的骨架行的部件属性。</summary>
    [Parameter] public Attrs? PropsSkeleton { get; set; }

    /// <summary>外层滚动区域（ScrollArea）的部件属性：放进弹出层、弹性布局时给它 Sx；树元素本身是它的视口。</summary>
    [Parameter] public Attrs? PropsScrollArea { get; set; }

    string TreeId => AdditionalAttributes?.GetValueOrDefault("id") as string ?? generatedId;

    TreeAccessors<TItem> Accessors => field is { } cached && cached.GetKey == GetKey && cached.GetChildren == GetChildren && cached.HasChildren == HasChildren
        ? cached
        : field = new(GetKey, GetChildren, HasChildren);

    TreeFilter? Filtering
    {
        get
        {
            var input = (Items, string.IsNullOrWhiteSpace(Filter) ? null : Filter.Trim(), FilterBy, Config);
            if (filterInput == input) return filterResult;
            filterInput = input;
            var (items, query, match, config) = input;
            return filterResult = match is not null ? TreeData.Filter(items, Accessors, match)
                : query is not null ? TreeData.Filter(items, Accessors, node => config.Matches(GetLabel(node), query))
                : null;
        }
    }

    IReadOnlyList<string> ShownExpanded => Filtering is null ? expanded.Value : filterExpanded;

    internal IReadOnlyList<FlatTreeNode<TItem>> Rows => rows.Get((Items, ShownExpanded, Filtering), this,
        static (k, owner) => TreeData.Flatten(k.Items, owner.Accessors, owner.expandedSet.Get(k.Expanded, static keys => [.. keys]), k.Filter?.Include));

    int[] RowIndexes => rowIndexes.Get(Rows.Count, static count => [.. Enumerable.Range(0, count)]);

    bool IsVirtual => RendererInfo.IsInteractive && Rows.Count > VirtualThreshold;

    // 勾选联动要看到折叠着的节点：只在联动勾选时建整棵树的索引
    TreeIndex? Index
    {
        get
        {
            var input = (Items, Checkable && !CheckStrictly);
            if (indexInput == input) return indexResult;
            indexInput = input;
            return indexResult = input.Item2 ? TreeData.Index(Items, Accessors) : null;
        }
    }

    IReadOnlyDictionary<string, CheckState>? CheckStates
    {
        get
        {
            var input = (Index, checkedKeys.Value);
            if (statesInput == input) return statesResult;
            statesInput = input;
            return statesResult = input.Index is { } i ? TreeData.CheckStates(i, input.Value) : null;
        }
    }

    TreeItemState<TItem> StateOf(FlatTreeNode<TItem> row) => new(
        row.Node,
        row.Key,
        row.Depth,
        row.Expandable && ShownExpanded.Contains(row.Key),
        selectedSet.Get(selected.Value, static keys => [.. keys]).Contains(row.Key),
        CheckStates is { } states ? states.GetValueOrDefault(row.Key)
            : checkedSet.Get(checkedKeys.Value, static keys => [.. keys]).Contains(row.Key) ? CheckState.Checked : CheckState.Unchecked);

    Attrs? PropsItemOf(TItem node, TreeItemState<TItem> state) => PropsItemFor is { } compute ? compute(state) : PropsItem;

    // 树上由组件管理的属性：可聚焦、交给 JS 的键盘约定与各行的结构（看不见的行不在 DOM 里，键盘照样能移到）
    Attrs RootManaged
    {
        get
        {
            var (options, structure) = tables.Get(Rows, this, static (rows, owner) => owner.Tables(rows));
            return new()
            {
                Id = TreeId,
                Role = "tree",
                ["tabindex"] = Rows.Count > 0 ? "0" : "-1",
                ["aria-multiselectable"] = SelectionMode == UI.SelectionMode.Multiple ? "true" : null,
                ["aria-busy"] = Loading ? "true" : null,
                ["data-rune-list"] = "typeahead",
                ["data-rune-tree"] = OnMove.HasDelegate ? "move" : "",
                ["data-rune-options"] = options,
                ["data-rune-tree-rows"] = structure,
                ["data-rune-selected"] = FirstSelectedRow is >= 0 and var first ? first.ToString(CultureInfo.InvariantCulture) : null,
                ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(this, OnCommandAsync),
            };
        }
    }

    // 第一次进入树时落在第一个已选的看得见的节点上（它可能没渲染）
    int FirstSelectedRow
    {
        get
        {
            var keys = selectedSet.Get(selected.Value, static keys => [.. keys]);
            for (var i = 0; i < Rows.Count; i++)
                if (keys.Contains(Rows[i].Key)) return i;
            return -1;
        }
    }

    Attrs ItemManaged(int index, FlatTreeNode<TItem> row, TreeItemState<TItem> state, bool disabled, bool loadingChildren) => new()
    {
        Id = $"{TreeId}-{index}",
        Role = "treeitem",
        ["data-index"] = index.ToString(CultureInfo.InvariantCulture),
        ["aria-level"] = (row.Depth + 1).ToString(CultureInfo.InvariantCulture),
        ["aria-setsize"] = row.SetSize.ToString(CultureInfo.InvariantCulture),
        ["aria-posinset"] = row.Position.ToString(CultureInfo.InvariantCulture),
        ["aria-expanded"] = row.Expandable ? (state.Expanded ? "true" : "false") : null,
        ["aria-selected"] = SelectionMode is null ? null : state.Selected ? "true" : "false",
        ["aria-checked"] = !Checkable ? null : state.Checked switch
        {
            CheckState.Checked => "true",
            CheckState.Indeterminate => "mixed",
            _ => "false",
        },
        ["aria-disabled"] = disabled ? "true" : null,
        ["aria-busy"] = loadingChildren ? "true" : null,
        ["data-state"] = state.Selected ? "selected" : null,
        Vars = row.Depth > 0 ? [TreeStyles.Indent.Set(CssLength.Em(row.Depth * Indent))] : null,
    };

    TreeTables Tables(IReadOnlyList<FlatTreeNode<TItem>> rows)
    {
        var rowOf = new Dictionary<string, int>(rows.Count);
        for (var i = 0; i < rows.Count; i++) rowOf[rows[i].Key] = i;
        // 不可用的节点也能经过（同 vue-tsx-pro）：选项表里不标不可用，选中、勾选时由 C# 拦下
        var options = ListboxInterop.OptionTable(rows.Select(row => new ListEntry<FlatTreeNode<TItem>>(new ListboxOption(row.Key, GetLabel(row.Node)), row)).ToArray());
        var structure = ListboxInterop.TreeStructure(
            rows.Select(row => row.Parent is { } parent ? rowOf[parent] : -1),
            rows.Select((row, i) => (row, i)).Where(x => x.row.Expandable).Select(x => x.i),
            rows.Select((row, i) => (row, i)).Where(x => x.row.Expandable && ShownExpanded.Contains(x.row.Key)).Select(x => x.i));
        return new(options, structure);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        indicator.Update(Loading);
        // 过滤结果变了（换了搜索词、数据）：展开匹配项的祖先
        var filter = Filtering;
        if (ReferenceEquals(filter, lastFilter)) return;
        lastFilter = filter;
        filterExpanded = filter is null ? [] : [.. filter.Expand];
    }

    /// <inheritdoc/>
    protected override void OnDispose() => delayedLoading?.Dispose();

    async Task OnCommandAsync(CommandEventArgs e)
    {
        var parts = (e.Text ?? "").Split(':');
        if (!int.TryParse(parts[0], CultureInfo.InvariantCulture, out var position) || position < 0 || position >= Rows.Count) return;
        var row = Rows[position];
        switch (e.Name)
        {
            case "expand":
                await SetOpenAsync(row, true);
                break;
            case "collapse":
                await SetOpenAsync(row, false);
                break;
            case "enter":
                if (SelectionMode is not null) await SelectAsync(row);
                else await ToggleAsync(row);
                break;
            case "space":
                if (Checkable) await CheckAsync(row);
                else await SelectAsync(row);
                break;
            case "move" when parts.Length > 1:
                await MoveByKeyAsync(row, parts[1]);
                break;
            // 指针拖动松开：{被拖的行}:{落点的行}:{before | after | inside}
            case "drop" when parts.Length > 2 && int.TryParse(parts[1], CultureInfo.InvariantCulture, out var at) && at >= 0 && at < Rows.Count:
                if (TargetOf(row, Rows[at], parts[2]) is { } target) await EmitMoveAsync(row, target);
                break;
        }
    }

    Task OnItemClickAsync(FlatTreeNode<TItem> row) =>
        SelectionMode is not null ? SelectAsync(row) : Checkable ? CheckAsync(row) : ToggleAsync(row);

    Task ToggleAsync(FlatTreeNode<TItem> row) => row.Expandable ? SetOpenAsync(row, !ShownExpanded.Contains(row.Key)) : Task.CompletedTask;

    Task SelectAsync(FlatTreeNode<TItem> row)
    {
        if (SelectionMode is not { } mode || (IsDisabled?.Invoke(row.Node) ?? false)) return Task.CompletedTask;
        return ListState.Toggle(selected.Value, row.Key, mode, required: false) is { } next ? selected.SetAsync(next) : Task.CompletedTask;
    }

    Task CheckAsync(FlatTreeNode<TItem> row)
    {
        if (!Checkable || (IsDisabled?.Invoke(row.Node) ?? false)) return Task.CompletedTask;
        var next = Index is { } index
            ? TreeData.ToggleChecked(index, checkedKeys.Value, row.Key)
            : ListState.Toggle(checkedKeys.Value, row.Key, UI.SelectionMode.Multiple, required: false) ?? checkedKeys.Value;
        return checkedKeys.SetAsync(next);
    }

    Task SetExpandedAsync(IReadOnlyList<string> keys)
    {
        if (Filtering is null) return expanded.SetAsync(keys);
        filterExpanded = keys;
        return Task.CompletedTask;
    }

    /// <summary>展开或收起；展开一个还没有子节点的节点时懒加载。</summary>
    async Task SetOpenAsync(FlatTreeNode<TItem> row, bool open)
    {
        var current = ShownExpanded;
        if (current.Contains(row.Key) == open) return;
        if (!open)
        {
            await SetExpandedAsync([.. current.Where(key => key != row.Key)]);
            return;
        }
        await SetExpandedAsync([.. current, row.Key]);
        // 正在加载（收起后又展开）：等那一次，不重复请求
        if (!LoadChildren.HasDelegate || loadingKeys.Contains(row.Key) || (GetChildren(row.Node)?.Count ?? 0) > 0) return;
        loadingKeys.Add(row.Key);
        StateHasChanged();
        try
        {
            await LoadChildren.InvokeAsync(row.Node);
        }
        catch (Exception exception)
        {
            // 失败时收回去：留着展开却没有子节点，看起来像是本来就没有；再展开会重试
            await SetExpandedAsync([.. ShownExpanded.Where(key => key != row.Key)]);
            if (OnLoadError.HasDelegate) await OnLoadError.InvokeAsync(new(exception, row.Node));
            else await DispatchExceptionAsync(exception);
        }
        finally
        {
            loadingKeys.Remove(row.Key);
            StateHasChanged();
        }
    }

    /// <summary>Alt + ↑↓ 在同级里移动，Alt + 向内（从左到右时是 →）放进上一个同级的最后，Alt + 向外移到父节点后面。</summary>
    async Task MoveByKeyAsync(FlatTreeNode<TItem> row, string direction)
    {
        TreeMoveTarget? target = null;
        switch (direction)
        {
            case "up" when row.Position > 1:
                target = new(row.Parent, row.Position - 2);
                break;
            case "down" when row.Position < row.SetSize:
                target = new(row.Parent, row.Position);
                break;
            case "in":
                var previous = Rows.FirstOrDefault(item => item.Parent == row.Parent && item.Position == row.Position - 1);
                if (previous.Key is null) break;
                target = new(previous.Key, int.MaxValue);
                // 展开新的父节点：移过去之后还看得见
                await SetOpenAsync(previous, true);
                break;
            case "out":
                var parent = Rows.FirstOrDefault(item => item.Key == row.Parent);
                if (parent.Key is not null) target = new(parent.Parent, parent.Position);
                break;
        }
        if (target is { } t && (CanDrop is null || CanDrop(row.Node, t))) await EmitMoveAsync(row, t);
    }

    /// <summary>落点换算成移动的目标（按移走之后的同级算）；放进自己或自己的子孙里、CanDrop 不让放时为 null。</summary>
    TreeMoveTarget? TargetOf(FlatTreeNode<TItem> moving, FlatTreeNode<TItem> row, string zone)
    {
        // 落点是被拖的节点自己或它的子孙（看得见的行里，祖先总是看得见的）
        for (var at = row.Key; at is not null; at = Rows.FirstOrDefault(item => item.Key == at).Parent)
            if (at == moving.Key) return null;
        TreeMoveTarget target;
        if (zone == "inside") target = new(row.Key, int.MaxValue);
        else
        {
            var offset = zone == "before" ? row.Position - 1 : row.Position;
            // 同一层里从前面移过来：移走之后，后面的都往前挪了一位
            var shift = moving.Parent == row.Parent && moving.Position <= offset ? 1 : 0;
            target = new(row.Parent, offset - shift);
        }
        return CanDrop is null || CanDrop(moving.Node, target) ? target : null;
    }

    /// <summary>报告移动，并告诉读屏移到了哪里。</summary>
    async Task EmitMoveAsync(FlatTreeNode<TItem> row, TreeMoveTarget target)
    {
        var parent = target.Parent is null ? (FlatTreeNode<TItem>?)null : Rows.FirstOrDefault(item => item.Key == target.Parent);
        // 移走之后的同级个数：同一层里移动时要去掉自己
        var count = (parent is { } p ? GetChildren(p.Node)?.Count ?? 0 : Items.Count) - (row.Parent == target.Parent ? 1 : 0);
        await OnMove.InvokeAsync(new(row.Key, row.Node, target));
        announcement = Config.Lang.TreeMoved(GetLabel(row.Node), parent is { } q ? GetLabel(q.Node) : null, Math.Min(target.Index, count) + 1);
    }
}
