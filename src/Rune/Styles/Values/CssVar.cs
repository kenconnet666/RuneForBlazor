namespace Rune.Styles;

/// <summary>
/// 一直在变的值（进度、宽度、拖动位置）用的 CSS 变量：样式里引用它（类不变），值写在元素上，只改元素的 style。
/// 每个新值都写进样式会让每个值多一条规则，所以不要那样做。
/// <code>
/// static readonly CssVar&lt;CssLength&gt; fillWidth = CssVar.Length("progress-fill");
/// static Style Fill => Css.Define(static () => new Style { Width = fillWidth });
/// // 渲染：&lt;div @attributes="Attrs.Merge(new() { Sx = Fill, Vars = [fillWidth.Set($"{percent}%")] })"&gt;
/// </code>
/// </summary>
/// <typeparam name="T">值的类型：<see cref="CssLength"/>、<see cref="CssColor"/> 或 <see cref="CssValue"/>。</typeparam>
public sealed class CssVar<T>
{
    readonly Func<T, string> format;

    internal CssVar(string name, T reference, Func<T, string> format)
    {
        Name = name;
        Reference = reference;
        this.format = format;
    }

    /// <summary>变量名（<c>--progress-fill</c>）。</summary>
    public string Name { get; }

    /// <summary>在样式里引用它（<c>var(--progress-fill)</c>）。</summary>
    public T Reference { get; }

    /// <summary>给元素设置这个变量的值，交给 <see cref="Attrs.Vars"/>。</summary>
    public CssVarValue Set(T value) => new(Name, format(value));

    /// <summary>直接写在样式属性上：<c>Width = fillWidth</c>。</summary>
    public static implicit operator T(CssVar<T> variable) => variable.Reference;

    /// <summary>引用它的写法（<c>var(--progress-fill)</c>）：插进 calc() 这类字符串里时用得上。</summary>
    public override string ToString() => Reference?.ToString() ?? "";
}

/// <summary>创建 <see cref="CssVar{T}"/>。</summary>
public static class CssVar
{
    /// <summary>长度变量；数字值按 px。</summary>
    public static CssVar<CssLength> Length(string name) => new($"--{name}", $"var(--{name})", static v => v.ToString());

    /// <summary>颜色变量。</summary>
    public static CssVar<CssColor> Color(string name) => new($"--{name}", $"var(--{name})", static v => v.ToString());

    /// <summary>其他变量（数字不补单位）。</summary>
    public static CssVar<CssValue> Value(string name) => new($"--{name}", $"var(--{name})", static v => v.Format(NumberUnit.None));
}

/// <summary>一个 CSS 变量与它在元素上的值。</summary>
/// <param name="Name">变量名。</param>
/// <param name="Value">值的 CSS 文本。</param>
public readonly record struct CssVarValue(string Name, string Value)
{
    /// <summary>写进 style 属性的形式：<c>--name:value</c>。</summary>
    public override string ToString() => $"{Name}:{Value}";
}
