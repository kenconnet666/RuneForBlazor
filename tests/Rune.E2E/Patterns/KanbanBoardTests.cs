namespace Rune.E2E;

/// <summary>
/// 看板：键盘拿起、换列、放下（焦点回到新列里的把手），不能放的列弹回并说明原因；指针拖到别的列松开后换列，拖动中 Esc 不动。
/// 卡片换列由示例改数据完成（页面上的提示跟着变）。
/// </summary>
public class KanbanBoardTests(DemoServer demo) : E2ETest(demo)
{
    static ILocator Column(IPage page, string name) => page.Locator("[data-rune-kanban-column]").Filter(new() { Has = page.GetByText(name, new() { Exact = true }) });

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘拿起换列放下_不能放的列弹回(string mode)
    {
        var page = await OpenAsync(mode, "kanban-board");
        var handle = page.GetByRole(AriaRole.Button, new() { Name = "移动 T-101 登录页验证码不刷新" });
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Column(page, "处理中")).ToHaveAttributeAsync("data-drop", "allowed");
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(page.GetByText("T-101 移到了“处理中”第 1 位")).ToBeVisibleAsync();
        await Assertions.Expect(Column(page, "处理中").Locator("[data-rune-kanban-card=\"T-101\"]")).ToHaveCountAsync(1);
        await Assertions.Expect(handle).ToBeFocusedAsync();

        // 处理中 → 已完成隔了一列：标红、写出原因，放下时弹回
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Column(page, "已完成")).ToHaveAttributeAsync("data-drop", "denied");
        await Assertions.Expect(Column(page, "已完成").Locator("[data-rune-kanban-reason]")).ToHaveTextAsync("只能挪到相邻的一列");
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(page.GetByText("T-101 没有移动：只能挪到相邻的一列")).ToBeVisibleAsync();
        await Assertions.Expect(Column(page, "处理中").Locator("[data-rune-kanban-card=\"T-101\"]")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("[data-drop]")).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 拖到别的列松开后换列_拖动中Esc不动(string mode)
    {
        var page = await OpenAsync(mode, "kanban-board");
        var card = page.Locator("[data-rune-kanban-card=\"T-102\"]");
        var target = await Column(page, "处理中").Locator("ul").BoundingBoxAsync();
        var from = await card.BoundingBoxAsync();

        await page.Mouse.MoveAsync(from!.X + from.Width / 2, from.Y + 12);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(target!.X + target.Width / 2, target.Y + 4, new() { Steps = 10 });
        // 影子卡跟着指针，插入线在落点
        await Assertions.Expect(page.Locator("[data-rune-kanban-ghost][data-shown]")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("[data-rune-kanban-indicator][data-shown]")).ToHaveCountAsync(1);
        await page.Mouse.UpAsync();
        await Assertions.Expect(page.GetByText("T-102 移到了“处理中”第 1 位")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-rune-kanban-ghost][data-shown]")).ToHaveCountAsync(0);

        var other = await page.Locator("[data-rune-kanban-card=\"T-104\"]").BoundingBoxAsync();
        await page.Mouse.MoveAsync(other!.X + other.Width / 2, other.Y + 12);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(target.X + target.Width / 2, target.Y + 4, new() { Steps = 10 });
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
        await Assertions.Expect(Column(page, "待处理").Locator("[data-rune-kanban-card=\"T-104\"]")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("[data-rune-kanban-card][data-dragging]")).ToHaveCountAsync(0);
    }
}
