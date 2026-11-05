using System.Collections.Concurrent;
using System.Text;

namespace Rune.Styles;

/// <summary>
/// 主题：一组 token（CSS 变量）的取值。token 本身是写在样式里的 <c>var(--…)</c>，主题决定它们等于什么：
/// <code>
/// public static Theme Light => Css.Define(static () => new Theme("light")
/// {
///     [Space.Md] = 12,
///     [Colors.Text] = "#1f2328",
/// });
/// public static Theme Dark => Css.Define(static () => new Theme("dark", basedOn: Light)
/// {
///     [Colors.Text] = "#e6edf3",   // 只写和基础主题不同的
/// });
/// </code>
/// 主题经 <see cref="ThemeStyles.ClassFor"/> 变成一个只设置变量的类，挂在容器上，容器里的组件就用这套取值。
/// </summary>
public sealed class Theme
{
    readonly Dictionary<string, string> own = new(StringComparer.Ordinal);
    readonly Dictionary<string, Theme> conditions = new(StringComparer.Ordinal);

    /// <summary>创建主题。</summary>
    /// <param name="name">名字（调试与类名里用）。</param>
    /// <param name="basedOn">基础主题：没写的 token 取它的值。</param>
    public Theme(string name, Theme? basedOn = null)
    {
        Name = name;
        BasedOn = basedOn;
    }

    /// <summary>名字。</summary>
    public string Name { get; }

    /// <summary>基础主题。</summary>
    public Theme? BasedOn { get; }

    /// <summary>是暗色主题：浏览器的原生控件、滚动条按暗色画（color-scheme: dark）。</summary>
    public bool IsDark { get; init; }

    /// <summary>
    /// 条件成立时（@media 规则）另取的值，只写和平时不同的 token：
    /// <c>[Media.ReducedMotion] = new("reduced-motion") { [Durations.Md] = "0ms" }</c>。
    /// </summary>
    public Theme this[Condition condition]
    {
        init
        {
            if (!condition.IsAtRule) throw new ArgumentException($"主题的条件只能是 @ 规则，收到 {condition.Key}", nameof(condition));
            conditions[condition.Key] = value;
        }
    }

    /// <summary>全部条件块（含基础主题的）：条件 → 按变量名排序的取值；同一条件下这个主题写的覆盖基础主题的。</summary>
    public IReadOnlyList<KeyValuePair<string, IReadOnlyList<KeyValuePair<string, string>>>> ConditionalValues
    {
        get
        {
            var all = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
            for (var theme = this; theme is not null; theme = theme.BasedOn)
                foreach (var (condition, block) in theme.conditions)
                {
                    if (!all.TryGetValue(condition, out var values)) all[condition] = values = new(StringComparer.Ordinal);
                    foreach (var (name, value) in block.own) values.TryAdd(name, value);
                }
            return [.. all.Select(c => new KeyValuePair<string, IReadOnlyList<KeyValuePair<string, string>>>(c.Key, [.. c.Value]))];
        }
    }

    /// <summary>长度 token 的取值（数字按 px）。</summary>
    public CssLength this[CssLength token] { init => own[VariableOf(token.ToString())] = value.ToString(); }

    /// <summary>颜色 token 的取值。</summary>
    public CssColor this[CssColor token] { init => own[VariableOf(token.ToString())] = value.ToString(); }

    /// <summary>其他 token 的取值（字重、时长、阴影……；数字不补单位）。</summary>
    public CssValue this[CssValue token] { init => own[VariableOf(token.ToString())] = value.Format(NumberUnit.None); }

    /// <summary>全部取值（含基础主题的），变量名 → 值，按变量名排序。</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Values
    {
        get
        {
            var all = new SortedDictionary<string, string>(StringComparer.Ordinal);
            for (var theme = this; theme is not null; theme = theme.BasedOn)
                foreach (var (name, value) in theme.own)
                    all.TryAdd(name, value);
            return [.. all];
        }
    }

    /// <summary>某个 token 在这个主题里的取值；没有定义时返回 null。</summary>
    public string? ValueOf(string token)
    {
        var variable = VariableOf(token);
        for (var theme = this; theme is not null; theme = theme.BasedOn)
            if (theme.own.TryGetValue(variable, out var value)) return value;
        return null;
    }

    /// <inheritdoc/>
    public override string ToString() => $"Theme({Name})";

