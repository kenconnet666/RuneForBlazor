using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class DescriptionsTests : BunitContext
{
    public DescriptionsTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    static RenderFragment Item(string label, string value, int span = 1, bool copyable = false, string? copyText = null) => builder =>
    {
        builder.OpenComponent<DescriptionsItem>(0);
        builder.AddAttribute(1, nameof(DescriptionsItem.Label), label);
        builder.AddAttribute(2, nameof(DescriptionsItem.Value), value);
        builder.AddAttribute(3, nameof(DescriptionsItem.Span), span);
        builder.AddAttribute(4, nameof(DescriptionsItem.Copyable), copyable);
        builder.AddAttribute(5, nameof(DescriptionsItem.CopyText), copyText);
        builder.CloseComponent();
    };

    [Fact]
    public void 语义是dl里的dt与dd_标题在列表外()
    {
        var cut = Render<Descriptions>(p => p.Add(x => x.Title, "订单信息").Add(x => x.ChildContent, Item("订单号", "A-1024")));

        Assert.Equal("订单信息", cut.Find("[data-part=title]").TextContent);
        var item = cut.Find("dl > div");
        Assert.Equal("订单号", item.QuerySelector("dt")!.TextContent);
        Assert.Equal("A-1024", item.QuerySelector("dd")!.TextContent.Trim());
        Assert.Equal("horizontal", cut.Find("[data-scope=descriptions][data-part=root]").GetAttribute("data-layout"));
    }

    [Fact]
    public void 复制按钮的名称带标签_复制的是CopyText_没有时是Value()
    {
        var cut = Render<Descriptions>(p => p.Add(x => x.ChildContent, b =>
        {
            Item("银行账号", "6214 8301", copyable: true, copyText: "62148301")(b);
            Item("信用代码", "9131", copyable: true)(b);
        }));

        var buttons = cut.FindAll("dd button");
        Assert.Equal(["复制银行账号", "复制信用代码"], buttons.Select(b => b.GetAttribute("aria-label")));
        Assert.Equal(["62148301", "9131"], buttons.Select(b => b.GetAttribute("data-rune-copy")));
    }

    [Fact]
    public void 固定栏数时跨栏按栏数封顶_窄容器里排成一列()
    {
        var cut = Render<Descriptions>(p => p.Add(x => x.Columns, 2).Add(x => x.ChildContent, Item("地址", "上海", span: 3)));

        var className = cut.Find("dl > div").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{className}") && rule.Contains("grid-column:span 2"));
        Assert.Contains(Registry.Rules, rule => rule.Contains("@container (max-width: 32em)") && rule.Contains("grid-template-columns:1fr"));
    }

    [Fact]
    public void 自适应栏数按容器查询_每档一个最小宽度()
    {
        Render<Descriptions>(p => p.Add(x => x.MinItemWidth, 220).Add(x => x.MaxColumns, 3).Add(x => x.ChildContent, Item("地址", "上海", span: 3)));

        Assert.Contains(Registry.Rules, rule => rule.Contains("@container (min-width: calc(2 * 220px))") && rule.Contains("repeat(2, minmax(0, 1fr))"));
        Assert.Contains(Registry.Rules, rule => rule.Contains("@container (min-width: calc(3 * 220px))") && rule.Contains("grid-column:span 3"));
        Assert.DoesNotContain(Registry.Rules, rule => rule.Contains("calc(4 * 220px)"));
    }

    [Fact]
    public void 项放在Descriptions外时报错()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DescriptionsItem>(p => p.Add(x => x.Label, "订单号")));
    }
}
