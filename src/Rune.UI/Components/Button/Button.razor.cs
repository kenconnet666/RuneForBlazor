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
    protected override ButtonVariant DefaultVariant => ButtonVariant.Solid;

    /// <inheritdoc/>
    protected override Tone DefaultTone => UI.Tone.Accent;

    /// <summary>button 上由组件管理的属性：不可用、加载中（可聚焦但不可操作）。只有四种组合，预先建好，渲染时不分配。</summary>
    Attrs Managed => managedByState[(Disabled ? 2 : 0) + (Loading ? 1 : 0)];

    static readonly Attrs[] managedByState =
    [
        Attrs.Empty,
        StateAttrs(disabled: false, loading: true),
        StateAttrs(disabled: true, loading: false),
        StateAttrs(disabled: true, loading: true),
    ];

    static Attrs StateAttrs(bool disabled, bool loading) => new()
    {
        ["disabled"] = disabled ? true : null,
        ["aria-busy"] = loading ? "true" : null,
        AriaDisabled = loading ? true : null,
        ["data-loading"] = loading ? true : null,
    };
}
