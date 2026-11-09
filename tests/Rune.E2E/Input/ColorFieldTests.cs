namespace Rune.E2E;

/// <summary>颜色输入：输入任意写法回车后规范成 #hex；写不成颜色时标无效、失焦后恢复；色块点开取色器，输入、点色板写回。</summary>
public class ColorFieldTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 输入写法规范成hex_无效时恢复_取色器写回(string mode)
    {
        var page = await OpenAsync(mode, "color-field");
        var field = page.GetByTestId("any");
        var value = page.GetByTestId("any-value");

        await field.FillAsync("rgb(255 99 71)");
        await field.PressAsync("Enter");
        await Assertions.Expect(value).ToHaveTextAsync("#ff6347");
        await Assertions.Expect(field).ToHaveValueAsync("#ff6347");

        await field.FillAsync("tomatoes");
        await Assertions.Expect(field).ToHaveAttributeAsync("aria-invalid", "true");
        await field.BlurAsync();
        await Assertions.Expect(field).ToHaveValueAsync("#ff6347");
        await Assertions.Expect(field).Not.ToHaveAttributeAsync("aria-invalid", "true");

        // 色块点开取色器：在输入框里写颜色，实时写回
        await page.Locator("[data-rune-control]").Filter(new() { Has = field }).Locator("button[data-part=swatch]").ClickAsync();
        var picker = page.Locator("[data-scope=color-picker][data-part=root]").First;
        await Assertions.Expect(picker).ToBeVisibleAsync();
        var hex = picker.GetByRole(AriaRole.Textbox);
        await hex.FillAsync("#00ff00");
        await hex.PressAsync("Enter");
        await Assertions.Expect(value).ToHaveTextAsync("#00ff00");
        // 色板：点中间右上，饱和度、明度都高
        var area = picker.Locator("[data-color-area]");
        var box = (await area.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + box.Width * 0.9f, box.Y + box.Height * 0.1f);
        await Assertions.Expect(value).Not.ToHaveTextAsync("#00ff00");
    }
}
