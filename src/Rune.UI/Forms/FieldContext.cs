using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>
/// <see cref="Field"/> 交给里面的控件的信息（级联值）：控件的 id（标签的 for 指向它）、说明与错误的 id（aria-describedby），
/// 以及必填、不可用、只读、校验未通过。控件上显式写的状态优先于这里的；控件不在 Field 里时照常工作。
/// </summary>
/// <param name="ControlId">控件主元素的 id。</param>
/// <param name="LabelId">标签的 id：控件不是单个原生输入元素（单选组）时用 aria-labelledby 指向它；没有标签时为 null。</param>
/// <param name="DescribedBy">说明与错误信息的 id，空格分隔；没有时为 null。</param>
/// <param name="Invalid">校验未通过（给了 Error，或表单里这个字段有错误）。</param>
/// <param name="Required">必填。</param>
/// <param name="Disabled">不可用。</param>
/// <param name="ReadOnly">只读。</param>
/// <param name="Registration">控件把自己绑定的字段报给 Field，Field 据此取表单的错误信息。</param>
public sealed record FieldContext(
    string ControlId,
    string? LabelId,
    string? DescribedBy,
    bool Invalid,
    bool Required,
    bool Disabled,
    bool ReadOnly,
    FieldRegistration Registration);

/// <summary>
/// 控件向所在的 <see cref="Field"/> 报告它绑定的表单字段（<c>@bind-Value</c> 的表达式）。
/// 放在信号里：Field 渲染时读过它，控件报上来后 Field 自动重新渲染，显示这个字段的错误信息。
/// </summary>
public sealed class FieldRegistration
{
    readonly State<FieldIdentifier?> identifier = new(null);

    /// <summary>控件绑定的字段；还没有控件报告时为 null。</summary>
    public FieldIdentifier? Field => identifier.Value;

    /// <summary>控件报告自己绑定的字段。</summary>
    public void Register(FieldIdentifier field) => identifier.Value = field;
}
