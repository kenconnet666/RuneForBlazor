using System.Text.RegularExpressions;

namespace Rune.E2E;

/// <summary>
/// 上传的图片墙：选的图片用本地地址马上显示、上传完成、按墙上的顺序预览，移除后本地地址收回；
/// 粘贴截图加进来；拖动或键盘调整顺序（网格），顺序由 C# 写回（封面跟着变）。
/// </summary>
public class UploadTests(DemoServer demo) : E2ETest(demo)
{
    // 1 × 1 的 PNG
    static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 选的图片马上显示_按墙上的顺序预览_移除后本地地址收回(string mode)
    {
        var page = await OpenAsync(mode, "upload");
        var wall = page.Locator("[data-rune-upload-previews]");
        await wall.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "现场.png", MimeType = "image/png", Buffer = Png });

        var card = wall.Locator("[role=listitem]").Nth(3);
        var thumbnail = card.Locator("img[data-rune-image]");
        await Assertions.Expect(thumbnail).ToHaveAttributeAsync("src", new Regex("^blob:"));
        await Assertions.Expect(thumbnail).ToHaveAttributeAsync("data-state", "loaded");
        await Assertions.Expect(card).ToHaveAttributeAsync("data-state", "done");
        var url = await thumbnail.GetAttributeAsync("src");
        await Assertions.Expect(wall).ToHaveAttributeAsync("data-rune-upload-previews", url!);

        await page.GetByRole(AriaRole.Button, new() { Name = "查看大图：现场.png" }).ClickAsync();
        var dialog = page.Locator("dialog[open]");
        await Assertions.Expect(dialog.Locator("[aria-live=polite]")).ToHaveTextAsync("第 4 张，共 4 张");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator("dialog[open]")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "移除 现场.png" }).ClickAsync();
        await Assertions.Expect(wall).ToHaveAttributeAsync("data-rune-upload-previews", "");
        // 收回之后这个地址取不到了
        await page.WaitForFunctionAsync("url => fetch(url).then(() => false, () => true)", url);
    }

    [Fact]
    public async Task 粘贴截图加进来()
    {
        var page = await OpenAsync("server", "upload");
        var wall = page.Locator("[data-rune-upload-previews]");
        await wall.Locator("input[type=file]").FocusAsync();
        await page.EvaluateAsync("""
            bytes => {
              const transfer = new DataTransfer();
              transfer.items.add(new File([new Uint8Array(bytes)], '截图.png', { type: 'image/png' }));
              document.activeElement.dispatchEvent(new ClipboardEvent('paste', { clipboardData: transfer, bubbles: true, cancelable: true }));
            }
            """, Png.Select(b => (int)b).ToArray());
        await Assertions.Expect(wall.Locator("[role=listitem]")).ToHaveCountAsync(4);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "查看大图：截图.png" })).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 拖到第一格换封面_键盘移回去(string mode)
    {
        var page = await OpenAsync(mode, "upload");
        var cover = page.GetByTestId("cover");
        var first = await page.Locator("[data-rune-upload-previews] [role=listitem]").First.BoundingBoxAsync();
        var handle = page.GetByRole(AriaRole.Button, new() { Name = "移动 机柜导轨.svg" });
        var grip = await handle.BoundingBoxAsync();

        await page.Mouse.MoveAsync(grip!.X + grip.Width / 2, grip.Y + grip.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(first!.X + first.Width / 2, first.Y + first.Height / 2, new() { Steps = 10 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(cover).ToHaveTextAsync("封面：机柜导轨.svg");
        // 顺序跟上后不留位移
        await Assertions.Expect(page.Locator("[data-rune-sortable-item][style*=transform]")).ToHaveCountAsync(0);

        await handle.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(cover).ToHaveTextAsync("封面：显示器支架.svg");
        await Assertions.Expect(handle).ToBeFocusedAsync();
    }
}
