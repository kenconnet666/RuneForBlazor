using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rune.Generators;

/// <summary>
/// RUNE009：在静态初始化里调用 <c>Attrs.Merge</c>。Merge 把样式规则登记到当前渲染的作用域（每个请求、电路一份），
/// 静态字段只在第一次用到时初始化一次：那次之后的请求拿到的类名没有对应的规则。交互模式下常被别处的登记掩盖，
/// 静态 SSR 的页面上样式就悄悄丢了。静态的层用 <c>new Attrs { Sx = … }</c> 或 <c>Attrs.Layer</c>（样式留在 Sx 上，渲染时再合成）。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticMergeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Diagnostics.StaticMerge];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Merge" }) return;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol { IsStatic: true } method
            || method.ContainingType.ToDisplayString() != "Rune.Attrs") return;
        if (!InStaticInitializer(invocation)) return;
        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StaticMerge, invocation.GetLocation()));
    }

    // 往外找到所在的成员：静态字段、静态属性的初始值、静态构造函数里算；中间隔着 lambda、局部函数（之后才执行）不算
    static bool InStaticInitializer(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax:
                    return false;
                case FieldDeclarationSyntax field:
                    return field.Modifiers.Any(SyntaxKind.StaticKeyword) || field.Modifiers.Any(SyntaxKind.ConstKeyword);
                case PropertyDeclarationSyntax property:
                    // 只有 { get; } = … 的初始值在静态初始化时执行；=> 与 get 的方法体在每次读取时执行
                    return property.Modifiers.Any(SyntaxKind.StaticKeyword) && property.Initializer is not null && property.Initializer.Span.Contains(node.Span);
                case ConstructorDeclarationSyntax constructor:
                    return constructor.Modifiers.Any(SyntaxKind.StaticKeyword);
                case MemberDeclarationSyntax:
                    return false;
            }
        }
        return false;
    }
}
