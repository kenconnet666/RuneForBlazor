namespace Rune.E2E;

/// <summary>颜色输入：输入任意写法回车后规范成 #hex；写不成颜色时标无效、失焦后恢复；原生取色器选色写回。</summary>
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

        // 原生取色器：改值后发 input（对话框没法在测试里操作）
        var picker = page.Locator("[data-rune-control]").Filter(new() { Has = field }).Locator("input[type=color]");
        await picker.EvaluateAsync("p => { p.value = '#00ff00'; p.dispatchEvent(new Event('input', { bubbles: true })); }");
        await Assertions.Expect(value).ToHaveTextAsync("#00ff00");
    }
}
