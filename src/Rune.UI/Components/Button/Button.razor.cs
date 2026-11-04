using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>按钮（button 元素）。</summary>
public partial class Button : ButtonBase
{
    /// <summary>按钮文字；加载中仍保留（读屏名称不变）。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>不可用：不能点击、不能聚焦（原生 disabled）。需要保持可聚焦时用 Loading 或自己写 aria-disabled。</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>占满容器宽度。</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>button 的 type：button（默认）、submit、reset。</summary>
    [Parameter] public string Type { get; set; } = "button";

    /// <inheritdoc/>
    protected override bool SubmitsForm => Type == "submit";

    /// <inheritdoc/>
    protected override ButtonVariant DefaultVariant => ButtonVariant.Solid;

    /// <inheritdoc/>
    protected override Tone DefaultTone => UI.Tone.Accent;

    /// <summary>button 上由组件管理的属性：不可用、加载中（可聚焦但不可操作）、波纹。只有八种组合，预先建好，渲染时不分配。</summary>
    Attrs Managed => managedByState[(Disabled ? 4 : 0) + (IsLoading ? 2 : 0) + (RippleEnabled ? 1 : 0)];

    static readonly Attrs[] managedByState = [.. Enumerable.Range(0, 8).Select(i => StateAttrs(disabled: (i & 4) != 0, loading: (i & 2) != 0, ripple: (i & 1) != 0))];

    static Attrs StateAttrs(bool disabled, bool loading, bool ripple) => new()
    {
        ["disabled"] = disabled ? true : null,
        ["aria-busy"] = loading ? "true" : null,
        AriaDisabled = loading ? true : null,
        ["data-loading"] = loading ? true : null,
        ["data-rune-ripple"] = ripple ? true : null,
    };
}
