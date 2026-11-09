namespace Rune.E2E;

/// <summary>手写签名：拖一笔就有了值（隐藏输入框与 C# 都拿到 PNG），撤销后回到没签。</summary>
public class SignaturePadTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("static")]
    public async Task 拖一笔就有值_撤销后没签(string mode)
    {
        var page = await OpenAsync(mode, "signature-pad");
        var pad = page.Locator("[data-rune-signature]").First;
        var canvas = pad.Locator("canvas");
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(box.X + 40, box.Y + 40);
        await page.Mouse.DownAsync();
        for (var step = 1; step <= 10; step++) await page.Mouse.MoveAsync(box.X + 40 + step * 25, box.Y + 40 + step * 6);
        await page.Mouse.UpAsync();

        await Assertions.Expect(pad).ToHaveAttributeAsync("data-signed", "");
        await Assertions.Expect(pad.Locator("input[type=hidden]")).ToHaveValueAsync(new System.Text.RegularExpressions.Regex("^data:image/png;base64,"));
        // 交互模式下值回到 C#：签了才能确认
        if (mode == "server") await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "确认签收" })).ToBeEnabledAsync();

        await pad.Locator("[data-signature-action=undo]").ClickAsync();
        await Assertions.Expect(pad).Not.ToHaveAttributeAsync("data-signed", "");
        await Assertions.Expect(pad.Locator("input[type=hidden]")).ToHaveValueAsync("");
    }
}
