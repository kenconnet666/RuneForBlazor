using System.Reflection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.Styles;

public class ThemeTests : IDisposable
{
    readonly StyleRegistry registry = new();
    readonly IDisposable scope;

    public ThemeTests() => scope = StyleRegistry.Enter(registry);

    public void Dispose() => scope.Dispose();

    static readonly CssColor Ink = "var(--test-ink)";
    static readonly CssLength Gap = "var(--test-gap)";

    [Fact]
    public void 没写的token取基础主题的值()
    {
        var light = new Theme("light") { [Ink] = "black", [Gap] = 4 };
        var dark = new Theme("dark", basedOn: light) { [Ink] = "white" };

        Assert.Equal("white", dark.ValueOf("var(--test-ink)"));
        Assert.Equal("4px", dark.ValueOf("var(--test-gap)"));
    }

    [Fact]
    public void 只能给单个变量引用赋值()
    {
        CssColor notToken = "#fff";

        Assert.Throws<ArgumentException>(() => new Theme("bad") { [notToken] = "red" });
    }

    [Fact]
    public void 跟随系统时暗色主题写进媒体查询()
    {
        var light = new Theme("light") { [Ink] = "black" };
        var dark = new Theme("dark", basedOn: light) { [Ink] = "white" };

        var className = ThemeStyles.ClassFor(light, dark, ColorScheme.System);

        var rule = Assert.Single(registry.Rules);
        Assert.Contains($".{className}{{--test-ink:black;color-scheme:light dark;}}", rule);
        Assert.Contains($"@media (prefers-color-scheme: dark){{.{className}{{--test-ink:white;}}}}", rule);
    }

    [Fact]
    public void 固定暗色时只写暗色主题()
    {
        var light = new Theme("light") { [Ink] = "black" };
        var dark = new Theme("dark", basedOn: light) { [Ink] = "white" };

        ThemeStyles.ClassFor(light, dark, ColorScheme.Dark);

        var rule = Assert.Single(registry.Rules);
        Assert.Contains("--test-ink:white;color-scheme:dark;", rule);
        Assert.DoesNotContain("@media", rule);
    }

    [Fact]
    public void 内置主题给每个token都定义了值()
    {
        var tokens = typeof(Themes).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true, Namespace: "Rune.UI" })
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(CssLength) || f.FieldType == typeof(CssColor) || f.FieldType == typeof(CssValue))
            .Select(f => (Name: $"{f.DeclaringType!.Name}.{f.Name}", Token: f.GetValue(null)!.ToString()!))
            .ToList();

        Assert.NotEmpty(tokens);
        foreach (var theme in new[] { Themes.Light, Themes.Dark })
        {
            var missing = tokens.Where(t => theme.ValueOf(t.Token) is null).Select(t => t.Name).ToList();
            Assert.True(missing.Count == 0, $"{theme.Name} 主题缺少：{string.Join("、", missing)}");
        }
    }
}
