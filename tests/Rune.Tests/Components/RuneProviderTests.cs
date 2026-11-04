using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.Components;

public class RuneProviderTests : BunitContext
{
    static Style Bold => Css.Define(static () => new Style { FontWeight = 700 });

    static readonly ComponentDefaults outline = new() { new CollapsibleDefaults { Variant = CollapsibleVariant.Outline } };

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
            .AddChildContent<Collapsible>(c => c.Add(x => x.Title, "取默认值"))
            .AddChildContent<Collapsible>(c => c.Add(x => x.Title, "显式 Plain").Add(x => x.Variant, CollapsibleVariant.Plain)));

        var roots = cut.FindAll("[data-scope=collapsible][data-part=root]");
        Assert.NotEqual(roots[0].ClassName, roots[1].ClassName);
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{roots[0].ClassName}{{") && rule.Contains("border-style:solid"));
    }

    [Fact]
    public void 嵌套的Provider按项叠加_内层优先()
    {
        var outer = new ComponentDefaults { new CollapsibleDefaults { Variant = CollapsibleVariant.Outline } };
        var inner = new ComponentDefaults { new CollapsibleDefaults() };

        var merged = outer.Then(inner).Get<CollapsibleDefaults>();

        Assert.Equal(CollapsibleVariant.Outline, merged?.Variant);
    }

    [Fact]
    public void 按部件定制的样式排在组件样式之后()
    {
        var styles = new ComponentStyles { { CollapsiblePart.Trigger, Bold } };
        var cut = Render<RuneProvider>(p => p
            .Add(x => x.Styles, styles)
            .AddChildContent<Collapsible>(c => c.Add(x => x.Title, "标题")));

        var trigger = cut.Find("[data-part=trigger]");
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{trigger.ClassName}{{") && rule.Contains("font-weight:700"));
    }

    [Fact]
    public void 参数没变时Provider给出同一个配置()
    {
        var cut = Render<RuneProvider>(p => p.Add(x => x.Defaults, outline).AddChildContent<Collapsible>());
        var child = cut.FindComponent<Collapsible>();

        cut.Render(p => p.Add(x => x.Defaults, outline));

        // 配置没变：子组件的参数（级联的配置）没变，跳过渲染
        Assert.Equal(1, child.RenderCount);
    }
}
