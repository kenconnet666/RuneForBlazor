using Microsoft.AspNetCore.Components;

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

/// <summary>
/// 抽屉：从页面一边滑出的模态面板（详情、筛选、较长的表单）。用法同 <see cref="Dialog"/>，多一个 <see cref="Placement"/>。
/// </summary>
public sealed class Drawer : DialogBase
{
    /// <summary>从哪一边滑出，默认结束边（右侧）。Start、End 时宽度按 Width，Top、Bottom 时占满宽度。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.End;

    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts => DialogStyles.Drawer(Placement, Width, Config.Styles);
}
