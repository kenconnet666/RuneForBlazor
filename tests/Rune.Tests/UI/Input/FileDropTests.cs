using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>选文件的区域：accept 的匹配、选到的文件按 accept 过滤（拖放不受原生 accept 限制）、单选只留一个、不可用与只读。</summary>
public class FileDropTests : BunitContext
{
    public FileDropTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Theory]
    [InlineData("photo.PNG", "image/png", "image/*", true)]
    [InlineData("report.pdf", "application/pdf", ".pdf,.docx", true)]
    [InlineData("data.json", "application/json", "application/json", true)]
    [InlineData("data.json", "application/json", "image/*,.pdf", false)]
    [InlineData("anything.bin", "", null, true)]
    public void accept按扩展名_类型_通配匹配(string name, string type, string? accept, bool expected) =>
        Assert.Equal(expected, FileAccept.Matches(name, type, accept));

    [Fact]
    public void 选到的文件按accept过滤_不符合的经OnReject交出去()
    {
        IReadOnlyList<IBrowserFile>? chosen = null, rejected = null;
        var cut = Render<FileDrop>(p => p
            .Add(x => x.Accept, "image/*")
            .Add(x => x.Multiple, true)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, v => chosen = v))
            .Add(x => x.OnReject, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, v => rejected = v)));

        var input = cut.Find("input[type=file]");
        Assert.Equal("image/*", input.GetAttribute("accept"));
        Assert.True(input.HasAttribute("multiple"));
        Assert.NotNull(cut.Find("label[data-rune-file-drop]"));

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "a.png", contentType: "image/png"),
            InputFileContent.CreateFromText("b", "b.txt", contentType: "text/plain"),
            InputFileContent.CreateFromText("c", "c.jpg", contentType: "image/jpeg"));

        Assert.Equal(["a.png", "c.jpg"], chosen!.Select(f => f.Name));
        Assert.Equal(["b.txt"], rejected!.Select(f => f.Name));
    }

    [Fact]
    public void 单选只留第一个_只读与不可用时选不了()
    {
        IReadOnlyList<IBrowserFile>? chosen = null;
        var cut = Render<FileDrop>(p => p.Add(x => x.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, v => chosen = v)));
        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "a.txt"),
            InputFileContent.CreateFromText("b", "b.txt"));
        Assert.Equal(["a.txt"], chosen!.Select(f => f.Name));

        var readOnly = Render<FileDrop>(p => p.Add(x => x.ReadOnly, true));
        Assert.True(readOnly.Find("input[type=file]").HasAttribute("disabled"));
        Assert.Equal("true", readOnly.Find("label").GetAttribute("data-readonly"));
    }
}
