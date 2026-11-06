using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 命令式浮层（<see cref="DialogService.Open"/>）共用的选项：对话框、抽屉的参数，正文写在 <see cref="Content"/>。
/// 改属性用句柄的 <see cref="DialogHandle.Update"/>：<c>handle.Update(o =&gt; o with { Title = "…" })</c>。
/// </summary>
public abstract record OverlayOptions
{
    /// <summary>标题，同时是对话框的名称。</summary>
    public required string Title { get; init; }

    /// <summary>标题下的说明。</summary>
    public string? Description { get; init; }

    /// <summary>
    /// 正文：参数是句柄，正文里的按钮可以 <c>handle.Close()</c>、<c>handle.Update(…)</c>；正文里的组件也能经级联参数拿到句柄
    /// （<c>[CascadingParameter] DialogHandle? Dialog</c>）。正文是一个组件时用 <see cref="DialogService.Open{TComponent}"/>。
    /// </summary>
    public RenderFragment<DialogHandle>? Content { get; init; }

    /// <summary>页脚（写了 OnConfirm 时排在“取消 / 确认”前面）。</summary>
    public RenderFragment? Footer { get; init; }

    /// <summary>确认：写了页脚就有“取消 / 确认”，执行期间加载中，完成后关闭（原因 Confirm）；参数的 KeepOpen 设为 true 时保持打开。</summary>
    public Func<ConfirmEventArgs, Task>? OnConfirm { get; init; }

    /// <summary>确认按钮的文字，默认取语言包。</summary>
    public string? ConfirmText { get; init; }

    /// <summary>取消按钮的文字，默认取语言包。</summary>
    public string? CancelText { get; init; }

    /// <summary>确认按钮的色调，默认 Accent。</summary>
    public Tone Tone { get; init; } = Tone.Accent;

    /// <summary>标题行的关闭按钮，默认显示。</summary>
    public bool Closable { get; init; } = true;

    /// <summary>点遮罩关闭，默认是。</summary>
    public bool Dismissible { get; init; } = true;

    /// <summary>用户要关闭时先问它，返回 false 就不关。</summary>
    public Func<OverlayChangeReason, Task<bool>>? BeforeClose { get; init; }

    /// <summary>开了（原因 Imperative）、关了（关闭按钮、Esc、遮罩、确认、句柄的 Close）。</summary>
    public Action<OverlayOpenChange>? OnOpenChange { get; init; }
}

/// <summary>命令式对话框的选项（<see cref="DialogService.Open"/>）。</summary>
public sealed record DialogOptions : OverlayOptions
{
    /// <summary>宽度档位，默认 Xs。</summary>
    public DialogSize? Size { get; init; }
}

/// <summary>命令式抽屉的选项（<see cref="DialogService.Open"/>）。</summary>
public sealed record DrawerOptions : OverlayOptions
{
    /// <summary>从哪一边滑出，默认结束边。</summary>
    public DrawerSide Side { get; init; }

    /// <summary>宽度档位，默认 Xs。</summary>
    public ContainerSize? Size { get; init; }
}

/// <summary><see cref="DialogService.ConfirmAsync(ConfirmOptions)"/> 的选项：确认对话框（AlertDialog）的参数。</summary>
public sealed record ConfirmOptions
{
    /// <summary>标题：一句问话（“删除这个项目？”）。</summary>
    public required string Title { get; init; }

    /// <summary>后果的说明（“删除后无法恢复。”）。</summary>
    public string? Description { get; init; }

    /// <summary>说明下面的更多内容。</summary>
    public RenderFragment? Content { get; init; }

    /// <summary>确认按钮的文字，默认取语言包。</summary>
    public string? ConfirmText { get; init; }

    /// <summary>取消按钮的文字，默认取语言包。</summary>
    public string? CancelText { get; init; }

    /// <summary>确认按钮的色调；删除、驳回这类用 Danger。</summary>
    public Tone Tone { get; init; } = Tone.Accent;

    /// <summary>宽度档位，默认 Xs。</summary>
    public DialogSize? Size { get; init; }

    /// <summary>确认时执行（拿到填的理由，没有理由时是空字符串）：返回 false 时对话框保持打开，可以再试或取消。</summary>
    public Func<string, Task<bool>>? OnConfirm { get; init; }
}

/// <summary>确认时要填的理由（驳回、作废、退回）：对话框里多一个多行输入框。</summary>
/// <param name="Label">输入框的标签（“驳回理由”）。</param>
public sealed record ConfirmReason(string Label)
{
    /// <summary>必填：空着点确认时提示“必填”、不关闭。</summary>
    public bool Required { get; init; }

