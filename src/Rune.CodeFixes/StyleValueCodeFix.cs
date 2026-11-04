using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rune.Generators;

namespace Rune.CodeFixes;

/// <summary>
/// RUNE006 的修复：把字符串值换成分析器给出的写法（<c>"none"</c> → <c>OutlineStyle.None</c>）。
/// 简单名字在当前位置解析成别的东西时（组件里有同名参数 <c>Width</c>）写全名 <c>Rune.Styles.Width.Auto</c>。
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StyleValueCodeFix)), Shared]
public sealed class StyleValueCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["RUNE006"];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;
        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(StyleValueAnalyzer.ReplacementKey, out var replacement) || replacement is null) continue;
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not ExpressionSyntax value) continue;
            context.RegisterCodeFix(
                CodeAction.Create($"改成 {replacement}", token => ReplaceAsync(context.Document, value, replacement, token), equivalenceKey: "RUNE006"),
                diagnostic);
        }
    }

    static async Task<Document> ReplaceAsync(Document document, ExpressionSyntax value, string replacement, CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
        if (root is null || model is null) return document;
        var expression = SyntaxFactory.ParseExpression(replacement);
        // 方法调用（CssLength.Em(1)）看被调用的成员；其余看整个表达式
        var probe = expression is InvocationExpressionSyntax call ? call.Expression : expression;
        var info = model.GetSpeculativeSymbolInfo(value.SpanStart, probe, SpeculativeBindingOption.BindAsExpression);
        var bound = info.Symbol ?? (info.CandidateSymbols.IsDefaultOrEmpty ? null : info.CandidateSymbols[0]);
        if (bound?.ContainingType?.ContainingNamespace?.ToDisplayString() != "Rune.Styles")
            expression = SyntaxFactory.ParseExpression("Rune.Styles." + replacement);
        return document.WithSyntaxRoot(root.ReplaceNode(value, expression.WithTriviaFrom(value)));
    }
}
