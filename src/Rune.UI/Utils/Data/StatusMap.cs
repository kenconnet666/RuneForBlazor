namespace Rune.UI;

/// <summary>一种状态的外观与文案（<see cref="StatusMap{TKey}"/> 里的一项）。</summary>
/// <param name="Label">显示的名字。</param>
/// <param name="Tone">色调。</param>
/// <param name="Icon">名字前的图标；不写时 StatusBadge 画一个同色的圆点。</param>
/// <param name="Description">说明：筛选项、提示里显示（“验证不达标时退回方案设计”）。</param>
public sealed record StatusDefinition(string Label, Tone Tone = Tone.Neutral, IconData? Icon = null, string? Description = null);

/// <summary>
/// 状态映射（同 vue-tsx-pro 的 createStatusMap）：一处写好每种状态的名字、色调、图标与说明，
/// 标记（<see cref="StatusBadge{TKey}"/>）、表格的状态列、筛选的选项（<see cref="Options"/>）、步骤、看板的列共用，同一种状态在哪儿都是同一个颜色、同一个叫法。
/// 定义的顺序就是阶段的顺序。
/// <code>
/// static readonly StatusMap&lt;string&gt; TaskStatuses = new()
/// {
///     ["pending"] = new("待启动"),
///     ["designing"] = new("设计中", Tone.Info),
///     ["closed"] = new("关闭", Tone.Success),
/// };
/// &lt;StatusBadge Statuses="TaskStatuses" Value="@task.Status" /&gt;
/// </code>
/// </summary>
/// <typeparam name="TKey">状态的键：接口给的字符串，或者枚举。</typeparam>
public sealed class StatusMap<TKey> : IEnumerable<KeyValuePair<TKey, StatusDefinition>>
    where TKey : notnull
{
    readonly List<TKey> keys = [];
    readonly Dictionary<TKey, (StatusDefinition Definition, int Order)> definitions = [];

    /// <summary>全部状态的键，按定义的顺序（阶段顺序）。</summary>
    public IReadOnlyList<TKey> Keys => keys;

    /// <summary>
    /// 一种状态的定义；不认识的键（接口多给了新状态）当成中性色、名字就是键，不报错。
    /// 写入时按顺序追加（集合初始化器里用），同一个键不能写两次。
    /// </summary>
    public StatusDefinition this[TKey key]
    {
        get => definitions.TryGetValue(key, out var entry) ? entry.Definition : new StatusDefinition(key.ToString() ?? "");
        init
        {
            if (!definitions.TryAdd(key, (value, keys.Count)))
                throw new ArgumentException($"状态 {key} 定义了两次", nameof(key));
            keys.Add(key);
        }
    }

    /// <summary>在阶段里的位置（从 0 起）；不认识的键是 -1。</summary>
    public int Order(TKey key) => definitions.TryGetValue(key, out var entry) ? entry.Order : -1;

    /// <summary>
    /// 给 Select、Listbox、筛选用的选项：值是键（ToString），带名字、说明与图标；只要一部分时传 <paramref name="subset"/>（按它的顺序）。
    /// <code>&lt;Select Items="TaskStatuses.Options()" SelectionMode="SelectionMode.Multiple" @bind-Values="filter" /&gt;</code>
    /// </summary>
    public IReadOnlyList<ListboxOption> Options(IEnumerable<TKey>? subset = null) =>
        [.. (subset ?? keys).Select(key =>
        {
            var status = this[key];
            return new ListboxOption(key.ToString() ?? "", status.Label, Description: status.Description, Icon: status.Icon);
        })];

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, StatusDefinition>> GetEnumerator() =>
        keys.Select(key => new KeyValuePair<TKey, StatusDefinition>(key, definitions[key].Definition)).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
