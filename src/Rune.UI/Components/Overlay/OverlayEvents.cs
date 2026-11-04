using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>浮层打开或关闭（<c>@onrunetoggle</c> 的参数）。</summary>
public sealed class OverlayToggleEventArgs : EventArgs
{
    /// <summary>浮层现在是否打开。</summary>
    public bool Open { get; set; }
}

/// <summary>
/// 组件库自定义的 DOM 事件。原生的 <c>toggle</c>（popover）、<c>close</c>（dialog）不冒泡，Blazor 内置的 <c>@ontoggle</c> 也不带新状态；
/// JS 初始化器（<c>wwwroot/Rune.UI.lib.module.js</c>）把它们转发成冒泡的 <c>rune-toggle</c>，在这里登记成 <c>@onrunetoggle</c>。
/// </summary>
[EventHandler("onrunetoggle", typeof(OverlayToggleEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
public static class EventHandlers;
