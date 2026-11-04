using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>对话框（Dialog）与抽屉（Drawer）的基类：参数、状态与渲染相同，只有样式不同。</summary>
public abstract partial class DialogBase
{
    readonly string dialogId = UniqueId.Next("dialog");
    readonly string titleId = UniqueId.Next("dialog-title");
    Attrs? triggerAttrs;
    Memo<(bool Open, bool LabelledByTitle, bool LightDismiss), Attrs> managed;
    bool hasOpened;

    /// <summary>是否打开（<c>@bind-Open</c>）。只用 Trigger 打开时不用写。</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>打开或关闭后（Esc、点遮罩、关闭按钮、Trigger）。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>触发元素：参数是要加在它上面的属性（点击时打开）。</summary>
    [Parameter] public RenderFragment<Attrs>? Trigger { get; set; }

    /// <summary>标题（h2，也是对话框的名称）。没有标题时用 aria-label 给对话框起名。</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>正文。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>页脚：操作按钮，靠结束边排列。</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>宽度档位，默认 Md。</summary>
    [Parameter] public DialogWidth Width { get; set; } = DialogWidth.Md;

    /// <summary>点遮罩关闭，默认是；内容填了一半不能丢时关掉。</summary>
    [Parameter] public bool LightDismiss { get; set; } = true;

    /// <summary>不显示右上角的关闭按钮（Esc 仍可关闭）。</summary>
    [Parameter] public bool HideClose { get; set; }

    /// <summary>关闭按钮的名称（读屏读出）；不写时取语言包的“关闭”。</summary>
    [Parameter] public string? CloseLabel { get; set; }

    /// <summary>页头的部件属性。</summary>
    [Parameter] public Attrs? PropsHeader { get; set; }

    /// <summary>标题的部件属性。</summary>
    [Parameter] public Attrs? PropsTitle { get; set; }

    /// <summary>关闭按钮的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsClose { get; set; }

    /// <summary>正文的部件属性。</summary>
    [Parameter] public Attrs? PropsBody { get; set; }

    /// <summary>页脚的部件属性。</summary>
    [Parameter] public Attrs? PropsFooter { get; set; }

    Bindable<bool> open { get => field ??= new(() => Open, () => OpenChanged); }

    /// <summary>各部件的默认层（对话框居中，抽屉贴边）。</summary>
    protected abstract PartLayers<DialogPart> Parts { get; }

    // 交互模式下第一次打开才渲染内容；静态渲染一开始就渲染（由 JS 直接打开，来不及等渲染）
    bool ShouldRenderContent => hasOpened || open.Value || !RendererInfo.IsInteractive;

    Attrs TriggerAttrs => triggerAttrs ??= new Attrs { ["data-rune-open"] = dialogId, ["aria-haspopup"] = "dialog" };

    // dialog 上由组件管理的属性：data-open 交给 JS 同步原生状态（showModal / close）
    Attrs Managed => managed.Get((open.Value, Title is not null, LightDismiss), (dialogId, titleId), static (k, ids) => new Attrs
    {
        Id = ids.dialogId,
        AriaLabelledBy = k.LabelledByTitle ? ids.titleId : null,
        ["data-rune-overlay"] = true,
        ["data-open"] = k.Open ? "true" : "false",
        ["data-rune-light-dismiss"] = k.LightDismiss ? true : null,
    });

    Task HandleToggle(OverlayToggleEventArgs e)
    {
        if (e.Open) hasOpened = true;
        return e.Open == open.Value ? Task.CompletedTask : open.SetAsync(e.Open);
    }
}
