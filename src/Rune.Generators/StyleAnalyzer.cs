using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Rune.Generators;

/// <summary>
/// 检查样式的写法，让它们能被缓存、能热重载（见 docs/DESIGN.md §5）：
/// RUNE004 静态 <c>Style</c> 字段/带初始化器的属性；RUNE005 组件渲染路径上 <c>new Style</c>。
/// 项目没引用 Rune 时什么都不做。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StyleAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Diagnostics.StaticStyleMember, Diagnostics.StyleCreatedWhileRendering];

    public override void Initialize(AnalysisContext context)
    {
        // Razor 生成的 BuildRenderTree 也要分析；诊断位置由 #line 映射回 .razor
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var style = context.Compilation.GetTypeByMetadataName("Rune.Styles.Style");
        if (style is null) return;
        var css = context.Compilation.GetTypeByMetadataName("Rune.Styles.Css");
        var component = context.Compilation.GetTypeByMetadataName("Rune.RuneComponent");

        context.RegisterSymbolAction(c => AnalyzeStaticMember(c, style), SymbolKind.Field, SymbolKind.Property);
        if (component is not null)
            context.RegisterOperationAction(c => AnalyzeCreation(c, style, css, component), OperationKind.ObjectCreation);
    }

    /// <summary>RUNE004：<c>static readonly Style</c> 字段，或带初始化器的静态自动属性。</summary>
    static void AnalyzeStaticMember(SymbolAnalysisContext context, INamedTypeSymbol style)
    {
        var symbol = context.Symbol;
        if (!symbol.IsStatic || symbol.IsImplicitlyDeclared) return;

        var matches = symbol switch
        {
            IFieldSymbol field => field.IsReadOnly && IsStyle(field.Type, style),
            IPropertySymbol property => IsStyle(property.Type, style) && HasInitializer(property),
            _ => false,
        };
        if (matches)
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StaticStyleMember, symbol.Locations[0], symbol.Name));
    }

    static bool HasInitializer(IPropertySymbol property) =>
        property.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is PropertyDeclarationSyntax { Initializer: not null });

    /// <summary>RUNE005：RuneComponent 的实例方法 / 实例属性 getter 里 new Style（Css.Define 的 lambda 里除外）。</summary>
    static void AnalyzeCreation(OperationAnalysisContext context, INamedTypeSymbol style, INamedTypeSymbol? css, INamedTypeSymbol component)
    {
        var creation = (IObjectCreationOperation)context.Operation;
        if (!IsStyle(creation.Type, style)) return;
        if (!IsRenderPathMember(context.ContainingSymbol, component)) return;
        if (IsInsideCssDefine(creation, css)) return;

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StyleCreatedWhileRendering, creation.Syntax.GetLocation()));
    }

    /// <summary>实例方法（含 BuildRenderTree）与实例属性 getter；构造函数、字段初始化器、静态成员不算渲染路径。</summary>
    static bool IsRenderPathMember(ISymbol symbol, INamedTypeSymbol component)
    {
        // lambda 与本地函数归入外层成员
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } local)
            symbol = local.ContainingSymbol;

        if (symbol.IsStatic || symbol is not IMethodSymbol method) return false;
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.PropertyGet or MethodKind.ExplicitInterfaceImplementation or MethodKind.Conversion)) return false;
        return InheritsFrom(method.ContainingType, component);
    }

    static bool IsInsideCssDefine(IOperation operation, INamedTypeSymbol? css)
    {
        if (css is null) return false;
        for (var current = operation.Parent; current is not null; current = current.Parent)
            if (current is IInvocationOperation invocation
                && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, css)
                && invocation.TargetMethod.Name == "Define")
                return true;
        return false;
    }

    static bool IsStyle(ITypeSymbol? type, INamedTypeSymbol style) =>
        SymbolEqualityComparer.Default.Equals(type?.OriginalDefinition, style);

    static bool InheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
    {
        for (; type is not null; type = type.BaseType)
            if (SymbolEqualityComparer.Default.Equals(type, baseType)) return true;
        return false;
    }
}
