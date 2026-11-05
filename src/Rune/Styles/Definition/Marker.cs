namespace Rune.Styles;

/// <summary>
/// 标记（同 vue-tsx-pro 的 <c>css.defineMarker</c>）：挂到被观察的元素上，里面的部件按它的状态取样式。
/// <code>
/// public static readonly Marker Option = Css.Marker("rune-listbox-option");
/// static Style Check => Css.Define(static () => new Style { [When.Inside(When.Selected, Option)] = new() { Visibility = Visibility.Visible } });
/// // 渲染：&lt;div @attributes="Attrs.Merge(parts[…], user, Option.Attrs)"&gt;
/// </code>
/// 标记就是一个固定的类名（不含样式），只用来把条件限定在这一种元素上。
/// </summary>
/// <param name="ClassName">标记的类名。</param>
public sealed record Marker(string ClassName)
{
    /// <summary>挂标记的属性层（只有 class），交给 <see cref="Attrs.Merge"/>：class 会与其他层的类拼在一起。</summary>
    public Attrs Attrs { get; } = new() { ["class"] = ClassName };
}
