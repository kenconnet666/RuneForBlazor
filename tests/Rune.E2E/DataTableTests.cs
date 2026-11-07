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

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 方向键走格_Enter编辑提交_接口拒绝时留着编辑器_Esc放弃_行校验标在格上(string mode)
    {
        var page = await OpenAsync(mode, "data-table");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "采购申请明细" });
        var part = grid.Locator("tr[data-key='2'] td[data-column=part]");

        await Assertions.Expect(grid.Locator("tr[data-key='3'] td[data-column=due]")).ToHaveAttributeAsync("aria-invalid", "true");

        await part.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(grid.Locator("tr[data-key='3'] td[data-column=part]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(part).ToBeFocusedAsync();

        // 直接打字开始编辑：打的字替换原值（中文经输入法不发 keydown，这里打字母）
        await page.Keyboard.PressAsync("H");
        var editor = part.GetByRole(AriaRole.Textbox);
        await Assertions.Expect(editor).ToBeFocusedAsync();
        await Assertions.Expect(editor).ToHaveValueAsync("H");
        await page.Keyboard.TypeAsync("-2 底座");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(part).ToHaveTextAsync("H-2 底座");
        await Assertions.Expect(part).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByText("第 2 行已保存", new() { Exact = true })).ToBeVisibleAsync();

        // 已下单的行改数量：接口拒绝，编辑器留着并显示原因；Esc 放弃
        var quantity = grid.Locator("tr[data-key='1'] td[data-column=quantity]");
        await quantity.DblClickAsync();
        var number = quantity.GetByRole(AriaRole.Spinbutton);
        await Assertions.Expect(number).ToBeFocusedAsync();
        await number.FillAsync("50");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(quantity.GetByRole(AriaRole.Alert)).ToHaveTextAsync("已下单的行不能改数量，请走变更单");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(quantity).ToHaveTextAsync("40");
        await Assertions.Expect(quantity).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘拿起行下移一位_排序时收起把手(string mode)
    {
        var page = await OpenAsync(mode, "data-table");
        var table = page.GetByRole(AriaRole.Table, new() { Name = "需求排期" });
        var rows = table.Locator("tbody tr[data-key]");

        var handle = table.GetByRole(AriaRole.Button, new() { Name = "移动 R-201" });
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(rows.Nth(1)).ToHaveAttributeAsync("data-key", "R-201");
        await Assertions.Expect(rows.Nth(1).Locator("td").Nth(0)).ToContainTextAsync("2");
        await Assertions.Expect(handle).ToBeFocusedAsync();

        await table.GetByRole(AriaRole.Button, new() { Name = "预计工时" }).ClickAsync();
        await Assertions.Expect(table.Locator("[data-rune-sortable-handle]")).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 勾选后出现批量操作栏_取消选择后隐藏_列菜单切换列_合计按万元(string mode)
    {
        var page = await OpenAsync(mode, "data-table");
        var table = page.GetByRole(AriaRole.Table, new() { Name = "项目台账" });
        // 第一个是栏里的数目，第二个是栏外的播报区
        var bar = page.GetByText("已选 1 项", new() { Exact = true }).First;

        await Assertions.Expect(table.Locator("tfoot td").Nth(5)).ToHaveTextAsync("106.14");
        await Assertions.Expect(table.Locator("tfoot td").Nth(6)).ToHaveTextAsync("48.85");
        await Assertions.Expect(table.Locator("tr[data-key='PO-2026-0302']").GetByRole(AriaRole.Checkbox)).ToBeDisabledAsync();

        await table.Locator("tr[data-key='PO-2026-0301']").GetByRole(AriaRole.Checkbox).CheckAsync();
        await Assertions.Expect(bar).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "取消选择" }).ClickAsync();
        await Assertions.Expect(bar).ToBeHiddenAsync();

        await Assertions.Expect(table.Locator("th[data-column=dmc]")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Button, new() { Name = "列", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "运费节省（元/单）" }).ClickAsync();
        await Assertions.Expect(table.Locator("th[data-column=dmc]")).ToHaveCountAsync(1);
    }
}
