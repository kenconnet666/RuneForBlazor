namespace Rune.E2E;

/// <summary>工具栏：整栏一个 Tab 位置，方向键跨过里面切换按钮组的按钮；Tab 进入落在第一项；竖排用上下键。</summary>
public class ToolbarTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 整栏一个Tab位置_方向键跨过切换按钮组_Tab进入落在第一项(string mode)
    {
        var page = await demo.OpenAsync(mode, "toolbar");
        var toolbar = page.GetByTestId("format");

        // 里面的切换按钮组不自己管焦点
        await Assertions.Expect(toolbar.Locator("[role=group][data-rune-roving]")).ToHaveCountAsync(0);
        await Assertions.Expect(toolbar.Locator("[tabindex='0']")).ToHaveCountAsync(1);
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "撤销" })).ToHaveAttributeAsync("tabindex", "0");

        await toolbar.GetByRole(AriaRole.Button, new() { Name = "撤销" }).FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "插入链接" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "右对齐" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(page.GetByTestId("format-summary")).ToContainTextAsync("right");
    }

    [Fact]
    public async Task 竖排工具栏用上下键()
    {
        var page = await demo.OpenAsync("server", "toolbar");
        var tools = page.GetByTestId("tools");
        await tools.GetByRole(AriaRole.Button, new() { Name = "选择" }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(tools.GetByRole(AriaRole.Button, new() { Name = "平移" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(tools.GetByRole(AriaRole.Button, new() { Name = "平移" })).ToBeFocusedAsync();
    }
}
