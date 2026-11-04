using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Rune.Generators;

/// <summary>
/// 检查样式属性的取值（见 docs/DESIGN.md §5）：
/// RUNE006 写成了字符串、但有现成的常量或类型化写法（<c>OutlineStyle = "none"</c> → <c>OutlineStyle.None</c>，
/// <c>Width = "100%"</c> → <c>CssLength.Percent(100)</c>）；替换文本放在诊断的 <see cref="ReplacementKey"/> 里，代码修复照它替换。
/// RUNE007 长度写成 px（非零数字或带 px 的字符串）：组件内部尺寸用 em，边框一类的细线经 BorderWidths token；默认关闭，组件库里打开。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StyleValueAnalyzer : DiagnosticAnalyzer
{
    /// <summary>诊断属性里替换文本的键。</summary>
    public const string ReplacementKey = "replacement";

    const string StylesNamespace = "Rune.Styles.";

    // 带单位的长度 → CssLength 的方法
    static readonly Regex UnitLength = new(@"^(-?(?:\d+\.?\d*|\.\d+))(em|rem|%|vw|dvh|ch)$", RegexOptions.CultureInvariant);
    static readonly Dictionary<string, string> UnitMethods = new()
    {
        ["em"] = "Em", ["rem"] = "Rem", ["%"] = "Percent", ["vw"] = "Vw", ["dvh"] = "Dvh", ["ch"] = "Ch",
    };

    static readonly Regex Pixels = new(@"(?<![\w.-])-?(?:\d*\.)?[1-9]\d*(?:\.\d+)?px\b", RegexOptions.CultureInvariant);

    // 允许 px 的属性：边框、描边、焦点环这类细线（应经 BorderWidths token，这里只管“写了 px”）
    static readonly Regex ThinLine = new(@"^(Border\w*Width|Outline(Width|Offset)|ColumnRuleWidth|TextDecorationThickness|StrokeWidth)$", RegexOptions.CultureInvariant);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Diagnostics.StringStyleValue, Diagnostics.PixelLength];

    public override void Initialize(AnalysisContext context)
    {
        // .razor 的 @code 里定义的样式也要查；诊断位置由 #line 映射回 .razor
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var style = context.Compilation.GetTypeByMetadataName(StylesNamespace + "Style");
        if (style is null) return;
        var types = new KnownTypes(context.Compilation);
        context.RegisterOperationAction(c => Analyze(c, style, types), OperationKind.SimpleAssignment);
    }

    static void Analyze(OperationAnalysisContext context, INamedTypeSymbol style, KnownTypes types)
    {
        var assignment = (ISimpleAssignmentOperation)context.Operation;
        if (assignment.Target is not IPropertyReferenceOperation { Property: var property }
            || !SymbolEqualityComparer.Default.Equals(property.ContainingType, style)) return;
        // 只看字面量：CssWide.Inherit 这类常量的值也是字符串，但已经是推荐的写法
        var value = Unwrap(assignment.Value);
        if (value is not ILiteralOperation && value is not IUnaryOperation { Operand: ILiteralOperation } || !value.ConstantValue.HasValue) return;
        var type = UnderlyingType(property.Type);
        var isLength = SymbolEqualityComparer.Default.Equals(type, types.Length);
        var location = value.Syntax.GetLocation();

        switch (value.ConstantValue.Value)
        {
            case string text:
                if (isLength && Pixels.IsMatch(text) && !ThinLine.IsMatch(property.Name))
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.PixelLength, location, property.Name, $"\"{text}\""));
                if (Suggest(property.Name, type, text, types) is { } replacement)
                {
                    var properties = ImmutableDictionary<string, string?>.Empty.Add(ReplacementKey, replacement);
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StringStyleValue, location, properties, text, replacement));
                }
                break;
            case int or double or float or decimal when isLength && System.Convert.ToDouble(value.ConstantValue.Value, CultureInfo.InvariantCulture) != 0
                                                       && !ThinLine.IsMatch(property.Name):
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.PixelLength, location, property.Name, value.Syntax.ToString()));
                break;
        }
    }

    /// <summary>字符串值的替换写法；没有时 null。顺序：全局关键字、属性自己的关键字类、颜色关键字、长度的常量与单位。</summary>
    static string? Suggest(string propertyName, ITypeSymbol? type, string text, KnownTypes types)
    {
        if (ConstantNamed(types.Wide, text, ignoreCase: false) is { } wide) return $"CssWide.{wide}";
        if (KeywordMember(propertyName, type, text, types.Compilation) is { } keyword) return keyword;
        if (SymbolEqualityComparer.Default.Equals(type, types.Color))
            return ConstantNamed(types.Colors, text, ignoreCase: true) is { } color ? $"CssColors.{color}" : null;
        if (!SymbolEqualityComparer.Default.Equals(type, types.Length)) return null;
        if (text == "auto") return "CssLength.Auto";
        if (text == "none") return "CssLength.None";
        var match = UnitLength.Match(text);
        if (!match.Success) return null;
        var number = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        return $"CssLength.{UnitMethods[match.Groups[2].Value]}({number})";
    }

    /// <summary>属性的关键字类（与属性同名，或重名时加 Keywords 后缀）里对应这个关键字的成员。成员名按生成器的规则从关键字推出。</summary>
    static string? KeywordMember(string propertyName, ITypeSymbol? type, string text, Compilation compilation)
    {
        var member = MemberName(text);
        if (member is null) return null;
        foreach (var className in new[] { propertyName, propertyName + "Keywords" })
        {
            var keywords = compilation.GetTypeByMetadataName(StylesNamespace + className);
            var field = keywords?.GetMembers(member).OfType<IFieldSymbol>().FirstOrDefault(f => f.IsStatic);
            if (field is not null && SymbolEqualityComparer.Default.Equals(field.Type, type)) return $"{className}.{member}";
        }
        return null;
    }

    /// <summary>关键字 → 成员名（同 tools/css-gen 的 keywordMemberName）：按连字符、空格拆开，首字母大写；数字开头加下划线。</summary>
    static string? MemberName(string keyword)
    {
        if (keyword.Length == 0 || !keyword.All(c => char.IsLetterOrDigit(c) || c is '-' or ' ')) return null;
        var name = string.Concat(keyword.Split(new[] { '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));
        return name.Length > 0 && char.IsDigit(name[0]) ? "_" + name : name;
    }

    /// <summary>静态类里值等于 <paramref name="text"/> 的字符串常量名。</summary>
    static string? ConstantNamed(INamedTypeSymbol? holder, string text, bool ignoreCase) =>
        holder?.GetMembers().OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.IsConst && f.ConstantValue is string value
                && string.Equals(value, text, ignoreCase ? System.StringComparison.OrdinalIgnoreCase : System.StringComparison.Ordinal))?.Name;

    // 去掉隐式转换（string → CssLength → CssLength?）
    static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion) operation = conversion.Operand;
        return operation;
    }

    static ITypeSymbol? UnderlyingType(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;

    sealed class KnownTypes(Compilation compilation)
    {
        public Compilation Compilation { get; } = compilation;
        public INamedTypeSymbol? Length { get; } = compilation.GetTypeByMetadataName(StylesNamespace + "CssLength");
        public INamedTypeSymbol? Color { get; } = compilation.GetTypeByMetadataName(StylesNamespace + "CssColor");
        public INamedTypeSymbol? Wide { get; } = compilation.GetTypeByMetadataName(StylesNamespace + "CssWide");
        public INamedTypeSymbol? Colors { get; } = compilation.GetTypeByMetadataName(StylesNamespace + "CssColors");
    }
}
