using Bunit;
using Rune.UI;

namespace Rune.Tests.UI;

// Diff：字段与行的比较；FieldCompare：表格列出全部、列表只列改了的，变化量带正负号与好坏
public class FieldCompareTests : BunitContext
{
    public FieldCompareTests() => Services.AddRuneUI();

    sealed record Estimate(string Id, decimal Price, int Volume, string Material, IReadOnlyList<string> Tags);

    static readonly Estimate V1 = new("e", 128.60m, 42_000, "6061", ["a"]);
    static readonly Estimate V2 = new("e", 116.80m, 42_000, "6063", ["a"]);

    [Fact]
    public void 按字段比较_数字给出精确的差与变化率_集合逐项比较()
    {
        var fields = Diff.Fields(V1, V2);

        Assert.Equal(["id", "price", "volume", "material", "tags"], fields.Select(f => f.Key));
        var price = fields.Single(f => f.Key == "price");
        Assert.Equal((true, -11.80m), (price.Changed, price.Delta));
        Assert.Equal(-0.0918m, Math.Round(price.Ratio!.Value, 4));
        Assert.False(fields.Single(f => f.Key == "tags").Changed);
        Assert.Equal([true], Diff.Fields<Estimate>(null, V2, ["material"]).Select(f => f.Changed));
    }

    [Fact]
    public void 两份列表按key比较_新增修改删除_删除的排在最后()
    {
        Estimate[] before = [V1, new("x", 1m, 1, "m", [])];
        Estimate[] after = [new("n", 2m, 2, "m", []), V2 with { Id = "e" }];

        var rows = Diff.Rows(before, after, e => e.Id);

        Assert.Equal([("n", RowChange.Inserted), ("e", RowChange.Updated), ("x", RowChange.Deleted)], rows.Select(r => (r.Key, r.Change!.Value)));
        Assert.Equal(["price", "material"], rows[1].ChangedFields!);
    }

    [Fact]
    public void 表格列出全部字段_变化量带正负号_越低越好时降是好()
    {
        var cut = Render<FieldCompare<Estimate>>(p =>
        {
            p.Add(x => x.Before, V1).Add(x => x.After, V2);
            p.AddChildContent<PropertyColumn<Estimate, decimal>>(c => c.Add(x => x.Property, e => e.Price).Add(x => x.Title, "单价").Add(x => x.Better, CompareBetter.Lower));
            p.AddChildContent<PropertyColumn<Estimate, int>>(c => c.Add(x => x.Property, e => e.Volume).Add(x => x.Title, "用量"));
            p.AddChildContent<PropertyColumn<Estimate, string>>(c => c.Add(x => x.Property, e => e.Material).Add(x => x.Title, "材料"));
        });

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);
        Assert.Equal(["单价", "128.6", "116.8", "-11.8（-9.2%）"], rows[0].Children.Select(td => td.TextContent.Trim()));
        Assert.NotNull(rows[0].GetAttribute("data-changed"));
        Assert.Null(rows[1].GetAttribute("data-changed"));
        Assert.Equal("success", cut.FindComponents<TextSpan>()[0].Instance.Tone.ToString()!.ToLowerInvariant());
    }

    [Fact]
    public void 列表只列改了的_没有改动时说明()
    {
        var cut = Render<FieldCompare<Estimate>>(p =>
        {
            p.Add(x => x.Before, V1).Add(x => x.After, V1 with { Material = "7075" }).Add(x => x.Layout, FieldCompareLayout.List);
            p.AddChildContent<PropertyColumn<Estimate, decimal>>(c => c.Add(x => x.Property, e => e.Price).Add(x => x.Title, "单价"));
            p.AddChildContent<PropertyColumn<Estimate, string>>(c => c.Add(x => x.Property, e => e.Material).Add(x => x.Title, "材料"));
        });

        Assert.Equal(["材料"], cut.FindAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(("6061", "7075"), (cut.Find("del").TextContent, cut.Find("ins").TextContent));

        cut.Render(p => p.Add(x => x.After, V1));
        Assert.Equal("没有改动", cut.Find("div").TextContent.Trim());
    }

    [Fact]
    public void 换了对比的记录_没改的行不再标成改了()
    {
        var cut = Render<FieldCompare<Estimate>>(p =>
        {
            p.Add(x => x.Before, V1).Add(x => x.After, V2);
            p.AddChildContent<PropertyColumn<Estimate, decimal>>(c => c.Add(x => x.Property, e => e.Price));
            p.AddChildContent<PropertyColumn<Estimate, string>>(c => c.Add(x => x.Property, e => e.Material));
        });
        Assert.Equal(2, cut.FindAll("tbody tr[data-changed]").Count);

        cut.Render(p => p.Add(x => x.After, V1 with { Material = "7075" }));
        Assert.Single(cut.FindAll("tbody tr[data-changed]"));
    }
}
