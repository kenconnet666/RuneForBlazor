using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rune.Generators;
using Rune.UI;

namespace Rune.Tests.Generators;

/// <summary>
/// [FormSchema] 的源生成器：属性按类型与特性生成字段描述（标签、控件、选项、条件），生成不了的报诊断；
/// 生成的代码能编译，组件按 TModel.FormSchemaFields 取到字段。
/// </summary>
public partial class FormSchemaGeneratorTests
{
    static readonly CSharpParseOptions parseOptions = new(LanguageVersion.Preview);

    static (ImmutableArray<Diagnostic> Diagnostics, string Generated) Run(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location.Length > 0)
            .Append(typeof(SchemaField<>).Assembly)
            .Append(typeof(SchemaDateControls).Assembly)
            .Append(typeof(RequiredAttribute).Assembly)
            .Append(typeof(System.Linq.Expressions.Expression).Assembly)
            .Distinct()
            .Select(a => MetadataReference.CreateFromFile(a.Location));
        var compilation = CSharpCompilation.Create("FormSchemaTest", [CSharpSyntaxTree.ParseText(source, parseOptions)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var driver = CSharpGeneratorDriver.Create([new FormSchemaGenerator().AsSourceGenerator()], parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        return (generatorDiagnostics.AddRange(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)), generated);
    }

    [Fact]
    public void 按类型与特性选控件_生成的代码能编译()
    {
        var (diagnostics, generated) = Run("""
            using System.Collections.Generic;
            using System.ComponentModel.DataAnnotations;
            using Rune.UI;
            namespace App;
            public enum Category { [Display(Name = "机械件")] Machined, Plastic }
            [FormSchema] public partial class Line { public string? Part { get; set; } [FormField(Sum = true, Unit = "件")] public int Quantity { get; set; } }
            [FormSchema]
            public partial class Application
            {
                [Display(Name = "公司全称", Prompt = "营业执照上的名称", GroupName = "企业信息", Order = 1), Required, StringLength(60)]
                public string? Company { get; set; }
                [Display(Name = "补充说明"), StringLength(500)] public string? Note { get; set; }
                public Category? Category { get; set; }
                [FormField(Control = SchemaControl.Radio)] public Category Kind { get; set; }
                public decimal? Amount { get; set; }
                public bool Urgent { get; set; }
                public System.DateOnly? Due { get; set; }
                public IReadOnlyList<Category> Tags { get; set; } = [];
                [FormField(VisibleWhen = nameof(Overseas), Control = SchemaControl.Table)] public List<Line> Lines { get; set; } = [];
                public bool Overseas => Kind == App.Category.Plastic;
                [ScaffoldColumn(false)] public string? Secret { get; set; }
            }
            """);

        Assert.Empty(diagnostics);
        Assert.Contains("SchemaControls.Text", generated);
        Assert.Contains("SchemaControl.Textarea", generated);
        Assert.Contains("SchemaControls.NullableEnumSelect<global::App.Category>", generated);
        Assert.Contains("SchemaControls.EnumRadio<global::App.Category>", generated);
        Assert.Contains("SchemaControls.Number<decimal?>", generated);
        Assert.Contains("SchemaDateControls.Date<global::System.DateOnly?>", generated);
        Assert.Contains("SchemaControls.EnumMultiSelect<global::App.Category>", generated);
        Assert.Contains("List<global::App.Line>", generated);
        Assert.Contains("VisibleWhen: static m => m.Overseas", generated);
        Assert.Contains("new(\"Machined\", \"机械件\")", generated);
        Assert.Contains("Placeholder = \"营业执照上的名称\"", generated);
        Assert.DoesNotContain("Secret", generated);
        // 算出来的只读属性不是字段
        Assert.DoesNotContain("\"Overseas\"", generated);
        // [Display(Order)] 写了的排在前面
        Assert.True(generated.IndexOf("\"Company\"", StringComparison.Ordinal) < generated.IndexOf("\"Note\"", StringComparison.Ordinal));
    }

    [Fact]
    public void 不是partial_属性生成不了_条件找不到时报诊断()
    {
        var (diagnostics, _) = Run("""
            using Rune.UI;
            [FormSchema] public class Plain { public string? Name { get; set; } }
            [FormSchema] public partial class Odd
            {
                public System.Uri? Site { get; set; }
                [FormField(RequiredWhen = "Missing")] public string? Name { get; set; }
            }
            """);

        Assert.Contains(diagnostics, d => d.Id == "RUNE011");
        Assert.Contains(diagnostics, d => d.Id == "RUNE012" && d.GetMessage().Contains("Site"));
        Assert.Contains(diagnostics, d => d.Id == "RUNE013" && d.GetMessage().Contains("Missing"));
    }

    public enum Tone { [Display(Name = "暖")] Warm, Cool }

    [FormSchema]
    public partial class Sample
    {
        [Display(Name = "名称", Order = 1), Required] public string? Name { get; set; }

        [FormField(RequiredWhen = nameof(Warm))] public Tone? Tone { get; set; }

        public bool Warm => Tone == Generators.FormSchemaGeneratorTests.Tone.Warm;
    }

    [Fact]
    public void 编译进程序集的模型_按接口取到字段与条件()
    {
        var fields = FieldsOf<Sample>();

        Assert.Equal(["Name", "Tone"], fields.Select(f => f.Info.Key));
        Assert.True(fields[0].Info.Required);
        Assert.Equal("名称", fields[0].Info.Label);
        Assert.Equal([new ListboxOption("Warm", "暖"), new ListboxOption("Cool", "Cool")], fields[1].Info.Options);
        var sample = new Sample { Tone = Tone.Warm, Name = "甲" };
        Assert.True(fields[1].RequiredWhen!(sample));
        Assert.Equal("甲", fields[0].Read(sample));
    }

    static IReadOnlyList<SchemaField<T>> FieldsOf<T>() where T : IFormSchema<T> => T.FormSchemaFields;
}
