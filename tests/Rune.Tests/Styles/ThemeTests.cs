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

    static readonly CssColor ink = "var(--test-ink)";
    static readonly CssLength gap = "var(--test-gap)";

    [Fact]
    public void 没写的token取基础主题的值()
    {
        var light = new Theme("light") { [ink] = "black", [gap] = 4 };
        var dark = new Theme("dark", basedOn: light) { [ink] = "white" };

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
        var light = new Theme("light") { [ink] = "black" };
        var dark = new Theme("dark", basedOn: light) { IsDark = true, [ink] = "white" };

        var className = ThemeStyles.ClassFor(light, dark, ColorScheme.System);

        // color-scheme 按各自主题是亮是暗；主题变量在最低的层，组件的规则（含它们改的变量）在上面的层
        var rule = Assert.Single(registry.Rules);
        Assert.StartsWith("@layer rune.vars{", rule);
        Assert.Contains($".{className}{{--test-ink:black;color-scheme:light;}}", rule);
        Assert.Contains($"@media (prefers-color-scheme: dark){{.{className}{{--test-ink:white;color-scheme:dark;}}}}", rule);
    }

    [Fact]
    public void 可选主题按根元素的data_theme取用_写在跟随系统的两段之后()
    {
        var light = new Theme("light") { [ink] = "black" };
        var dark = new Theme("dark", basedOn: light) { IsDark = true, [ink] = "white" };
        var paper = new Theme("paper", basedOn: light) { [ink] = "brown" };

        var className = ThemeStyles.ClassFor(light, dark, ColorScheme.System, [light, dark, paper]);

        var rule = Assert.Single(registry.Rules);
        var paperRule = $":root[data-theme=\"paper\"] .{className}{{--test-ink:brown;color-scheme:light;}}";
        Assert.Contains(paperRule, rule);
        Assert.True(rule.IndexOf(paperRule, StringComparison.Ordinal) > rule.IndexOf("@media (prefers-color-scheme: dark)", StringComparison.Ordinal));
        // 同样的几套（按名字）复用同一个类
        Assert.Equal(className, ThemeStyles.ClassFor(light, dark, ColorScheme.System, [light, dark, paper]));
        Assert.NotEqual(className, ThemeStyles.ClassFor(light, dark));
    }

    [Fact]
    public void 跟随系统时系统要求增强对比度改用高对比的两套_写在明暗两段之后()
    {
        var light = new Theme("light") { [ink] = "black" };
        var dark = new Theme("dark", basedOn: light) { IsDark = true, [ink] = "white" };
        var contrastLight = new Theme("contrast-light", basedOn: light) { [ink] = "#000" };
        var contrastDark = new Theme("contrast-dark", basedOn: dark) { IsDark = true, [ink] = "#fff" };

        var className = ThemeStyles.ClassFor(light, dark, ColorScheme.System, contrast: (contrastLight, contrastDark));

        var rule = Assert.Single(registry.Rules);
        var contrastLightRule = $"@media (prefers-contrast: more){{.{className}{{--test-ink:#000;color-scheme:light;}}}}";
        var contrastDarkRule = $"@media (prefers-contrast: more) and (prefers-color-scheme: dark){{.{className}{{--test-ink:#fff;color-scheme:dark;}}}}";
        Assert.Contains(contrastLightRule, rule);
        Assert.Contains(contrastDarkRule, rule);
        // 同一具体度，后写的胜：亮暗 → 高对比亮 → 高对比暗
        Assert.True(rule.IndexOf("@media (prefers-color-scheme: dark)", StringComparison.Ordinal) < rule.IndexOf(contrastLightRule, StringComparison.Ordinal));
        Assert.True(rule.IndexOf(contrastLightRule, StringComparison.Ordinal) < rule.IndexOf(contrastDarkRule, StringComparison.Ordinal));
        Assert.NotEqual(className, ThemeStyles.ClassFor(light, dark));
        // 固定亮暗时不跟随系统的对比度
        ThemeStyles.ClassFor(light, dark, ColorScheme.Light, contrast: (contrastLight, contrastDark));
        Assert.DoesNotContain(registry.Rules, r => r != rule && r.Contains("prefers-contrast"));
    }

    [Fact]
    public void 固定暗色时只写暗色主题()
    {
        var light = new Theme("light") { [ink] = "black" };
        var dark = new Theme("dark", basedOn: light) { IsDark = true, [ink] = "white" };

        ThemeStyles.ClassFor(light, dark, ColorScheme.Dark);

        var rule = Assert.Single(registry.Rules);
        Assert.Contains("--test-ink:white;color-scheme:dark;", rule);
        Assert.DoesNotContain("@media", rule);
    }

    [Fact]
    public void 条件取值写成包在外面的媒体查询_基础主题的条件块会继承()
    {
        var baseTheme = new Theme("base") { [gap] = 4, [Media.ReducedMotion] = new("reduced") { [gap] = 0 } };
        var light = new Theme("light", basedOn: baseTheme) { [ink] = "black" };

        var className = ThemeStyles.ClassFor(light);

        var rule = Assert.Single(registry.Rules);
        Assert.Contains($"@media (prefers-reduced-motion: reduce){{.{className}{{--test-gap:0;}}}}", rule);
        Assert.Throws<ArgumentException>(() => new Theme("x") { [When.Hover] = new("y") });
    }

    [Fact]
    public void 内置主题给每个token都定义了值()
    {
        var fields = typeof(Themes).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true, Namespace: "Rune.UI" })
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .ToList();
        var single = fields
            .Where(f => f.FieldType == typeof(CssLength) || f.FieldType == typeof(CssColor) || f.FieldType == typeof(CssValue))
            .Select(f => (Name: $"{f.DeclaringType!.Name}.{f.Name}", Token: f.GetValue(null)!.ToString()!));
        // 色调：每个色调一组颜色 token（Tones.Accent.Solid……）；中性色直接用 Colors 里的 token，不另设变量
        var tones = fields
            .Where(f => f.FieldType == typeof(ToneColors) && f.Name != nameof(Tones.Neutral))
            .SelectMany(f => ((ToneColors)f.GetValue(null)!).All.Select(token => (Name: $"Tones.{f.Name}", Token: token.ToString())));
        var tokens = single.Concat(tones).ToList();

        Assert.NotEmpty(tokens);
        foreach (var theme in Themes.All)
        {
            var missing = tokens.Where(t => theme.ValueOf(t.Token) is null).Select(t => t.Name).ToList();
            Assert.True(missing.Count == 0, $"{theme.Name} 主题缺少：{string.Join("、", missing)}");
        }
    }

    [Fact]
    public void 内置主题_暗色主题声明color_scheme_减少动效时时长为0()
    {
        ThemeStyles.ClassFor(Themes.Cyberpunk);

        var rule = Assert.Single(registry.Rules);
        Assert.Contains("color-scheme:dark;", rule);
        Assert.Contains("--rune-durations-md:200ms;", rule);
        Assert.Contains("@media (prefers-reduced-motion: reduce){", rule);
        Assert.Contains("--rune-durations-md:0ms;", rule);
        Assert.Equal(16, Themes.All.Count);
    }
}
