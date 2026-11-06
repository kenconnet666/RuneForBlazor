using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>通知为什么关闭。</summary>
public enum ToastCloseReason
{
    /// <summary>到时间自动消失。</summary>
    Timeout,

    /// <summary>用户点了关闭、滑走、按了 Esc，或代码调用了 Dismiss。</summary>
    Dismissed,
}

/// <summary>一条通知的内容与行为（同 vue-tsx-pro 的 ToastOptions）。</summary>
public sealed record ToastOptions
{
    /// <summary>标题（加粗的第一行）。</summary>
    public string? Title { get; init; }

    /// <summary>正文。</summary>
    public string? Description { get; init; }

    /// <summary>色调，默认 Neutral；也决定图标。</summary>
    public Tone? Tone { get; init; }

    /// <summary>图标；不写时按色调选。</summary>
    public IconData? Icon { get; init; }

    /// <summary>不显示图标。</summary>
    public bool HideIcon { get; init; }

    /// <summary>进行中：图标换成加载指示，不自动消失。</summary>
    public bool Loading { get; init; }

    /// <summary>右侧的操作（如“撤销”按钮）。</summary>
    public RenderFragment? Action { get; init; }

    /// <summary>显示关闭按钮，默认 true；false 时也不能滑走、按 Esc 关闭。</summary>
    public bool Closable { get; init; } = true;

    /// <summary>显示多久后自动消失；<see cref="Timeout.InfiniteTimeSpan"/> 不自动消失；不写时用 <see cref="ToastService.DefaultDuration"/>。</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>重要：读屏立即打断播报（aria-live="assertive"），用于出错、需要马上处理的通知。</summary>
    public bool Important { get; init; }

    /// <summary>编号：同一个编号的通知只有一条，再 Show 时换掉它（接口重试、连点保存时只留一条）；不写时自动生成。</summary>
    public string? Id { get; init; }

    /// <summary>开始关闭时调用（自动消失或被关闭）；同一条通知只调用一次。</summary>
    public Action<ToastCloseReason>? OnClose { get; init; }
}

/// <summary>列表里的一条通知。</summary>
/// <param name="Id">编号（Show 的返回值，列表的 key）。</param>
/// <param name="Options">内容与行为。</param>
/// <param name="Closing">正在离场：已关闭，等离场过渡结束后移除。</param>
/// <param name="Countdown">本轮自动关闭的计时（剩余时间条按它画）；不自动关闭（加载中、不限时）时为 null。</param>
public sealed record Toast(string Id, ToastOptions Options, bool Closing, ToastCountdown? Countdown);

/// <summary>一轮自动关闭的计时：总时长与编号（重新计时时换一个）。</summary>
public sealed record ToastCountdown(TimeSpan Duration, long Id);

/// <summary><see cref="ToastService.PromiseAsync{T}"/> 的三个阶段：进行中，以及由结果算出的成功、失败的内容。</summary>
public sealed record ToastPromiseMessages<T>(ToastOptions Loading, Func<T, ToastOptions> Success, Func<Exception, ToastOptions> Error);

/// <summary>
/// 通知（同 vue-tsx-pro 的 createToaster 控制器）：操作结果的短暂提示，几秒后自动消失。注入后调用，页面上放一个 <see cref="Toaster"/> 显示：
/// <code>
/// @inject ToastService Toasts
/// Toasts.Success("已保存");
/// var id = Toasts.Show(new() { Title = "已删除", Action = @&lt;Button @onclick="Undo"&gt;撤销&lt;/Button&gt; });
/// Toasts.Loading("正在导出", new() { Id = "export" }); …; Toasts.Success("导出完成", new() { Id = "export" });   同一个 Id 换掉原来那条
/// await Toasts.PromiseAsync(UploadAsync(), new(new() { Title = "正在上传" }, _ => new() { Title = "上传完成" }, e => new() { Title = e.Message }));
/// </code>
/// 计时：默认 5 秒；指针在通知区域上、焦点在区域里时暂停（Toaster 调 Pause / Resume）；加载中的、不限时的不自动消失；
/// 只给显示出来的（最新的 <see cref="Limit"/> 条）计时，没轮到的等它出现再开始。
/// 列表放在信号里，Toaster 读到它就会在变化时重新渲染；后台线程里调用也安全。每个用户（Server 的每个电路、WebAssembly 的每个页面）一份（scoped）。
/// </summary>
/// <param name="time">计时用的时钟（测试里换成假的）；不给时用系统时钟。</param>
public sealed class ToastService(TimeProvider? time = null) : IDisposable
{
    readonly TimeProvider time = time ?? TimeProvider.System;

    /// <summary>离场过渡（高度收起、淡出）的时间：之后从列表里移除。</summary>
    static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(300);

