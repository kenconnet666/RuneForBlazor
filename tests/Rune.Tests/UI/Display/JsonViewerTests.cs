using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>JSON 查看器：第一层展开、值按类型写、收起时项数；选中一项后路径栏写路径，复制的值是原文；全部展开、收起。</summary>
public class JsonViewerTests : BunitContext
{
    static readonly JsonElement Data = JsonDocument.Parse("""
        {"name":"林小满","items":[1,{"unit-price":12.50}],"ok":true,"note":null}
        """).RootElement;

    public JsonViewerTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    string Rows(IRenderedComponent<JsonViewer> cut) =>
        string.Join("|", cut.FindAll("[role=treeitem] [data-part=row]").Select(row => string.Concat(row.TextContent.Where(c => !char.IsWhiteSpace(c)))));

    [Fact]
    public void 第一层展开_值按类型写_收起的显示项数()
    {
        var cut = Render<JsonViewer>(p => p.Add(x => x.Value, Data).AddUnmatched("aria-label", "响应"));
        Assert.Equal("""name:"林小满"|items:[…]2项|0:1|1:{…}1项|ok:true|note:null""", Rows(cut));
        Assert.Equal("响应", cut.Find("[role=tree]").GetAttribute("aria-label"));
        Assert.Null(cut.Find("[data-scope=json-viewer][data-part=root]").GetAttribute("aria-label"));
    }

    [Fact]
    public void 选中一项后路径栏写路径_对象复制成缩进的JSON()
    {
        var cut = Render<JsonViewer>(p => p.Add(x => x.Value, Data).Add(x => x.ExpandDepth, 3));
        Assert.Contains("unit-price:12.50", Rows(cut));

        cut.FindAll("[role=treeitem]").Single(row => row.TextContent.Contains("unit-price")).Click();
        Assert.Equal("""$.items[1]["unit-price"]""", cut.Find("[data-part=path]").TextContent);

        cut.FindAll("[role=treeitem]").Single(row => row.TextContent.Trim().StartsWith("items")).Click();
        var values = cut.FindAll("[data-part=pathBar] [data-rune-copy]").Select(button => button.GetAttribute("data-rune-copy")).ToList();
        Assert.Equal("$.items", values[0]);
        Assert.Equal("[\n  1,\n  {\n    \"unit-price\": 12.50\n  }\n]", values[1]!.Replace("\r\n", "\n"));
    }

    [Fact]
    public void 全部展开与收起()
    {
        var cut = Render<JsonViewer>(p => p.Add(x => x.Value, Data).Add(x => x.ExpandDepth, 0));
        Assert.Equal(4, cut.FindAll("[role=treeitem]").Count);
        cut.FindAll("[data-part=toolbar] button")[0].Click();
        Assert.Equal(7, cut.FindAll("[role=treeitem]").Count);
        cut.FindAll("[data-part=toolbar] button")[1].Click();
        Assert.Equal(4, cut.FindAll("[role=treeitem]").Count);
    }
}
