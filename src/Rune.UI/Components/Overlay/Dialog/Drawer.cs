using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>抽屉从哪一边滑出（同 vue-tsx-pro 的 DrawerSides）；Start / End 是逻辑方向，从右到左排版下左右互换。</summary>
public enum DrawerSide
{
    /// <summary>结束边（从左到右的页面是右侧，默认）：详情、筛选、表单。</summary>
    End,

    /// <summary>起始边：导航菜单。</summary>
    Start,

    /// <summary>顶部。</summary>
    Top,

    /// <summary>底部：手机上的操作面板。</summary>
    Bottom,
}

/// <summary>
/// 抽屉：贴着视口一侧滑入的模态面板（同 vue-tsx-pro 的 Drawer）。用法同 <see cref="Dialog"/>，多一个 <see cref="Side"/>。
/// </summary>
public sealed class Drawer : DialogBase
{
    /// <summary>从哪一边滑出，默认结束边。</summary>
    [Parameter] public DrawerSide Side { get; set; }

    /// <summary>宽度（左右两侧）档位，默认 Xs（30rem）；上下两侧时高度由内容决定，最多到视口高度。</summary>
    [Parameter] public ContainerSize? Size { get; set; }

    /// <inheritdoc/>
    protected override PartLayers<DialogPart> Parts =>
        DialogStyles.Drawer(Side, Size ?? Config.Defaults.Get<DrawerDefaults>()?.Size ?? ContainerSize.Xs, Config.Styles);
}
