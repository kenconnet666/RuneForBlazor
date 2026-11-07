using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Rune.Generators;

namespace Rune.Tests.Generators;

public class ComponentAttrsAnalyzerTests
{
    // 模拟 Razor 生成的渲染代码：打开一个组件或元素，再展开一层属性
    static async Task<string[]> Ids(string open, string value)
    {
        var source = $$"""
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using Rune;
            public class Child : RuneComponent { }
            public class Plain : ComponentBase { }
            public class Host
            {
                static readonly Attrs Layer = new();
                Attrs Rest => new();
                void Render(RenderTreeBuilder __builder, Attrs local)
                {
                    {{open}};
                    __builder.AddAttribute(1, "id", "x");
                    __builder.AddMultipleAttributes(2, {{value}});
                }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source)],
            AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.Location.Length > 0).Select(a => MetadataReference.CreateFromFile(a.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = await compilation.WithAnalyzers([new ComponentAttrsAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        return [.. diagnostics.Select(d => d.Id)];
    }

    [Theory]
    [InlineData("Layer")]
    [InlineData("Rest")]
    [InlineData("Attrs.Merge(Layer, Rest)")]
    [InlineData("Attrs.Layer(Layer)")]
    public async Task 直接展开给Rune组件报RUNE010(string value) =>
        Assert.Equal(["RUNE010"], await Ids("__builder.OpenComponent<Child>(0)", value));

    [Theory]
    [InlineData("__builder.OpenComponent<Child>(0)", "Attrs.MergeProps(Layer, Rest)")]
    [InlineData("__builder.OpenComponent<Child>(0)", "local")]
    [InlineData("__builder.OpenComponent<Plain>(0)", "Attrs.Merge(Layer, Rest)")]
    [InlineData("__builder.OpenElement(0, \"div\")", "Attrs.Merge(Layer, Rest)")]
    public async Task MergeProps_局部变量_框架组件_元素不报(string open, string value) =>
        Assert.Empty(await Ids(open, value));
}
