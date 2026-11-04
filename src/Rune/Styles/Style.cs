namespace Rune.Styles;

/// <summary>
/// 一段样式：CSS 声明加上按条件分组的嵌套样式。属性表（数百个 CSS 属性，带中文说明）在 Style.Properties.g.cs，
/// 由 tools/css-gen 生成。
/// <code>
/// new Style
/// {
///     Padding = Space.Md,
///     Color = Colors.Text,
///     [When.Hover] = new() { Color = Colors.Accent },
///     [Media.Up(768)] = new() { Padding = Space.Lg },
/// }
/// </code>
/// 只在对象初始化器里赋值，创建后不再修改；同一个实例可以被任意多个元素共用。
/// 组件里的样式用 <see cref="Css.Define"/> 定义成静态属性，渲染时不要新建 Style（每次新建都要重新算类名）。
/// </summary>
public sealed partial class Style
{
    readonly SortedDictionary<string, string> declarations = new(StringComparer.Ordinal);
    readonly SortedDictionary<string, Style> conditions = new(StringComparer.Ordinal);

    /// <summary>CSS 声明：属性名（连字符写法）→ 值。</summary>
    internal IReadOnlyDictionary<string, string> Declarations => declarations;

    /// <summary>条件（如 <c>&amp;:hover</c>、<c>@media (min-width: 768px)</c>）→ 条件成立时的样式。</summary>
    internal IReadOnlyDictionary<string, Style> Conditions => conditions;

    /// <summary>引擎为这个实例算出的结果，按“后面还跟着哪些样式”缓存（见 <see cref="StyleCompiler"/>）。</summary>
    internal readonly StyleCompiler.InstanceCache Compiled = new();

    /// <summary>条件成立时追加的样式，写在对象初始化器里：<c>[When.Hover] = new() { … }</c>。</summary>
    public Style this[Condition condition]
    {
        get => conditions.TryGetValue(condition.Key, out var style)
            ? style
            : throw new KeyNotFoundException($"样式里没有条件 {condition.Key}");
        init => conditions[condition.Key] = value;
    }

    /// <summary>
    /// 没有生成属性的声明（新属性、自定义属性 <c>--x</c>）：<c>["--gap"] = "4px"</c>。
    /// 属性名写连字符形式。
    /// </summary>
    public string this[string property]
    {
        get => declarations.TryGetValue(property, out var value)
            ? value
            : throw new KeyNotFoundException($"样式里没有属性 {property}");
        init => declarations[property] = value;
    }

    // 生成的属性表调用下面三个方法；null 表示“不写这个属性”

    void Set(string property, CssValue? value, NumberUnit unit)
    {
        if (value is { } v) declarations[property] = v.Format(unit);
    }

    void Set(string property, CssLength? value)
    {
        if (value is { } v) declarations[property] = v.ToString();
    }

    void Set(string property, CssColor? value)
    {
        if (value is { } v) declarations[property] = v.ToString();
    }
}
