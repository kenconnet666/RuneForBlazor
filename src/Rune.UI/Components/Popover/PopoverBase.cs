using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 弹出层（Popover、Menu）的基类：原生 <c>popover="auto"</c> 元素，点外面、按 Esc 关闭，同一时间只开一个；
/// 触发元素由使用方写，组件把要加的属性交给它：
/// <code>
/// &lt;Popover&gt;
///     &lt;Trigger Context="t"&gt;&lt;Button @attributes="t"&gt;筛选&lt;/Button&gt;&lt;/Trigger&gt;
///     &lt;ChildContent&gt;…&lt;/ChildContent&gt;
/// &lt;/Popover&gt;
/// </code>
/// 打开靠原生的 <c>popovertarget</c>，静态 SSR 也能用；位置用 CSS 锚点定位（<see cref="Anchor"/>）。
/// </summary>
public abstract class PopoverBase : UIComponent
{
    Attrs? triggerAttrs;
    Memo<(bool? Open, Placement Placement, string? Role, string? LabelledBy), Attrs> overlayAttrs;

    /// <summary>触发元素：参数是要加在触发元素上的属性（<c>popovertarget</c>、锚点名等）。</summary>
    [Parameter] public RenderFragment<Attrs>? Trigger { get; set; }

    /// <summary>相对触发元素的位置，默认下方；放不下时翻到对面。</summary>
    [Parameter] public Placement Placement { get; set; }

    /// <summary>受控的打开状态（<c>@bind-Open</c>）。不写时由浏览器管理：点触发元素打开，点外面或按 Esc 关闭。</summary>
    [Parameter] public bool? Open { get; set; }

    /// <summary>打开或关闭后（用户操作或 <see cref="Open"/> 变化）。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>浮层元素的 id。</summary>
    protected string OverlayId { get; } = UniqueId.Next("popover");

    /// <summary>浮层是否打开过。</summary>
    protected bool HasOpened { get; private set; }

    /// <summary>触发元素上的 aria-haspopup：dialog（Popover）、menu（Menu）。</summary>
    protected abstract string HasPopup { get; }

    /// <summary>交给触发元素的属性；创建一次。</summary>
    protected Attrs TriggerAttrs => triggerAttrs ??= Attrs.Merge(
        Anchor.Trigger(OverlayId),
        new Attrs { ["popovertarget"] = OverlayId, ["aria-haspopup"] = HasPopup });

    /// <summary>浮层元素上由组件管理的属性：popover、id、角色、受控状态、位置、锚点；输入不变时是同一个对象。</summary>
    /// <param name="role">浮层的角色（dialog、menu）。</param>
    /// <param name="labelledBy">标题元素的 id。</param>
    protected Attrs OverlayAttrs(string? role, string? labelledBy = null) =>
        overlayAttrs.Get((Open, Placement, role, labelledBy), OverlayId, static (k, id) => Attrs.Merge(
            Anchor.Overlay(id),
            new Attrs
            {
                Id = id,
                Role = k.Role,
                AriaLabelledBy = k.LabelledBy,
                ["popover"] = "auto",
                ["data-rune-overlay"] = true,
                ["data-placement"] = Anchor.NameOf(k.Placement),
                ["data-open"] = k.Open is { } open ? (open ? "true" : "false") : null,
            }));

    /// <summary>浏览器报告打开或关闭（<c>@onrunetoggle</c>）。</summary>
    protected Task HandleToggle(OverlayToggleEventArgs e)
    {
        if (e.Open) HasOpened = true;
        return OpenChanged.InvokeAsync(e.Open);
    }
}
