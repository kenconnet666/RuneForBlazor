using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>浮层打开或关闭（<c>@onrunetoggle</c> 的参数）。</summary>
public sealed class OverlayToggleEventArgs : EventArgs
{
    /// <summary>浮层现在是否打开。</summary>
    public bool Open { get; set; }
}

/// <summary>用户要关闭对话框（<c>@onrunecloserequest</c> 的参数）：带 <c>data-rune-guard-close</c> 的对话框不直接关，先问 C#。</summary>
public sealed class CloseRequestEventArgs : EventArgs
{
    /// <summary>原因：escape（按 Esc）、outside（点遮罩）、close（关闭按钮、取消按钮）。</summary>
    public string Reason { get; set; } = "";
}

/// <summary>复制按钮写完剪贴板（<c>@onrunecopied</c> 的参数）。</summary>
public sealed class CopiedEventArgs : EventArgs
{
    /// <summary>是否写入成功（不安全的上下文、用户拒绝权限时失败）。</summary>
    public bool Success { get; set; }
}

/// <summary>
/// 组件库自定义的 DOM 事件，由 JS 初始化器（<c>wwwroot/Rune.UI.lib.module.js</c>）发出：
/// <list type="bullet">
/// <item><c>@onrunetoggle</c>：浮层开关。原生的 <c>toggle</c>（popover）、<c>close</c>（dialog）不冒泡，Blazor 内置的 <c>@ontoggle</c> 也不带新状态，JS 转发成冒泡的 <c>rune-toggle</c>。</item>
/// <item><c>@onrunecopied</c>：复制按钮写完剪贴板（成功或失败）。</item>
/// <item><c>@onrunecloserequest</c>：用户要关闭对话框（Esc、点遮罩、关闭按钮），对话框带 <c>data-rune-guard-close</c> 时由 C# 决定关不关。</item>
/// <item><c>@onrunedismiss</c>：可移除的元素（标签）离场动画结束，该从列表里拿掉了（<c>data-rune-dismiss</c> 的按钮触发）。</item>
/// </list>
/// </summary>
[EventHandler("onrunetoggle", typeof(OverlayToggleEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunedismiss", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunecloserequest", typeof(CloseRequestEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunecopied", typeof(CopiedEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
public static class EventHandlers;
