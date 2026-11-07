using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 选择器（DatePicker、TimePicker、DateRangePicker）共用的打开按钮与弹层属性（同 vue-tsx-pro 的 usePickerPopover）：
/// 输入框外壳是定位的锚点，外壳里的按钮用 popovertarget 打开（静态 SSR 也能打开），弹层是 role="dialog" 的原生 popover（Esc、点外部关闭），
/// 开关经 rune-toggle 报给组件；关闭时浏览器把焦点还给按钮。
/// </summary>
static class PickerParts
{
    /// <summary>外壳里的打开按钮。</summary>
    public static RenderFragment Trigger(IconData icon, string label, bool editable, bool open, string popoverId, Attrs? user) => builder =>
    {
        builder.OpenComponent<IconButton>(0);
        builder.AddComponentParameter(1, nameof(IconButton.Icon), icon);
        builder.AddComponentParameter(2, nameof(IconButton.Label), label);
        builder.AddComponentParameter(3, nameof(IconButton.Radius), Radius.Full);
        builder.AddComponentParameter(4, nameof(IconButton.Disabled), !editable);
        builder.AddMultipleAttributes(5, Attrs.MergeProps(null, user, new Attrs
        {
            ["popovertarget"] = popoverId,
            ["aria-haspopup"] = "dialog",
            ["aria-expanded"] = open ? "true" : "false",
            ["aria-controls"] = popoverId,
        }));
        builder.CloseComponent();
    };

    /// <summary>弹层上由组件管理的属性：popover、贴着外壳定位（列表与外壳同宽）、开关报给组件。</summary>
    public static Attrs Popover(string id, string label, Placement placement, bool open, string anchorId, bool matchWidth, EventCallback<OverlayToggleEventArgs> toggled) =>
        Attrs.Merge(Floating.Placed(placement, null), new Attrs
        {
            Id = id,
            Role = "dialog",
            AriaLabel = label,
            ["popover"] = "auto",
            ["data-rune-overlay"] = true,
            ["data-open"] = open ? "true" : "false",
            ["data-anchor"] = anchorId,
            ["data-match-width"] = matchWidth ? true : null,
            ["onrunetoggle"] = toggled,
        });
}
