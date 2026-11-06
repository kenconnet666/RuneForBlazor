using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>按钮（button 元素）。</summary>
public partial class Button : ButtonCore
{
    /// <summary>按钮文字；加载中仍保留（读屏名称不变）。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>不可用：原生 disabled，不能聚焦。</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// 不可用时仍可聚焦：改用 aria-disabled，点击无效。需要用 Tooltip 说明“为什么不能点”时用，
    /// 或者按钮在一组键盘导航里、不希望焦点跳过它。
    /// </summary>
    [Parameter] public bool FocusableWhenDisabled { get; set; }

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

    /// <summary>button 上由组件管理的属性（见 <see cref="ButtonCore.StateAttrs"/>）。只有十六种组合，预先建好，渲染时不分配。</summary>
    Attrs Managed => managedByState[(Disabled ? 8 : 0) + (FocusableWhenDisabled ? 4 : 0) + (IsLoading ? 2 : 0) + (RippleEnabled ? 1 : 0)];

    static readonly Attrs[] managedByState =
        [.. Enumerable.Range(0, 16).Select(i => StateAttrs((i & 8) != 0, (i & 4) != 0, (i & 2) != 0, (i & 1) != 0))];
}
