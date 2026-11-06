using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class RuneProviderTests : BunitContext
{
    static Style Bold => Css.Define(static () => new Style { FontWeight = 700 });

    static readonly ComponentDefaults outline = new() { new CardDefaults { Variant = CardVariant.Outline } };

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
            .Add(x => x.Defaults, outline)
            .AddChildContent<Card>(c => c.AddChildContent("取默认值"))
            .AddChildContent<Card>(c => c.AddChildContent("显式 Elevated").Add(x => x.Variant, CardVariant.Elevated)));

        var roots = cut.FindAll("[data-scope=card][data-part=root]");
        Assert.NotEqual(roots[0].ClassName, roots[1].ClassName);
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{roots[0].ClassName}{{") && rule.Contains("border-color:var(--rune-colors-border)"));
    }

    [Fact]
    public void 嵌套的Provider按项叠加_内层优先()
    {
        var outer = new ComponentDefaults { new CardDefaults { Variant = CardVariant.Outline } };
        var inner = new ComponentDefaults { new CardDefaults() };

        var merged = outer.Then(inner).Get<CardDefaults>();

        Assert.Equal(CardVariant.Outline, merged?.Variant);
    }

    [Fact]
    public void 按部件定制的样式排在组件样式之后()
    {
        var styles = new ComponentStyles { { CardPart.Header, Bold } };
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Styles, styles)
            .AddChildContent<Card>(c => c.Add(x => x.Header, b => b.AddContent(0, "标题"))));

        // 组件自己的一段、定制的一段：定制在后面的层，盖过组件的
        var classes = cut.Find("[data-scope=card][data-part=header]").ClassList.ToArray();
        Assert.Equal(2, classes.Length);
        Assert.Contains(Registry.Rules, rule => rule.StartsWith("@layer rune.s1{") && rule.Contains($".{classes[1]}{{") && rule.Contains("font-weight:700"));
    }

    [Fact]
    public void 参数没变时Provider给出同一个配置()
    {
        // 探针用没有内容片段参数的组件：只看配置对象本身有没有换
        var cut = Render<RuneProvider>(p => p.Add(x => x.Defaults, outline).AddChildContent<Icon>(i => i.Add(x => x.Data, BuiltInIcons.Check)));
        var child = cut.FindComponent<Icon>();

        cut.Render(p => p.Add(x => x.Defaults, outline));

        // 配置没变：子组件的参数（级联的配置）没变，跳过渲染
        Assert.Equal(1, child.RenderCount);
    }
}
