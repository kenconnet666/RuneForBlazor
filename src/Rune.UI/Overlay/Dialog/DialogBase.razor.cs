using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>对话框（Dialog）与抽屉（Drawer）的基类：参数、状态与渲染相同，只有样式不同。</summary>
public abstract partial class DialogBase
{
    readonly string autoId = UniqueId.Next("dialog");
    Memo<string, Attrs> triggerAttrs;
    Memo<(string Id, bool Open, bool Described, bool Dismissible, bool Guarded, string? InlineStyle, string? Role), Attrs> managed;
    bool hasOpened;
    // 等确认的结果：这期间关不掉
    bool pending;

    /// <summary>dialog 元素的 id，默认自动生成：别处的按钮写 <c>data-rune-open="这个 id"</c> 也能直接打开它（AppShell 顶栏的菜单按钮）。</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>是否打开（<c>@bind-Open</c>）。只用 Trigger 打开时不用写。</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>打开或关闭后（Esc、点遮罩、关闭按钮、确认、Trigger）。</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>用户（或确认）打开、关上了对话框，带原因：关闭按钮 Close、Esc Escape、点遮罩 Outside、确认 Confirm、Trigger 打开 Trigger；父级直接改 Open 时不报告。</summary>
    [Parameter] public EventCallback<OverlayOpenChange> OnOpenChange { get; set; }

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
    [Parameter] public Func<OverlayChangeReason, Task<bool>>? BeforeClose { get; set; }

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

    string DialogId => Id ?? autoId;

    string TitleId => $"{DialogId}-title";

    string DescriptionId => $"{DialogId}-description";

    // 交互模式下第一次打开才渲染正文；静态渲染一开始就渲染（由 JS 直接打开，来不及等渲染）
    bool ShouldRenderContent => !Lazy || hasOpened || open.Value || !RendererInfo.IsInteractive;

    Attrs TriggerAttrs => triggerAttrs.Get(DialogId, static id => new Attrs { ["data-rune-open"] = id, ["aria-haspopup"] = "dialog" });

    // dialog 上由组件管理的属性：data-open 交给 JS 同步原生状态（showModal / close）；
    // 有 BeforeClose 或确认中时 data-rune-guard-close：Esc、遮罩、关闭按钮不直接关，先问 C#
    /// <summary>子类给 dialog 元素的行内样式（拖动后的抽屉宽度）；没有时为 null。</summary>
    protected virtual string? RootInlineStyle => null;

    /// <summary>标题行上由组件管理的属性（可拖动的对话框把它当把手）。</summary>
    protected virtual Attrs? HeaderManaged => null;

    /// <summary>子类放在 dialog 里、各部件之后的内容（抽屉的宽度调整手柄）。</summary>
    protected virtual RenderFragment? Edge => null;

    /// <summary>dialog 的角色：普通对话框不写（原生 dialog），确认对话框是 alertdialog。</summary>
    protected virtual string? Role => null;

    /// <summary>点遮罩关闭（确认对话框不关）。</summary>
    protected virtual bool LightDismiss => Dismissible;

    /// <summary>标题行显示关闭按钮（确认对话框不显示）。</summary>
    protected virtual bool ShowClose => Closable;

    /// <summary>页脚有“取消 / 确认”（确认对话框总有）。</summary>
    protected virtual bool HasConfirm => OnConfirm.HasDelegate;

    /// <summary>取消按钮的默认层：确认对话框打开时焦点先落在取消上（误按 Enter 不会执行破坏性操作）。</summary>
    protected virtual Attrs? CancelDefaults => null;

    Attrs Managed => managed.Get((DialogId, open.Value, Description is not null, LightDismiss, BeforeClose is not null || pending, RootInlineStyle, Role), static k => new Attrs
    {
        Id = k.Id,
        Role = k.Role,
        InlineStyle = k.InlineStyle,
        AriaLabelledBy = $"{k.Id}-title",
        AriaDescribedBy = k.Described ? $"{k.Id}-description" : null,
        ["data-rune-overlay"] = true,
        ["data-open"] = k.Open ? "true" : "false",
        ["data-rune-light-dismiss"] = k.Dismissible ? true : null,
        ["data-rune-guard-close"] = k.Guarded ? true : null,
    });

    async Task HandleToggle(OverlayToggleEventArgs e)
    {
        if (e.Open) hasOpened = true;
        if (e.Open == open.Value) return;
        await open.SetAsync(e.Open);
        await OnOpenChange.InvokeAsync(new(e.Open, OverlayOpenChange.Parse(e.Reason, e.Open)));
    }

    Task OnCloseRequest(CloseRequestEventArgs e) => RequestCloseAsync(OverlayOpenChange.Parse(e.Reason, open: false));

    /// <summary>用户要关闭：确认中不关；有 BeforeClose 先问它，同意了才关（受控的 Open 变成 false，JS 关上原生 dialog）。</summary>
    async Task RequestCloseAsync(OverlayChangeReason reason)
    {
        if (pending) return;
        if (BeforeClose is not null && !await BeforeClose(reason)) return;
        await CloseAsync(reason);
    }

    async Task CloseAsync(OverlayChangeReason reason)
    {
        await open.SetAsync(false);
        await OnOpenChange.InvokeAsync(new(false, reason));
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
        if (!args.KeepOpen) await CloseAsync(OverlayChangeReason.Confirm);
    }
}
