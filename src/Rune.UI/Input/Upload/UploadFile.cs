using Microsoft.AspNetCore.Components.Forms;

namespace Rune.UI;

/// <summary>上传列表里一个文件的状态。</summary>
public enum UploadStatus
{
    /// <summary>选了、还没上传（没写上传函数时一直是它，等提交时一起上传）。</summary>
    Selected,

    /// <summary>上传中。</summary>
    Uploading,

    /// <summary>完成（已有的文件也是）。</summary>
    Done,

    /// <summary>失败，或没通过大小、类型检查。</summary>
    Error,
}

/// <summary>上传列表里的一个文件（同 vue-tsx-pro 的 UploadFile）。列表是不可变的：组件每次改状态都交出新的列表。</summary>
public sealed record UploadFile
{
    /// <summary>组件给的 id，同一个列表里唯一。</summary>
    public required string Id { get; init; }

    /// <summary>文件名。</summary>
    public required string Name { get; init; }

    /// <summary>大小（字节）；已有文件不知道时为 null。</summary>
    public long? Size { get; init; }

    /// <summary>选的文件；已有文件没有。</summary>
    public IBrowserFile? File { get; init; }

    /// <summary>已有文件的地址：文件名链到它。</summary>
    public string? Url { get; init; }

    /// <summary>状态。</summary>
    public UploadStatus Status { get; init; }

    /// <summary>上传进度 0–1；上传函数没报告进度时为 null（不确定进度）。</summary>
    public double? Progress { get; init; }

    /// <summary>错误信息。</summary>
    public string? Error { get; init; }

    /// <summary>没通过大小或类型检查：不会上传，也不能重试。</summary>
    public bool Rejected { get; init; }

    /// <summary>上传函数的返回值（如文件在服务端的地址）。</summary>
    public object? Response { get; init; }

    /// <summary>之前上传过、从后端读回来的文件：状态是完成，没有 File；给了地址时文件名链到它，可以移除。</summary>
    public static UploadFile Existing(string name, string? url = null, long? size = null) =>
        new() { Id = UniqueId.Next("upload"), Name = name, Url = url, Size = size, Status = UploadStatus.Done };
}

/// <summary>
/// 上传一个文件：读 <c>file.OpenReadStream(上限)</c> 发给后端，用 <paramref name="progress"/> 报告进度（0–1），
/// <paramref name="cancellation"/> 在移除、重新上传、组件销毁时取消；返回值记在 <see cref="UploadFile.Response"/> 上，抛异常算失败（消息显示在这一项上）。
/// </summary>
public delegate Task<object?> UploadHandler(IBrowserFile file, IProgress<double> progress, CancellationToken cancellation);
