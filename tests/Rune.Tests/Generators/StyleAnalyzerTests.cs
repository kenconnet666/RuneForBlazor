using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Rune.Generators;

namespace Rune.Tests.Generators;

public class StyleAnalyzerTests
{
    static readonly CSharpParseOptions parseOptions = new(LanguageVersion.CSharp13);

    /// <summary>编译一段源码并运行样式分析器，返回分析器报出的诊断。</summary>
    static async Task<ImmutableArray<Diagnostic>> Analyze(string source, bool referenceRune = true)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location.Length > 0)
            .Where(a => referenceRune || !a.GetName().Name!.StartsWith("Rune", StringComparison.Ordinal))
            .Distinct();
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            assemblies.Select(a => MetadataReference.CreateFromFile(a.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var withAnalyzers = compilation.WithAnalyzers([new StyleAnalyzer()]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    static async Task<string[]> Ids(string source) => (await Analyze(source)).Select(d => d.Id).Order().ToArray();

    [Fact]
    public async Task 静态只读Style字段报RUNE004()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public static class S
            {
                public static readonly Style Root = new() { };
            }
            """);
        Assert.Equal(["RUNE004"], ids);
    }

    [Fact]
    public async Task 带初始化器的静态自动属性报RUNE004()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public static class S
            {
                public static Style Root { get; } = new Style { };
                static Style? Other { get; } = new() { };
            }
            """);
        Assert.Equal(["RUNE004", "RUNE004"], ids);
    }

    [Fact]
    public async Task 用CssDefine定义的静态属性不报()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public static class S
            {
                public static Style Root => Css.Define(static () => new Style
                {
                    [When.Hover] = new() { },
                });
            }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task 非Style类型的静态只读字段不报()
    {
        var ids = await Ids("""
            public static class S
            {
                public static readonly string Name = "x";
                public static int Count { get; } = 1;
            }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task 组件实例方法里显式newStyle报RUNE005()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public class C : Rune.RuneComponent
            {
                void Render() { var s = new Style { }; }
            }
            """);
        Assert.Equal(["RUNE005"], ids);
    }

    [Fact]
    public async Task 组件里目标类型new推断为Style时报RUNE005()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public class C : Rune.RuneComponent
            {
                Style Current { get { Style s = new(); return s; } }
                Style Expr => new() { };
            }
            """);
        Assert.Equal(["RUNE005", "RUNE005"], ids);
    }

    [Fact]
    public async Task BuildRenderTree里newStyle报RUNE005()
    {
        var ids = await Ids("""
            using Rune.Styles;
            using Microsoft.AspNetCore.Components.Rendering;
            public class C : Rune.RuneComponent
            {
                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    var s = new Style { };
                    void Local() { _ = new Style { }; }
                    System.Action a = () => { _ = new Style { }; };
                }
            }
            """);
        Assert.Equal(["RUNE005", "RUNE005", "RUNE005"], ids);
    }

    [Fact]
    public async Task 组件里CssDefine的lambda中不报()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public class C : Rune.RuneComponent
            {
                Style Root => Css.Define(static () => new Style { [When.Hover] = new() { } });
                void Render() { var s = Css.Define(static () => new Style { }); }
            }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task 组件的静态成员与字段初始化器和构造函数里不报RUNE005()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public class C : Rune.RuneComponent
            {
                static Style Make() => new Style { };
                static Style Prop { get { return new Style { }; } }
                Style field = new();
                public C() { _ = new Style { }; }
            }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task 非RuneComponent类里newStyle不报()
    {
        var ids = await Ids("""
            using Rune.Styles;
            public class Plain
            {
                Style Make() => new Style { };
            }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task 没有引用Rune的项目什么都不报()
    {
        var diagnostics = await Analyze("""
            public class Plain
            {
                static readonly object Root = new();
            }
            """, referenceRune: false);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task 诊断位置跟随line映射回razor文件()
    {
        var diagnostics = await Analyze("""
            using Rune.Styles;
            public class C : Rune.RuneComponent
            {
                void Render()
                {
            #line 7 "Pages/Home.razor"
                    var s = new Style { };
            #line default
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        var mapped = diagnostic.Location.GetMappedLineSpan();
        Assert.Equal("Pages/Home.razor", mapped.Path);
        Assert.Equal(6, mapped.StartLinePosition.Line); // #line 7 → 0 起始的 6
    }
}
