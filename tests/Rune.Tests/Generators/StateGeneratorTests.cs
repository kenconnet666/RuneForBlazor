using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Rune.Generators;

namespace Rune.Tests.Generators;

public class StateGeneratorTests
{
    static readonly CSharpParseOptions parseOptions = new(LanguageVersion.CSharp13);

    /// <summary>编译一段源码并运行生成器，返回生成后的编译结果与全部诊断。</summary>
    static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string Generated) Run(string source, params AdditionalText[] additionalTexts)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location.Length > 0)
            .Append(typeof(State<>).Assembly)
            .Distinct()
            .Select(a => MetadataReference.CreateFromFile(a.Location));
        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create([new StateGenerator().AsSourceGenerator()], additionalTexts, parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        return (output, generatorDiagnostics.AddRange(output.GetDiagnostics()), generated);
    }

    [Fact]
    public void 生成懒创建的状态与读写代理()
    {
        var (_, diagnostics, generated) = Run("""
            namespace App;
            public partial class Counter
            {
                [Rune.State(5)] public partial int Count { get; set; }
            }
            """);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("__rune_Count ??= new(5)", generated);
        Assert.Contains("public global::Rune.State<int> CountState", generated);
    }

    [Fact]
    public void 私有set时交出信号的属性也是私有的()
    {
        var (_, diagnostics, generated) = Run("""
            public partial class Store
            {
                [Rune.State] public partial string Name { get; private set; }
            }
            """);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("private global::Rune.State<string> NameState", generated);
        Assert.Contains("private set => NameState.Value = value;", generated);
    }

    [Fact]
    public void 嵌套类型逐层生成partial声明()
    {
        var (_, diagnostics, _) = Run("""
            public partial class Outer
            {
                public partial record Inner<T>
                {
                    [Rune.State] public partial T? Value { get; set; }
                }
            }
            """);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void 静态的与没有set的报错()
    {
        var (_, diagnostics, _) = Run("""
            public partial class Store
            {
                [Rune.State] public static partial int Shared { get; set; }
                [Rune.State] public partial int ReadOnly { get; }
            }
            """);

        Assert.Contains(diagnostics, d => d.Id == "RUNE001");
        Assert.Contains(diagnostics, d => d.Id == "RUNE002");
    }

    [Fact]
    public void 写在razor的code块里报错并指向razor文件()
    {
        var razor = new InMemoryText("Pages/Counter.razor", """
            <p>@Count</p>
            @code {
                [State] partial int Count { get; set; }
            }
            """);

        var (_, diagnostics, _) = Run("public class Placeholder { }", razor);

        var error = Assert.Single(diagnostics, d => d.Id == "RUNE003");
        Assert.Equal("Pages/Counter.razor", error.Location.GetLineSpan().Path);
        Assert.Equal(2, error.Location.GetLineSpan().StartLinePosition.Line);
    }

    sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
}
