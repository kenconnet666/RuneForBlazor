using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 由 <see cref="FormSchemaAttribute"/> 的源生成器实现：模型的各个字段的描述。
/// SchemaForm 等组件按 <c>TModel.FormSchemaFields</c> 取，不用反射。
/// </summary>
/// <typeparam name="TSelf">模型自己。</typeparam>
public interface IFormSchema<TSelf> where TSelf : IFormSchema<TSelf>
{
    /// <summary>各个字段，按 <c>[Display(Order)]</c>、再按声明的顺序。</summary>
    static abstract IReadOnlyList<SchemaField<TSelf>> FormSchemaFields { get; }
}

/// <summary>字段的描述（同 vue-tsx-pro 的 SchemaField 里与类型无关的部分）。</summary>
/// <param name="Key">属性名；字段路径由所在的位置拼出（<c>Items.Price</c>）。</param>
/// <param name="Label">标签：<c>[Display(Name)]</c>，没写时是属性名。</param>
/// <param name="Control">控件。</param>
public sealed record SchemaFieldInfo(string Key, string Label, SchemaControl Control)
{
    /// <summary>说明：<c>[Display(Description)]</c>。</summary>
    public string? Description { get; init; }

    /// <summary>占位文字：<c>[Display(Prompt)]</c>。</summary>
    public string? Placeholder { get; init; }

    /// <summary>分组：<c>[Display(GroupName)]</c>，同组的字段放进一个 FormSection。</summary>
    public string? Group { get; init; }

    /// <summary>页签。</summary>
    public string? Tab { get; init; }

    /// <summary>数字的单位。</summary>
    public string? Unit { get; init; }

    /// <summary>占几栏；null 是一栏。</summary>
    public int? Span { get; init; }

    /// <summary>占满一行。</summary>
    public bool FullRow { get; init; }

    /// <summary>模型上写了 <c>[Required]</c>。</summary>
    public bool Required { get; init; }

    /// <summary>可选值（枚举的各成员，文字取 <c>[Display(Name)]</c>）。</summary>
    public IReadOnlyList<ListboxOption> Options { get; init; } = [];

    /// <summary>选项来源的名字（<see cref="FormSources.Options"/>）。</summary>
    public string? Source { get; init; }

    /// <summary>明细表的这一列在表底求和。</summary>
    public bool Sum { get; init; }
}

/// <summary>字段的条件（<c>[FormField(VisibleWhen, RequiredWhen, ReadOnlyWhen)]</c>）：模型上的 bool 成员。</summary>
/// <param name="VisibleWhen">显示条件。</param>
/// <param name="RequiredWhen">必填条件。</param>
/// <param name="ReadOnlyWhen">只读条件。</param>
public sealed record SchemaConditions<TModel>(Func<TModel, bool>? VisibleWhen = null, Func<TModel, bool>? RequiredWhen = null, Func<TModel, bool>? ReadOnlyWhen = null);

/// <summary>按字段的类型分别处理（渲染、遍历、显示）：字段把自己的静态类型交给访问者，组件里不用反射。</summary>
/// <typeparam name="TResult">结果。</typeparam>
public interface ISchemaVisitor<out TResult>
{
    /// <summary>普通的值字段。</summary>
    TResult Value<TModel, TValue>(SchemaValueField<TModel, TValue> field, TModel model);

    /// <summary>嵌套的对象。</summary>
    TResult Object<TModel, TChild>(SchemaObjectField<TModel, TChild> field, TModel model) where TChild : class, IFormSchema<TChild>;

    /// <summary>对象的列表（List、Table）。</summary>
    TResult List<TModel, TItem>(SchemaListField<TModel, TItem> field, TModel model) where TItem : class, IFormSchema<TItem>, new();
}

