using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

public class TableTests : BunitContext
{
    public TableTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    StyleRegistry Registry => Services.GetRequiredService<StyleRegistry>();

    [Fact]
    public void 表头的th是列标题_表体里是行标题_表尾的格加粗()
    {
        var cut = Render<Table>(p => p.AddUnmatched("aria-label", "明细").AddChildContent(builder =>
        {
            builder.OpenComponent<TableHead>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => { b.OpenComponent<TableHeaderCell>(0); b.AddAttribute(1, "Width", (CssLength?)"8rem"); b.CloseComponent(); }));
            builder.CloseComponent();
            builder.OpenComponent<TableBody>(2);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => { b.OpenComponent<TableHeaderCell>(0); b.AddAttribute(1, "Scope", "row"); b.CloseComponent(); }));
            builder.CloseComponent();
            builder.OpenComponent<TableFoot>(4);
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => { b.OpenComponent<TableCell>(0); b.CloseComponent(); }));
            builder.CloseComponent();
        }));

        Assert.Equal("明细", cut.Find("table").GetAttribute("aria-label"));
        var column = cut.Find("thead th");
        Assert.Equal("col", column.GetAttribute("scope"));
        Assert.Contains("--rune-table-column-width:8rem", column.GetAttribute("style"));
        Assert.Equal("row", cut.Find("tbody th").GetAttribute("scope"));
        Assert.NotEqual(cut.Find("tbody th").ClassName, column.ClassName);
        var foot = cut.Find("tfoot td").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{foot}{{") && rule.Contains("border-top-width"));
    }

    [Fact]
    public void 斑马纹隔行浅色底()
    {
        var cut = Render<Table>(p => p.Add(x => x.Striped, true).AddChildContent<TableBody>(b => b.AddChildContent<TableRow>()));
        var row = cut.Find("tr").ClassName;
        Assert.Contains(Registry.Rules, rule => rule.Contains($".{row}:nth-child(even)"));
    }
}
