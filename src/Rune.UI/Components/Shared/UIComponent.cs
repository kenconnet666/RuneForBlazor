using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 组件库里组件的基类：在 <see cref="RuneComponent"/> 之上提供配置（主题、默认值、样式定制）。
/// </summary>
/// <remarks>
/// 配置先取最近的 <see cref="RuneProvider"/>，没有时取应用级的（DI）。级联值不能跨渲染模式边界：
/// 交互区域里拿到的是应用级配置，需要局部修改时在交互区域里再放一个 RuneProvider。
/// </remarks>
public abstract class UIComponent : RuneComponent
{
    /// <summary>最近的 RuneProvider 给的配置。</summary>
    [CascadingParameter] UIConfig? ProvidedConfig { get; set; }

    /// <summary>应用级配置。</summary>
    [Inject] UIConfig AppConfig { get; set; } = default!;

    /// <summary>当前生效的配置。</summary>
    protected UIConfig Config => ProvidedConfig ?? AppConfig;

    /// <summary>是否在某个 RuneProvider 里面（同一渲染模式的区域内）。</summary>
    protected bool IsInsideProvider => ProvidedConfig is not null;
}
