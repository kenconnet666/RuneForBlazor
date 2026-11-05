using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 确认对话框（role="alertdialog"，同 vue-tsx-pro 的 AlertDialog）：打断当前操作、要用户明确选择“确认”或“取消”，如删除前的确认。
/// <code>
/// &lt;AlertDialog @bind-Open="confirming" Tone="Tone.Danger" Title="删除这个项目？" Description="删除后无法恢复。"
///              ConfirmText="删除" OnConfirm="RemoveAsync" /&gt;
/// </code>
/// 没有右上角的关闭按钮，点遮罩不关闭（Esc 仍然等于取消）；打开时焦点落在“取消”上，误按 Enter 不会执行破坏性操作（同 Radix AlertDialog）。
/// 没写 OnConfirm 也有确认按钮（点了就关）；OnConfirm 执行期间确认按钮加载中、关不掉，完成后关闭。
/// 只想在一个操作前问一句时用 <see cref="DialogService.ConfirmAsync(ConfirmOptions)"/>，不必自己管 Open。
/// </summary>
public sealed class AlertDialog : DialogBase
{
    static readonly Attrs FocusFirst = new() { ["autofocus"] = true };

    /// <summary>宽度档位，默认 Xs（30rem）。</summary>
    [Parameter] public DialogSize? Size { get; set; }

    /// <summary>取消按钮先拿到焦点，默认是；对话框里有要先填的输入框时写 false（焦点落在输入框的 autofocus 上）。</summary>
    [Parameter] public bool FocusCancel { get; set; } = true;

    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts =>
        DialogStyles.Dialog(Size ?? Config.Defaults.Get<DialogDefaults>()?.Size ?? DialogSize.Xs, Config.Styles);

    /// <inheritdoc/>
    protected override string Role => "alertdialog";

    /// <inheritdoc/>
    protected override bool LightDismiss => false;

    /// <inheritdoc/>
    protected override bool ShowClose => false;

    /// <inheritdoc/>
    protected override bool HasConfirm => true;

    /// <inheritdoc/>
    protected override Attrs? CancelDefaults => FocusCancel ? FocusFirst : null;
}
