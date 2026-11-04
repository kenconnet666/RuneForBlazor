using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class LayoutTests : BunitContext
{
    public LayoutTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    string RuleOf(string className) => Assert.Single(Registry.Rules, rule => rule.Contains($".{className}{{"));

    [Fact]
    public void Flex的档位合成一个类()
    {
        var cut = Render<Flex>(p => p
            .Add(x => x.Direction, LayoutDirection.Column)
            .Add(x => x.Align, ItemAlign.Center)
            .Add(x => x.Justify, ContentJustify.Between)
            .Add(x => x.Gap, SpaceScale.Sm)
            .AddChildContent("<span>A</span><span>B</span>"));

        var rule = RuleOf(cut.Find("div").ClassName!);
        Assert.Contains("display:flex", rule);
        Assert.Contains("flex-direction:column", rule);
        Assert.Contains("align-items:center", rule);
        Assert.Contains("justify-content:space-between", rule);
        Assert.Contains("gap:var(--rune-space-sm)", rule);
        Assert.Equal(2, cut.FindAll("span").Count);
    }

    [Fact]
    public void 行间隙盖过间隙()
    {
        var cut = Render<Flex>(p => p.Add(x => x.Wrap, true).Add(x => x.Gap, SpaceScale.Sm).Add(x => x.RowGap, SpaceScale.Lg));

        var rule = RuleOf(cut.Find("div").ClassName!);
        // 简写在前、长属性在后，row-gap 盖过 gap 的行方向
        Assert.True(rule.IndexOf("gap:var(--rune-space-sm)", StringComparison.Ordinal)
                    < rule.IndexOf("row-gap:var(--rune-space-lg)", StringComparison.Ordinal));
    }

    [Fact]
    public void Grid固定列数()
    {
        var cut = Render<Grid>(p => p.Add(x => x.Columns, 3));

        Assert.Contains("grid-template-columns:repeat(3, minmax(0, 1fr))", RuleOf(cut.Find("div").ClassName!));
        Assert.Null(cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Grid自动换列_宽度经变量写在元素上_不同宽度共用一个类()
    {
        var narrow = Render<Grid>(p => p.Add(x => x.MinItemWidth, "10em"));
        var wide = Render<Grid>(p => p.Add(x => x.MinItemWidth, "20em"));

        Assert.Equal(narrow.Find("div").ClassName, wide.Find("div").ClassName);
        Assert.Equal("--rune-grid-min-item-width:10em", narrow.Find("div").GetAttribute("style"));
        Assert.Equal("--rune-grid-min-item-width:20em", wide.Find("div").GetAttribute("style"));
        Assert.Contains("auto-fit", RuleOf(narrow.Find("div").ClassName!));
    }

    [Fact]
    public void GridItem跨列与占满一行()
    {
        var span = Render<GridItem>(p => p.Add(x => x.Span, 2));
        var full = Render<GridItem>(p => p.Add(x => x.Span, 2).Add(x => x.FullRow, true));

        Assert.Contains("grid-column:span 2", RuleOf(span.Find("div").ClassName!));
        Assert.Contains("grid-column:1 / -1", RuleOf(full.Find("div").ClassName!));
    }
}
