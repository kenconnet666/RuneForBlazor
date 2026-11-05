namespace Rune.E2E;

/// <summary>按钮组：组里的按钮照常点击；放进工具栏时方向键跨过各组。</summary>
public class ButtonGroupTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 翻页组照常点击_工具栏里方向键跨过各组(string mode)
    {
        var page = await demo.OpenAsync(mode, "button");
        await page.GetByRole(AriaRole.Button, new() { Name = "下一页" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("page")).ToHaveTextAsync("第 4 页");

        var toolbar = page.GetByRole(AriaRole.Toolbar, new() { Name = "编辑器工具栏" });
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "重做" }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "加粗" })).ToBeFocusedAsync();
        await Assertions.Expect(toolbar.Locator("[tabindex='0']")).ToHaveCountAsync(1);
    }
}
