using Bunit;
using Rune.UI;

namespace Rune.Tests.UI;

// 列与 DataTable 共用；合并单元格：MergeRepeated 层层分组，CellSpan 优先
public class ColumnTableTests : BunitContext
{
    public ColumnTableTests() => Services.AddRuneUI();

    sealed record Line(string Supplier, string Category, string Part, decimal? Price);

    static readonly Line[] Lines =
    [
        new("华辰", "机械件", "支架", 10m),
        new("华辰", "机械件", "底座", 20m),
        new("华辰", "钣金件", "盖板", null),
        new("启航", "钣金件", "盖板", 5m),
    ];

    [Fact]
    public void 上下相同的格合并_后一列不跨前一列的分组_被合并掉的格不渲染_合计精确相加()
    {
        var cut = Render<ColumnTable<Line>>(p =>
        {
            p.Add(x => x.Items, Lines).Add(x => x.MergeRepeated, ["supplier", "category"])
                .Add(x => x.CellSpan, (line, key, _) => line.Price is null && key == "part" ? new CellSpan(ColSpan: 2) : line.Price is null && key == "price" ? new CellSpan(ColSpan: 0) : null);
            p.AddChildContent<PropertyColumn<Line, string>>(c => c.Add(x => x.Property, l => l.Supplier));
            p.AddChildContent<PropertyColumn<Line, string>>(c => c.Add(x => x.Property, l => l.Category));
            p.AddChildContent<PropertyColumn<Line, string>>(c => c.Add(x => x.Property, l => l.Part));
            p.AddChildContent<PropertyColumn<Line, decimal?>>(c => c.Add(x => x.Property, l => l.Price).Add(x => x.Summary, ColumnSummary.Sum));
        });

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(("3", "2"), (rows[0].Children[0].GetAttribute("rowspan"), rows[0].Children[1].GetAttribute("rowspan")));
        Assert.Equal(["支架", "10"], rows[0].Children.Skip(2).Select(td => td.TextContent.Trim()));
        Assert.Equal(["底座", "20"], rows[1].Children.Select(td => td.TextContent.Trim()));
        // 第三行：供应商合并掉了；品类在供应商的同一格里另起一组；盖板占两列
        Assert.Equal(["钣金件", "盖板"], rows[2].Children.Select(td => td.TextContent.Trim()));
        Assert.Equal("2", rows[2].Children[1].GetAttribute("colspan"));
        // 不同供应商下的同名品类不合
        Assert.Equal(["启航", "钣金件", "盖板", "5"], rows[3].Children.Select(td => td.TextContent.Trim()));
        Assert.Equal(["合计", "", "", "35"], cut.FindAll("tfoot td").Select(td => td.TextContent.Trim()));
    }
}
