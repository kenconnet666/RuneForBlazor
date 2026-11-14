using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>之前没有对应组件的 HTML 标签（同 vue-tsx-pro 0.26.0）：上下标、增删、划掉、缩写、引用的出处、插图、宽高比。</summary>
public class ProseElementsTests : BunitContext
{
    public ProseElementsTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    string RuleOf(string className) => string.Join("", Registry.Rules.Where(rule => className.Split(' ').Any(name => rule.Contains($".{name}{{"))));

    [Fact]
    public void 上下标不撑高所在的行_相对基线挪位置()
    {
        var sub = Render<Sub>(p => p.AddChildContent("2")).Find("sub");
        var sup = Render<Sup>(p => p.AddChildContent("2")).Find("sup");

        Assert.Contains("line-height:0", RuleOf(sub.ClassName!));
        Assert.Contains("vertical-align:baseline", RuleOf(sub.ClassName!));
        Assert.Contains("bottom:-0.25em", RuleOf(sub.ClassName!));
        Assert.Contains("top:-0.5em", RuleOf(sup.ClassName!));
        // Text 家族：字号、色调这些参数通用
        Assert.Equal("text", sup.GetAttribute("data-scope"));
    }

    [Fact]
    public void 增删与划掉_各自的元素与样式()
    {
        var del = Render<Del>(p => p.AddChildContent("旧")).Find("del");
        var ins = Render<Ins>(p => p.AddChildContent("新")).Find("ins");
        var strike = Render<Strike>(p => p.Add(x => x.Tone, TextTone.Muted).AddChildContent("¥199")).Find("s");

        Assert.Contains("text-decoration-line:line-through", RuleOf(del.ClassName!));
        Assert.Contains("color:var(--rune-colors-text-muted)", RuleOf(del.ClassName!));
        // 加进来的内容：浅绿底，不用下划线（和链接分不清）
        Assert.Contains("text-decoration-line:none", RuleOf(ins.ClassName!));
        Assert.Contains("success-soft)", RuleOf(ins.ClassName!));
        Assert.Contains("text-decoration-line:line-through", RuleOf(strike.ClassName!));
    }

    [Fact]
    public void 缩写_全称在提示里_能聚焦_点按也显示()
    {
        var cut = Render<Abbr>(p => p.Add(x => x.Title, "服务等级协议").AddChildContent("SLA"));

        var abbr = cut.Find("abbr");
        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal("SLA", abbr.TextContent);
        Assert.Equal("服务等级协议", tooltip.TextContent.Trim());
        // 不写原生的 title（只有鼠标悬停才看得到）；提示是它的描述
        Assert.False(abbr.HasAttribute("title"));
        Assert.Equal(tooltip.Id, abbr.GetAttribute("aria-describedby"));
        Assert.Equal(tooltip.Id, abbr.GetAttribute("data-rune-tooltip"));
        Assert.Equal("0", abbr.GetAttribute("tabindex"));
        Assert.True(abbr.HasAttribute("data-rune-tooltip-press"));
        Assert.Equal("200", tooltip.GetAttribute("data-delay"));
    }

    [Fact]
    public void 引用的出处_在footer里_前面带破折号_里面是cite()
    {
        var plain = Render<Blockquote>(p => p.AddChildContent("好的设计让人看得出该怎么用。"));
        Assert.Empty(plain.FindAll("footer"));

        var cut = Render<Blockquote>(p => p.Add(x => x.Source, "《设计心理学》").AddChildContent("好的设计让人看得出该怎么用。"));
        var footer = cut.Find("blockquote > footer");
        Assert.Equal(("blockquote", "source"), (footer.GetAttribute("data-scope"), footer.GetAttribute("data-part")));
        Assert.StartsWith("—", footer.TextContent.Trim());
        Assert.Equal("《设计心理学》", footer.QuerySelector("cite")!.TextContent);
        // 书名、作者不用斜体
        Assert.Contains("font-style:normal", RuleOf(footer.QuerySelector("cite")!.ClassName!));
    }

    [Fact]
    public void 插图_说明在下面或上面_对齐()
    {
        var bare = Render<Figure>(p => p.AddChildContent("<img alt=\"\" />"));
        Assert.Empty(bare.FindAll("figcaption"));
        Assert.Contains("margin:0", RuleOf(bare.Find("figure").ClassName!));

        var cut = Render<Figure>(p => p.Add(x => x.Caption, "表 2：交付周期").Add(x => x.CaptionSide, FigureCaptionSide.Top).Add(x => x.Align, FigureAlign.Center)
            .AddChildContent("<table></table>"));
        var figure = cut.Find("figure");
        Assert.Equal("表 2：交付周期", cut.Find("figure > figcaption").TextContent);
        // 说明在上面靠倒排，标记里仍在内容后面（读屏、不带样式时顺序不变）
        Assert.Contains("flex-direction:column-reverse", RuleOf(figure.ClassName!));
        Assert.Contains("align-items:center", RuleOf(figure.ClassName!));
        Assert.Equal("table", figure.FirstElementChild!.LocalName);
    }

    [Fact]
    public void 定比例的框_默认16比9_里面的元素铺满是基础规则()
    {
        var cut = Render<AspectRatio>(p => p.AddChildContent("<img alt=\"\" />"));
        var root = cut.Find("div");
        Assert.Equal(("aspect-ratio", "root"), (root.GetAttribute("data-scope"), root.GetAttribute("data-part")));
        Assert.Contains("aspect-ratio:16 / 9", RuleOf(root.ClassName!));

        var classic = Render<AspectRatio>(p => p.Add(x => x.Ratio, "4 / 3")).Find("div");
        Assert.Contains("aspect-ratio:4 / 3", RuleOf(classic.ClassName!));

        // 铺满、图片按 cover 裁切：在最前面的层里按选择器生效，使用方的样式盖得过
        Assert.Contains(Registry.Rules, rule => rule.Contains("""[data-scope="aspect-ratio"][data-part="root"]) > *{""") && rule.Contains("height:100%"));
        Assert.Contains(Registry.Rules, rule => rule.Contains("> :where(img, video){") && rule.Contains("object-fit:cover"));
    }
}
