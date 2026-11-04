using Microsoft.AspNetCore.Components;

namespace Rune.UI;

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
