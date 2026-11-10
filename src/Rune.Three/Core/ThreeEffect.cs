using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 建在 <see cref="EffectSurface"/> 上的效果共有的参数（同 vue-tsx-pro 的 EffectProps）：画质、暂停、分辨率、降级内容、部件属性与叠在上面的内容。
/// </summary>
public abstract class ThreeEffect : UIComponent
{
    /// <summary>画质档位：像素比上限、粒子密度、抗锯齿；Off 不渲染，显示 Fallback。默认 Auto。</summary>
    [Parameter] public ThreeQuality Quality { get; set; }

    /// <summary>暂停动画，停在当前画面。</summary>
    [Parameter] public bool Paused { get; set; }

    /// <summary>渲染分辨率相对像素比的倍数（0.1–1），见 ThreeCanvas；各效果有自己的默认值。</summary>
    [Parameter] public double? Resolution { get; set; }

    /// <summary>没有 WebGL（或 Quality 是 Off）时显示的内容，默认是主题色的柔和渐变。</summary>
    [Parameter] public RenderFragment? Fallback { get; set; }

    /// <summary>画布（ThreeCanvas）的部件属性。</summary>
    [Parameter] public Attrs? PropsCanvas { get; set; }

    /// <summary>内容外层的部件属性。</summary>
    [Parameter] public Attrs? PropsContent { get; set; }

    /// <summary>叠在效果上面的内容。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
