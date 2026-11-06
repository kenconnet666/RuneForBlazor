using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>
/// <see cref="Form"/> 交给里面组件的状态（级联值）：是否正在提交（提交按钮据此显示加载中），
/// 以及各个 Field 登记的标签与控件 id（错误汇总据此写出“姓名：请填写姓名”并跳到控件）。标签布局与只读、不可用在 <see cref="FieldLayout"/>。
/// </summary>
public sealed class FormState
{
    static long nextOrder;
    static readonly ConcurrentDictionary<(Type, string), int> propertyRanks = new();
    readonly Dictionary<FieldIdentifier, (string? Label, string ControlId, long Order)> fields = [];

    /// <summary>正在执行提交的回调。</summary>
    public bool Submitting { get; internal set; }

    /// <summary>提交过、没通过：错误汇总显示出来（之后改对的项从汇总里消失）。</summary>
    public bool ShowErrors { get; internal set; }

    /// <summary>表单的 EditContext（错误汇总据此读错误、订阅校验结果的变化）。</summary>
    public EditContext? Context { get; internal set; }

    /// <summary>Field 创建时取的序号：越早创建越小。</summary>
    public static long NextOrder() => Interlocked.Increment(ref nextOrder);

    /// <summary>Field 登记它绑定的字段、标签、控件 id 与创建序号（每次渲染后调用，重复登记会覆盖）。</summary>
    public void Register(FieldIdentifier field, string? label, string controlId, long order) => fields[field] = (label, controlId, order);

    /// <summary>Field 移除时取消登记。</summary>
    public void Unregister(FieldIdentifier field) => fields.Remove(field);

    /// <summary>
    /// 当前的错误：有登记的 Field 时写成“标签：错误”并带控件 id，按字段在表单里的顺序；
    /// 没有 Field 的字段、整个对象的错误排在后面，只有文字。
    /// </summary>
    public IReadOnlyList<(string Text, string? ControlId)> Errors(Lang lang)
    {
        if (Context is null) return [];
        var errors = new List<(string, string?)>();
        var rest = Context.GetValidationMessages().ToList();
        foreach (var (identifier, (label, controlId, _)) in InFormOrder())
        {
            foreach (var message in Context.GetValidationMessages(identifier))
            {
                errors.Add((label is null ? message : lang.FormErrorItem(label, message), controlId));
                rest.Remove(message);
            }
        }
        errors.AddRange(rest.Select(message => (message, (string?)null)));
        return errors;
    }

    // 字段在表单里的顺序：Blazor 先创建外层的组件、后创建 Grid 之类里面的，创建先后不是页面上的先后。
    // 表单一般照模型属性的顺序摆：同一个模型的字段按属性的声明顺序，模型之间按各自最早创建的字段（报价单的各行）
    IEnumerable<KeyValuePair<FieldIdentifier, (string? Label, string ControlId, long Order)>> InFormOrder()
    {
        var modelRanks = new Dictionary<object, long>(ReferenceEqualityComparer.Instance);
        foreach (var (identifier, entry) in fields)
            modelRanks[identifier.Model] = modelRanks.TryGetValue(identifier.Model, out var rank) ? Math.Min(rank, entry.Order) : entry.Order;
        return fields
            .OrderBy(field => modelRanks[field.Key.Model])
            .ThenBy(field => PropertyRank(field.Key.Model.GetType(), field.Key.FieldName))
            .ThenBy(field => field.Value.Order);
    }

    // 属性的声明顺序（元数据记号按声明递增）；不是属性的字段名排在最后
    static int PropertyRank(Type type, string name) => propertyRanks.GetOrAdd((type, name), static key => FindRank(key.Item1, key.Item2));

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "字段来自控件绑定的表达式（() => model.Email），表达式树引用了这个属性，裁剪时会保留；找不到时排在最后，不会出错。")]
    static int FindRank(Type type, string name) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(p => p.Name == name) is { } property
            ? property.MetadataToken
            : int.MaxValue;
}
