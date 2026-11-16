using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary><see cref="FormDialog"/> 校验通过后的提交（<c>OnSubmit</c> 的参数）。</summary>
/// <param name="EditContext">表单的 EditContext。</param>
public sealed class FormDialogSubmitEventArgs(EditContext EditContext) : EventArgs
{
    /// <summary>表单的 EditContext。</summary>
    public EditContext EditContext { get; } = EditContext;

    /// <summary>对话框留着（后端拒绝了：同时给 FormDialog 的 Error 说明原因）。</summary>
    public bool KeepOpen { get; set; }
}
