namespace Rune.E2E;

/// <summary>拖动排序：键盘拿起、挪动、放下（焦点留在把手上），指针拖动松开后换位，拖动中 Esc 取消。顺序由 C# 写回（页面上的文字跟着变）。</summary>
public class SortableTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘拿起挪两位放下_焦点留在把手上(string mode)
    {
        var page = await OpenAsync(mode, "sortable");
        var order = page.GetByTestId("order");
        var handle = page.GetByRole(AriaRole.Button, new() { Name = "移动 评审首页改版设计稿" });
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(order).ToContainTextAsync("invoice → review → interview");
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(order).ToContainTextAsync("invoice → interview → review");
        await Assertions.Expect(handle).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-pressed", "false");

        // Esc：回到拿起时的位置
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("Home");
        await Assertions.Expect(order).ToContainTextAsync("review → invoice → interview");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(order).ToContainTextAsync("invoice → interview → review");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 拖到第一位松开后换位_拖动中Esc不动(string mode)
    {
        var page = await OpenAsync(mode, "sortable");
        var order = page.GetByTestId("order");
        var first = await page.GetByRole(AriaRole.Button, new() { Name = "移动 评审首页改版设计稿" }).BoundingBoxAsync();
        var third = await page.GetByRole(AriaRole.Button, new() { Name = "移动 面试前端候选人" }).BoundingBoxAsync();

        await page.Mouse.MoveAsync(third!.X + third.Width / 2, third.Y + third.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(first!.X + first.Width / 2, first.Y - 4, new() { Steps = 8 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(order).ToContainTextAsync("interview → review → invoice");
        // 顺序跟上后不留位移
        await Assertions.Expect(page.Locator("[data-rune-sortable-item][style*=transform]")).ToHaveCountAsync(0);

        var docs = await page.GetByRole(AriaRole.Button, new() { Name = "移动 补充接口文档" }).BoundingBoxAsync();
        await page.Mouse.MoveAsync(docs!.X + docs.Width / 2, docs.Y + docs.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(first.X + first.Width / 2, first.Y - 4, new() { Steps = 8 });
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
        await Assertions.Expect(order).ToContainTextAsync("interview → review → invoice → docs → weekly");
    }
}
