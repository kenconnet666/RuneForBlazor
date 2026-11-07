namespace Rune.E2E;

/// <summary>数据表格：一万行只渲染看得见的一段，滚到中间换成那一段（上方的占位行撑出高度）；点列标题排序；勾选后工具栏显示已选几单；点行不算勾选。</summary>
public class DataTableTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 只渲染看得见的一段_点列标题排序_勾选(string mode)
    {
        var page = await OpenAsync(mode, "data-table");
        var table = page.GetByRole(AriaRole.Table, new() { Name = "订单" });
        var rows = table.Locator("tbody tr[data-key]");

        await Assertions.Expect(table).ToHaveAttributeAsync("aria-rowcount", "10001");
        await Assertions.Expect(rows.First).ToHaveAttributeAsync("data-key", "SO100000");
        Assert.InRange(await rows.CountAsync(), 1, 50);

        // 滚到中间：渲染的是那一段，行号接着算
        await table.EvaluateAsync("table => table.closest('[data-rune-virtual]').scrollTop = 44 * 5000");
        await Assertions.Expect(table.Locator("tbody tr[aria-rowindex='5002']")).ToBeAttachedAsync();
        Assert.InRange(await rows.CountAsync(), 1, 50);

        await table.EvaluateAsync("table => table.closest('[data-rune-virtual]').scrollTop = 0");
        await Assertions.Expect(rows.First).ToHaveAttributeAsync("data-key", "SO100000");
        await table.GetByRole(AriaRole.Button, new() { Name = "订单号" }).ClickAsync();
        await Assertions.Expect(table.Locator("th[aria-sort='ascending']")).ToContainTextAsync("订单号");
        await Assertions.Expect(rows.First).ToHaveAttributeAsync("data-key", "SO100000");
        await table.GetByRole(AriaRole.Button, new() { Name = "订单号" }).ClickAsync();
        await Assertions.Expect(rows.First).ToHaveAttributeAsync("data-key", "SO109999");

        await rows.First.GetByRole(AriaRole.Checkbox).CheckAsync();
        await Assertions.Expect(page.GetByText("已选 1 单")).ToBeVisibleAsync();
        await Assertions.Expect(rows.First).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 表头筛选_列菜单隐藏列_键盘调整列宽(string mode)
    {
        var page = await OpenAsync(mode, "data-table");
        var table = page.GetByRole(AriaRole.Table, new() { Name = "采购单" });
        var summary = page.GetByTestId("po-summary");

        await table.GetByRole(AriaRole.Button, new() { Name = "筛选“状态”" }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog, new() { Name = "筛选“状态”" }).GetByRole(AriaRole.Checkbox, new() { Name = "已到货" }).CheckAsync();
        await Assertions.Expect(summary).ToContainTextAsync("状态：已到货");
        await Assertions.Expect(table.GetByRole(AriaRole.Button, new() { Name = "筛选“状态”（已筛选）" })).ToBeAttachedAsync();
        foreach (var badge in await table.Locator("tbody [data-status]").AllAsync()) Assert.Equal("Received", await badge.GetAttributeAsync("data-status"));
        await page.Keyboard.PressAsync("Escape");

        await table.GetByRole(AriaRole.Button, new() { Name = "“品类”列的操作" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "隐藏这一列" }).ClickAsync();
        await Assertions.Expect(table.Locator("th[data-column=category]")).ToHaveCountAsync(0);

        var resizer = table.GetByRole(AriaRole.Separator, new() { Name = "调整“采购员”的列宽" });
        await resizer.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(table.Locator("col[data-column=buyer]")).ToHaveAttributeAsync("style", "width:156px");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "还原列设置" })).ToBeEnabledAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 展开详情_分组表头两行(string mode)
    {
        var page = await OpenAsync(mode, "data-table");
        var proposals = page.GetByRole(AriaRole.Table, new() { Name = "供应商改进建议" });

        await Assertions.Expect(proposals.GetByText("强度裕度由 1.42 降到 1.31")).ToBeVisibleAsync();
        var toggle = proposals.Locator("tr[data-key='GYS-2026-034'] button[aria-expanded]");
        await toggle.ClickAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        var detail = proposals.Locator("#" + await toggle.GetAttributeAsync("aria-controls"));
        await Assertions.Expect(detail).ToContainTextAsync("引入第二供应商");
        await Assertions.Expect(proposals.Locator("tr[data-key='GYS-2026-040'] button[aria-expanded]")).ToHaveCountAsync(0);

        var changes = page.GetByRole(AriaRole.Table, new() { Name = "采购方案调整" });
        await Assertions.Expect(changes.Locator("thead tr")).ToHaveCountAsync(2);
        await Assertions.Expect(changes.Locator("th[scope=colgroup]")).ToHaveTextAsync(["调整前", "调整后"]);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 定位到行_点行看详情_滚动加载_单选(string mode)
    {
        var page = await OpenAsync(mode, "data-table");

        var tasks = page.GetByRole(AriaRole.Table, new() { Name = "项目任务" });
        await page.GetByRole(AriaRole.Button, new() { Name = "定位", Exact = true }).ClickAsync();
        var target = tasks.Locator("tr[data-key='PO-2026-1360']");
        await Assertions.Expect(target).ToBeInViewportAsync();
        await Assertions.Expect(target).ToHaveAttributeAsync("data-tone", "info");
        await page.GetByLabel("任务编码").FillAsync("PO-2026-9999");
        await page.GetByRole(AriaRole.Button, new() { Name = "定位", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByText("没有这个任务", new() { Exact = true })).ToBeVisibleAsync();

        var todos = page.GetByRole(AriaRole.Table, new() { Name = "待办" });
        await Assertions.Expect(todos.Locator("tr[aria-current=true]")).ToHaveAttributeAsync("data-key", "t2");
        await todos.Locator("tr[data-key=t3]").GetByText("财务复核").ClickAsync();
        await Assertions.Expect(todos.Locator("tr[aria-current=true]")).ToHaveAttributeAsync("data-key", "t3");
        await Assertions.Expect(page.GetByText("机柜导轨第二供应商", new() { Exact = true })).ToBeVisibleAsync();

        var events = page.GetByRole(AriaRole.Table, new() { Name = "事件流" });
        await Assertions.Expect(page.GetByText("已加载 40 / 400 条")).ToBeVisibleAsync();
        await events.EvaluateAsync("table => { const v = table.closest('[data-rune-virtual]'); v.scrollTop = v.scrollHeight; }");
        await Assertions.Expect(page.GetByText("已加载 80 / 400 条")).ToBeVisibleAsync();

        var quotes = page.GetByRole(AriaRole.Table, new() { Name = "供应商报价" });
        await quotes.GetByRole(AriaRole.Radio, new() { Name = "选择第 4 行" }).CheckAsync();
        await Assertions.Expect(page.GetByText("选中：星河紧固件")).ToBeVisibleAsync();
        await Assertions.Expect(quotes.Locator("tr[data-key=q4]")).ToHaveAttributeAsync("aria-selected", "true");
    }
}
