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
    public async Task 缩放_显示全部_导出数据(string mode)
    {
        var page = await OpenAsync(mode, "line-chart");
        var chart = page.Locator("[role=figure][aria-label=\"月度订单与退货\"]");
        await chart.ScrollIntoViewIfNeededAsync();
        // 一开始只显示最后 12 个月：每个系列 12 个点的线（十字线数据里 12 个类目）
        await Assertions.Expect(page.GetByText("显示：2026-01 到 2026-12")).ToBeVisibleAsync();
        await chart.GetByRole(AriaRole.Button, new() { Name = "显示全部" }).ClickAsync();
        await Assertions.Expect(page.GetByText("显示：全部 36 个月")).ToBeVisibleAsync();
        await Assertions.Expect(chart.GetByRole(AriaRole.Button, new() { Name = "显示全部" })).ToHaveCountAsync(0);

        // 导出数据：CSV 的文件名是图表的名称，内容是读屏的数据表（带 BOM）
        await chart.GetByRole(AriaRole.Button, new() { Name = "导出" }).ClickAsync();
        var download = await page.RunAndWaitForDownloadAsync(() => page.GetByRole(AriaRole.Menuitem, new() { Name = "导出数据（CSV）" }).ClickAsync());
        Assert.Equal("月度订单与退货.csv", download.SuggestedFilename);
        var text = await File.ReadAllTextAsync((await download.PathAsync())!, TestContext.Current.CancellationToken);
        Assert.StartsWith("类目,订单（单）,退货（单）", text.TrimStart('﻿'));
        Assert.Equal(37, text.Split("\r\n").Length);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 瀑布图_提示框列出变动与累计(string mode)
    {
        var page = await OpenAsync(mode, "waterfall-chart");
        var chart = page.Locator("[role=figure][aria-label=\"V1 到 V2 的测算差异\"]");
        var bar = chart.Locator("[data-part=bar][data-index=\"1\"]");
        await Assertions.Expect(bar).ToHaveCountAsync(1);
        var box = (await chart.Locator("[data-rune-chart] svg").BoundingBoxAsync())!;
        var target = (await bar.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(target.X + target.Width / 2, box.Y + box.Height / 2);
        var tooltip = chart.Locator("[data-part=tooltip]");
        await Assertions.Expect(tooltip).ToContainTextAsync("材料单价");
        await Assertions.Expect(tooltip).ToContainTextAsync("-18.2 万元");
        await Assertions.Expect(tooltip).ToContainTextAsync("108.2 万元");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 环形图_悬停扇区_提示框有数值与占比_其余变淡(string mode)
    {
        var page = await OpenAsync(mode, "donut-chart");
        var chart = page.Locator("[role=figure][aria-label=\"访问来源\"]");
        var first = chart.Locator("[data-part=segment][data-index=\"0\"]");
        await Assertions.Expect(first).ToHaveCountAsync(1);
        // 扇区外接框的中心可能落在环中间的空洞里：取路径上的一点（四分之一处），换成页面坐标
        var point = await first.EvaluateAsync<double[]>("""
            path => {
              const p = path.getPointAtLength(path.getTotalLength() / 4);
              const m = path.getScreenCTM();
              return [p.x * m.a + p.y * m.c + m.e, p.x * m.b + p.y * m.d + m.f];
            }
            """);
        await page.Mouse.MoveAsync((float)point[0], (float)point[1]);
        await Assertions.Expect(chart.Locator("[data-part=tooltip]")).ToContainTextAsync("搜索引擎");
        await Assertions.Expect(chart.Locator("[data-part=tooltip]")).ToContainTextAsync("40.3%");
        await Assertions.Expect(chart.Locator("[data-part=segment][data-index=\"1\"]")).ToHaveAttributeAsync("data-dimmed", "");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 网络图_点节点选中_拖动画布不算点击_点空白取消(string mode)
    {
        var page = await OpenAsync(mode, "graph-chart");
        var chart = page.Locator("[role=figure][aria-label=\"团队与供应商的协作\"]");
        await chart.ScrollIntoViewIfNeededAsync();
        var node = chart.Locator("[data-part=node][data-index=\"0\"] path");
        await Assertions.Expect(node).ToHaveCountAsync(1);
        await node.ClickAsync(new() { Force = true });
        await Assertions.Expect(page.GetByText("选中：技术部", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(chart.Locator("[data-part=node][data-index=\"5\"]")).ToHaveAttributeAsync("data-faded", "");

        // 在空白处拖动：画布平移，选中不变
        var box = (await chart.Locator("[data-part=background]").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 8, box.Y + 8);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + 60, box.Y + 40, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(chart.Locator("[data-pan-zoom]")).ToHaveAttributeAsync("transform", "translate(52 32) scale(1)");
        await Assertions.Expect(page.GetByText("选中：技术部", new() { Exact = true })).ToBeVisibleAsync();

        await page.Mouse.ClickAsync(box.X + 8, box.Y + box.Height - 8);
        await Assertions.Expect(page.GetByText("点一个节点", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 地图_缩放按钮_地点不变大_点省份下钻(string mode)
    {
        var page = await OpenAsync(mode, "map-chart");
        var chart = page.Locator("[role=figure][aria-label=\"各省销售额\"]");
        await chart.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(chart.Locator("[data-part=feature]")).Not.ToHaveCountAsync(0);

        // 放大一档：区域整体缩放 1.5 倍，地点只跟着移动（transform 只有平移）
        await chart.GetByRole(AriaRole.Button, new() { Name = "放大" }).ClickAsync();
        await Assertions.Expect(chart.Locator("[data-part=features]")).ToHaveAttributeAsync("transform", new System.Text.RegularExpressions.Regex(@"scale\(1\.5\)$"));
        await Assertions.Expect(chart.Locator("[data-part=points] > g").First).ToHaveAttributeAsync("transform", new System.Text.RegularExpressions.Regex(@"^translate\([-\d.]+ [-\d.]+\)$"));
        await chart.GetByRole(AriaRole.Button, new() { Name = "复位" }).ClickAsync();
        await Assertions.Expect(chart.Locator("[data-part=features]")).ToHaveAttributeAsync("transform", "translate(0 0) scale(1)");

        // 点第一个省份（甘肃，形状不规则：直接派发点击）：下面显示它的金额
        await chart.Locator("[data-part=feature][data-index=\"0\"]").DispatchEventAsync("click");
        await Assertions.Expect(page.GetByText(new System.Text.RegularExpressions.Regex(@"^甘肃：[\d.]+ 万元$"))).ToBeVisibleAsync();
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
