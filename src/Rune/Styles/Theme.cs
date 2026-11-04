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
    static readonly ConcurrentDictionary<(Theme Light, Theme? Dark, ColorScheme Scheme), CompiledStyle> compiled = new();

    /// <summary>
    /// 主题对应的类名。只能在 <see cref="RuneComponent"/> 的渲染中调用（要登记规则）。
    /// 跟随系统时生成两段：默认用亮色主题，<c>prefers-color-scheme: dark</c> 时用暗色主题。
    /// </summary>
    public static string ClassFor(Theme light, Theme? dark = null, ColorScheme scheme = ColorScheme.System)
    {
        var style = compiled.GetOrAdd((light, dark, scheme), static key => Compile(key.Light, key.Dark, key.Scheme));
        StyleRegistry.Register(style);
        return style.ClassName;
    }

    static CompiledStyle Compile(Theme light, Theme? dark, ColorScheme scheme)
    {
        var (main, alternative) = (scheme, dark) switch
        {
            (ColorScheme.Dark, not null) => (dark, (Theme?)null),
            (ColorScheme.System, not null) => (light, dark),
            _ => (light, null),
        };
        var colorScheme = scheme switch
        {
            ColorScheme.Light => "light",
            ColorScheme.Dark when dark is not null => "dark",
            _ when dark is not null => "light dark",
            _ => "light",
        };

        var body = Declarations(main) + $"color-scheme:{colorScheme};";
        var media = alternative is null ? "" : "@media (prefers-color-scheme: dark){.{0}{" + Declarations(alternative) + "}}";
        var className = "rt" + StableHash.Of(body + media);
        var css = $".{className}{{{body}}}" + media.Replace("{0}", className);
        return new CompiledStyle(className, css);
    }

    static string Declarations(Theme theme)
    {
        var builder = new StringBuilder();
        foreach (var (name, value) in theme.Values) builder.Append(name).Append(':').Append(value).Append(';');
        return builder.ToString();
    }
}
