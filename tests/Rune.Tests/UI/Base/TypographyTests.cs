using Microsoft.AspNetCore.Components;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class TypographyTests : BunitContext
{
    public TypographyTests() => Services.AddRuneUI();

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    string RuleOf(string className) => Assert.Single(Registry.Rules, rule => rule.Contains($".{className}{{"));

    [Fact]
    public void 文字不写参数时不带样式声明_都继承上下文()
    {
        var cut = Render<TextSpan>(p => p.AddChildContent("说明"));

        var span = cut.Find("span");
        Assert.Equal("说明", span.TextContent);
        Assert.Equal("text", span.GetAttribute("data-scope"));
        Assert.True(string.IsNullOrEmpty(span.ClassName));
    }

    [Fact]
    public void 次要文字用次要文字色()
    {
        var cut = Render<TextSpan>(p => p.Add(x => x.Tone, TextTone.Muted).AddChildContent("3 分钟前"));

        Assert.Contains("color:var(--rune-colors-text-muted)", RuleOf(cut.Find("span").ClassName!));
    }

    [Fact]
    public void Strong与Small有自己的默认值()
    {
        var strong = Render<Strong>(p => p.AddChildContent("重要"));
        var small = Render<Small>(p => p.AddChildContent("细则"));

        Assert.Contains("font-weight:var(--rune-font-weights-semibold)", RuleOf(strong.Find("strong").ClassName!));
        Assert.Contains("font-size:var(--rune-font-sizes-xs)", RuleOf(small.Find("small").ClassName!));
    }

    [Fact]
    public void 段落去掉外边距并且多行省略()
    {
        var cut = Render<Paragraph>(p => p.Add(x => x.LineClamp, 2).AddChildContent("很长的描述"));

        var rule = RuleOf(cut.Find("p").ClassName!);
        Assert.Contains("margin:0", rule);
        Assert.Contains("-webkit-line-clamp:2", rule);
    }

    [Fact]
    public void 标题按级别选元素与默认字号_字号可以单独改()
    {
        var h1 = Render<H1>(p => p.AddChildContent("订单详情"));
        var h2 = Render<H2>(p => p.Add(x => x.Size, Size.Lg).AddChildContent("收货地址"));

        var h1Rule = RuleOf(h1.Find("h1").ClassName!);
        Assert.Contains("font-size:var(--rune-font-sizes-xxxl)", h1Rule);
        Assert.Contains("font-weight:var(--rune-font-weights-semibold)", h1Rule);
        Assert.Contains("text-wrap:balance", h1Rule);
        Assert.Contains("font-size:var(--rune-font-sizes-lg)", RuleOf(h2.Find("h2").ClassName!));
        Assert.Equal("heading", h2.Find("h2").GetAttribute("data-scope"));
    }

    [Fact]
    public void H6默认比H5小一档_字号档位带上这一档的控件高度()
    {
        var h5 = Render<H5>(p => p.AddChildContent("配送方式"));
        var h6 = Render<H6>(p => p.AddChildContent("备注"));

        Assert.Contains("font-size:var(--rune-font-sizes-md)", RuleOf(h5.Find("h5").ClassName!));
        var h6Rule = RuleOf(h6.Find("h6").ClassName!);
        Assert.Contains("font-size:var(--rune-font-sizes-sm)", h6Rule);
        // 字号的 2.25 倍取整到 4px；Xs 用 2 倍，不与 Sm 同高
        Assert.Contains("--rune-sizes-control:round(nearest, 2.25em, 4px)", h6Rule);
        Assert.Equal("round(nearest, 2em, 4px)", ScaleStyles.FontSize(Size.Xs)["--rune-sizes-control"]);
    }

    [Fact]
    public void 使用方的属性落到元素上()
    {
        var cut = Render<H3>(p => p.AddUnmatched("id", "install").AddChildContent("安装"));

        Assert.Equal("install", cut.Find("h3").Id);
    }

    [Fact]
    public void 按键与视觉隐藏()
    {
        var kbd = Render<Kbd>(p => p.AddChildContent("Ctrl"));
        var hidden = Render<VisuallyHidden>(p => p.AddChildContent("（在新窗口打开）"));

        Assert.Equal("Ctrl", kbd.Find("kbd").TextContent);
        Assert.Contains("clip:rect(0, 0, 0, 0)", RuleOf(hidden.Find("span").ClassName!));
    }

    [Fact]
    public void 外部链接新窗口打开并带读屏提示()
    {
        var cut = Render<TextLink>(p => p
            .Add(x => x.Href, "https://example.com")
            .Add(x => x.External, true)
            .AddChildContent("帮助文档"));

        var a = cut.Find("a");
        Assert.Equal("https://example.com", a.GetAttribute("href"));
        Assert.Equal("_blank", a.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
        Assert.Contains("（在新窗口打开）", a.TextContent);
        Assert.NotNull(cut.Find("svg"));
    }

    [Fact]
    public void 站内链接没有新窗口属性_下划线按默认值配置()
    {
        Services.AddRuneUI(o => o.Components = new() { new ComponentConfig<LinkDefaults, LinkPart> { Defaults = new() { Underline = LinkUnderline.Hover } } });

        var cut = Render<TextLink>(p => p.Add(x => x.Href, "/orders").AddChildContent("查看订单"));

        var a = cut.Find("a");
        Assert.Null(a.GetAttribute("target"));
        Assert.Contains("text-decoration-line:none", RuleOf(a.ClassName!));
    }

    [Fact]
    public void 容器给的下划线_条目列表与卡片页头悬停才有_显式写的优先_卡片正文照旧()
    {
        RenderFragment Link(string href, LinkUnderline? underline = null) => b =>
        {
            b.OpenComponent<TextLink>(0);
            b.AddComponentParameter(1, nameof(TextLink.Href), href);
            if (underline is not null) b.AddComponentParameter(2, nameof(TextLink.Underline), underline);
            b.AddComponentParameter(3, nameof(TextLink.ChildContent), (RenderFragment)(t => t.AddContent(0, href)));
            b.CloseComponent();
        };
        var list = Render<ItemList>(p => p.AddChildContent<ItemListItem>(item => item
            .Add(x => x.TitleContent, Link("#scoped"))
            .Add(x => x.End, Link("#explicit", LinkUnderline.Always))));
        var card = Render<Card>(p => p.Add(x => x.Extra, Link("#extra")).Add(x => x.ChildContent, Link("#body")));

        Assert.Contains("text-decoration-line:none", RuleOf(list.Find("a[href='#scoped']").ClassName!));
        Assert.Contains("text-decoration-line:underline", RuleOf(list.Find("a[href='#explicit']").ClassName!));
        Assert.Contains("text-decoration-line:none", RuleOf(card.Find("a[href='#extra']").ClassName!));
        Assert.Contains("text-decoration-line:underline", RuleOf(card.Find("a[href='#body']").ClassName!));
    }

    [Fact]
    public void 标题的锚点在有id时出现_指向本节()
    {
        var cut = Render<H3>(p => p.Add(x => x.Anchor, true).AddUnmatched("id", "security").AddChildContent("安全与登录"));
        var withoutId = Render<H3>(p => p.Add(x => x.Anchor, true).AddChildContent("没有 id"));

        var anchor = cut.Find("[data-part=anchor]");
        Assert.Equal("#security", anchor.GetAttribute("href"));
        Assert.Equal("链接到本节", anchor.GetAttribute("aria-label"));
        Assert.Empty(withoutId.FindAll("a"));
    }

    [Fact]
    public void 外部链接带箭头图标与读屏提示_图标与提示之间没有空白()
    {
        var cut = Render<TextLink>(p => p.Add(x => x.Href, "https://vuejs.org").Add(x => x.External, true).AddChildContent("Vue 文档"));

        var link = cut.Find("a");
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("arrow-up-right", cut.Find("svg").GetAttribute("data-icon"));
        Assert.Equal("Vue 文档（在新窗口打开）", link.TextContent.Trim());
    }

    [Fact]
    public void 行内语义元素各有自己的标签()
    {
        Assert.NotNull(Render<Mark>(p => p.AddChildContent("高亮")).Find("mark"));
        Assert.NotNull(Render<Code>(p => p.AddChildContent("npm")).Find("code"));
        Assert.NotNull(Render<Blockquote>(p => p.AddChildContent("引文")).Find("blockquote"));
        Assert.NotNull(Render<Em>(p => p.AddChildContent("强调")).Find("em"));
    }
}