/// <summary>模型 <typeparamref name="TModel"/> 的一个字段：描述、条件与按类型的处理。由源生成器创建。</summary>
public abstract class SchemaField<TModel>
{
    private protected SchemaField(SchemaFieldInfo info, SchemaConditions<TModel>? conditions)
    {
        Info = info;
        VisibleWhen = conditions?.VisibleWhen;
        RequiredWhen = conditions?.RequiredWhen;
        ReadOnlyWhen = conditions?.ReadOnlyWhen;
    }

    /// <summary>描述。</summary>
    public SchemaFieldInfo Info { get; }

    /// <summary>显示条件；null 是一直显示。</summary>
    public Func<TModel, bool>? VisibleWhen { get; }

    /// <summary>必填条件。</summary>
    public Func<TModel, bool>? RequiredWhen { get; }

    /// <summary>只读条件。</summary>
    public Func<TModel, bool>? ReadOnlyWhen { get; }

    /// <summary>读出这个字段在模型里的值（显示、合计、条件求值用）。</summary>
    public abstract object? Read(TModel model);

    /// <summary>把自己的静态类型交给访问者。</summary>
    public abstract TResult Accept<TResult>(TModel model, ISchemaVisitor<TResult> visitor);

    /// <summary>值字段（源生成器用）。</summary>
    public static SchemaValueField<TModel, TValue> Value<TValue>(SchemaFieldInfo info, Func<TModel, TValue> get, Action<TModel, TValue> set,
        Func<TModel, Expression<Func<TValue>>> expression, SchemaControlRenderer<TValue> control, SchemaConditions<TModel>? conditions = null) =>
        new(info, get, set, expression, control, conditions);

    /// <summary>嵌套对象的字段（源生成器用）。</summary>
    public static SchemaObjectField<TModel, TChild> Object<TChild>(SchemaFieldInfo info, Func<TModel, TChild?> get, SchemaConditions<TModel>? conditions = null)
        where TChild : class, IFormSchema<TChild> => new(info, get, conditions);

    /// <summary>对象列表的字段（源生成器用）。</summary>
    public static SchemaListField<TModel, TItem> List<TItem>(SchemaFieldInfo info, Func<TModel, IList<TItem>?> get, SchemaConditions<TModel>? conditions = null)
        where TItem : class, IFormSchema<TItem>, new() => new(info, get, conditions);
}

/// <summary>值字段：读写模型的一个属性，按 <see cref="SchemaFieldInfo.Control"/> 渲染控件。</summary>
public sealed class SchemaValueField<TModel, TValue> : SchemaField<TModel>
{
    internal SchemaValueField(SchemaFieldInfo info, Func<TModel, TValue> get, Action<TModel, TValue> set,
        Func<TModel, Expression<Func<TValue>>> expression, SchemaControlRenderer<TValue> control, SchemaConditions<TModel>? conditions) : base(info, conditions)
    {
        Get = get;
        Set = set;
        Expression = expression;
        Control = control;
    }

    /// <summary>读值。</summary>
    public Func<TModel, TValue> Get { get; }

    /// <summary>写值。</summary>
    public Action<TModel, TValue> Set { get; }

    /// <summary>绑定用的表达式（<c>() =&gt; model.Name</c>）：Field 据此取校验错误。</summary>
    public Func<TModel, Expression<Func<TValue>>> Expression { get; }

    /// <summary>控件。</summary>
    public SchemaControlRenderer<TValue> Control { get; }

    /// <inheritdoc/>
    public override object? Read(TModel model) => Get(model);

    /// <inheritdoc/>
    public override TResult Accept<TResult>(TModel model, ISchemaVisitor<TResult> visitor) => visitor.Value(this, model);
}

/// <summary>嵌套对象的字段：一组字段（FormSection），字段是 <typeparamref name="TChild"/> 的。</summary>
public sealed class SchemaObjectField<TModel, TChild> : SchemaField<TModel> where TChild : class, IFormSchema<TChild>
{
    internal SchemaObjectField(SchemaFieldInfo info, Func<TModel, TChild?> get, SchemaConditions<TModel>? conditions) : base(info, conditions) => Get = get;

    /// <summary>读出嵌套的对象。</summary>
    public Func<TModel, TChild?> Get { get; }

