namespace Rune.E2E;

/// <summary>图表：量到尺寸后画出 SVG；悬停时十字线与提示框在 JS 里画（不往返）；点图例隐藏系列；点柱子下钻；读屏的数据表。</summary>
public class ChartTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 折线图_悬停提示框_图例隐藏系列(string mode)
    {
        var page = await OpenAsync(mode, "line-chart");
        var chart = page.Locator("[role=figure][aria-label=\"月度销售趋势\"]");
        var svg = chart.Locator("[data-rune-chart] svg");
        await Assertions.Expect(chart.Locator("[data-part=line]")).ToHaveCountAsync(2);

        // 悬停在 7 月附近：提示框列出这一类目的两个系列，十字线出现
        var box = (await svg.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width * 0.58f, box.Y + box.Height / 2);
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Assertions.Expect(tooltip).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(tooltip).ToContainTextAsync("实际销售");
        await Assertions.Expect(tooltip).ToContainTextAsync("万元");
        await Assertions.Expect(chart.Locator("[data-part=crosshair]")).Not.ToHaveAttributeAsync("display", "none");

        // 离开：提示框收起
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height + 80);
        await Assertions.Expect(tooltip).Not.ToHaveAttributeAsync("data-open", "");

        // 点图例隐藏一个系列：只剩一条线，按钮 aria-pressed=false；数据表仍是全部
        var legend = chart.GetByRole(AriaRole.Button, new() { Name = "预期销售" });
        await legend.ClickAsync();
        await Assertions.Expect(legend).ToHaveAttributeAsync("aria-pressed", "false");
        await Assertions.Expect(chart.Locator("[data-part=line]")).ToHaveCountAsync(1);
        await Assertions.Expect(chart.Locator("table th[scope=col]")).ToHaveCountAsync(3);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 柱状图_点柱子下钻(string mode)
    {
        var page = await OpenAsync(mode, "bar-chart");
        var chart = page.Locator("[role=figure][aria-label=\"各团队的销售额对着指标\"]");
        await chart.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(chart.Locator("[data-part=target]")).ToHaveCountAsync(2);
        await chart.Locator("[data-part=bar][data-index=\"2\"]").First.ClickAsync(new() { Force = true });
        await Assertions.Expect(page.GetByText("点了：华北大区")).ToBeVisibleAsync();
    }
}
