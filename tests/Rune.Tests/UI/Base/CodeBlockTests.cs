using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class CodeBlockTests : BunitContext
{
    public CodeBlockTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 整段文字交给JS上色_复制的是代码本身()
    {
        var cut = Render<CodeBlock>(p => p.Add(x => x.Code, "var a = 1;\n").Add(x => x.Lang, "csharp"));
        var code = cut.Find("pre > code");
        Assert.Equal("csharp", code.GetAttribute("data-rune-highlight"));
        Assert.Equal("var a = 1;\n", code.TextContent);
        Assert.Equal("var a = 1;\n", cut.Find("button[data-rune-copy]").GetAttribute("data-rune-copy"));
    }

    [Fact]
    public void 不高亮时没有标记_没有语言时也没有()
    {
        Assert.False(Render<CodeBlock>(p => p.Add(x => x.Code, "x").Add(x => x.Lang, "csharp").Add(x => x.Highlight, false)).Find("code").HasAttribute("data-rune-highlight"));
        Assert.False(Render<CodeBlock>(p => p.Add(x => x.Code, "x")).Find("code").HasAttribute("data-rune-highlight"));
    }

    [Fact]
    public void 按行渲染_行号对读屏隐藏_增删与强调各有标记()
    {
        var cut = Render<CodeBlock>(p => p
            .Add(x => x.Code, "a\nb\nc\nd\n")
            .Add(x => x.LineNumbers, true)
            .Add(x => x.AddedLines, "2")
            .Add(x => x.RemovedLines, "3")
            .Add(x => x.HighlightLines, "1,4"));

        var lines = cut.FindAll("code > span");
        Assert.Equal(4, lines.Count);
        Assert.Equal(["a", "b", "c", "d"], cut.FindAll("[data-rune-code-text]").Select(t => t.TextContent));
        Assert.Equal("true", lines[0].QuerySelector("span")!.GetAttribute("aria-hidden"));
        Assert.Equal(["added", "removed"], lines.Select(l => l.GetAttribute("data-diff")).OfType<string>());
    }

    [Fact]
    public void 超过MaxLines折叠_按钮展开收起()
    {
        var cut = Render<CodeBlock>(p => p.Add(x => x.Code, string.Join('\n', Enumerable.Range(1, 12))).Add(x => x.MaxLines, 5));
        Assert.Contains("--rune-code-block-max-lines:5", cut.Find("pre").GetAttribute("style"));
        var toggle = cut.Find("button[aria-expanded]");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        toggle.Click();
        Assert.Equal("true", cut.Find("button[aria-expanded]").GetAttribute("aria-expanded"));
        Assert.Null(cut.Find("pre").GetAttribute("style"));
    }

    [Theory]
    [InlineData("1,3-5", new[] { 1, 3, 4, 5 })]
    [InlineData(" 2 , x, 7-6 ", new[] { 2 })]
    [InlineData(null, new int[0])]
    public void 行号写法(string? spec, int[] lines) => Assert.Equal(lines, LineSpec.Parse(spec).Order());

    [Fact]
    public void 文案里反引号中的是行内代码()
    {
        var cut = Render<CodeText>(p => p.Add(x => x.Text, "运行 `dotnet test` 后提交"));
        Assert.Equal("dotnet test", cut.Find("code").TextContent);
        Assert.Contains("后提交", cut.Markup);
    }

    [Fact]
    public void RuneProvider的高亮器_渲染时就上色_按行时按br拆开()
    {
        static string Levels(string code, string lang) =>
            string.Join("<br>", code.Split('\n').Select(line => line.Replace("ERROR", "<span class=\"level\">ERROR</span>")));
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Highlighter, (CodeHighlighter)Levels)
            .AddChildContent<CodeBlock>(c => c.Add(x => x.Code, "ok\nERROR boom\n").Add(x => x.Lang, "log").Add(x => x.LineNumbers, true)));
        var code = cut.Find("pre > code");
        Assert.False(code.HasAttribute("data-rune-highlight"));
        var lines = cut.FindAll("[data-rune-code-text]");
        Assert.Equal(["ok", "ERROR boom"], lines.Select(line => line.TextContent));
        Assert.Equal("ERROR", lines[1].QuerySelector("span.level")?.TextContent);
    }
}
