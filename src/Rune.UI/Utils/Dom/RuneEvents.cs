using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>浮层打开或关闭（<c>@onrunetoggle</c> 的参数）。</summary>
public sealed class OverlayToggleEventArgs : EventArgs
{
    /// <summary>浮层现在是否打开。</summary>
    public bool Open { get; set; }

    /// <summary>
    /// 原因：打开时是 trigger；用户关上时是 close（关闭按钮、data-rune-close）、escape、outside（点遮罩）；
    /// C# 改了 Open 引起的开关没有（null）。
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>浮层开关的原因（同 vue-tsx-pro 的 OpenChangeReasons）：OnOpenChange、BeforeClose 拿到它。</summary>
public enum OverlayChangeReason
{
    /// <summary>触发元素打开（或关上）。</summary>
    Trigger,

    /// <summary>关闭按钮、取消按钮（或写了 data-rune-close 的按钮）。</summary>
    Close,

    /// <summary>按了 Esc。</summary>
    Escape,

    /// <summary>点了遮罩。</summary>
    Outside,

    /// <summary>确认完成后关上。</summary>
    Confirm,

    /// <summary>代码调用（命令式对话框的 Open、Close）。</summary>
    Imperative,
}

/// <summary>浮层开了或关了（OnOpenChange 的参数）；父级直接改 Open 引起的不报告。</summary>
/// <param name="Open">现在是否打开。</param>
/// <param name="Reason">原因。</param>
public readonly record struct OverlayOpenChange(bool Open, OverlayChangeReason Reason)
{
    /// <summary>JS 报来的原因（trigger、close、escape、outside）；没有时按开关取 Trigger / Close。</summary>
    public static OverlayChangeReason Parse(string? reason, bool open) => reason switch
    {
        "escape" => OverlayChangeReason.Escape,
        "outside" => OverlayChangeReason.Outside,
        "close" => OverlayChangeReason.Close,
        "trigger" => OverlayChangeReason.Trigger,
        _ => open ? OverlayChangeReason.Trigger : OverlayChangeReason.Close,
    };
}

/// <summary>用户要关闭对话框（<c>@onrunecloserequest</c> 的参数）：带 <c>data-rune-guard-close</c> 的对话框不直接关，先问 C#。</summary>
public sealed class CloseRequestEventArgs : EventArgs
{
    /// <summary>原因：escape（按 Esc）、outside（点遮罩）、close（关闭按钮、取消按钮）。</summary>
    public string Reason { get; set; } = "";
}

/// <summary>键盘命令（<c>@onrunecommand</c> 的参数）：JS 初始化器把要 C# 处理的按键转成命名的命令，不用每次按键都往返一次。</summary>
public sealed class CommandEventArgs : EventArgs
{
    /// <summary>
    /// 命令名：remove-last（Backspace，移除最后一个已选项）、clear（Esc，清空输入）、show-all（↑↓ 打开，显示全部选项）、
    /// add（Enter，把输入加成标签）、paste（粘贴了多段文字）、fullscreen-exit（AppShell：离开了浏览器全屏或按了 Esc）、
    /// leave（LeaveConfirmation：点了去本站别的页面的链接，Text 是目标地址）。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>命令带的文字（paste 时是粘贴的整段内容，leave 时是目标地址）；没有时为 null。</summary>
    public string? Text { get; set; }
}

/// <summary>滑块被拖动、点击或用键盘改了值（<c>@onruneslide</c> 的参数）：值由 JS 算好（夹在范围内、对齐步长）并已画出来。</summary>
public sealed class SlideEventArgs : EventArgs
{
    /// <summary>第几个滑块（范围时 0 是低端、1 是高端）。</summary>
    public int Index { get; set; }

    /// <summary>新值（不随文化变化的十进制文本）。</summary>
    public string Value { get; set; } = "";

    /// <summary>一次操作结束（松开、按键）：OnValueCommit 在这时调用。</summary>
    public bool Commit { get; set; }

    /// <summary>JS 发出的序号（逐次加一）：组件处理完写回 data-ack，JS 据此知道 C# 的渲染追上了没有。</summary>
    public long Seq { get; set; }
}

/// <summary>宽度调整手柄（<c>data-rune-resize</c>）调整完一次（松开、按键、双击）：<c>@onruneresize</c> 的参数。</summary>
public sealed class ResizeEventArgs : EventArgs
{
    /// <summary>新的宽度（横向手柄时是高度），px，已经夹在最小、最大之间；data-rune-observe-size 时是元素内容的宽度（取整）。</summary>
    public double Size { get; set; }
}

/// <summary>拖动标签（或在标签上按 Alt + ←→）改了顺序：<c>@onrunereorder</c> 的参数。</summary>
public sealed class ReorderEventArgs : EventArgs
{
    /// <summary>新的顺序（各项的值）。</summary>
    public string[] Values { get; set; } = [];
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
/// <item><c>@onrunecommand</c>：键盘、粘贴转成的命令（<c>data-rune-backspace</c>、<c>data-rune-escape</c>、<c>data-rune-enter</c>、<c>data-rune-paste</c>，见 JS 初始化器顶部的约定）。</item>
/// <item><c>@onrunedismiss</c>：可移除的元素（标签）离场动画结束，该从列表里拿掉了（<c>data-rune-dismiss</c> 的按钮触发）。</item>
/// <item><c>@onruneresize</c>：宽度调整手柄调整完一次（拖动中由 JS 当场改父元素的尺寸，松开时才交给 C#）。</item>
/// <item><c>@onrunereorder</c>：标签栏（<c>data-reorderable</c> 的 tablist）拖动或按 Alt + ←→ 改了标签的顺序。</item>
/// <item><c>@onruneslide</c>：滑块（<c>data-rune-slider</c>）的值变了，拖动中每变一次发一次，松开或按键时带 commit。</item>
/// </list>
/// </summary>
[EventHandler("onrunetoggle", typeof(OverlayToggleEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunedismiss", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunecloserequest", typeof(CloseRequestEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunecommand", typeof(CommandEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunecopied", typeof(CopiedEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onruneresize", typeof(ResizeEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onruneslide", typeof(SlideEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrunereorder", typeof(ReorderEventArgs), enableStopPropagation: true, enablePreventDefault: false)]
public static class EventHandlers;
