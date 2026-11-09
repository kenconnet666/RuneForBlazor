using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>列虚拟化：跳过的列段（两端固定列总在、前后多渲染几列）；宽表只渲染看得见的列，每行仍跨满全部列，横向滚动后换一批，分组表头的列数对得上。</summary>
public class VirtualColumnsTests : BunitContext
{
    const int Days = 60;

    public VirtualColumnsTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Store(string Name, int[] Sales);

    static readonly Store[] Stores = [.. Enumerable.Range(1, 5).Select(i => new Store($"门店 {i}", [.. Enumerable.Range(0, Days).Select(day => i * 100 + day)]))];

    static ColumnView<Store> View(ColumnSticky? sticky = null, double? width = null) =>
        new(new TemplateColumn<Store>(), sticky, width, ColumnType.Text);

    [Fact]
    public void 跳过的列段_两端固定列总在_前后多渲染三列()
    {
        // 开头一列固定（100px），中间 20 列各 150px，末尾一列固定
        IReadOnlyList<ColumnView<Store>> columns = [View(ColumnSticky.Start, 100), .. Enumerable.Range(0, 20).Select(_ => View()), View(ColumnSticky.End, 80)];
        // 视口在起点、宽 600：看得见第 1–4 列（数据列 0..3 落在 100..700），多渲染 3 列到第 7 列
        Assert.Equal([new ColumnSkip(8, 21)], ColumnWindow.Skips(columns, 0, 600, 0));
        // 滚到 1500：看得见 1500..2100 → 第 10（1450..1600）到第 14 列，前后各多 3
        Assert.Equal([new ColumnSkip(1, 7), new ColumnSkip(18, 21)], ColumnWindow.Skips(columns, 1500, 600, 0));
        Assert.Equal([(0, (ColumnSkip?)null), (1, new ColumnSkip(1, 7)), (7, null)], ColumnWindow.Walk(8, [new ColumnSkip(1, 7)]));
    }

    IRenderedComponent<DataTable<Store>> RenderWide(bool grouped) => Render<DataTable<Store>>(p =>
    {
        p.Add(x => x.Items, Stores).Add(x => x.GetKey, store => store.Name).Add(x => x.VirtualColumns, true);
        p.AddChildContent<TemplateColumn<Store>>(c => c.Add(x => x.Key, "name").Add(x => x.Title, "门店").Add(x => x.Width, 120).Add(x => x.Sticky, ColumnSticky.Start)
            .Add(x => x.ChildContent, store => b => b.AddContent(0, store.Name)));
        for (var day = 0; day < Days; day++)
        {
            var index = day;
            p.AddChildContent<TemplateColumn<Store>>(c => c.Add(x => x.Key, $"d{index}").Add(x => x.Title, $"第 {index + 1} 天").Add(x => x.Group, grouped ? $"{index / 30 + 1} 月" : null)
                .Add(x => x.ChildContent, store => b => b.AddContent(0, store.Sales[index])));
        }
    });

    static int Span(AngleSharp.Dom.IElement row) => row.Children.Sum(cell => int.TryParse(cell.GetAttribute("colspan"), out var span) ? span : 1);

    [Fact]
    public void 宽表只渲染看得见的列_每行跨满_滚动后换一批()
    {
        var cut = RenderWide(grouped: false);
        Assert.Equal((Days + 1).ToString(), cut.Find("table").GetAttribute("aria-colcount"));
        var row = cut.Find("tbody tr[data-key]");
        Assert.True(row.Children.Length < 20, $"渲染了 {row.Children.Length} 格");
        Assert.Equal(Days + 1, Span(row));
        Assert.Equal(Days + 1, Span(cut.Find("thead tr")));
        Assert.Equal("1", row.Children[0].GetAttribute("aria-colindex"));

        // JS 报告滚到了中间：换一批列
        cut.Find("[data-rune-virtual]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "columns", Text = "4000 1200" });
        row = cut.Find("tbody tr[data-key]");
        Assert.Equal(Days + 1, Span(row));
        Assert.Contains(row.Children, cell => cell.TextContent.Trim() == "128");
        Assert.DoesNotContain(row.Children, cell => cell.TextContent.Trim() == "100");
    }

    [Fact]
    public void 分组表头的列数对得上()
    {
        var cut = RenderWide(grouped: true);
        cut.Find("[data-rune-virtual]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "columns", Text = "4000 1200" });
        var rows = cut.FindAll("thead tr");
        // 第一行：固定列、跳过的占位（跨两行）与露出的组；第二行：露出的列
        var first = Span(rows[0]);
        var second = rows[1].Children.Length;
        var spanningTwo = rows[0].Children.Where(cell => cell.GetAttribute("rowspan") == "2").Sum(cell => int.TryParse(cell.GetAttribute("colspan"), out var span) ? span : 1);
        Assert.Equal(Days + 1, first);
        Assert.Equal(Days + 1, spanningTwo + second);
        Assert.Equal(Days + 1, Span(cut.Find("tbody tr[data-key]")));
    }
}
