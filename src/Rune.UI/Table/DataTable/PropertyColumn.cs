using System.ComponentModel;
using System.Globalization;
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

    /// <summary>
    /// 可以就地编辑（DataTable 的 OnCellChange 写回）：表格随之变成可以用方向键走格的网格。默认编辑器按值类型选：
    /// 状态列是 Select，数字是 NumberField，文字是 TextField；别的类型写 <see cref="Editor"/>。
    /// </summary>
    [Parameter] public bool Editable { get; set; }

    /// <summary>按行判断能不能编辑（写了时代替 <see cref="Editable"/>）：已下单的行不能改数量这类。</summary>
    [Parameter] public Func<TItem, bool>? IsEditable { get; set; }

    /// <summary>提交前校验这一格：返回原因就不提交，编辑器标红并说明。</summary>
    [Parameter] public Func<TValue?, TItem, string?>? Validate { get; set; }

    /// <summary>自己写的编辑器（日期、下拉选人这类）：拿到草稿与提交、放弃。</summary>
    [Parameter] public RenderFragment<CellEditor<TItem, TValue>>? Editor { get; set; }

    /// <summary>值是数字：默认编辑器是 NumberField，打字开始编辑时只认数字。</summary>
    static readonly bool Numeric = (Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue)) is var type
        && (type == typeof(decimal) || type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long) || type == typeof(short));

    internal override bool Edits => Editable || IsEditable is not null;

    internal override bool CanEdit(TItem item) => IsEditable?.Invoke(item) ?? Editable;

    internal override string? ValidateDraft(object? draft, TItem item) => Validate?.Invoke(Cast(draft), item);

    internal override bool TryDraftFromText(string text, out object? draft)
    {
        draft = null;
        if (!Numeric) return base.TryDraftFromText(text, out draft);
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var number)) return false;
        draft = Convert.ChangeType(number, Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue), CultureInfo.InvariantCulture);
        return true;
    }

    internal override RenderFragment? EditorFor(TItem item, CellEditSession session)
    {
        if (Editor is { } editor) return editor(new(item, Cast(session.Draft), value => session.Change(value), session.CommitAsync, session.Cancel, session.Error));
        return DefaultEditor(session);
    }

    /// <summary>按值类型选的编辑器：数字是 NumberField（不要步进按钮），文字是 TextField；占满格子。</summary>
    private protected virtual RenderFragment? DefaultEditor(CellEditSession session)
    {
        if (Numeric)
        {
            return builder =>
            {
                builder.OpenComponent<NumberField<TValue>>(0);
                builder.AddComponentParameter(1, nameof(NumberField<TValue>.Value), Cast(session.Draft));
                builder.AddComponentParameter(2, nameof(NumberField<TValue>.ValueChanged), new EventCallback<TValue>(null, (Action<TValue>)(value => session.Change(value))));
                builder.AddComponentParameter(3, nameof(NumberField<TValue>.Stepper), false);
                EditorAttributes(builder, 4, session);
                builder.CloseComponent();
            };
        }
        if (typeof(TValue) != typeof(string)) return null;
        return builder =>
        {
            builder.OpenComponent<TextField>(0);
            builder.AddComponentParameter(1, nameof(TextField.Value), session.Draft as string);
            builder.AddComponentParameter(2, nameof(TextField.ValueChanged), new EventCallback<string?>(null, (Action<string?>)(value => session.Change(value))));
            builder.AddComponentParameter(3, nameof(TextField.Immediate), true);
            EditorAttributes(builder, 4, session);
            builder.CloseComponent();
        };
    }

    // 编辑器的回调不带接收者：草稿改了由 DataTable 自己重新渲染（列组件重新渲染会在收集之外把自己再交一次）

    /// <summary>默认编辑器共有的：小号、校验没过时标红、名称是列名、占满格子。</summary>
    private protected static void EditorAttributes(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence, CellEditSession session)
    {
        builder.AddComponentParameter(sequence, "Size", UI.Size.Sm);
        builder.AddComponentParameter(sequence + 1, "Invalid", session.Error is not null);
        builder.AddComponentParameter(sequence + 2, "aria-label", session.Label);
        builder.AddComponentParameter(sequence + 3, "Sx", EditorSx);
    }

    // 组件参数要的是 Sx（不是 Style）：按名字传参不经过隐式转换
    static readonly Rune.Styles.Sx EditorSx = Rune.Styles.Sx.Of(DataTableStyles.EditorControl);

    static TValue? Cast(object? value) => value is TValue typed ? typed : default;

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

    // 状态：Select，选了就提交
    private protected override RenderFragment? DefaultEditor(CellEditSession session) => builder =>
    {
        builder.OpenComponent<Select<ListboxOption>>(0);
        builder.AddComponentParameter(1, nameof(Select<ListboxOption>.Items), Statuses.Options());
        builder.AddComponentParameter(2, nameof(Select<ListboxOption>.Value), session.Draft?.ToString());
        builder.AddComponentParameter(3, nameof(Select<ListboxOption>.ValueChanged), new EventCallback<string?>(null, (Func<string?, Task>)(async value =>
        {
            session.Change(Statuses.Keys.FirstOrDefault(key => key.ToString() == value));
            await session.CommitAsync();
        })));
        EditorAttributes(builder, 4, session);
        builder.CloseComponent();
    };

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