    /// <summary>输入框的占位文字。</summary>
    public string? Placeholder { get; init; }

    /// <summary>输入框下的说明（“申请人会收到这段理由”）。</summary>
    public string? Description { get; init; }

    /// <summary>最多几个字，显示字数。</summary>
    public int? MaxLength { get; init; }
}

/// <summary>
/// 命令式浮层的句柄（<see cref="DialogService.Open"/> 的结果）：关上、改属性、等它关上。方法不依赖调用处，可以直接传出去。
/// </summary>
public sealed class DialogHandle
{
    readonly DialogService service;
    readonly TaskCompletionSource<OverlayChangeReason> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal DialogHandle(DialogService service, OverlayOptions options)
    {
        this.service = service;
        Options = options;
    }

    /// <summary>当前的选项。</summary>
    public OverlayOptions Options { get; private set; }

    /// <summary>关上之后完成，得到关上的原因：确认是 Confirm，Esc、遮罩、关闭按钮、<see cref="Close()"/> 各是 Escape、Outside、Close、Imperative。</summary>
    public Task<OverlayChangeReason> Closed => closed.Task;

    /// <summary>还开着。</summary>
    public bool IsOpen { get; private set; } = true;

    /// <summary>关上时交回的结果（<see cref="Close(object?)"/> 给的）；用户取消、Esc、点遮罩关上时是 null。</summary>
    public object? Result { get; private set; }

    /// <summary>关上（原因 Imperative）；已经关上时什么也不做。</summary>
    public void Close() => Close(null);

    /// <summary>关上并交回结果（原因 Imperative）：正文里选好了一项、填完了表单。已经关上时什么也不做。</summary>
    /// <param name="result">结果，之后从 <see cref="Result"/> 或 <see cref="ResultAsync{TResult}"/> 取。</param>
    public void Close(object? result)
    {
        if (!IsOpen) return;
        Result = result;
        Options.OnOpenChange?.Invoke(new(false, OverlayChangeReason.Imperative));
        MarkClosed(OverlayChangeReason.Imperative);
    }

    /// <summary>等它关上，得到交回的结果；没有交回（取消、Esc）或类型不对时是 default。</summary>
    /// <typeparam name="TResult">结果的类型。</typeparam>
    public async Task<TResult?> ResultAsync<TResult>()
    {
        await Closed;
        return Result is TResult result ? result : default;
    }

    /// <summary>改属性（标题、宽度、确认按钮的文字、正文……）：<c>handle.Update(o =&gt; o with { Title = "…" })</c>。</summary>
    public void Update(Func<OverlayOptions, OverlayOptions> change)
    {
        Options = change(Options);
        service.NotifyChanged();
    }

    /// <summary>用户（或确认）关上了：DialogHost 报来。</summary>
    internal void MarkClosed(OverlayChangeReason reason)
    {
        if (!IsOpen) return;
        IsOpen = false;
        service.Remove(this, () => closed.TrySetResult(reason));
    }
}

/// <summary>命令式的确认（<see cref="DialogService.ConfirmAsync(ConfirmOptions)"/>）：DialogHost 渲染成 AlertDialog。</summary>
internal sealed class ConfirmRequest(ConfirmOptions options, ConfirmReason? reason)
{
    readonly TaskCompletionSource<string?> result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConfirmOptions Options { get; } = options;

    public ConfirmReason? Reason { get; } = reason;

    /// <summary>填的理由。</summary>
    public string Text { get; set; } = "";

    /// <summary>点过确认：之后必填没填时提示。</summary>
    public bool Tried { get; set; }

    public bool Confirmed { get; set; }

    public bool IsOpen { get; set; } = true;

    public bool Missing => Reason is { Required: true } && string.IsNullOrWhiteSpace(Text);

    /// <summary>确认了得到理由（没有理由时是空字符串），取消得到 null。</summary>
    public Task<string?> Result => result.Task;

    public void Finish() => result.TrySetResult(Confirmed ? Text.Trim() : null);
}

