namespace Rune.E2E;

/// <summary>透视表：相同的区域合并成一格、合计行与合计列、热力图着色；计数与平均。</summary>
public class PivotTableTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 区域合并_合计_热力图_计数(string mode)
    {
        var page = await OpenAsync(mode, "pivot-table");
        var heatmap = page.GetByRole(AriaRole.Table, new() { Name = "各区域、供应商的季度采购额（万元）" });

        await Assertions.Expect(heatmap.Locator("thead th")).ToHaveTextAsync(["区域", "供应商", "Q1", "Q2", "Q3", "Q4", "合计"]);
        await Assertions.Expect(heatmap.Locator("tbody tr").First.Locator("td").First).ToHaveAttributeAsync("rowspan", "2");
        await Assertions.Expect(heatmap.Locator("tfoot td").Last).ToHaveTextAsync("1,366.6");
        var styled = await heatmap.Locator("tbody td[style*='color-mix']").CountAsync();
        Assert.True(styled > 10);

        var counts = page.GetByRole(AriaRole.Table, new() { Name = "各主办人的任务数" });
        await Assertions.Expect(counts.Locator("thead th")).ToHaveTextAsync(["主办人", "待开始", "进行中", "待审核", "已完成", "合计"]);
        await Assertions.Expect(counts.Locator("tfoot td").Last).ToHaveTextAsync("46");
    }
}
