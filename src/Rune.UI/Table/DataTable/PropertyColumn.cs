using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 取一个属性的列（同 QuickGrid 的 PropertyColumn）：<c>&lt;PropertyColumn Property="o =&gt; o.Amount" Type="ColumnType.Currency" Sortable /&gt;</c>。
/// key 是属性路径（首字母小写，<c>customer.name</c>，同 JSON 与服务端排序的字段名），标题取 <c>[Display(Name = …)]</c>，其次 <c>[DisplayName]</c>，再次属性名；
/// 单元格按值的类型格式化。
/// </summary>
/// <typeparam name="TItem">一行的类型。</typeparam>
/// <typeparam name="TValue">属性的类型。</typeparam>
public class PropertyColumn<TItem, TValue> : DataTableColumn<TItem>
{
    Expression<Func<TItem, TValue>>? compiledFrom;
    Func<TItem, TValue> read = static _ => default!;
    string? path;
    string? title;

    /// <summary>取哪个属性：<c>o =&gt; o.Customer.Name</c>。</summary>
    [Parameter, EditorRequired] public Expression<Func<TItem, TValue>> Property { get; set; } = null!;

    private protected override string? DefaultKey => path;

    private protected override string? DefaultTitle => title;

    internal override object? ValueOf(TItem item) => read(item);

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(Property, compiledFrom)) return;
        compiledFrom = Property;
        read = Property.Compile();
        var members = Members(Property.Body).ToList();
        path = members.Count == 0 ? null : string.Join(".", members.Select(static member => char.ToLowerInvariant(member.Name[0]) + member.Name[1..]));
        // GetName 每次取：带资源类型时按当前的界面文化取文字（这里每次换表达式时取一次）
        title = members.Count == 0 ? null : members[^1].GetCustomAttribute<DisplayAttribute>()?.GetName() ?? members[^1].GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? members[^1].Name;
    }

    // o => o.Customer.Name 的成员链（从外到里）；不是成员访问时为空
    static IEnumerable<MemberInfo> Members(Expression body)
    {
        var chain = new Stack<MemberInfo>();
        var current = body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : body;
        while (current is MemberExpression member)
        {
            chain.Push(member.Member);
            current = member.Expression;
        }
        return current is ParameterExpression ? chain : [];
    }
}

/// <summary>
/// 状态列：值是状态的键，显示成 StatusBadge（同 vue-tsx-pro 列的 statuses）；表头筛选的选项按状态的顺序、显示状态的名字。
/// <c>&lt;StatusColumn Property="o =&gt; o.Status" Statuses="OrderStatuses" /&gt;</c>
/// </summary>
/// <typeparam name="TItem">一行的类型。</typeparam>
/// <typeparam name="TKey">状态的键。</typeparam>
public sealed class StatusColumn<TItem, TKey> : PropertyColumn<TItem, TKey> where TKey : notnull
{
    /// <summary>状态映射。</summary>
    [Parameter, EditorRequired] public StatusMap<TKey> Statuses { get; set; } = null!;

    internal override bool HasStatuses => true;

    // 状态按阶段的顺序、显示状态名；只列出现过的状态
    internal override IReadOnlyList<ListboxOption> FilterOptionsFrom(IEnumerable<TItem> items, string culture)
    {
        var seen = items.Select(ValueOf).OfType<TKey>().ToHashSet();
        return Statuses.Options(Statuses.Keys.Where(seen.Contains).Concat(seen.Where(key => Statuses.Order(key) < 0)));
    }

    internal override RenderFragment? StatusContent(object value) => builder =>
    {
        builder.OpenComponent<StatusBadge<TKey>>(0);
        builder.AddComponentParameter(1, nameof(StatusBadge<TKey>.Statuses), Statuses);
        builder.AddComponentParameter(2, nameof(StatusBadge<TKey>.Value), (TKey)value);
        builder.CloseComponent();
    };
}

/// <summary>
/// 自己写内容的列（同 QuickGrid 的 TemplateColumn）：操作按钮、组合几个字段。
/// <c>&lt;TemplateColumn Title="操作" Sticky="ColumnSticky.End" Width="120" Context="order"&gt;&lt;Button …&gt;编辑&lt;/Button&gt;&lt;/TemplateColumn&gt;</c>
/// 要排序、合计时给 <see cref="Value"/>（与 Key）。
/// </summary>
/// <typeparam name="TItem">一行的类型。</typeparam>
public sealed class TemplateColumn<TItem> : DataTableColumn<TItem>
{
    /// <summary>单元格的内容（Context 是这一行）。</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ChildContent { get; set; } = null!;

    /// <summary>排序、合计用的值；不写时为 null。</summary>
    [Parameter] public Func<TItem, object?>? Value { get; set; }

    internal override object? ValueOf(TItem item) => Value?.Invoke(item);

    internal override RenderFragment? CellContent(TItem item) => ChildContent(item);
}
