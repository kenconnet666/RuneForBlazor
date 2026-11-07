using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rune.Generators;

/// <summary>
/// RUNE010：把一层 <c>Attrs</c> 直接展开给组件（<c>&lt;Avatar @attributes="TimelineStyles.Avatar" /&gt;</c>）。展开时 Sx 先合成普通的 class，
/// 落在子组件自己的样式层里，谁生效取决于规则插入的先后（Timeline 的头像就这样丢了尺寸）。给组件的层要经 <c>Attrs.MergeProps</c>：
/// 样式作为子组件的 Sx 参数交下去，排在它自己的样式之后。原生元素用 <c>Attrs.Merge</c>，不受影响。
/// 只看字段、属性与方法调用：模板的 Context 参数、局部变量（多半已经 MergeProps 过）不报。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentAttrsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Diagnostics.ComponentAttrs];

    public override void Initialize(AnalysisContext context)
    {
        // 只在 Razor 生成的代码里出现
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "AddMultipleAttributes" } || invocation.ArgumentList.Arguments.Count != 2) return;
        var model = context.SemanticModel;
        if (OpenedComponent(invocation) is not { } component || !IsRuneComponent(model.GetTypeInfo(component, context.CancellationToken).Type)) return;

        var value = Unwrap(invocation.ArgumentList.Arguments[1].Expression);
        if (model.GetTypeInfo(value, context.CancellationToken).Type?.ToDisplayString() != "Rune.Attrs") return;
        var symbol = model.GetSymbolInfo(value, context.CancellationToken).Symbol;
        var flagged = symbol switch
        {
            IFieldSymbol or IPropertySymbol => true,
            IMethodSymbol method => !(method.Name == "MergeProps" && method.ContainingType.ToDisplayString() == "Rune.Attrs"),
            _ => false,
        };
        if (flagged) context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ComponentAttrs, value.GetLocation(), value.ToString()));
    }

    // Razor 生成的是 RuntimeHelpers.TypeCheck<…>(值)：取里面的值
    static ExpressionSyntax Unwrap(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.Text: "TypeCheck" } }, ArgumentList.Arguments.Count: 1 } check
            ? Unwrap(check.ArgumentList.Arguments[0].Expression)
            : expression is ParenthesizedExpressionSyntax parenthesized ? Unwrap(parenthesized.Expression) : expression;

    // 有 Sx 参数的才是：框架的组件（EditForm、InputFile）没有，展开成 class 落到它的元素上正合适
    static bool IsRuneComponent(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
            if (current.ToDisplayString() == "Rune.RuneComponent") return true;
        return false;
    }

    // 同一块里往前找最近打开的是组件还是元素（中间是 AddAttribute、AddComponentParameter 这类）；是组件时交出 OpenComponent<T> 的 T
    static TypeSyntax? OpenedComponent(InvocationExpressionSyntax invocation)
    {
        if (invocation.Parent is not ExpressionStatementSyntax { Parent: BlockSyntax block } statement) return null;
        for (var index = block.Statements.IndexOf(statement) - 1; index >= 0; index--)
        {
            if (block.Statements[index] is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } }) continue;
            switch (member.Name.Identifier.Text)
            {
                case "OpenComponent":
                    return member.Name is GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic ? generic.TypeArgumentList.Arguments[0] : null;
                case "OpenElement" or "OpenRegion" or "CloseElement" or "CloseComponent" or "CloseRegion":
                    return null;
            }
        }
        return null;
    }
}
