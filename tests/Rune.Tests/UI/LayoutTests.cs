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
    public void GridItem跨列跨行与占满一行()
    {
        var span = Render<GridItem>(p => p.Add(x => x.Span, 2).Add(x => x.RowSpan, 3));
        var full = Render<GridItem>(p => p.Add(x => x.Span, 2).Add(x => x.FullRow, true));

        var rule = RuleOf(span.Find("div").ClassName!);
        Assert.Contains("grid-column:span 2 / span 2", rule);
        Assert.Contains("grid-row:span 3 / span 3", rule);
        // 跨多列时把列数交给 JS：网格的列比它少时改成跨当前的列数
        Assert.Equal("2", span.Find("div").GetAttribute("data-rune-grid-span"));
        Assert.Contains("grid-column:1 / -1", RuleOf(full.Find("div").ClassName!));
        Assert.False(full.Find("div").HasAttribute("data-rune-grid-span"));
    }

    [Fact]
    public void 列表版的布局是ul_去掉圆点并补上列表角色()
    {
        var grid = Render<GridList>(p => p.Add(x => x.MinItemWidth, "9rem").AddChildContent("<li>a</li>"));
        var flex = Render<FlexList>(p => p.AddChildContent("<li>a</li>"));
        var item = Render<GridListItem>(p => p.Add(x => x.Span, 2));

        Assert.Equal("list", grid.Find("ul").GetAttribute("role"));
        Assert.Contains("list-style:none", RuleOf(grid.Find("ul").ClassName!));
        Assert.Equal("list", flex.Find("ul").GetAttribute("role"));
        Assert.NotNull(item.Find("li"));
    }

    [Fact]
    public void 容器限制最大宽度并居中()
    {
        var cut = Render<Container>(p => p.Add(x => x.Size, ContainerSize.Xs));

        var rule = RuleOf(cut.Find("div").ClassName!);
        Assert.Contains("max-width:var(--rune-sizes-xs)", rule);
        Assert.Contains("margin-inline:auto", rule);
        Assert.Contains("padding-inline:var(--rune-space-lg)", rule);
    }

    [Fact]
    public void 分隔线默认装饰性_带文字时两边各一段线()
    {
        var plain = Render<Separator>();
        var labelled = Render<Separator>(p => p.Add(x => x.Decorative, false).AddChildContent("或者"));

        Assert.Equal("true", plain.Find("div").GetAttribute("aria-hidden"));
        var root = labelled.Find("[data-part=root]");
        Assert.Equal("separator", root.GetAttribute("role"));
        Assert.Equal(labelled.Find("[data-part=label]").Id, root.GetAttribute("aria-labelledby"));
        Assert.Equal(2, labelled.FindAll("[data-part=line]").Count);
    }

    [Fact]
    public void 图标的像素大小与描线写成属性()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Data, BuiltInIcons.X).Add(x => x.Size, 40).Add(x => x.Draw, true));

        var svg = cut.Find("svg");
        Assert.Contains("font-size:40px", svg.GetAttribute("style"));
        Assert.Equal("true", svg.GetAttribute("data-rune-draw"));
    }
}
