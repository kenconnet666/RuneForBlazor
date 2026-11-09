namespace Rune.E2E;

/// <summary>裁剪图片：封面按 16:9 裁出 1280 × 720；上传头像时 Transform 弹出裁剪，确定后图片墙里是裁好的图，取消就不加。</summary>
public class ImageCropperTests(DemoServer demo) : E2ETest(demo)
{
    // 1 × 1 的红色 PNG：浏览器画在画布上再裁
    static readonly byte[] RedPixel = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==");

    [Fact]
    public async Task 封面裁出指定的像素大小()
    {
        var page = await OpenAsync("server", "image-cropper");
        await page.GetByRole(AriaRole.Button, new() { Name = "裁出 1280 × 720" }).ClickAsync();
        var result = page.GetByAltText("裁好的封面");
        await Assertions.Expect(result).ToHaveJSPropertyAsync("naturalWidth", 1280);
        await Assertions.Expect(result).ToHaveJSPropertyAsync("naturalHeight", 720);
    }

    [Fact]
    public async Task 上传头像时先裁剪_取消就不加()
    {
        var page = await OpenAsync("server", "image-cropper");
        var input = page.Locator("[data-rune-upload-previews] input[type=file]").First;
        var file = new FilePayload { Name = "me.png", MimeType = "image/png", Buffer = RedPixel };

        await input.SetInputFilesAsync(file);
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "裁剪头像" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "取消" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-rune-upload-previews] [role=listitem]")).ToHaveCountAsync(0);

        await input.SetInputFilesAsync(file);
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "使用这张" }).ClickAsync();
        var card = page.Locator("[data-rune-upload-previews] [role=listitem]");
        await Assertions.Expect(card).ToHaveCountAsync(1);
        await Assertions.Expect(card.First).ToHaveAttributeAsync("data-label", "me.png");
        await Assertions.Expect(card.First.Locator("img")).ToHaveAttributeAsync("src", new System.Text.RegularExpressions.Regex("^data:image/png;base64,"));
    }
}
