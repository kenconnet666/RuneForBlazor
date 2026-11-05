using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rune.UI;

/// <summary>折叠面板：一个按钮控制一块内容的展开与收起。</summary>
public partial class Collapsible : UIComponent
{
    // 按钮与内容区互相引用（aria-controls / aria-labelledby）
    readonly string triggerId = UniqueId.Next("collapsible-trigger");
    readonly string contentId = UniqueId.Next("collapsible-content");

    /// <summary>按钮上的标题。</summary>
    [Parameter] public RenderFragment? Title { get; set; }

    /// <summary>展开后显示的内容；收起时不渲染。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>外观；不写时取配置里的默认值（<see cref="CollapsibleDefaults"/>），再没有就是 <see cref="CollapsibleVariant.Plain"/>。</summary>
    [Parameter] public CollapsibleVariant? Variant { get; set; }

    /// <summary>是否展开。可以 <c>@bind-Open</c> 双向绑定，也可以只给初始值。</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>展开状态变化（用户点击按钮）。给了它就由父级决定是否采纳。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>按钮的部件属性（样式、aria、事件；展开状态与 id 由组件管理）。</summary>
    [Parameter] public Attrs? PropsTrigger { get; set; }

    /// <summary>内容区的部件属性。</summary>
    [Parameter] public Attrs? PropsContent { get; set; }

    Bindable<bool> open { get => field ??= new(this, () => Open, () => OpenChanged); }

    /// <summary>各部件的默认层：外观取参数、配置的默认值、组件默认值，定制取配置。</summary>
    PartLayers<CollapsiblePart> Parts => CollapsibleStyles.Of(
        Variant ?? Config.Defaults.Get<CollapsibleDefaults>()?.Variant ?? CollapsibleVariant.Plain,
        Config.Styles);

    /// <summary>按钮上由组件管理的属性：展开状态、关联的内容区、点击切换。</summary>
    Attrs TriggerManaged => new()
    {
        Id = triggerId,
        AriaExpanded = open.Value,
        AriaControls = contentId,
        OnClick = ToggleAsync,
    };

    /// <summary>内容区上由组件管理的属性：作为按钮所控制的区域，名称取自按钮。</summary>
    Attrs ContentManaged => new()
    {
        Id = contentId,
        Role = "region",
        AriaLabelledBy = triggerId,
    };

    Task ToggleAsync(MouseEventArgs _) => open.SetAsync(!open.Value);
}
