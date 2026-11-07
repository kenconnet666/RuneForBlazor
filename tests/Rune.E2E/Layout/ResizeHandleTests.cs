namespace Rune.E2E;

/// <summary>宽度调整手柄：拖动、方向键、双击在 JS 里当场改父元素的宽度，结束时交给 C#；抽屉靠正文的一边可以拖宽。</summary>
public class ResizeHandleTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 拖动改宽度_夹在范围内_方向键与双击_松开后交给CSharp(string mode)
    {
        var page = await OpenAsync(mode, "resize-handle");
        var panel = page.GetByTestId("panel");
        var handle = page.GetByTestId("handle");
        var width = page.GetByTestId("width").Locator("dd");
        var box = await handle.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到手柄");

        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2 + 60, box.Y + box.Height / 2, new() { Steps = 6 });
        Assert.Equal(280, Math.Round(await panel.EvaluateAsync<double>("p => p.getBoundingClientRect().width")));
        await page.Mouse.MoveAsync(box.X + 400, box.Y + box.Height / 2, new() { Steps = 6 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(width).ToHaveTextAsync("360 px");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "360");

        await handle.FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(width).ToHaveTextAsync("344 px");
        await handle.DblClickAsync();
        await Assertions.Expect(width).ToHaveTextAsync("220 px");
    }

    [Fact]
    public async Task 抽屉靠正文的一边可以拖宽_按键调整()
    {
        var page = await OpenAsync("server", "drawer");
        await page.GetByRole(AriaRole.Button, new() { Name = "编辑", Exact = true }).ClickAsync();
        var drawer = page.GetByTestId("edit-drawer");
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        var handle = drawer.GetByRole(AriaRole.Separator);
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "560");

        await handle.FocusAsync();
        // 结束边的抽屉：手柄在起始边，← 是变宽
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "576");
        Assert.Equal(576, Math.Round(await drawer.EvaluateAsync<double>("d => d.getBoundingClientRect().width")));
    }
}
