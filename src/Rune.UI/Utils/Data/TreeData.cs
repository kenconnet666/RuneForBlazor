namespace Rune.UI;

/// <summary>
/// 怎样从数据里读出树的结构（同 vue-tsx-pro 的 TreeAccessors）：数据形状不限，Tree、TreeSelect、TreeTransfer 共用。
/// </summary>
/// <param name="GetKey">节点的 key，整棵树里唯一。</param>
/// <param name="GetChildren">子节点；叶子或还没加载时返回 null 或空列表。</param>
/// <param name="HasChildren">有没有子节点（懒加载时子节点还没取回来也算有）；不写时看 GetChildren 是否非空。</param>
public sealed record TreeAccessors<T>(Func<T, string> GetKey, Func<T, IReadOnlyList<T>?> GetChildren, Func<T, bool>? HasChildren = null)
{
    /// <summary>节点能不能展开。</summary>
    public bool IsExpandable(T node) => HasChildren?.Invoke(node) ?? (GetChildren(node)?.Count ?? 0) > 0;
}

/// <summary>按展开状态展开成一列的一行。</summary>
/// <param name="Node">原始数据。</param>
/// <param name="Key">节点的 key。</param>
/// <param name="Depth">层级，第一层为 0。</param>
/// <param name="Parent">父节点的 key；第一层为 null。</param>
/// <param name="Expandable">能不能展开（有子节点，或懒加载时声明了有）。</param>
/// <param name="SetSize">同级一共几个（aria-setsize）。</param>
/// <param name="Position">在同级里排第几，从 1 开始（aria-posinset）。</param>
public readonly record struct FlatTreeNode<T>(T Node, string Key, int Depth, string? Parent, bool Expandable, int SetSize, int Position);

/// <summary>整棵（已加载的）树的索引：勾选联动要看到折叠着的节点。</summary>
/// <param name="Order">深度优先的顺序。</param>
/// <param name="Parents">key → 父节点的 key（第一层为 null）。</param>
/// <param name="Children">key → 已加载的子节点的 key。</param>
public sealed record TreeIndex(IReadOnlyList<string> Order, IReadOnlyDictionary<string, string?> Parents, IReadOnlyDictionary<string, IReadOnlyList<string>> Children);

/// <summary>勾选状态。</summary>
public enum CheckState
{
    /// <summary>没勾。</summary>
    Unchecked,

    /// <summary>勾上了。</summary>
    Checked,

    /// <summary>子孙里有勾上的，自己没有全勾上（半选，aria-checked="mixed"）。</summary>
    Indeterminate,
}

/// <summary>勾选结果的取法（同 Ant Design 的 showCheckedStrategy）。</summary>
public enum CheckStrategy
{
    /// <summary>勾上的全部节点（默认）。</summary>
    All,

    /// <summary>父节点全勾上时只要父节点，不再列出它的子孙（提交“整个部门”而不是逐个成员）。</summary>
    Parent,

    /// <summary>只要叶子（没有已加载的子节点的节点）。</summary>
    Child,
}

/// <summary>搜索过滤的结果。</summary>
/// <param name="Include">要显示的节点：匹配的节点与它们的祖先（交给 <see cref="TreeData.Flatten{T}"/>）。</param>
/// <param name="Expand">要展开的节点：匹配的节点的祖先，展开后才看得到匹配项。</param>
/// <param name="Matched">匹配的节点（高亮用）。</param>
public sealed record TreeFilter(IReadOnlySet<string> Include, IReadOnlySet<string> Expand, IReadOnlySet<string> Matched);

/// <summary>移动的目标：放到哪个节点下（null 是第一层）的第几位（按移走之后的同级算，超出时放到最后）。</summary>
/// <param name="Parent">新的父节点的 key；第一层时为 null。</param>
/// <param name="Index">在新的同级里的位置。</param>
public readonly record struct TreeMoveTarget(string? Parent, int Index);