    /// <summary><c>var(--rune-space-md)</c> → <c>--rune-space-md</c>；不是单个变量引用时报错（主题只能给 token 赋值）。</summary>
    static string VariableOf(string token)
    {
        if (token.StartsWith("var(--", StringComparison.Ordinal) && token.EndsWith(')') && !token.Contains(','))
            return token[4..^1];
        throw new ArgumentException($"主题只能给 token（单个 CSS 变量引用）赋值，收到 {token}", nameof(token));
    }
}

/// <summary>亮暗模式。</summary>
public enum ColorScheme
{
    /// <summary>跟随系统设置（有暗色主题时）。</summary>
    System,

    /// <summary>始终用亮色主题。</summary>
    Light,

    /// <summary>始终用暗色主题。</summary>
    Dark,
}

/// <summary>把主题变成只设置变量的类，并登记到当前作用域（由 <see cref="RuneStyles"/> 输出）。</summary>
public static class ThemeStyles
{
    /// <summary>
    /// 页面上选中的主题写在根元素（html）的这个属性上（<c>data-theme="paper"</c>）：写了可选主题（choices）时，
    /// 主题类按它取那一套的变量，由浏览器端的主题切换改写，不用重新渲染。
    /// </summary>
    public const string ChoiceAttribute = "data-theme";

    static readonly ConcurrentDictionary<(Theme Light, Theme? Dark, ColorScheme Scheme, string Choices), CompiledStyle> compiled = new();

    /// <summary>
    /// 主题对应的类名。只能在 <see cref="RuneComponent"/> 的渲染中调用（要登记规则）。
    /// 跟随系统时生成两段：默认用亮色主题，<c>prefers-color-scheme: dark</c> 时用暗色主题。
    /// 写了 <paramref name="choices"/> 时每套再生成一段 <c>:root[data-theme="名字"] .类</c>（按名字区分，名字要唯一）：
    /// 根元素上写了哪套就用哪套，没写时同上。
    /// </summary>
    public static string ClassFor(Theme light, Theme? dark = null, ColorScheme scheme = ColorScheme.System, IReadOnlyList<Theme>? choices = null)
    {
        var key = (light, dark, scheme, choices is null ? "" : string.Join(',', choices.Select(t => t.Name)));
        var style = compiled.GetOrAdd(key, static (key, choices) => Compile(key.Light, key.Dark, key.Scheme, choices), choices);
        StyleRegistry.Register(style);
        return style.ClassName;
    }

    static CompiledStyle Compile(Theme light, Theme? dark, ColorScheme scheme, IReadOnlyList<Theme>? choices)
    {
        var (main, alternative) = (scheme, dark) switch
        {
            (ColorScheme.Dark, not null) => (dark, (Theme?)null),
            (ColorScheme.System, not null) => (light, dark),
            _ => (light, null),
        };
        // 先用占位的类名拼出规则，按内容算出类名后再换上（类名按内容哈希：服务端、浏览器、所有请求同名）
        const string placeholder = ".{0}";
        var css = Rules(placeholder, main);
        if (alternative is not null) css += "@media (prefers-color-scheme: dark){" + Rules(placeholder, alternative) + "}";
        // 选中的主题写在根元素上，选择器更具体，盖过上面两段
        foreach (var choice in choices ?? [])
            css += Rules($":root[{ChoiceAttribute}=\"{choice.Name}\"] {placeholder}", choice);
        var className = "rt" + StableHash.Of(css);
        return new CompiledStyle(className, css.Replace("{0}", className, StringComparison.Ordinal));
    }

    // 一套主题的规则：变量与 color-scheme（按主题本身是亮是暗），再加各条件块（@media 包在外面）
    static string Rules(string selector, Theme theme)
    {
        var css = new StringBuilder();
        css.Append(selector).Append('{');
        Declarations(css, theme.Values);
        css.Append("color-scheme:").Append(theme.IsDark ? "dark" : "light").Append(";}");
        foreach (var (condition, values) in theme.ConditionalValues)
        {
            css.Append(condition).Append('{').Append(selector).Append('{');
            Declarations(css, values);
            css.Append("}}");
        }
        return css.ToString();
    }

    static void Declarations(StringBuilder css, IReadOnlyList<KeyValuePair<string, string>> values)
    {
        foreach (var (name, value) in values) css.Append(name).Append(':').Append(value).Append(';');
    }
}
