using Bunit;
using Rune.UI;

namespace Rune.Tests.UI;

// 透视：分组聚合、顺序、合计与热力图；PivotTable 建在 ColumnTable 上（相同的上级合并）
public class PivotTableTests : BunitContext
{
    public PivotTableTests() => Services.AddRuneUI();

    sealed record Sale(string Region, string Supplier, string Quarter, decimal Amount);

    static readonly Sale[] Sales =
    [
        new("华南", "粤海", "Q2", 2.5m),
        new("华东", "华辰", "Q1", 1.1m),
        new("华东", "华辰", "Q1", 0.2m),
        new("华东", "恒远", "Q2", 4m),
    ];

    static readonly IReadOnlyList<PivotDimension<Sale>> Rows =
    [
        new("region", "区域", s => s.Region) { Order = ["华东", "华南"] },
        new("supplier", "供应商", s => s.Supplier),
    ];

    static readonly PivotDimension<Sale> Quarter = new("quarter", "季度", s => s.Quarter);

    static readonly PivotValue<Sale> Amount = new("amount", "金额", s => s.Amount);

    [Fact]
    public void 按行维度逐级排序_每格精确求和_合计与热力图的范围()
    {
        var result = Pivot.Compute(Sales, Rows, Quarter, Amount, PivotAggregate.Sum, "zh-CN");

        // 区域按 Order，供应商按拼音：恒远（heng）在华辰（hua）前
        Assert.Equal(["华东/恒远", "华东/华辰", "华南/粤海"], result.RowKeys.Select(row => string.Join("/", row)));
        Assert.Equal(["Q1", "Q2"], result.ColumnKeys);
        Assert.Equal(1.3m, result.Cell(["华东", "华辰"], "Q1"));
        Assert.Null(result.Cell(["华东", "华辰"], "Q2"));
        Assert.Equal((5.3m, 6.5m, 7.8m), (result.RowTotal(["华东", "恒远"])!.Value + result.RowTotal(["华东", "华辰"])!.Value, result.ColumnTotals["Q2"]!.Value, result.GrandTotal!.Value));
        Assert.Equal((1.3m, 4m), result.Extent);
        Assert.Equal(2, Pivot.Compute(Sales, Rows, Quarter, null, PivotAggregate.Count, "zh-CN").Cell(["华东", "华辰"], "Q1"));
    }

    [Fact]
    public void 相同的区域合并成一格_没有明细的格显示空值_合计列加粗_热力图着色()
    {
        var cut = Render<PivotTable<Sale>>(p => p.Add(x => x.Data, Sales).Add(x => x.Rows, Rows).Add(x => x.Columns, Quarter).Add(x => x.Value, Amount)
            .Add(x => x.Heatmap, true));

        Assert.Equal(["区域", "供应商", "Q1", "Q2", "合计"], cut.FindAll("thead th").Select(th => th.TextContent.Trim()));
        var rows = cut.FindAll("tbody tr");
        Assert.Equal("2", rows[0].Children[0].GetAttribute("rowspan"));
        Assert.Equal(["恒远", "—", "4", "4"], rows[0].Children.Skip(1).Select(td => td.TextContent.Trim()));
        Assert.Contains("color-mix", rows[0].Children[3].GetAttribute("style"));
        Assert.Equal(["合计", "", "1.3", "6.5", "7.8"], cut.FindAll("tfoot td").Select(td => td.TextContent.Trim()));
    }
}
