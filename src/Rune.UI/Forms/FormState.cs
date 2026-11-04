using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>
/// <see cref="Form"/> 交给里面组件的状态（级联值）：是否正在提交（提交按钮据此显示加载中），
/// 以及各个 Field 登记的标签与控件 id（错误汇总据此写出“姓名：请填写姓名”并跳到控件）。
/// </summary>
public sealed class FormState
{
    readonly Dictionary<FieldIdentifier, (string? Label, string ControlId)> fields = [];

    /// <summary>正在执行提交的回调。</summary>
    public bool Submitting { get; internal set; }

    /// <summary>Field 登记它绑定的字段、标签与控件 id（每次渲染后调用，重复登记会覆盖）。</summary>
    public void Register(FieldIdentifier field, string? label, string controlId) => fields[field] = (label, controlId);

    /// <summary>Field 移除时取消登记。</summary>
    public void Unregister(FieldIdentifier field) => fields.Remove(field);

    /// <summary>登记过的字段，按登记顺序（Field 在页面上的顺序）。</summary>
    internal IEnumerable<(FieldIdentifier Field, string? Label, string ControlId)> Fields =>
        fields.Select(f => (f.Key, f.Value.Label, f.Value.ControlId));
}
