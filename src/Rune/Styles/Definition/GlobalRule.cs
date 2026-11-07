namespace Rune.Styles;

/// <summary>
/// 按选择器生效、不挂在类上的基础规则（同 vue-tsx-pro 的 <c>css.globalRule</c>）：放在最前面的级联层 <c>rune.base</c>，
/// 组件与使用方的样式都盖得过它；选择器写在 <c>:where(…)</c> 里时也不加特异性。
/// 以单例注册进 DI，每个作用域的 <see cref="StyleRegistry"/> 创建时登记：
/// <code>services.AddSingleton(new GlobalRule(":where([data-part])", new Style { BoxSizing = BoxSizing.BorderBox }));</code>
/// </summary>
/// <param name="selector">选择器。</param>
/// <param name="style">样式；条件（悬停等）接在选择器后面。</param>
public sealed class GlobalRule(string selector, Style style)
{
    CompiledStyle? compiled;

    /// <summary>编译后的规则：照常编译成一个类，再把层换成 rune.base、类名换成选择器。</summary>
    internal CompiledStyle Compiled => compiled ??= Compile();

    CompiledStyle Compile()
    {
        var asClass = StyleCompiler.Compile([style]) ?? throw new ArgumentException("基础规则的样式是空的", nameof(style));
        var css = asClass.Css
            .Replace("@layer rune.s0{", $"@layer {StyleCompiler.BaseLayer}{{", StringComparison.Ordinal)
            .Replace($".{asClass.ClassName}", selector, StringComparison.Ordinal);
        return new CompiledStyle("rg" + StableHash.Of(css), css);
    }
}
