using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rune.Generators;

/// <summary>
/// RUNE008：<c>for</c> 循环变量被循环体里的 lambda 捕获。Razor 里组件的子内容、事件处理都是 lambda，
/// 渲染时才执行：那时循环已经走完，拿到的是循环变量最后的值（<c>@for (var i …) { &lt;Tag&gt;@items[i]&lt;/Tag&gt; }</c> 越界）。
/// <c>foreach</c> 的变量每次迭代都是新的，不受影响。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoopCaptureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Diagnostics.LoopVariableCaptured];

    public override void Initialize(AnalysisContext context)
    {
        // .razor 生成的 BuildRenderTree 也要查；诊断位置由 #line 映射回 .razor
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForStatement);
    }

    static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var loop = (ForStatementSyntax)context.Node;
        if (loop.Declaration is null) return;
        var variables = loop.Declaration.Variables
            .Select(v => context.SemanticModel.GetDeclaredSymbol(v, context.CancellationToken))
            .OfType<ILocalSymbol>()
            .ToImmutableHashSet<ISymbol>(SymbolEqualityComparer.Default);
        if (variables.IsEmpty) return;

        foreach (var lambda in loop.Statement.DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>())
        {
            var used = lambda.DescendantNodes().OfType<IdentifierNameSyntax>()
                .FirstOrDefault(id => context.SemanticModel.GetSymbolInfo(id, context.CancellationToken).Symbol is { } symbol && variables.Contains(symbol));
            if (used is null) continue;
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.LoopVariableCaptured, used.GetLocation(), used.Identifier.Text));
        }
    }
}
