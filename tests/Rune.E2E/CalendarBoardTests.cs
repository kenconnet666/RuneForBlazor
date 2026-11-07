namespace Rune.E2E;

/// <summary>整页月历：跨天日程铺在每一天，每天最多三条、其余收成“还有 N 项”；点格子空白处选中；方向键在日期之间走。</summary>
public class CalendarBoardTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 日程_选中_方向键(string mode)
    {
        var page = await OpenAsync(mode, "calendar-board");
        var board = page.Locator("[data-rune-calendar][aria-label=\"项目日程\"]");

        // 出差 14–16 日：三天都有；19 日四条只显示三条
        foreach (var day in new[] { 14, 15, 16 })
            await Assertions.Expect(board.Locator($"td[data-board-cell=\"2026-10-{day}\"]")).ToContainTextAsync("杭州工厂审核");
        await Assertions.Expect(board.Locator("td[data-board-cell=\"2026-10-19\"] li")).ToHaveCountAsync(4);
        await Assertions.Expect(board.GetByRole(AriaRole.Button, new() { Name = "还有 1 项" })).ToBeVisibleAsync();

        // 点 12 日格子的空白处：选中，右边列出那天的两条
        await board.Locator("td[data-board-cell=\"2026-10-12\"]").ClickAsync(new() { Position = new() { X = 60, Y = 90 } });
        await Assertions.Expect(board.Locator("td[data-board-cell=\"2026-10-12\"]")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(page.GetByText("14:00 供应商比价评审", new() { Exact = true }).Last).ToBeVisibleAsync();

        // 方向键：焦点在日期按钮之间走，Enter 选中
        await board.Locator("button[data-date=\"2026-10-12\"]").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.Locator(":focus")).ToHaveAttributeAsync("data-date", "2026-10-19");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(board.Locator("td[data-board-cell=\"2026-10-19\"]")).ToHaveAttributeAsync("aria-selected", "true");
    }
}
