namespace Rune.E2E;

/// <summary>分栏：方向键、Home / End、拖动在 JS 里当场改比例，结束时交给 C#；Enter 收起、展开可收起的一侧；双击回到初始比例。</summary>
public class SplitterTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘与拖动调比例_Enter收起_双击复位(string mode)
    {
        var page = await OpenAsync(mode, "splitter");
        var handle = page.GetByRole(AriaRole.Separator).First;
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "40");

        await handle.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "45");
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "80");

        // 收起右栏：它的格子宽度变成 0，C# 也知道了（按钮的名称换成“展开”）
        await page.Keyboard.PressAsync("Enter");
        var splitter = page.Locator("[data-rune-splitter]").First;
        await Assertions.Expect(splitter).ToHaveAttributeAsync("data-collapsed", "end");
        await Assertions.Expect(splitter.Locator("[id$=-end]")).ToHaveAttributeAsync("inert", "");

        // 拖动即展开（在分隔条上部按下：正中间叠着收起按钮）；双击回到初始比例
        var box = (await handle.BoundingBoxAsync())!;
        var y = box.Y + box.Height / 4;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X - 100, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(splitter).Not.ToHaveAttributeAsync("data-collapsed", new System.Text.RegularExpressions.Regex(".*"));
        Assert.True(double.Parse((await handle.GetAttributeAsync("aria-valuenow"))!, System.Globalization.CultureInfo.InvariantCulture) <= 80);

        await handle.DblClickAsync(new() { Position = new() { X = box.Width / 2, Y = box.Height / 4 } });
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "40");
    }
}