    readonly State<ImmutableList<Toast>> toasts = new([]);
    readonly State<bool> paused = new(false);
    readonly Dictionary<string, Countdown> timers = [];
    readonly Lock gate = new();
    readonly CancellationTokenSource disposed = new();
    long lastId;
    long countdowns;
    int limit = int.MaxValue;

    // 一条通知的计时：剩余时间、本段开始的时间戳（单调时钟，不受改系统时间影响）、正在等待的取消源（停着时为 null）
    sealed class Countdown(TimeSpan remaining)
    {
        public TimeSpan Remaining = remaining;
        public long StartedAt;
        public CancellationTokenSource? Running;
    }

    /// <summary>默认显示时长，默认 5 秒。</summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>当前的通知，旧的在前。渲染时读取会登记依赖。</summary>
    public IReadOnlyList<Toast> Items => toasts.Value;

    /// <summary>计时是否暂停（悬停、聚焦时）。渲染时读取会登记依赖（剩余时间条跟着停）。</summary>
    public bool Paused => paused.Value;

    /// <summary>最多显示几条（Toaster 的 Max）：只给显示出来的通知计时。</summary>
    public int Limit
    {
        get => limit;
        set
        {
            lock (gate)
            {
                limit = Math.Max(1, value);
                Sync();
            }
        }
    }

    /// <summary>显示一条通知，返回它的编号；<see cref="ToastOptions.Id"/> 已有时换掉那一条（不与它合并）。</summary>
    public string Show(ToastOptions options)
    {
        lock (gate)
        {
            var list = toasts.Peek();
            var existing = options.Id is null ? null : list.Find(t => t.Id == options.Id);
            if (existing is { Closing: false })
            {
                Replace(existing with { Options = options });
                return existing.Id;
            }
            // 同一个编号的那条正在离场：先移走，新的接着出现
            if (existing is not null) RemoveLocked(existing.Id);
            var id = options.Id ?? $"toast-{Interlocked.Increment(ref lastId)}";
            toasts.Value = toasts.Peek().Add(Schedule(new Toast(id, options, false, null)));
            Sync();
            return id;
        }
    }

    /// <summary>成功的通知：<c>Toasts.Success("已保存")</c>。</summary>
    public string Success(string title, ToastOptions? options = null) => Show((options ?? new()) with { Title = title, Tone = UI.Tone.Success });

    /// <summary>出错的通知（读屏立即播报）。</summary>
    public string Error(string title, ToastOptions? options = null) => Show((options ?? new()) with { Title = title, Tone = UI.Tone.Danger, Important = true });

    /// <summary>警告的通知。</summary>
    public string Warning(string title, ToastOptions? options = null) => Show((options ?? new()) with { Title = title, Tone = UI.Tone.Warning });

    /// <summary>提示的通知。</summary>
    public string Info(string title, ToastOptions? options = null) => Show((options ?? new()) with { Title = title, Tone = UI.Tone.Info });

    /// <summary>进行中的通知（不自动消失、不能关闭）：完成后用同一个 Id 调 Success / Error 换掉它，或 Dismiss。</summary>
    public string Loading(string title, ToastOptions? options = null) =>
        Show((options ?? new()) with { Title = title, Tone = UI.Tone.Neutral, Loading = true, Closable = false });

    /// <summary>改一条通知的内容（如加载完成）：<c>Toasts.Update(id, o => o with { Title = "完成", Loading = false })</c>；计时按新的时长重新开始。</summary>
    public void Update(string id, Func<ToastOptions, ToastOptions> change)
    {
        lock (gate)
        {
            if (toasts.Peek().Find(t => t.Id == id) is not { Closing: false } toast) return;
            Replace(toast with { Options = change(toast.Options) });
        }
    }

    /// <summary>关闭一条通知；不给编号时关闭全部。</summary>
    public void Dismiss(string? id = null)
    {
        List<Toast> closed;
        lock (gate)
        {
            closed = [.. toasts.Peek().Where(t => !t.Closing && (id is null || t.Id == id))];
            foreach (var toast in closed) CloseLocked(toast);
        }
        foreach (var toast in closed) Finish(toast, ToastCloseReason.Dismissed);
    }

    /// <summary>进行中 → 成功 / 失败：同一条通知依次更新；返回原任务的结果（失败时照样抛出）。</summary>
    public async Task<T> PromiseAsync<T>(Task<T> task, ToastPromiseMessages<T> messages)
    {
        var id = Show(messages.Loading with { Loading = true, Closable = false });
        try
        {
            var value = await task;
            Update(id, _ => messages.Success(value) is var done && done.Tone is null ? done with { Tone = UI.Tone.Success } : done);
            return value;
        }
        catch (Exception error)
        {
            Update(id, _ => messages.Error(error) is var failed && failed.Tone is null ? failed with { Tone = UI.Tone.Danger } : failed);
            throw;
        }
    }

