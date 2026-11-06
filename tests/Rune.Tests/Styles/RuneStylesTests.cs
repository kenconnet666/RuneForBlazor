using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;

namespace Rune.Tests.Styles;

/// <summary>样式的输出：每块开头声明级联层的顺序（规则都在层里，层名第一次出现之前就要定好顺序）。</summary>
public class RuneStylesTests : BunitContext
{
    public RuneStylesTests() => Services.AddRune();

    [Fact]
    public void 每块样式开头声明层的顺序_规则在层里()
    {
        var registry = Services.GetRequiredService<StyleRegistry>();
        using (StyleRegistry.Enter(registry)) Attrs.Merge(new Style { Color = "red" });

        var cut = Render<RuneStyles>();

        Assert.StartsWith(StyleCompiler.LayerOrder + "@layer rune.s0{", cut.Find("style[data-rune]").TextContent);
    }
}
