namespace Rune.UI;

/// <summary>
/// 人员选择器能选的类型（同 vue-tsx-pro 的 PeopleTypes）：标准的是人员、部门、角色；岗位、项目组这类由接入方自己定一个名字，
/// 名称写进语言包的 <c>PeopleTypes</c>。
/// </summary>
public static class PeopleType
{
    /// <summary>人员。</summary>
    public const string User = "user";

    /// <summary>部门：选中就是部门里的人（怎么展开由后端定）。</summary>
    public const string Department = "department";

    /// <summary>角色：流程的角色收件箱。</summary>
    public const string Role = "role";
}

/// <summary>一个人员、部门或角色（同 vue-tsx-pro 的 PeopleEntry）：选中的值就是它的列表，带着名字，回显不用再查。</summary>
/// <param name="Id">标识：同一种类型里唯一（组织表的主键、工号、编码）。</param>
/// <param name="Type">类型（<see cref="PeopleType"/>）。</param>
/// <param name="Name">名字。</param>
public record PeopleEntry(string Id, string Type, string Name)
{
    /// <summary>说明：人员的部门与角色、部门的上级路径；搜索结果里分辨同名的。</summary>
    public string? Description { get; init; }

    /// <summary>头像地址（人员）；没有时显示名字的头一个字。</summary>
    public string? Avatar { get; init; }

    /// <summary>部门下还有子部门或人员；false 时没有“下级”，不写时按有。</summary>
    public bool? HasChildren { get; init; }

    /// <summary>同一个人员、部门、角色的键：类型加标识。</summary>
    [System.Text.Json.Serialization.JsonIgnore] public string Key => $"{Type}:{Id}";

    /// <summary>只留这一项本身（写进选中的值、最近选过的）：去掉 <see cref="PeopleNode"/> 的下级。</summary>
    public PeopleEntry ToValue() => new(Id, Type, Name) { Description = Description, Avatar = Avatar, HasChildren = HasChildren };
}

/// <summary>组织架构的一个节点：部门带着下级（<see cref="PeopleSource.Local"/> 用）。</summary>
public sealed record PeopleNode(string Id, string Type, string Name) : PeopleEntry(Id, Type, Name)
{
    /// <summary>子部门与人员。</summary>
    public IReadOnlyList<PeopleNode> Children { get; init; } = [];
}

/// <summary>
/// 人员选择器的数据来源（同 vue-tsx-pro 的 PeopleSource）：接入方按自己的接口实现；令牌在换了部门、改了搜索词、关掉选择器时取消。
/// <code>
/// new PeopleSource((parent, ct) => api.ChildrenAsync(parent?.Id, ct)) { Search = api.SearchAsync, Roles = api.RolesAsync }
/// </code>
/// 组织架构不大、已经整棵拿到时用 <see cref="Local"/>。
/// </summary>
/// <param name="Children">一个部门下的子部门与人员（部门在前）；parent 为 null 时是最上层。</param>
public sealed record PeopleSource(Func<PeopleEntry?, CancellationToken, Task<IReadOnlyList<PeopleEntry>>> Children)
{
    /// <summary>按关键字搜，人员、部门、角色都可以返回；不写时没有搜索框。</summary>
    public Func<string, CancellationToken, Task<IReadOnlyList<PeopleEntry>>>? Search { get; init; }

    /// <summary>全部角色；不写时没有“角色”页。</summary>
    public Func<CancellationToken, Task<IReadOnlyList<PeopleEntry>>>? Roles { get; init; }

    /// <summary>
    /// 整棵在内存里的组织架构做成数据来源：示例、测试与规模不大的系统直接用。
    /// 搜索按名字与说明（不分大小写），角色单独给；部门的 HasChildren 按有没有下级填上。
    /// </summary>
    public static PeopleSource Local(IReadOnlyList<PeopleNode> tree, IReadOnlyList<PeopleEntry>? roles = null)
    {
        var byKey = new Dictionary<string, PeopleNode>();
        var all = new List<PeopleNode>();
        void Visit(IReadOnlyList<PeopleNode> level)
        {
            foreach (var node in level)
            {
                byKey[node.Key] = node;
                all.Add(node);
                Visit(node.Children);
            }
        }
        Visit(tree);
        roles ??= [];
        static PeopleEntry View(PeopleNode node) =>
            node.Type == PeopleType.Department ? node.ToValue() with { HasChildren = node.Children.Count > 0 } : node.ToValue();
        static bool Matches(PeopleEntry entry, string text) =>
            entry.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) || entry.Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true;

        return new((parent, _) => Task.FromResult<IReadOnlyList<PeopleEntry>>(
            [.. (parent is null ? tree : byKey.GetValueOrDefault(parent.Key)?.Children ?? []).Select(View)]))
        {
            Search = (query, _) =>
            {
                var text = query.Trim();
                return Task.FromResult<IReadOnlyList<PeopleEntry>>([.. all.Select(View).Concat(roles.Select(r => r.ToValue())).Where(entry => Matches(entry, text))]);
            },
            Roles = roles.Count > 0 ? _ => Task.FromResult<IReadOnlyList<PeopleEntry>>([.. roles.Select(r => r.ToValue())]) : null,
        };
    }
}
