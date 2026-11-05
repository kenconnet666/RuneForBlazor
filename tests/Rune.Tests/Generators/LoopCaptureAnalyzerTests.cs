using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Rune.Generators;

namespace Rune.Tests.Generators;

public class LoopCaptureAnalyzerTests
{
    static async Task<string[]> Ids(string body)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            public static class S
            {
                public static void M(List<Action> actions, int[] items)
                {
                    {{body}}
                }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source)],
            AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.Location.Length > 0).Select(a => MetadataReference.CreateFromFile(a.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = await compilation.WithAnalyzers([new LoopCaptureAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        return [.. diagnostics.Select(d => d.Id)];
    }

    [Fact]
    public async Task lambda捕获for循环变量报RUNE008() =>
        Assert.Equal(["RUNE008"], await Ids("for (var i = 0; i < items.Length; i++) actions.Add(() => Console.WriteLine(items[i]));"));

    [Theory]
    [InlineData("for (var i = 0; i < items.Length; i++) { var item = items[i]; actions.Add(() => Console.WriteLine(item)); }")]
    [InlineData("foreach (var item in items) actions.Add(() => Console.WriteLine(item));")]
    [InlineData("for (var i = 0; i < items.Length; i++) Console.WriteLine(items[i]);")]
    public async Task 复制成局部变量_foreach_不在lambda里时不报(string body) =>
        Assert.Empty(await Ids(body));
}
