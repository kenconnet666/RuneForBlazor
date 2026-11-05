using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>用户要关闭对话框的原因（<see cref="DialogBase.BeforeClose"/> 的参数）。</summary>
public enum DialogCloseReason
{
    /// <summary>点了关闭按钮、取消按钮（或写了 data-rune-close 的按钮）。</summary>
    Close,

    /// <summary>按了 Esc。</summary>
    Escape,

    /// <summary>点了遮罩。</summary>
    Outside,
}

/// <summary>对话框（Dialog）与抽屉（Drawer）的基类：参数、状态与渲染相同，只有样式不同。</summary>
public abstract partial class DialogBase
{
    readonly string dialogId = UniqueId.Next("dialog");
    Attrs? triggerAttrs;
    Memo<(bool Open, bool Described, bool Dismissible, bool Guarded), Attrs> managed;
    bool hasOpened;
    // 等确认的结果：这期间关不掉
    bool pending;

    /// <summary>是否打开（<c>@bind-Open</c>）。只用 Trigger 打开时不用写。</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>打开或关闭后（Esc、点遮罩、关闭按钮、确认、Trigger）。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>触发元素：参数是要加在它上面的属性（点击时打开）。</summary>
    [Parameter] public RenderFragment<Attrs>? Trigger { get; set; }

    /// <summary>标题（h2），同时是对话框的名称。</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>标题下的说明，读屏打开时一并念出（aria-describedby）。</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>正文。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>页脚：操作按钮，靠结束边排列（写了 OnConfirm 时排在“取消 / 确认”前面）。</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>标题行右侧的关闭按钮，默认显示。</summary>
    [Parameter] public bool Closable { get; set; } = true;

    /// <summary>点遮罩关闭，默认是；需要用户明确选择时关掉（Esc 仍可关闭）。</summary>
    [Parameter] public bool Dismissible { get; set; } = true;

    /// <summary>
    /// 用户要关闭时（关闭按钮、取消、Esc、点遮罩）先问它：返回 false 就不关，如表单有未保存的改动时先确认。
    /// 确认按钮与父级把 Open 改成 false 不经过它。
    /// </summary>
    [Parameter] public Func<DialogCloseReason, Task<bool>>? BeforeClose { get; set; }

    /// <summary>
    /// 确认：写了就在页脚放“取消”与“确认”两个按钮。执行期间确认按钮加载中，取消、Esc、遮罩、关闭按钮暂不可用，完成后关闭；
    /// 参数的 KeepOpen 设为 true 时保持打开（校验没过、提交失败由调用方提示原因）。
    /// </summary>
    [Parameter] public EventCallback<ConfirmEventArgs> OnConfirm { get; set; }

    /// <summary>确认按钮的文字；不写时取语言包的“确定”。</summary>
    [Parameter] public string? ConfirmText { get; set; }

    /// <summary>取消按钮的文字；不写时取语言包的“取消”。</summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>确认按钮的色调，默认 Accent；删除、驳回这类破坏性操作用 Danger。</summary>
    [Parameter] public Tone Tone { get; set; } = Tone.Accent;

    /// <summary>
    /// 交互模式下第一次打开才渲染正文（默认）。打开时就要聚焦正文里的元素（autofocus）时写 false。
    /// </summary>
    [Parameter] public bool Lazy { get; set; } = true;

    /// <summary>确认按钮的部件属性（交给 Button；表单的 type="submit" 与 form 写在这里）。</summary>
    [Parameter] public Attrs? PropsConfirm { get; set; }

    /// <summary>取消按钮的部件属性（交给 Button）。</summary>
    [Parameter] public Attrs? PropsCancel { get; set; }

    /// <summary>标题行的部件属性。</summary>
    [Parameter] public Attrs? PropsHeader { get; set; }

    /// <summary>标题的部件属性（交给 H2）。</summary>
    [Parameter] public Attrs? PropsTitle { get; set; }

    /// <summary>说明的部件属性（交给 Paragraph）。</summary>
    [Parameter] public Attrs? PropsDescription { get; set; }

    /// <summary>关闭按钮的部件属性（交给 CloseButton）。</summary>
    [Parameter] public Attrs? PropsClose { get; set; }

    /// <summary>正文的部件属性。</summary>
    [Parameter] public Attrs? PropsBody { get; set; }

    /// <summary>页脚的部件属性。</summary>
    [Parameter] public Attrs? PropsFooter { get; set; }

    Bindable<bool> open { get => field ??= new(this, () => Open, () => OpenChanged); }

    /// <summary>各部件的默认层（对话框居中，抽屉贴边）。</summary>
    protected abstract PartLayers<DialogPart> Parts { get; }

    string TitleId => $"{dialogId}-title";

    string DescriptionId => $"{dialogId}-description";

    // 交互模式下第一次打开才渲染正文；静态渲染一开始就渲染（由 JS 直接打开，来不及等渲染）
    bool ShouldRenderContent => !Lazy || hasOpened || open.Value || !RendererInfo.IsInteractive;

    Attrs TriggerAttrs => triggerAttrs ??= new Attrs { ["data-rune-open"] = dialogId, ["aria-haspopup"] = "dialog" };

    // dialog 上由组件管理的属性：data-open 交给 JS 同步原生状态（showModal / close）；
    // 有 BeforeClose 或确认中时 data-rune-guard-close：Esc、遮罩、关闭按钮不直接关，先问 C#
    Attrs Managed => managed.Get((open.Value, Description is not null, Dismissible, BeforeClose is not null || pending), dialogId, static (k, id) => new Attrs
    {
        Id = id,
        AriaLabelledBy = $"{id}-title",
        AriaDescribedBy = k.Described ? $"{id}-description" : null,
        ["data-rune-overlay"] = true,
        ["data-open"] = k.Open ? "true" : "false",
        ["data-rune-light-dismiss"] = k.Dismissible ? true : null,
        ["data-rune-guard-close"] = k.Guarded ? true : null,
    });

    Task HandleToggle(OverlayToggleEventArgs e)
    {
        if (e.Open) hasOpened = true;
        return e.Open == open.Value ? Task.CompletedTask : open.SetAsync(e.Open);
    }

    Task OnCloseRequest(CloseRequestEventArgs e) => RequestCloseAsync(e.Reason switch
    {
        "escape" => DialogCloseReason.Escape,
        "outside" => DialogCloseReason.Outside,
        _ => DialogCloseReason.Close,
    });

    /// <summary>用户要关闭：确认中不关；有 BeforeClose 先问它，同意了才关（受控的 Open 变成 false，JS 关上原生 dialog）。</summary>
    async Task RequestCloseAsync(DialogCloseReason reason)
    {
        if (pending) return;
        if (BeforeClose is not null && !await BeforeClose(reason)) return;
        await open.SetAsync(false);
    }

    async Task ConfirmAsync()
    {
        if (pending) return;
        pending = true;
        var args = new ConfirmEventArgs();
        try
        {
            await OnConfirm.InvokeAsync(args);
        }
        finally
        {
            pending = false;
        }
        if (!args.KeepOpen) await open.SetAsync(false);
    }
}
