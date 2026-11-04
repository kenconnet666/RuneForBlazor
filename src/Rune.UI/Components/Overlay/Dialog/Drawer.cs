using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>抽屉从哪一边滑出（同 vue-tsx-pro 的 DrawerSides）。</summary>
public enum DrawerSide
{
    /// <summary>结束边（从左到右的页面是右侧，默认）。</summary>
    End,

    /// <summary>起始边。</summary>
    Start,

    /// <summary>顶部。</summary>
    Top,

    /// <summary>底部。</summary>
    Bottom,
}

/// <summary>
/// 抽屉：从页面一边滑出的模态面板（详情、筛选、较长的表单）。用法同 <see cref="Dialog"/>，多一个 <see cref="Side"/>。
/// </summary>
public sealed class Drawer : DialogBase
{
    /// <summary>从哪一边滑出，默认结束边。Start、End 时宽度按 Width，Top、Bottom 时占满宽度。</summary>
    [Parameter] public DrawerSide Side { get; set; }

    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts => DialogStyles.Drawer(Side, Width, Config.Styles);
}
