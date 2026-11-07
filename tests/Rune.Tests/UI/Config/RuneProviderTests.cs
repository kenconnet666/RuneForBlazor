using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class RuneProviderTests : BunitContext
{
    static Style Bold => Css.Define(static () => new Style { FontWeight = 700 });

    static Style Dashed => Css.Define(static () => new Style { BorderStyle = BorderStyle.Dashed });

    static readonly ComponentConfigs outline = new() { new ComponentConfig<CardDefaults, CardPart> { Defaults = new() { Variant = CardVariant.Outline } } };

    public RuneProviderTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    [Fact]
    public void 最外层套上应用级主题()
    {
        var cut = Render<RuneProvider>(p => p.AddChildContent("<p>内容</p>"));

        var wrapper = cut.Find("[data-rune-theme]");
        Assert.Equal("light/dark", wrapper.GetAttribute("data-rune-theme"));
        Assert.StartsWith("rt", wrapper.ClassName);
        Assert.Contains(Registry.Rules, rule => rule.Contains("--rune-colors-text:"));
    }

    [Fact]
    public void 默认值作用于没写参数的组件_组件上写的优先()
    {
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Components, outline)
            .AddChildContent<Card>(c => c.AddChildContent("取默认值"))
            .AddChildContent<Card>(c => c.AddChildContent("显式 Elevated").Add(x => x.Variant, CardVariant.Elevated)));

        var roots = cut.FindAll("[data-scope=card][data-part=root]");
        Assert.NotEqual(roots[0].ClassName, roots[1].ClassName);
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{roots[0].ClassName}{{") && rule.Contains("border-color:var(--rune-colors-border)"));
    }

    [Fact]
    public void 嵌套的Provider按项叠加_内层优先()
    {
        var outer = new ComponentConfigs { new ComponentConfig<CardDefaults, CardPart> { Defaults = new() { Variant = CardVariant.Outline } } };
        var inner = new ComponentConfigs { new ComponentConfig<CardDefaults, CardPart> { Defaults = new() { Radius = Radius.Sm } } };

        var merged = new UIConfig(Themes.Light, null, ColorScheme.Light, outer.Then(inner)).Defaults.Get<CardDefaults>();

        Assert.Equal((CardVariant.Outline, Radius.Sm), (merged?.Variant, merged?.Radius));
    }

    [Fact]
    public void 按部件定制的样式排在组件样式之后()
    {
        var components = new ComponentConfigs { new ComponentConfig<CardPart> { Styles = { [CardPart.Header] = Bold } } };
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Components, components)
            .AddChildContent<Card>(c => c.Add(x => x.Header, b => b.AddContent(0, "标题"))));

        // 组件自己的一段、定制的一段：定制在后面的层，盖过组件的
        var classes = cut.Find("[data-scope=card][data-part=header]").ClassList.ToArray();
        Assert.Equal(2, classes.Length);
        Assert.Contains(Registry.Rules, rule => rule.StartsWith("@layer rune.s1{") && rule.Contains($".{classes[1]}{{") && rule.Contains("font-weight:700"));
    }

    [Fact]
    public void 按档位取值的定制只作用于取这个值的组件_排在按部件的定制之后()
    {
        var components = new ComponentConfigs
        {
            new ComponentConfig<ButtonDefaults, ButtonPart>
            {
                Styles = { [ButtonPart.Root] = Bold },
                Variants = { [ButtonVariant.Outline] = { [ButtonPart.Root] = Dashed } },
            },
        };
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Components, components)
            .AddChildContent<Button>(b => b.Add(x => x.Variant, ButtonVariant.Outline).AddChildContent("描边"))
            .AddChildContent<Button>(b => b.Add(x => x.Variant, ButtonVariant.Soft).AddChildContent("浅色")));

        var buttons = cut.FindAll("button");
        string CustomizedRule(int index) => Assert.Single(Registry.Rules, rule => rule.StartsWith("@layer rune.s1{") && rule.Contains($".{buttons[index].ClassList.Last()}{{"));
        Assert.Contains("border-style:dashed", CustomizedRule(0));
        Assert.Contains("font-weight:700", CustomizedRule(0));
        Assert.DoesNotContain("dashed", CustomizedRule(1));
        Assert.Contains("font-weight:700", CustomizedRule(1));
    }

    [Fact]
    public void 共用的空配置不能往里添加() =>
        Assert.Throws<InvalidOperationException>(() => ComponentConfigs.Empty.Add(new ComponentConfig<CardPart>()));

    [Fact]
    public void 参数没变时Provider给出同一个配置()
    {
        // 探针用没有内容片段参数的组件：只看配置对象本身有没有换
        var cut = Render<RuneProvider>(p => p.Add(x => x.Components, outline).AddChildContent<Icon>(i => i.Add(x => x.Data, BuiltInIcons.Check)));
        var child = cut.FindComponent<Icon>();

        cut.Render(p => p.Add(x => x.Components, outline));

        // 配置没变：子组件的参数（级联的配置）没变，跳过渲染
        Assert.Equal(1, child.RenderCount);
    }
}

/// <summary>应用配置了可选主题（ThemeChoices）时最外层的 RuneProvider。</summary>
public class RuneProviderChoicesTests : BunitContext
{
    public RuneProviderChoicesTests() => Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);

    string WrapperClass(Theme? theme) =>
        Render<RuneProvider>(p => p.Add(x => x.Theme, theme).Add(x => x.DarkTheme, theme)).Find("[data-rune-theme]").GetAttribute("class")!;

    [Fact]
    public void 最外层写了Theme时按它_不写时用可选主题()
    {
        var choices = WrapperClass(null);
        var paper = WrapperClass(Themes.Paper);
        Assert.NotEqual(choices, paper);
        Assert.Equal(paper, WrapperClass(Themes.Paper));
    }

    [Fact]
    public void 组件库注册了部件一律border_box的基础规则()
    {
        Assert.Contains(Services.GetRequiredService<StyleRegistry>().Rules, rule => rule.StartsWith("@layer rune.base{:where([data-part]){box-sizing:border-box;}", StringComparison.Ordinal));
    }
}
