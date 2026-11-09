using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rune.Styles;

namespace Rune.UI;

/// <summary>上传：选文件、列出文件、上传进度、失败重试与移除。</summary>
public partial class Upload
{
    // 正在上传的文件各一个：移除、重新上传、组件销毁时取消。上传函数的后续可能在别的线程上跑完，增删要加锁
    readonly Dictionary<string, CancellationTokenSource> running = [];
    readonly Lock gate = new();
    // 图片墙里选的图片的本地地址（JS 建的 object URL）：文件的键（名称|大小|修改时间）→ 地址
    readonly Dictionary<string, string> localPreviews = [];
    readonly string instructionsId = UniqueId.Next("upload-sortable");
    Memo<int, Attrs> thumbnailVars;
    bool previewing;
    int previewIndex;

    static readonly CssLength Fill = CssLength.Percent(100);

    /// <summary>怎样上传一个文件；不写时只选文件、不上传（停在 Selected）。</summary>
    [Parameter] public UploadHandler? UploadAsync { get; set; }

    /// <summary>
    /// 选到的（类型对的）文件加进列表前依次过一遍（同 vue-tsx-pro 的 transform）：裁剪（ImageCropper）、压缩，返回 null 不加。
    /// 拖放、对话框、粘贴都经过它。
    /// </summary>
    [Parameter] public Func<IBrowserFile, Task<IBrowserFile?>>? Transform { get; set; }

    /// <summary>接受的文件类型（同原生 accept）；不符合的列出来、带错误、不上传。</summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>可以选多个；不写时新文件替换旧的。</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>选文件夹（同 FileDrop 的 Directory）：里面的文件都加进来，列表里显示相对路径；隐含多选。</summary>
    [Parameter] public bool Directory { get; set; }

    FileDrop? drop;

    // 选文件夹隐含多选
    bool Many => Multiple || Directory;

    /// <summary>单个文件的大小上限（字节）；超过的列出来、带错误、不上传。</summary>
    [Parameter] public long? MaxSize { get; set; }

    /// <summary>最多几个文件，超出的不加进来。</summary>
    [Parameter] public int? MaxFiles { get; set; }

    /// <summary>选择区域里提示文字下面的说明（大小、类型限制）；图片墙里显示在墙下面。</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>列表的样子：一行一个文件（默认），或图片墙。</summary>
    [Parameter] public UploadListType ListType { get; set; }

    /// <summary>图片墙里缩略图的边长（px），默认 104。</summary>
    [Parameter] public int ThumbnailSize { get; set; } = DefaultThumbnailSize;

    /// <summary>拖动把手（或聚焦后用键盘）调整文件的顺序（图片墙）。</summary>
    [Parameter] public bool Sortable { get; set; }

    /// <summary>焦点在组件里时 Ctrl + V 把剪贴板里的图片、文件加进来。</summary>
    [Parameter] public bool Pasteable { get; set; }

    /// <summary>选择区域（FileDrop）的部件属性。</summary>
    [Parameter] public Attrs? PropsDrop { get; set; }

    /// <summary>列表的部件属性：一行一个文件时是 ul（交给 GridList），图片墙里是缩略图的列表。</summary>
    [Parameter] public Attrs? PropsList { get; set; }

    /// <summary>每一项（li，图片墙里是一格）的部件属性。</summary>
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

    /// <summary>拖动排序的把手的部件属性（交给 ButtonBase）。</summary>
    [Parameter] public Attrs? PropsHandle { get; set; }

    /// <summary>有地址时文件名的链接的部件属性（交给 TextLink）。</summary>
    [Parameter] public Attrs? PropsLink { get; set; }

    const int DefaultThumbnailSize = 104;

    IReadOnlyList<UploadFile> Files => CurrentValue ?? [];

    bool PasteEnabled => Pasteable && !IsReadOnly && !IsDisabled;

    // 到了 MaxFiles：图片墙收起“添加图片”
    bool IsFull => Many && MaxFiles is { } max && Files.Count >= max;

    Attrs? ThumbnailVars => ThumbnailSize == DefaultThumbnailSize ? null
        : thumbnailVars.Get(ThumbnailSize, static size => new Attrs { Vars = [UploadStyles.ThumbnailVar.Set(size)] });

    // 说明，粘贴的提示接在后面：列表模式在选择区域里，图片墙在墙下面
    RenderFragment? Hint => !PasteEnabled ? Description : builder =>
    {
        if (Description is not null)
        {
            builder.AddContent(0, Description);
            builder.AddContent(1, " · ");
        }
        builder.AddContent(2, Config.Lang.UploadPasteHint);
    };

    /// <summary>缩略图与大图的地址：已有文件的地址，或选的图片的本地地址；都没有时为 null。</summary>
    string? PreviewOf(UploadFile item) =>
        item.Url ?? (item.File is { } file && localPreviews.TryGetValue(KeyOf(file), out var url) ? url : null);

    // 还在用的本地地址：写在属性上，JS 收回别的
    string PreviewsInUse => string.Join(' ', Files
        .Where(item => !item.Rejected && item.File is not null)
        .Select(item => localPreviews.GetValueOrDefault(KeyOf(item.File!)))
        .OfType<string>()
        // data URL（Transform 的缩略图）不用 JS 收回，也别把整张图写进属性
        .Where(url => !url.StartsWith("data:", StringComparison.Ordinal))
        .Distinct());

