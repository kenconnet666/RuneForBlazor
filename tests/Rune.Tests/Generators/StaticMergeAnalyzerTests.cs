using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Rune.Generators;

namespace Rune.Tests.Generators;

public class StaticMergeAnalyzerTests
{
    static async Task<string[]> Ids(string members)
    {
        var source = $$"""
            using System;
            using Rune;
            public static class S
            {
                static readonly Attrs Layer = new();
                {{members}}
            }
            """;
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source)],
            AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.Location.Length > 0).Select(a => MetadataReference.CreateFromFile(a.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = await compilation.WithAnalyzers([new StaticMergeAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        return [.. diagnostics.Select(d => d.Id)];
    }

    [Theory]
    [InlineData("static readonly Attrs Root = Attrs.Merge(Layer);")]
    [InlineData("static Attrs Root { get; } = Attrs.Merge(Layer);")]
    [InlineData("static readonly Attrs Root; static S() { Root = Attrs.Merge(Layer); }")]
    public async Task 静态初始化里调用Merge报RUNE009(string members) =>
        Assert.Equal(["RUNE009"], await Ids(members));

    [Theory]
    [InlineData("static Attrs Root => Attrs.Merge(Layer);")]
    [InlineData("static readonly Func<Attrs> Root = () => Attrs.Merge(Layer);")]
    [InlineData("static readonly Attrs Root = Attrs.Layer(Layer);")]
    [InlineData("static Attrs Render() => Attrs.Merge(Layer);")]
    public async Task 渲染时才执行的_Layer_不报(string members) =>
        Assert.Empty(await Ids(members));
}
