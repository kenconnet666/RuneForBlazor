using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>上传：超限与类型不符的列出来不上传、单选替换、数量上限、进度与完成、失败重试、移除取消、已有文件、图片墙。</summary>
public class UploadTests : BunitContext
{
    readonly FakeUploads uploads = new();

    public UploadTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(999, "999 B")]
    [InlineData(1_234_567, "1.2 MB")]
    [InlineData(2_000_000_000, "2 GB")]
    public void 文件大小按1000进位(long bytes, string expected) => Assert.Equal(expected, LangFormat.FileSize(bytes, "en-US"));

    [Fact]
    public void 超过大小_类型不符的列出来带错误_不上传也不能重试()
    {
        var cut = Render<Upload>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Accept, "image/*")
            .Add(x => x.MaxSize, 3)
            .Add(x => x.UploadAsync, uploads.Handle));

        Pick(cut,
            InputFileContent.CreateFromText("large", "big.png", contentType: "image/png"),
            InputFileContent.CreateFromText("a", "notes.txt", contentType: "text/plain"));

        var items = cut.FindAll("li");
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal("error", item.GetAttribute("data-state")));
        Assert.Contains("文件不能超过 3 B", items[0].TextContent);
        Assert.Contains("不支持这种文件类型", items[1].TextContent);
        Assert.Empty(uploads.Calls);
        Assert.Empty(cut.FindAll("button[aria-label^='重新上传']"));
    }

    [Fact]
    public void 单选时新文件替换旧的_多选按MaxFiles截断()
    {
        var single = Render<Upload>();
        Pick(single, InputFileContent.CreateFromText("a", "a.txt"));
        Pick(single, InputFileContent.CreateFromText("b", "b.txt"));
        var only = Assert.Single(single.FindAll("li"));
        Assert.Contains("b.txt", only.TextContent);
        Assert.Equal("selected", only.GetAttribute("data-state"));

        var capped = Render<Upload>(p => p.Add(x => x.Multiple, true).Add(x => x.MaxFiles, 2));
        Pick(capped, InputFileContent.CreateFromText("a", "a.txt"));
        Pick(capped, InputFileContent.CreateFromText("b", "b.txt"), InputFileContent.CreateFromText("c", "c.txt"));
        Assert.Equal(2, capped.FindAll("li").Count);
    }

    [Fact]
    public void 给了上传函数时选好就上传_报告进度_完成后记下返回值()
    {
        // 受控（@bind-Value）：上传函数的后续在别的线程上，每次改状态都要交给父级、等父级传回来
        var host = Render<BoundUpload>(p => p.Add(x => x.UploadAsync, uploads.Handle));
        var cut = host.FindComponent<Upload>();

        Pick(cut, InputFileContent.CreateFromText("hello", "a.txt"));
        cut.WaitForAssertion(() => Assert.Equal("uploading", cut.Find("li").GetAttribute("data-state")));
        Assert.Null(cut.FindComponent<Progress>().Instance.Value);

        uploads.Calls[0].Progress.Report(0.42);
        cut.WaitForAssertion(() => Assert.Equal(42m, cut.FindComponent<Progress>().Instance.Value));

        uploads.Calls[0].Result.SetResult("/files/a.txt");
        cut.WaitForAssertion(() => Assert.Equal("done", cut.Find("li").GetAttribute("data-state")));
        Assert.Contains("已上传", cut.Find("li").TextContent);
        Assert.Equal("/files/a.txt", Assert.Single(host.Instance.Files).Response);
    }

    [Fact]
    public void 失败显示错误_重试重新上传()
    {
        var cut = Render<Upload>(p => p.Add(x => x.UploadAsync, uploads.Handle));
        Pick(cut, InputFileContent.CreateFromText("hello", "a.txt"));

        uploads.Calls[0].Result.SetException(new IOException("网络中断"));
        cut.WaitForAssertion(() => Assert.Equal("error", cut.Find("li").GetAttribute("data-state")));
        Assert.Contains("网络中断", cut.Find("li").TextContent);

        // 不可用时“重试”也不可用
        cut.Render(p => p.Add(x => x.Disabled, true));
        Assert.True(cut.Find("button[aria-label='重新上传 a.txt']").HasAttribute("disabled"));
        cut.Render(p => p.Add(x => x.Disabled, false));

        cut.Find("button[aria-label='重新上传 a.txt']").Click();
        cut.WaitForAssertion(() => Assert.Equal("uploading", cut.Find("li").GetAttribute("data-state")));
        Assert.Equal(2, uploads.Calls.Count);
    }

    [Fact]
    public void 移除正在上传的文件会取消上传()
    {
        var cut = Render<Upload>(p => p.Add(x => x.UploadAsync, uploads.Handle));
        Pick(cut, InputFileContent.CreateFromText("hello", "a.txt"));
        cut.WaitForAssertion(() => Assert.Single(uploads.Calls));

        cut.Find("button[aria-label='移除 a.txt']").Click();
        Assert.True(uploads.Calls[0].Cancellation.IsCancellationRequested);
        Assert.Empty(cut.FindAll("li"));
    }

    [Fact]
    public void 已有文件是完成状态_有地址时文件名是链接_只读时不能选也不能移除()
    {
        var cut = Render<Upload>(p => p
            .Add(x => x.Value, [UploadFile.Existing("report.pdf", "/files/1", 482_000)])
            .Add(x => x.ReadOnly, true));

        Assert.Equal("done", cut.Find("li").GetAttribute("data-state"));
        Assert.Equal("/files/1", cut.Find("li a").GetAttribute("href"));
        Assert.Contains("482 kB", cut.Find("li").TextContent);
        Assert.Empty(cut.FindAll("input[type=file]"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void 图片墙_本地地址由JS交来_按墙上的顺序预览_拖动排序写回()
    {
        IReadOnlyList<UploadFile> value = [UploadFile.Existing("a.png", "/a.png"), UploadFile.Existing("b.png", "/b.png")];
        var cut = Render<Upload>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, (IReadOnlyList<UploadFile> v) => value = v)
            .Add(x => x.ListType, UploadListType.Picture)
            .Add(x => x.Multiple, true)
            .Add(x => x.Sortable, true)
            .Add(x => x.Pasteable, true));
        // 父级把新值传回去（同 @bind-Value）
        void Sync() => cut.Render(p => p.Add(x => x.Value, value));

        var wall = cut.Find("[data-rune-upload-previews]");
        Assert.True(wall.HasAttribute("data-rune-paste-files"));
        Assert.Equal("grid", cut.Find("[role=list]").GetAttribute("data-rune-sortable"));
        Assert.Equal(["/a.png", "/b.png"], cut.FindAll("[role=listitem] img").Select(i => i.GetAttribute("src")));
        Assert.Equal("移动 a.png", cut.Find("[data-rune-sortable-handle]").GetAttribute("aria-label"));
        Assert.Contains("添加图片", cut.Find("[data-rune-file-drop]").TextContent);

        // 选的图片：JS 交来本地地址之前是图标与文件名，之后是缩略图，地址写在属性上（JS 据此收回不用的）
        Pick(cut, InputFileContent.CreateFromText("png", "c.png", contentType: "image/png"));
        Sync();
        Assert.Equal("c.png", cut.FindAll("[role=listitem]")[2].TextContent.Trim());
        var file = value[2].File!;
        var key = $"{file.Name}|{file.Size}|{file.LastModified.ToUnixTimeMilliseconds()}";
        cut.Find("[data-rune-upload-previews]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "file-previews", Text = $$"""{"{{key}}":"blob:c"}""" });
        Assert.Equal("blob:c", cut.FindAll("[role=listitem] img")[2].GetAttribute("src"));
        Assert.Equal("blob:c", cut.Find("[data-rune-upload-previews]").GetAttribute("data-rune-upload-previews"));

        // 按墙上的顺序预览：从点的那张开始
        cut.Find("button[aria-label='查看大图：b.png']").Click();
        Assert.Equal("true", cut.Find("dialog").GetAttribute("data-open"));
        Assert.Equal("第 2 张，共 3 张", cut.Find("dialog [aria-live=polite]").TextContent);

        // JS 交来新的顺序
        cut.Find("[role=list]").TriggerEvent("onrunereorder", new ReorderEventArgs { Values = [value[2].Id, value[0].Id, value[1].Id] });
        Sync();
        Assert.Equal(["blob:c", "/a.png", "/b.png"], cut.FindAll("[role=listitem] img").Select(i => i.GetAttribute("src")));
        Assert.Equal(["c.png", "a.png", "b.png"], value.Select(f => f.Name));
    }

    [Fact]
    public void 父级放进来的已选择文件_给了上传函数时开始上传_只上传一次()
    {
        IReadOnlyList<UploadFile> value = [new UploadFile { Id = "draft", Name = "草稿.pdf", Size = 3, File = new DraftFile(), Status = UploadStatus.Selected }];
        var cut = Render<Upload>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<UploadFile>>(this, v => value = v))
            .Add(x => x.UploadAsync, uploads.Handle));

        Assert.Single(uploads.Calls);
        Assert.Equal(UploadStatus.Uploading, Assert.Single(value).Status);
        cut.Render(p => p.Add(x => x.Value, value));
        Assert.Single(uploads.Calls);
    }

    sealed class DraftFile : IBrowserFile
    {
        public string Name => "草稿.pdf";
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => 3;
        public string ContentType => "application/pdf";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream([1, 2, 3]);
    }

    static void Pick(IRenderedComponent<Upload> cut, params InputFileContent[] files) =>
        cut.FindComponent<InputFile>().UploadFiles(files);

    // 真正的父级：回调之后重新渲染、把新值传回去（同 @bind-Value）
    sealed class BoundUpload : ComponentBase
    {
        public IReadOnlyList<UploadFile> Files { get; private set; } = [];

        [Parameter] public UploadHandler? UploadAsync { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Upload>(0);
            builder.AddComponentParameter(1, nameof(Upload.Value), Files);
            builder.AddComponentParameter(2, nameof(Upload.ValueChanged), EventCallback.Factory.Create<IReadOnlyList<UploadFile>>(this, v => Files = v));
            builder.AddComponentParameter(3, nameof(Upload.UploadAsync), UploadAsync);
            builder.CloseComponent();
        }
    }

    sealed class FakeUploads
    {
        public readonly List<(IBrowserFile File, IProgress<double> Progress, CancellationToken Cancellation, TaskCompletionSource<object?> Result)> Calls = [];

        public Task<object?> Handle(IBrowserFile file, IProgress<double> progress, CancellationToken cancellation)
        {
            var result = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Calls.Add((file, progress, cancellation, result));
            return result.Task;
        }
    }
}
