using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rune.Generators;

/// <summary>
/// 为 <c>[FormSchema] partial class</c> 实现 <c>Rune.UI.IFormSchema&lt;T&gt;</c>：每个公开的可读写属性一个字段描述
/// （标签、控件、选项、条件、读写委托与绑定表达式），SchemaForm 等组件按它渲染，不用反射。
/// 读的是 DataAnnotations 的 [Display]、[Required]、[StringLength] / [MaxLength]、[DataType]、[ScaffoldColumn] 与 Rune 的 [FormField]。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class FormSchemaGenerator : IIncrementalGenerator
{
    const string FormSchemaAttribute = "Rune.UI.FormSchemaAttribute";

    /// <summary>注册生成步骤。</summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var schemas = context.SyntaxProvider.ForAttributeWithMetadataName(
            FormSchemaAttribute,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, cancellation) => FormSchemaModel.From(ctx, cancellation));

        context.RegisterSourceOutput(schemas, static (output, result) =>
        {
            foreach (var diagnostic in result.Diagnostics) output.ReportDiagnostic(diagnostic.ToDiagnostic());
            if (result.Source is { } source) output.AddSource(result.HintName, source);
        });
    }
}
