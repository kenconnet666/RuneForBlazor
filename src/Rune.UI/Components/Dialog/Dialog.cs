namespace Rune.UI;

/// <summary>
/// 对话框（居中的模态 dialog）：
/// <code>
/// &lt;Dialog @bind-Open="editing" Title="编辑订单"&gt;
///     &lt;ChildContent&gt;…&lt;/ChildContent&gt;
///     &lt;Footer&gt;
///         &lt;Button Variant="ButtonVariant.Ghost" data-rune-close&gt;取消&lt;/Button&gt;
///         &lt;Button @onclick="SaveAsync"&gt;保存&lt;/Button&gt;
///     &lt;/Footer&gt;
/// &lt;/Dialog&gt;
/// </code>
/// 或者不写 Open，用 Trigger 打开：<c>&lt;Trigger Context="t"&gt;&lt;Button @attributes="t"&gt;编辑&lt;/Button&gt;&lt;/Trigger&gt;</c>。
/// </summary>
public sealed class Dialog : DialogBase
{
    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts => DialogStyles.Dialog(Width, Config.Styles);
}
