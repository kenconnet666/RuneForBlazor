using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class DocsKitTests : BunitContext
{
    public DocsKitTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 示例卡片_源码默认收起_JS切换展开_展开时才上色_写了id时源码区id固定()
    {
        var shown = false;
        var cut = Render<CodePreview>(p => p
            .Add(x => x.Code, "<Button>保存</Button>")
            .Add(x => x.ShowCode, shown)
            .Add(x => x.ShowCodeChanged, (bool v) => shown = v)
            .AddChildContent("<button>保存</button>"));

        Assert.Equal("预览", cut.Find("[role=group]").GetAttribute("aria-label"));
        var toggle = cut.Find("button[aria-controls]");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        // 两个文字都在，按 aria-expanded 显示其中一个（JS 切换时不用改文字）
        Assert.Equal(["显示代码", "隐藏代码"], toggle.QuerySelectorAll("span:not([data-part])").Select(s => s.TextContent).Where(t => t.Length > 0).ToArray());
        Assert.Equal(toggle.GetAttribute("aria-controls"), cut.Find("[data-rune-collapse]").Id);
        Assert.Equal("", toggle.GetAttribute("data-rune-disclose"));
        // 收起时是纯文本（页内搜索也找得到）；语言照常写上，JS 等内容区展开再上色
        Assert.Equal("razor", cut.Find("code").GetAttribute("data-rune-highlight"));
        Assert.Equal("<Button>保存</Button>", cut.Find("code").TextContent);

        // 点击由 JS 当场切换，再发 rune-toggle 让 C# 写回：这里模拟 JS 发的那一次
        cut.Find("[data-rune-collapse]").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = true });
        Assert.True(shown);
        Assert.Equal("<Button>保存</Button>", cut.Find("button[data-rune-copy]").GetAttribute("data-rune-copy"));

        var stable = Render<CodePreview>(p => p.Add(x => x.Code, "x").AddUnmatched("id", "basic-preview"));
        Assert.Equal(("basic-preview-code", "keep"), (stable.Find("button[aria-controls]").GetAttribute("aria-controls"), stable.Find("button[aria-controls]").GetAttribute("data-rune-disclose")));
        Assert.Equal("basic-preview-code", stable.Find("[data-rune-collapse]").Id);
    }

    [Fact]
    public void 代码组_默认选第一段_两组绑同一个值一起切换()
    {
        string? value = null;
        CodeGroupItem[] items = [new("CLI", "dotnet add package Rune.UI", "shellscript"), new("XML", "<PackageReference />", "xml") { Value = "xml" }];
        var cut = Render<CodeGroup>(p => p.Add(x => x.Items, items).Add(x => x.Value, value).Add(x => x.ValueChanged, (string? v) => value = v));

        Assert.Equal("CLI / XML", cut.Find("[role=tablist]").GetAttribute("aria-label"));
        Assert.Equal("true", cut.Find("[role=tab]").GetAttribute("aria-selected"));
        cut.FindAll("[role=tab]")[1].Click();
        Assert.Equal("xml", value);
    }

    [Fact]
    public void API表格_必填与可绑定的标记_默认值_档位的可选值()
    {
        ApiProp[] props =
        [
            new("Value", ApiPropKind.Prop, "double", "当前评分") { Bindable = true, Default = "0" },
            new("Size", ApiPropKind.Prop, "Size?", "尺寸") { Values = [new("Sm", "表格里"), new("Lg", "详情页")] },
            new("Label", ApiPropKind.Prop, "string", "名称") { Required = true },
        ];
        var cut = Render<PropsTable>(p => p.Add(x => x.Props, props).Add(x => x.Striped, true).Add(x => x.Caption, "Rating 的参数"));

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);
        Assert.Contains("可绑定", rows[0].TextContent);
        Assert.Contains("必填", rows[2].TextContent);
        Assert.Equal("0", rows[0].QuerySelectorAll("td")[2].TextContent.Trim());
        Assert.Equal("—", rows[2].QuerySelectorAll("td")[2].TextContent.Trim());
        Assert.Equal(["Sm", "Lg"], rows[1].QuerySelectorAll("dt").Select(d => d.TextContent));
        Assert.Equal("Rating 的参数", cut.Find("caption").TextContent);
        Assert.Equal(["名称", "类型", "默认值", "说明"], cut.FindAll("th").Select(th => th.TextContent));
    }

    [Fact]
    public void 文字表格_反引号是行内代码_第一列不折行_最小宽度按列数()
    {
        var cut = Render<TextTable>(p => p
            .Add(x => x.Head, ["选项", "含义"])
            .Add(x => x.Rows, [["`Ttl`", "过期时间"]]));

        Assert.Equal("Ttl", cut.Find("td code").TextContent);
        Assert.Contains("min-width:16rem", cut.Find("table").GetAttribute("style"));
    }

    [Fact]
    public void 文档页_标题与简介_小节的标题带锚点()
    {
        var cut = Render<DocArticle>(p => p
            .Add(x => x.Title, "缓存")
            .Add(x => x.Description, "两级缓存")
            .AddChildContent<DocSection>(s => s.Add(x => x.Id, "usage").Add(x => x.Title, "怎么用")));

        Assert.Equal("缓存", cut.Find("article h1").TextContent.Trim());
        Assert.Equal("两级缓存", cut.Find("article p").TextContent);
        var heading = cut.Find("section h2");
        Assert.Equal("usage", heading.Id);
        Assert.Equal("#usage", heading.QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void 色块_普通颜色显示取值_token交给JS按当前主题读()
    {
        var plain = Render<ColorSwatch>(p => p.Add(x => x.Color, (Rune.Styles.CssColor)"#3e63dd").Add(x => x.Label, "品牌色"));
        Assert.Equal(["品牌色", "#3e63dd"], plain.FindAll("span").Select(s => s.TextContent));
        Assert.Contains("--rune-color-swatch-fill:#3e63dd", plain.Find("[aria-hidden] > div").GetAttribute("style"));

        var token = Render<ColorSwatch>(p => p.Add(x => x.Color, Tones.Accent.Solid));
        Assert.Equal("--rune-tones-accent-solid", token.Find("[data-rune-token-value]").GetAttribute("data-rune-token-value"));
    }
}