    /// <summary>嵌套对象的字段。</summary>
    public static IReadOnlyList<SchemaField<TChild>> Fields => TChild.FormSchemaFields;

    /// <inheritdoc/>
    public override object? Read(TModel model) => Get(model);

    /// <inheritdoc/>
    public override TResult Accept<TResult>(TModel model, ISchemaVisitor<TResult> visitor) => visitor.Object(this, model);
}

/// <summary>对象列表的字段：可增删的行（List）或明细表（Table），每行的字段是 <typeparamref name="TItem"/> 的。</summary>
public sealed class SchemaListField<TModel, TItem> : SchemaField<TModel> where TItem : class, IFormSchema<TItem>, new()
{
    internal SchemaListField(SchemaFieldInfo info, Func<TModel, IList<TItem>?> get, SchemaConditions<TModel>? conditions) : base(info, conditions) => Get = get;

    /// <summary>读出列表。</summary>
    public Func<TModel, IList<TItem>?> Get { get; }

    /// <summary>每一行的字段。</summary>
    public static IReadOnlyList<SchemaField<TItem>> Fields => TItem.FormSchemaFields;

    /// <inheritdoc/>
    public override object? Read(TModel model) => Get(model);

    /// <inheritdoc/>
    public override TResult Accept<TResult>(TModel model, ISchemaVisitor<TResult> visitor) => visitor.List(this, model);
}

/// <summary>渲染一个字段的控件（值绑定、选项、占位已经准备好）。</summary>
public delegate RenderFragment SchemaControlRenderer<TValue>(SchemaControlArgs<TValue> args);

/// <summary>交给控件渲染函数的内容。</summary>
/// <param name="Field">字段的描述。</param>
/// <param name="Value">当前的值。</param>
/// <param name="ValueChanged">值变了。</param>
/// <param name="ValueExpression">绑定用的表达式；查询栏里没有（不在 EditForm 里）。</param>
/// <param name="Sources">数据来源。</param>
/// <param name="Owner">渲染它的组件：换算值的类型（枚举 ↔ 文字）时新建的回调交给它，改值后它重新渲染。</param>
/// <param name="Clearable">能清空（查询栏）。</param>
public readonly record struct SchemaControlArgs<TValue>(SchemaFieldInfo Field, TValue Value, EventCallback<TValue> ValueChanged,
    Expression<Func<TValue>>? ValueExpression, FormSources Sources, IHandleEvent Owner, bool Clearable = false);

/// <summary>
/// 表单的数据来源（同 vue-tsx-pro 的 FormSources）：按名字引用的选项（字典、远程搜索）、人员与部门、上传。
/// 在 SchemaForm、SchemaFields、SchemaView、SchemaFilters 的 Sources 上给。
/// </summary>
public sealed record FormSources
{
    /// <summary>没有来源。</summary>
    public static FormSources Empty { get; } = new();

    /// <summary>固定的选项（字典）：名字 → 选项；字段的 <c>[FormField(Source = "名字")]</c> 从这里取。</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ListboxOption>> Options { get; init; } = new Dictionary<string, IReadOnlyList<ListboxOption>>();

    /// <summary>远程搜索的选项：名字 → 按搜索词取（供应商、件号）。</summary>
    public IReadOnlyDictionary<string, OptionsLoader<ListboxOption>> Remote { get; init; } = new Dictionary<string, OptionsLoader<ListboxOption>>();

    /// <summary>人员、部门（People 控件）。</summary>
    public PeopleSource? People { get; init; }

    /// <summary>上传（File 控件）；不给时只选文件、不上传。</summary>
    public UploadHandler? Upload { get; init; }

    /// <summary>字段的选项：登记的字典，或字段自己的（枚举成员）。</summary>
    public IReadOnlyList<ListboxOption> OptionsOf(SchemaFieldInfo field) =>
        field.Source is { } name && Options.TryGetValue(name, out var options) ? options : field.Options;
}