    /// <summary>暂停所有通知的计时：Toaster 在悬停、聚焦时调用。</summary>
    public void Pause()
    {
        lock (gate)
        {
            if (paused.Peek()) return;
            paused.Value = true;
            Sync();
        }
    }

    /// <summary>继续计时，见 <see cref="Pause"/>。</summary>
    public void Resume()
    {
        lock (gate)
        {
            if (!paused.Peek()) return;
            paused.Value = false;
            Sync();
        }
    }

    /// <summary>从列表里移除（关闭后离场过渡结束时自动调用；直接调用则没有离场动画）。</summary>
    public void Remove(string id)
    {
        lock (gate) RemoveLocked(id);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (gate)
        {
            foreach (var timer in timers.Values) timer.Running?.Cancel();
            timers.Clear();
        }
        disposed.Cancel();
        disposed.Dispose();
    }

    // 换掉一条（内容变了）：重新计时
    void Replace(Toast toast)
    {
        var list = toasts.Peek();
        var index = list.FindIndex(t => t.Id == toast.Id);
        if (index < 0) return;
        toasts.Value = list.SetItem(index, Schedule(toast));
        Sync();
    }

    // 按内容定下这一轮的计时：加载中、不限时的不计时
    Toast Schedule(Toast toast)
    {
        Stop(toast.Id, forget: true);
        var duration = toast.Options.Loading ? Timeout.InfiniteTimeSpan : toast.Options.Duration ?? DefaultDuration;
        if (duration == Timeout.InfiniteTimeSpan || duration <= TimeSpan.Zero) return toast with { Countdown = null };
        timers[toast.Id] = new Countdown(duration);
        return toast with { Countdown = new ToastCountdown(duration, Interlocked.Increment(ref countdowns)) };
    }

    // 计时与显示对齐：显示出来的（最新的 limit 条）在走，其余停着；暂停时都停
    void Sync()
    {
        var shown = toasts.Peek().TakeLast(limit).Select(t => t.Id).ToHashSet();
        foreach (var (id, timer) in timers)
        {
            var run = !paused.Peek() && shown.Contains(id);
            if (run && timer.Running is null) Start(id, timer);
            else if (!run && timer.Running is not null) Stop(id, forget: false);
        }
    }

    void Start(string id, Countdown timer)
    {
        var running = CancellationTokenSource.CreateLinkedTokenSource(disposed.Token);
        timer.Running = running;
        timer.StartedAt = time.GetTimestamp();
        _ = ExpireAsync(id, timer.Remaining, running.Token);
    }

    // 停下计时；forget 时连剩余时间一起丢掉（换了内容或关闭）
    void Stop(string id, bool forget)
    {
        if (!timers.TryGetValue(id, out var timer)) return;
        if (timer.Running is { } running)
        {
            running.Cancel();
            timer.Running = null;
            timer.Remaining -= time.GetElapsedTime(timer.StartedAt);
        }
        if (forget) timers.Remove(id);
    }

    async Task ExpireAsync(string id, TimeSpan remaining, CancellationToken token)
    {
        try
        {
            await Task.Delay(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, time, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        Toast? closed;
        lock (gate)
        {
            closed = toasts.Peek().Find(t => t.Id == id && !t.Closing);
            if (closed is not null) CloseLocked(closed);
        }
        if (closed is not null) Finish(closed, ToastCloseReason.Timeout);
    }

    void CloseLocked(Toast toast)
    {
        Stop(toast.Id, forget: true);
        var list = toasts.Peek();
        var index = list.FindIndex(t => t.Id == toast.Id);
        if (index >= 0) toasts.Value = list.SetItem(index, toast with { Closing = true, Countdown = null });
    }

    // 锁外：通知调用方，等离场过渡结束后移除
    void Finish(Toast toast, ToastCloseReason reason)
    {
        toast.Options.OnClose?.Invoke(reason);
        _ = RemoveLaterAsync(toast.Id);
    }

    async Task RemoveLaterAsync(string id)
    {
        try
        {
            await Task.Delay(ExitDuration, time, disposed.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        lock (gate)
        {
            // 离场期间同一个编号又 Show 了一条新的：不要把新的移走
            if (toasts.Peek().Find(t => t.Id == id) is { Closing: true }) RemoveLocked(id);
        }
    }

    void RemoveLocked(string id)
    {
        Stop(id, forget: true);
        var list = toasts.Peek();
        var index = list.FindIndex(t => t.Id == id);
        if (index >= 0) toasts.Value = list.RemoveAt(index);
        // 腾出一个位置：下一条没轮到的通知出现，开始计时
        Sync();
    }
}
