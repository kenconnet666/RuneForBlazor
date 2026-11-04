using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Rune.Generators;

namespace Rune.Tests.Generators;

public class StyleValueAnalyzerTests
{
    /// <summary>编译一段写在 Style 初始化器里的属性赋值，运行分析器（RUNE007 默认关闭，这里打开），返回诊断。</summary>
    static async Task<ImmutableArray<Diagnostic>> Analyze(string assignments)
    {
        var source = $$"""
            using Rune.Styles;
            public static class S
            {
                public static Style Root => Css.Define(static () => new Style { {{assignments}} });
            }
            """;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.Location.Length > 0).Distinct();
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source)],
            assemblies.Select(a => MetadataReference.CreateFromFile(a.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
                .WithSpecificDiagnosticOptions([new("RUNE007", ReportDiagnostic.Warn)]));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers([new StyleValueAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    static async Task<string?> Replacement(string assignment)
    {
        var diagnostic = (await Analyze(assignment)).SingleOrDefault(d => d.Id == "RUNE006");
        return diagnostic?.Properties[StyleValueAnalyzer.ReplacementKey];
    }

    [Theory]
    [InlineData("""OutlineStyle = "none" """, "OutlineStyle.None")]
    [InlineData("""Contain = "inline-size" """, "Contain.InlineSize")]
    [InlineData("""Color = "inherit" """, "CssWide.Inherit")]
    [InlineData("""BackgroundColor = "transparent" """, "CssColors.Transparent")]
    [InlineData("""BorderColor = "currentColor" """, "CssColors.CurrentColor")]
    [InlineData("""Width = "100%" """, "CssLength.Percent(100)")]
    [InlineData("""MarginInlineEnd = "-1.5em" """, "CssLength.Em(-1.5)")]
    [InlineData("""MaxWidth = "20rem" """, "CssLength.Rem(20)")]
    [InlineData("""Inset = "auto" """, "CssLength.Auto")]
    [InlineData("""Height = "auto" """, "Height.Auto")]
    public async Task 有现成写法的字符串报RUNE006并给出替换(string assignment, string expected) =>
        Assert.Equal(expected, await Replacement(assignment));

    [Theory]
    [InlineData("""Color = "red" """)]
    [InlineData("""MaxWidth = "calc(100vw - 2em)" """)]
    [InlineData("""Translate = "0 100%" """)]
    [InlineData("""OutlineStyle = OutlineStyle.None""")]
    [InlineData("""Color = CssWide.Inherit""")]
    public async Task 没有现成写法或已经是常量时不报(string assignment) =>
        Assert.Null(await Replacement(assignment));

    [Theory]
    [InlineData("Padding = 8")]
    [InlineData("Width = 1.5")]
    [InlineData("""Translate = "0 8px" """)]
    public async Task 长度写成px报RUNE007(string assignment) =>
        Assert.Contains(await Analyze(assignment), d => d.Id == "RUNE007");

    [Theory]
    [InlineData("Margin = 0")]
    [InlineData("BorderWidth = 1")]
    [InlineData("OutlineOffset = 2")]
    [InlineData("""Width = "2em" """)]
    [InlineData("Opacity = 0.5")]
    public async Task 零_细线_非长度属性不报RUNE007(string assignment) =>
        Assert.DoesNotContain(await Analyze(assignment), d => d.Id == "RUNE007");
}
