using System.Collections.Immutable;

namespace Rune.UI;

/// <summary>一条消息提示。</summary>
/// <param name="Id">编号（列表的 key）。</param>
/// <param name="Message">内容。</param>
/// <param name="Title">标题。</param>
/// <param name="Tone">色调，也决定图标；Danger 用 role="alert" 立即播报。</param>
public sealed record Toast(long Id, string Message, string? Title, Tone Tone);

/// <summary>
/// 消息提示：操作完成后在页面角落短暂出现的反馈（“已保存”）。注入后调用，页面上放一个 <see cref="Toaster"/> 显示：
/// <code>
/// @inject ToastService Toasts
/// Toasts.Show("已保存", Tone.Success);
/// </code>
/// 列表放在信号里，Toaster 读到它就会在变化时重新渲染；后台线程里调用也安全。
/// 每个用户（Server 的每个电路、WebAssembly 的每个页面）一份（scoped）。
/// </summary>
public sealed class ToastService : IDisposable
{
    readonly State<ImmutableList<Toast>> toasts = new([]);
    readonly Lock updateLock = new();
    readonly CancellationTokenSource disposed = new();
    long lastId;

    /// <summary>自动消失前停留的时间，默认 5 秒。</summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>最多同时显示几条，多了先移走最早的，默认 5。</summary>
    public int MaxVisible { get; set; } = 5;

    /// <summary>正在显示的提示，最早的在前。渲染时读取会登记依赖。</summary>
    public IReadOnlyList<Toast> Items => toasts.Value;

    /// <summary>显示一条提示。</summary>
    /// <param name="message">内容。</param>
    /// <param name="tone">色调，默认 Info。</param>
    /// <param name="title">标题。</param>
    /// <param name="duration">停留时间；不写时用 <see cref="DefaultDuration"/>，Danger 默认不自动消失（错误要等用户看到）。</param>
    public Toast Show(string message, Tone tone = Tone.Info, string? title = null, TimeSpan? duration = null)
    {
        var toast = new Toast(Interlocked.Increment(ref lastId), message, title, tone);
        Update(list =>
        {
            list = list.Add(toast);
            return list.Count > MaxVisible ? list.RemoveRange(0, list.Count - MaxVisible) : list;
        });
        if ((duration ?? (tone == Tone.Danger ? null : DefaultDuration)) is { } stay)
            _ = DismissLaterAsync(toast, stay);
        return toast;
    }

    /// <summary>移除一条提示（关闭按钮、超时）。</summary>
    public void Dismiss(Toast toast) => Update(list => list.Remove(toast));

    /// <summary>移除全部提示。</summary>
    public void Clear() => Update(static _ => []);

    /// <inheritdoc/>
    public void Dispose()
    {
        disposed.Cancel();
        disposed.Dispose();
    }

    // 读改写放在锁里：两个线程同时 Show 不会丢一条
    void Update(Func<ImmutableList<Toast>, ImmutableList<Toast>> change)
    {
        lock (updateLock) toasts.Value = change(toasts.Peek());
    }

    async Task DismissLaterAsync(Toast toast, TimeSpan stay)
    {
        try
        {
            await Task.Delay(stay, disposed.Token);
            Dismiss(toast);
        }
        catch (OperationCanceledException)
        {
            // 服务已释放（用户离开）：不用再移除
        }
    }
}
