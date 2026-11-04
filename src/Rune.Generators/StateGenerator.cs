using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Rune.Generators;

/// <summary>
/// 为 <c>[State] partial T Name { get; set; }</c> 生成实现：懒创建的 <c>State&lt;T&gt;</c> 后备字段、
/// 交出信号的 <c>NameState</c> 属性、读写代理。
/// </summary>
/// <remarks>
/// 后备字段懒创建是为了热重载：新增的实例字段在已有的对象上是 null（字段初始化器不会再跑），
/// 第一次读取时才创建就不会空引用。
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class StateGenerator : IIncrementalGenerator
{
    const string StateAttribute = "Rune.StateAttribute";

    /// <summary>注册生成步骤。</summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var properties = context.SyntaxProvider.ForAttributeWithMetadataName(
            StateAttribute,
            predicate: static (node, _) => IsPartialPropertyDeclaration(node),
            transform: static (ctx, _) => StateProperty.From(ctx));

        context.RegisterSourceOutput(properties, static (output, result) =>
        {
            if (result.Error is { } error) output.ReportDiagnostic(error.ToDiagnostic());
            else if (result.Property is { } property) output.AddSource(property.HintName, property.ToSource());
        });

        // 写在 .razor 的 @code 里的 [State]：生成器看不到 Razor 生成的代码，编译器只会报一个看不懂的 CS9248。
        // Razor SDK 会把 .razor 文件作为附加文件交给生成器，这里直接扫源文件，给出原因与改法
        var razorFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
        context.RegisterSourceOutput(razorFiles, static (output, file) => ReportStateInRazorCode(output, file));
    }

    /// <summary>只有声明（没有访问器实现）的 partial 属性才由我们生成实现。</summary>
    static bool IsPartialPropertyDeclaration(SyntaxNode node) =>
        node is PropertyDeclarationSyntax { AccessorList: { } accessors } property
        && property.Modifiers.Any(SyntaxKind.PartialKeyword)
        && accessors.Accessors.All(a => a.Body is null && a.ExpressionBody is null);

    /// <summary>@code 块里形如 <c>[State…] … partial … Name {</c> 的声明。</summary>
    static readonly Regex stateInCode = new(
        @"\[State\b[^\]]*\][^;{]*?\bpartial\s+[^;{]+?\s(?<name>\w+)\s*\{",
        RegexOptions.Compiled);

    static void ReportStateInRazorCode(SourceProductionContext output, AdditionalText file)
    {
        var text = file.GetText(output.CancellationToken);
        if (text is null) return;
        var source = text.ToString();
        var codeStart = source.IndexOf("@code", StringComparison.Ordinal);
        if (codeStart < 0) return;

        foreach (Match match in stateInCode.Matches(source, codeStart))
        {
            var span = new TextSpan(match.Index, match.Length);
            var location = Location.Create(file.Path, span, text.Lines.GetLinePositionSpan(span));
            output.ReportDiagnostic(Diagnostic.Create(Diagnostics.StateInRazorCode, location, match.Groups["name"].Value));
        }
    }
}