/// <summary>
/// 树状数据的共用逻辑（同 vue-tsx-pro 的 utils/data/tree.ts）：按展开状态展开成一列行、勾选的父子联动（含半选）、
/// 搜索过滤、勾选结果的取法、到某个节点的路径、平铺的行组装成树、移动节点。都不改原数据。
/// <code>
/// var rows = TreeData.Flatten(roots, accessors, expanded);
/// var index = TreeData.Index(roots, accessors);
/// checkedKeys = TreeData.ToggleChecked(index, checkedKeys, key);   // 勾上 / 取消一项，子孙与祖先跟着变
/// var states = TreeData.CheckStates(index, checkedKeys);            // key → 勾选状态
/// var filter = TreeData.Filter(roots, accessors, node => node.Name.Contains(query));
/// roots = TreeData.Move(roots, accessors, (node, children) => node with { Children = children }, key, new(parent: "b", index: 0));
/// </code>
/// </summary>
public static class TreeData
{
    /// <summary>
    /// 按展开状态深度优先展开成一列：只有展开了的节点的子节点出现在结果里；include 不为 null 时只要其中的节点（搜索过滤）；
    /// order 给出同级之间的顺序（TreeDataTable 按当前排序），不写时按数据里的顺序。
    /// </summary>
    public static IReadOnlyList<FlatTreeNode<T>> Flatten<T>(IReadOnlyList<T> roots, TreeAccessors<T> accessors, IReadOnlySet<string> expanded, IReadOnlySet<string>? include = null,
        Func<IReadOnlyList<T>, IReadOnlyList<T>>? order = null)
    {
        var result = new List<FlatTreeNode<T>>();
        Visit(roots, 0, null);
        return result;

        void Visit(IReadOnlyList<T> level, int depth, string? parent)
        {
            var shown = new List<(T Node, string Key)>(level.Count);
            foreach (var node in order?.Invoke(level) ?? level)
            {
                var key = accessors.GetKey(node);
                if (include is null || include.Contains(key)) shown.Add((node, key));
            }
            for (var position = 0; position < shown.Count; position++)
            {
                var (node, key) = shown[position];
                var expandable = accessors.IsExpandable(node);
                result.Add(new(node, key, depth, parent, expandable, shown.Count, position + 1));
                if (expandable && expanded.Contains(key)) Visit(accessors.GetChildren(node) ?? [], depth + 1, key);
            }
        }
    }

    /// <summary>建立整棵已加载的树的索引。</summary>
    public static TreeIndex Index<T>(IReadOnlyList<T> roots, TreeAccessors<T> accessors)
    {
        var order = new List<string>();
        var parents = new Dictionary<string, string?>();
        var children = new Dictionary<string, IReadOnlyList<string>>();
        Visit(roots, null);
        return new(order, parents, children);

        void Visit(IReadOnlyList<T> level, string? parent)
        {
            var keys = level.Select(accessors.GetKey).ToArray();
            if (parent is not null) children[parent] = keys;
            for (var position = 0; position < level.Count; position++)
            {
                order.Add(keys[position]);
                parents[keys[position]] = parent;
                Visit(accessors.GetChildren(level[position]) ?? [], keys[position]);
            }
        }
    }

    /// <summary>
    /// 勾上或取消 key：子孙跟着变；祖先的子节点全勾上时祖先也勾上，否则取消。
    /// 返回勾上的全部节点（按树的顺序），可以直接写回勾选的值。
    /// </summary>
    public static IReadOnlyList<string> ToggleChecked(TreeIndex index, IReadOnlyList<string> checkedKeys, string key)
    {
        var result = Effective(index, checkedKeys);
        var next = !result.Contains(key);
        Mark(key);
        for (var parent = index.Parents.GetValueOrDefault(key); parent is not null; parent = index.Parents.GetValueOrDefault(parent))
        {
            if (index.Children[parent].All(result.Contains)) result.Add(parent);
            else result.Remove(parent);
        }
        return [.. index.Order.Where(result.Contains)];

        void Mark(string current)
        {
            if (next) result.Add(current);
            else result.Remove(current);
            foreach (var child in index.Children.GetValueOrDefault(current) ?? []) Mark(child);
        }
    }

