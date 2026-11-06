using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>上传：选文件、列出文件、上传进度、失败重试与移除。</summary>
public partial class Upload
{
    // 正在上传的文件各一个：移除、重新上传、组件销毁时取消。上传函数的后续可能在别的线程上跑完，增删要加锁
    readonly Dictionary<string, CancellationTokenSource> running = [];
    readonly Lock gate = new();

    /// <summary>怎样上传一个文件；不写时只选文件、不上传（停在 Selected）。</summary>
    [Parameter] public UploadHandler? UploadAsync { get; set; }

    /// <summary>接受的文件类型（同原生 accept）；不符合的列出来、带错误、不上传。</summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>可以选多个；不写时新文件替换旧的。</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>单个文件的大小上限（字节）；超过的列出来、带错误、不上传。</summary>
    [Parameter] public long? MaxSize { get; set; }

    /// <summary>最多几个文件，超出的不加进来。</summary>
    [Parameter] public int? MaxFiles { get; set; }

    /// <summary>选择区域里提示文字下面的说明（大小、类型限制）。</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>选择区域（FileDrop）的部件属性。</summary>
    [Parameter] public Attrs? PropsDrop { get; set; }

    /// <summary>列表（ul，交给 GridList）的部件属性。</summary>
    [Parameter] public Attrs? PropsList { get; set; }

    /// <summary>每一项（li）的部件属性。</summary>
    [Parameter] public Attrs? PropsItem { get; set; }

    /// <summary>每一项的文件图标（按状态是文件、对勾或警告）的部件属性。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>每一项里文件名与下一行的外层的部件属性。</summary>
    [Parameter] public Attrs? PropsBody { get; set; }

    /// <summary>每一项里文件名的部件属性。</summary>
    [Parameter] public Attrs? PropsName { get; set; }

    /// <summary>每一项里大小或错误信息的部件属性。</summary>
    [Parameter] public Attrs? PropsMeta { get; set; }

    /// <summary>每一项上传中的进度条的部件属性（交给 Progress；进度与名称由组件管理）。</summary>
    [Parameter] public Attrs? PropsProgress { get; set; }

    /// <summary>失败后的重试按钮的部件属性（交给 IconButton；名称按语言包的 UploadRetry 生成）。</summary>
    [Parameter] public Attrs? PropsRetry { get; set; }

    /// <summary>移除按钮的部件属性（交给 IconButton；名称按语言包的 UploadRemove 生成）。</summary>
    [Parameter] public Attrs? PropsRemove { get; set; }

    IReadOnlyList<UploadFile> Files => CurrentValue ?? [];

    static string StateOf(UploadStatus status) => status switch
    {
        UploadStatus.Uploading => "uploading",
        UploadStatus.Done => "done",
        UploadStatus.Error => "error",
        _ => "selected",
    };

    static decimal? PercentOf(UploadFile item) => item.Progress is { } fraction ? Math.Round((decimal)fraction * 100) : null;

    string? SizeOf(UploadFile item) => item.Size is { } bytes ? LangFormat.FileSize(bytes, Config.Lang.Code) : null;

    Task AddPicked(IReadOnlyList<IBrowserFile> files) => AddAsync(files, rejectedType: false);

    Task AddRejected(IReadOnlyList<IBrowserFile> files) => AddAsync(files, rejectedType: true);

    async Task AddAsync(IReadOnlyList<IBrowserFile> files, bool rejectedType)
    {
        var entries = files.Select(file =>
        {
            var entry = new UploadFile { Id = UniqueId.Next("upload"), Name = file.Name, Size = file.Size, File = file, Status = UploadStatus.Selected };
            if (rejectedType) return entry with { Status = UploadStatus.Error, Error = Config.Lang.FileTypeRejected, Rejected = true };
            if (MaxSize is { } max && file.Size > max)
                return entry with { Status = UploadStatus.Error, Error = Config.Lang.FileTooLarge(LangFormat.FileSize(max, Config.Lang.Code)), Rejected = true };
            return entry;
        }).ToList();
        if (!Multiple) CancelAll();
        var kept = Multiple ? Files : [];
        var room = MaxFiles is { } limit ? Math.Max(0, limit - kept.Count) : entries.Count;
        var added = (Multiple ? entries : entries.TakeLast(1)).Take(room).ToList();
        await SetValueAsync([.. kept, .. added]);
        foreach (var item in added.Where(item => item.Status == UploadStatus.Selected)) _ = StartAsync(item);
    }

    async Task StartAsync(UploadFile item)
    {
        if (UploadAsync is not { } upload || item.File is not { } file) return;
        var cancellation = new CancellationTokenSource();
        lock (gate)
        {
            if (running.Remove(item.Id, out var previous)) previous.Cancel();
            running[item.Id] = cancellation;
        }
        await PatchAsync(item.Id, f => f with { Status = UploadStatus.Uploading, Progress = null, Error = null });
        try
        {
            var response = await upload(file, new ProgressReporter(this, item.Id, cancellation.Token), cancellation.Token);
            if (!cancellation.IsCancellationRequested)
                await PatchAsync(item.Id, f => f with { Status = UploadStatus.Done, Progress = 1, Response = response });
        }
        catch (Exception) when (cancellation.IsCancellationRequested)
        {
            // 移除、重新上传或组件销毁：不是失败
        }
        catch (Exception error)
        {
            // 上传失败只显示在这一项上（可以重试），不往上抛
            await PatchAsync(item.Id, f => f with { Status = UploadStatus.Error, Error = string.IsNullOrEmpty(error.Message) ? Config.Lang.UploadFailed : error.Message });
        }
        finally
        {
            lock (gate)
            {
                if (running.TryGetValue(item.Id, out var current) && current == cancellation) running.Remove(item.Id);
            }
        }
    }

    Task RemoveAsync(string id)
    {
        lock (gate)
        {
            if (running.Remove(id, out var cancellation)) cancellation.Cancel();
        }
        return SetValueAsync([.. Files.Where(f => f.Id != id)]);
    }

    // 按 id 改一项：总是改当前的列表（上传函数的后续可能在别的线程上，切回渲染器的线程）
    Task PatchAsync(string id, Func<UploadFile, UploadFile> change) =>
        InvokeAsync(() => SetValueAsync([.. Files.Select(f => f.Id == id ? change(f) : f)]));

    void CancelAll()
    {
        lock (gate)
        {
            foreach (var cancellation in running.Values) cancellation.Cancel();
            running.Clear();
        }
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        base.OnDispose();
        CancelAll();
    }

    // 进度：按整数百分比变化才更新（Server 模式下每次更新都是一次推送）
    sealed class ProgressReporter(Upload owner, string id, CancellationToken cancellation) : IProgress<double>
    {
        int lastPercent = -1;

        public void Report(double value)
        {
            var percent = (int)Math.Round(Math.Clamp(value, 0, 1) * 100);
            if (cancellation.IsCancellationRequested || Interlocked.Exchange(ref lastPercent, percent) == percent) return;
            _ = owner.PatchAsync(id, f => f.Status == UploadStatus.Uploading ? f with { Progress = percent / 100d } : f);
        }
    }
}