/// <summary>
/// 命令式的对话框、抽屉与确认（同 vue-tsx-pro 的 useDialog、useDrawer、useConfirm）：不必在页面里放对话框、管 Open，
/// 从列表的一行、回调里直接弹出，关上后自动移除。注入后在布局里放一个 <see cref="DialogHost"/> 显示它们。
/// <code>
/// var handle = Dialogs.Open(new DialogOptions { Title = $"发票 {no}", Content = h =&gt; @&lt;Details OnDone="h.Close" /&gt; });
/// var reason = await handle.Closed;                                 // Confirm、Escape……
/// if (await Dialogs.ConfirmAsync(new() { Title = "退出登录？" })) …
/// var text = await Dialogs.ConfirmAsync(new() { Title = "驳回？", Tone = Tone.Danger }, new ConfirmReason("驳回理由") { Required = true });
/// </code>
/// </summary>
public sealed class DialogService
{
    /// <summary>关上后等离场过渡播完再移除。</summary>
    static readonly TimeSpan LeaveDuration = TimeSpan.FromMilliseconds(400);

    readonly Lock gate = new();
    readonly State<ImmutableList<object>> entries = new([]);
    // 句柄改了属性、开关变了：列表没变也要让 DialogHost 重新渲染
    readonly State<int> revision = new(0);

    /// <summary>开着（和正在离场）的浮层：<see cref="DialogHandle"/> 与确认。渲染里读它的组件跟着重新渲染。</summary>
    internal IReadOnlyList<object> Entries
    {
        get
        {
            _ = revision.Value;
            return entries.Value;
        }
    }

    /// <summary>打开一个对话框（<see cref="DialogOptions"/>）或抽屉（<see cref="DrawerOptions"/>），返回句柄。</summary>
    public DialogHandle Open(OverlayOptions options)
    {
        var handle = new DialogHandle(this, options);
        Add(handle);
        options.OnOpenChange?.Invoke(new(true, OverlayChangeReason.Imperative));
        return handle;
    }

    /// <summary>
    /// 打开一个正文是组件的对话框或抽屉（同 Blazor 的 DynamicComponent：组件类型加参数字典）。组件经级联参数拿到句柄，
    /// 用 <c>Dialog.Close(结果)</c> 关上并交回结果：
    /// <code>
    /// var handle = Dialogs.Open&lt;UserPicker&gt;(new DialogOptions { Title = "选择负责人" }, new() { ["Department"] = "采购部" });
    /// var user = await handle.ResultAsync&lt;User&gt;();
    /// </code>
    /// </summary>
    /// <param name="options">对话框（<see cref="DialogOptions"/>）或抽屉（<see cref="DrawerOptions"/>）的参数；其中的 Content 不用。</param>
    /// <param name="parameters">交给组件的参数（名字是参数属性名）。</param>
    /// <typeparam name="TComponent">正文组件。</typeparam>
    public DialogHandle Open<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        OverlayOptions options, IReadOnlyDictionary<string, object?>? parameters = null) where TComponent : IComponent =>
        Open(options with { Content = _ => builder =>
        {
            builder.OpenComponent<TComponent>(0);
            if (parameters is not null)
                foreach (var (name, value) in parameters) builder.AddComponentParameter(1, name, value);
            builder.CloseComponent();
        } });

    /// <summary>问一句：确认（OnConfirm 完成）得到 true，取消、Esc 得到 false。</summary>
    public async Task<bool> ConfirmAsync(ConfirmOptions options) => await AskAsync(options, null) is not null;

    /// <summary>要填理由：确认得到填的理由，取消、Esc 得到 null；必填没填时提示且不关闭。</summary>
    public Task<string?> ConfirmAsync(ConfirmOptions options, ConfirmReason reason) => AskAsync(options, reason);

    Task<string?> AskAsync(ConfirmOptions options, ConfirmReason? reason)
    {
        var request = new ConfirmRequest(options, reason);
        Add(request);
        return request.Result;
    }

    /// <summary>确认关上了：DialogHost 报来。</summary>
    internal void CloseConfirm(ConfirmRequest request)
    {
        if (!request.IsOpen) return;
        request.IsOpen = false;
        Remove(request, request.Finish);
    }

    internal void NotifyChanged()
    {
        lock (gate) revision.Value = revision.Peek() + 1;
    }

    void Add(object entry)
    {
        lock (gate) entries.Value = entries.Peek().Add(entry);
    }

    /// <summary>重新渲染（浮层关上、离场），过一会儿移除；结果在离场开始时就给出。</summary>
    internal void Remove(object entry, Action settle)
    {
        NotifyChanged();
        settle();
        _ = RemoveLaterAsync(entry);
    }

    async Task RemoveLaterAsync(object entry)
    {
        await Task.Delay(LeaveDuration);
        lock (gate) entries.Value = entries.Peek().Remove(entry);
    }
}