    /// <summary>按取法给出勾上的节点（按树的顺序）；勾选的值可以是 <see cref="ToggleChecked"/> 的结果，也可以是外部给的部分节点。</summary>
    public static IReadOnlyList<string> CheckedByStrategy(TreeIndex index, IReadOnlyList<string> checkedKeys, CheckStrategy strategy)
    {
        var effective = Effective(index, checkedKeys);
        return [.. index.Order.Where(key => effective.Contains(key) && strategy switch
        {
            CheckStrategy.Child => (index.Children.GetValueOrDefault(key)?.Count ?? 0) == 0,
            CheckStrategy.Parent => index.Parents[key] is not { } parent || !effective.Contains(parent),
            _ => true,
        })];
    }

    /// <summary>每个节点的勾选状态：子孙里有勾上的、但自己没有全勾上时是半选。</summary>
    public static IReadOnlyDictionary<string, CheckState> CheckStates(TreeIndex index, IReadOnlyList<string> checkedKeys)
    {
        var effective = Effective(index, checkedKeys);
        var states = new Dictionary<string, CheckState>(index.Order.Count);
        // 倒序：子节点先于父节点算好
        for (var i = index.Order.Count - 1; i >= 0; i--)
        {
            var key = index.Order[i];
            states[key] = effective.Contains(key) ? CheckState.Checked
                : index.Children.GetValueOrDefault(key)?.Any(child => states[child] != CheckState.Unchecked) == true ? CheckState.Indeterminate
                : CheckState.Unchecked;
        }
        return states;
    }

