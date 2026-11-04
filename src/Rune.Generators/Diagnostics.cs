using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Rune.Generators;

/// <summary>生成器报告的诊断。编号以 RUNE 开头，说明用中文并给出改法。</summary>
internal static class Diagnostics
{
    const string Category = "Rune";

    public static readonly DiagnosticDescriptor StaticState = new(
        "RUNE001", "[State] 不能是静态的",
        "'{0}' 是静态的 [State]：Blazor Server 下静态成员被所有用户共享；共享状态请放进 scoped 服务",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NeedsGetAndSet = new(
        "RUNE002", "[State] 需要 get 与 set",
        "'{0}' 要写成 partial 属性的声明：partial {1} {0} {{ get; set; }}（set 可以加访问修饰符，不能用 init）",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StateInRazorCode = new(
        "RUNE003", "[State] 不能写在 .razor 的 @code 里",
        "[State] '{0}' 写在了 .razor 的 @code 里，生成器看不到这里；请移到同名的 .razor.cs（partial class）中",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticStyleMember = new(
        "RUNE004", "静态 Style 成员应该用 Css.Define 定义",
        "'{0}' 在静态初始化时只创建一次，热重载改了样式值不会生效；改写成 static Style {0} => Css.Define(static () => new Style {{ … }});",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StyleCreatedWhileRendering = new(
        "RUNE005", "不要在渲染路径上新建 Style",
        "渲染时新建的 Style 每次都是新实例，引擎要重新计算类名；改成用 Css.Define 定义的静态属性",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}

/// <summary>
/// 诊断的可比较形式。增量生成器的中间结果要能按值比较才能缓存，
/// <see cref="Diagnostic"/> 与 <see cref="Location"/> 引用着语法树，不行。
/// </summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, string FilePath, TextSpan Span, LinePositionSpan LineSpan, string[] Arguments)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params string[] arguments) =>
        new(descriptor, location.SourceTree?.FilePath ?? "", location.SourceSpan, location.GetLineSpan().Span, arguments);

    public Diagnostic ToDiagnostic() =>
        Diagnostic.Create(Descriptor, Location.Create(FilePath, Span, LineSpan), Arguments);

    public bool Equals(DiagnosticInfo? other) =>
        other is not null && Descriptor.Id == other.Descriptor.Id && FilePath == other.FilePath
        && Span == other.Span && Arguments.SequenceEqual(other.Arguments);

    public override int GetHashCode() => (Descriptor.Id, FilePath, Span).GetHashCode();
}