    // 与 JS 的 files.js 一致：名称|大小|修改时间（毫秒）
    static string KeyOf(IBrowserFile file) => FileKey.Of(file);

    // 能预览的（按墙上的顺序）
    List<UploadFile> Previewable => [.. Files.Where(item => !item.Rejected && PreviewOf(item) is not null)];

    IReadOnlyList<LightboxItem> PreviewItems => [.. Previewable.Select(item => new LightboxItem(PreviewOf(item)!, item.Name)
    {
        Caption = builder => builder.AddContent(0, item.Name),
        DownloadName = item.Name,
    })];

    void OpenPreview(UploadFile item)
    {
        previewIndex = Previewable.FindIndex(other => other.Id == item.Id);
        previewing = previewIndex >= 0;
    }

    // JS 交来选到的图片的本地地址
    void OnCommand(CommandEventArgs e)
    {
        if (e.Name != "file-previews" || e.Text is not { } json) return;
        var urls = JsonSerializer.Deserialize(json, UIJson.Relaxed.DictionaryStringString);
        foreach (var (key, url) in urls ?? []) localPreviews[key] = url;
    }

    // JS 交来新的顺序（各项的 id）：按它重排；对不上（列表在这期间变了）时不动
    Task ReorderAsync(ReorderEventArgs e)
    {
        var byId = Files.ToDictionary(item => item.Id);
        if (e.Values.Length != byId.Count || !e.Values.All(byId.ContainsKey)) return Task.CompletedTask;
        return SetValueAsync([.. e.Values.Select(id => byId[id])]);
    }

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
        if (Transform is { } transform && !rejectedType)
        {
            var originals = files;
            files = await TransformAsync(files, transform);
            await PreviewTransformedAsync(files.Except(originals));
        }
        if (files.Count == 0) return;
        var entries = files.Select(file =>
        {
            // 选文件夹时显示相对路径（报告/附图/图 1.png），看得出文件在哪一层
            var entry = new UploadFile { Id = UniqueId.Next("upload"), Name = drop?.PathOf(file) ?? file.Name, Size = file.Size, File = file, Status = UploadStatus.Selected };
            if (rejectedType) return entry with { Status = UploadStatus.Error, Error = Config.Lang.FileTypeRejected, Rejected = true };
            if (MaxSize is { } max && file.Size > max)
                return entry with { Status = UploadStatus.Error, Error = Config.Lang.FileTooLarge(LangFormat.FileSize(max, Config.Lang.Code)), Rejected = true };
            return entry;
        }).ToList();
        if (!Many) CancelAll();
        var kept = Many ? Files : [];
        var room = MaxFiles is { } limit ? Math.Max(0, limit - kept.Count) : entries.Count;
        var added = (Many ? entries : entries.TakeLast(1)).Take(room).ToList();
        await SetValueAsync([.. kept, .. added]);
        StartPending();
    }

    // Transform 换出来的图片（裁好的头像）不是从这里的文件输入框选的，没有本地地址：小图读成 data URL 做缩略图
    async Task PreviewTransformedAsync(IEnumerable<IBrowserFile> files)
    {
        const long MaxPreview = 2 * 1024 * 1024;
        foreach (var file in files)
        {
            if (!file.ContentType.StartsWith("image/", StringComparison.Ordinal) || file.Size > MaxPreview) continue;
            using var buffer = new MemoryStream();
            await using (var stream = file.OpenReadStream(MaxPreview)) await stream.CopyToAsync(buffer);
            localPreviews[KeyOf(file)] = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
        }
    }

    // 选到的文件依次过一遍 Transform（裁剪、压缩）：返回 null 的不加
    static async Task<IReadOnlyList<IBrowserFile>> TransformAsync(IReadOnlyList<IBrowserFile> files, Func<IBrowserFile, Task<IBrowserFile?>> transform)
    {
        var result = new List<IBrowserFile>(files.Count);
        foreach (var file in files)
            if (await transform(file) is { } transformed) result.Add(transformed);
        return result;
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // 父级放进来的待上传文件（别处拖进来的、恢复的草稿）：给了 UploadAsync 时同样开始上传
        StartPending();
    }

    // 已选择、带文件、还没在上传的项开始上传（同一项只开始一次）
    void StartPending()
    {
        if (UploadAsync is null) return;
        foreach (var item in Files)
        {
            if (item.Status != UploadStatus.Selected || item.File is null) continue;
            lock (gate)
            {
                if (running.ContainsKey(item.Id)) continue;
            }
            _ = StartAsync(item);
        }
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
        var kept = Files.Where(f => f.Id != id).ToList();
        // 没有别的项用着的本地地址不再记着（JS 按属性收回它）
        if (Files.FirstOrDefault(f => f.Id == id)?.File is { } file && !kept.Any(f => f.File is { } other && KeyOf(other) == KeyOf(file)))
            localPreviews.Remove(KeyOf(file));
        return SetValueAsync(kept);
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
