using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 对话框（居中的模态 dialog，淡入放大；同 vue-tsx-pro 的 Dialog）：
/// <code>
/// &lt;Dialog @bind-Open="editing" Title="编辑资料" Description="修改后立即生效" OnConfirm="SaveAsync"&gt;
///     …表单…
/// &lt;/Dialog&gt;
/// </code>
/// 或者不写 Open，用 Trigger 打开：<c>&lt;Trigger Context="t"&gt;&lt;Button @attributes="t"&gt;编辑&lt;/Button&gt;&lt;/Trigger&gt;</c>。
/// Draggable 时按住标题栏把对话框挪开（看被挡住的内容），不超出窗口，每次打开回到正中；拖动在 JS 里，不经 C#。
/// </summary>
public sealed class Dialog : DialogBase
{
    /// <summary>宽度档位，默认 Xs（30rem）；窄屏时不超出视口。</summary>
    [Parameter] public DialogSize? Size { get; set; }

    /// <summary>可以拖动：按住标题栏把对话框挪开，不超出窗口；标题栏上的按钮照常点。</summary>
    [Parameter] public bool Draggable { get; set; }

    /// <inheritdoc/>
    protected override Attrs? HeaderManaged => Draggable ? DialogStyles.DragHandle : null;

    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts =>
        DialogStyles.Dialog(Size ?? Config.Defaults.Get<DialogDefaults>()?.Size ?? DialogSize.Xs, Config.Styles);
}