    /// <summary>从第一层到 key 的各节点的 key（含 key 自己）；key 不在树里时为空。</summary>
    public static IReadOnlyList<string> Path(TreeIndex index, string key)
    {
        if (!index.Parents.ContainsKey(key)) return [];
        var path = new List<string>();
        for (string? current = key; current is not null; current = index.Parents[current]) path.Add(current);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 搜索过滤：留下匹配的节点与它们的祖先，并展开祖先（只看已加载的节点）。
    /// 匹配的节点的子孙不跟着留下（它们自己匹配才留下），同 Ant Design TreeSelect 的搜索。
    /// </summary>
    public static TreeFilter Filter<T>(IReadOnlyList<T> roots, TreeAccessors<T> accessors, Func<T, bool> match)
    {
        HashSet<string> include = [], expand = [], matched = [];
        Visit(roots);
        return new(include, expand, matched);

        // 返回这一层的子树里有没有匹配的节点
        bool Visit(IReadOnlyList<T> level)
        {
            var any = false;
            foreach (var node in level)
            {
                var key = accessors.GetKey(node);
                var self = match(node);
                var below = Visit(accessors.GetChildren(node) ?? []);
                if (self) matched.Add(key);
                if (below) expand.Add(key);
                if (!self && !below) continue;
                include.Add(key);
                any = true;
            }
            return any;
        }
    }

    /// <summary>
    /// 平铺的行（每行带父 key）组装成树，返回第一层；同级保持原来的顺序，叶子的子节点是空列表。
    /// 父 key 是 null、是自己、或不在这批行里的行当作第一层。key 重复或父子成环时抛 <see cref="ArgumentException"/>。
    /// </summary>
    public static IReadOnlyList<T> Build<T>(IReadOnlyList<T> rows, Func<T, string> getKey, Func<T, string?> getParent, Func<T, IReadOnlyList<T>, T> withChildren)
    {
        var keys = new HashSet<string>();
        foreach (var row in rows)
            if (!keys.Add(getKey(row))) throw new ArgumentException($"TreeData.Build：key {getKey(row)} 出现了两次", nameof(rows));
        var roots = new List<T>();
        var childrenOf = new Dictionary<string, List<T>>();
        foreach (var row in rows)
        {
            var parent = getParent(row);
            if (parent is null || parent == getKey(row) || !keys.Contains(parent)) roots.Add(row);
            else (childrenOf.TryGetValue(parent, out var list) ? list : childrenOf[parent] = []).Add(row);
        }
        var built = 0;
        var tree = roots.Select(Node).ToArray();
        if (built < rows.Count) throw new ArgumentException("TreeData.Build：有的行的父子关系成了环，到不了第一层", nameof(rows));
        return tree;

        T Node(T row)
        {
            built++;
            return withChildren(row, [.. (childrenOf.GetValueOrDefault(getKey(row)) ?? []).Select(Node)]);
        }
    }

    /// <summary>
    /// 把 key 移到 target，返回新的第一层（沿途的节点用 withChildren 重建，其余节点原样复用）。
    /// key 不存在、目标是它自己或它的子孙时返回 null（拖到自己里面不成立）。
    /// </summary>
    public static IReadOnlyList<T>? Move<T>(IReadOnlyList<T> roots, TreeAccessors<T> accessors, Func<T, IReadOnlyList<T>, T> withChildren, string key, TreeMoveTarget target)
    {
        var index = Index(roots, accessors);
        if (!index.Parents.ContainsKey(key)) return null;
        if (target.Parent is { } targetParent && (!index.Parents.ContainsKey(targetParent) || Path(index, targetParent).Contains(key))) return null;

        T moving = default!;
        // 先摘下来：只重建 key 的祖先
        var removePath = Path(index, key).SkipLast(1).ToHashSet();
        var without = Remove(roots);
        if (target.Parent is null) return Insert(without);
        // 再插进去：只重建目标父节点的祖先（含它自己）
        var insertPath = Path(Index(without, accessors), target.Parent).ToHashSet();
        return Add(without);

        IReadOnlyList<T> Remove(IReadOnlyList<T> level)
        {
            var next = new List<T>(level.Count);
            foreach (var node in level)
            {
                var nodeKey = accessors.GetKey(node);
                if (nodeKey == key) moving = node;
                else next.Add(removePath.Contains(nodeKey) ? withChildren(node, Remove(accessors.GetChildren(node) ?? [])) : node);
            }
            return next;
        }

        IReadOnlyList<T> Insert(IReadOnlyList<T> level)
        {
            var next = level.ToList();
            next.Insert(Math.Clamp(target.Index, 0, next.Count), moving);
            return next;
        }

        IReadOnlyList<T> Add(IReadOnlyList<T> level) => [.. level.Select(node =>
        {
            var nodeKey = accessors.GetKey(node);
            if (!insertPath.Contains(nodeKey)) return node;
            var children = accessors.GetChildren(node) ?? [];
            return withChildren(node, nodeKey == target.Parent ? Insert(children) : Add(children));
        })];
    }

    /// <summary>
    /// 实际勾上的节点：自己或任何祖先在勾选的值里（父节点勾上之后才懒加载出来的子节点因此也算勾上），
    /// 或者已加载的子节点全勾上了（勾选的值由外部给出、没有写全父节点时）。
    /// </summary>
    static HashSet<string> Effective(TreeIndex index, IReadOnlyList<string> checkedKeys)
    {
        var listed = checkedKeys.ToHashSet();
        var result = new HashSet<string>();
        // Order 是深度优先：正序时父节点先于子节点，倒序时子节点先于父节点
        foreach (var key in index.Order)
            if (listed.Contains(key) || (index.Parents[key] is { } parent && result.Contains(parent))) result.Add(key);
        for (var i = index.Order.Count - 1; i >= 0; i--)
            if (index.Children.GetValueOrDefault(index.Order[i]) is { Count: > 0 } children && children.All(result.Contains)) result.Add(index.Order[i]);
        return result;
    }
}
