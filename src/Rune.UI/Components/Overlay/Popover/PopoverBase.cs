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
/// 打开靠原生的 <c>popovertarget</c>，静态 SSR 也能用；位置由 JS 初始化器用 Floating UI 计算（见 <see cref="Floating"/>），
/// 打开、定位好之后焦点移进去，关闭后回到触发元素。
/// </summary>
public abstract class PopoverBase : UIComponent
{
    Attrs? triggerAttrs;
    Memo<(bool? Open, Placement Placement, int? Offset, string? Role, string? LabelledBy, bool FitHeight), Attrs> overlayAttrs;

    /// <summary>触发元素：参数是要加在触发元素上的属性（<c>popovertarget</c>、id、aria-haspopup 等）。</summary>
    [Parameter] public RenderFragment<Attrs>? Trigger { get; set; }

    /// <summary>相对触发元素的位置，默认下方、与起始边对齐；空间不够时自动翻到另一侧。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>与触发元素的距离（px），默认 6。</summary>
    [Parameter] public int? Offset { get; set; }

    /// <summary>受控的打开状态（<c>@bind-Open</c>）。不写时由浏览器管理：点触发元素打开，点外面或按 Esc 关闭。</summary>
    [Parameter] public bool? Open { get; set; }

    /// <summary>打开或关闭后（用户操作或 <see cref="Open"/> 变化）。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>浮层元素的 id。</summary>
    protected string OverlayId { get; } = UniqueId.Next("popover");

    /// <summary>触发元素的 id：浮层默认以触发元素的文字作为名称（aria-labelledby）。</summary>
    protected string TriggerId => $"{OverlayId}-trigger";

    /// <summary>浮层是否打开过。</summary>
    protected bool HasOpened { get; private set; }

    /// <summary>触发元素上的 aria-haspopup：dialog（Popover）、menu（Menu）。</summary>
    protected abstract string HasPopup { get; }

    /// <summary>交给触发元素的属性；创建一次。</summary>
    protected Attrs TriggerAttrs => triggerAttrs ??= new Attrs
    {
        Id = TriggerId,
        ["popovertarget"] = OverlayId,
        ["aria-haspopup"] = HasPopup,
        ["aria-controls"] = OverlayId,
    };

    /// <summary>浮层元素上由组件管理的属性：popover、id、角色、受控状态、定位约定；输入不变时是同一个对象。</summary>
    /// <param name="role">浮层的角色（dialog、alertdialog、menu）。</param>
    /// <param name="labelledBy">名称来源元素的 id。</param>
    /// <param name="fitHeight">最大高度限制在视口剩下的空间里（菜单、选项列表；浮层自己滚动）。</param>
    protected Attrs OverlayAttrs(string? role, string? labelledBy = null, bool fitHeight = false) =>
        overlayAttrs.Get((Open, Placement, Offset, role, labelledBy, fitHeight), OverlayId, static (k, id) => Attrs.Merge(
            Floating.Placed(k.Placement, k.Offset),
            new Attrs
            {
                Id = id,
                Role = k.Role,
                AriaLabelledBy = k.LabelledBy,
                ["popover"] = "auto",
                ["data-rune-overlay"] = true,
                ["data-open"] = k.Open is { } open ? (open ? "true" : "false") : null,
                ["data-fit-height"] = k.FitHeight ? true : null,
            }));

    /// <summary>浏览器报告打开或关闭（<c>@onrunetoggle</c>）。</summary>
    protected Task HandleToggle(OverlayToggleEventArgs e)
    {
        if (e.Open) HasOpened = true;
        return OpenChanged.InvokeAsync(e.Open);
    }
}
