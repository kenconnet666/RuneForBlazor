using System.Text.RegularExpressions;

namespace Rune.E2E;

/// <summary>
/// 图片与图片预览：加载失败露出占位；组里从点的那张打开、左右切换、Esc 关上焦点回到缩略图；
/// 单张图片由 JS 直接打开（静态 SSR 也能看），缩放（按钮、键盘、滚轮、双击）与旋转都在浏览器里。
/// </summary>
public class ImageTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 组里从第二张打开_左右切换_Esc关上焦点回到缩略图(string mode)
    {
        var page = await OpenAsync(mode, "image");
        var thumbnail = page.GetByRole(AriaRole.Button, new() { Name = "查看大图：检修口盖板（DWG-1034）" });
        await thumbnail.ClickAsync();
        var dialog = page.Locator("dialog[open]");
        await Assertions.Expect(dialog.Locator("[aria-live=polite]")).ToHaveTextAsync("第 2 张，共 6 张");
        // 打开时焦点在图片区，名称是这张图的 Alt；整张放进视口（不超过原大）
        await Assertions.Expect(dialog.GetByRole(AriaRole.Img, new() { Name = "检修口盖板（DWG-1034）" })).ToBeFocusedAsync();
        await Assertions.Expect(dialog.Locator("[data-rune-lightbox-zoom]")).Not.ToHaveTextAsync("100%");

        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(dialog.Locator("[aria-live=polite]")).ToHaveTextAsync("第 3 张，共 6 张");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "上一张" }).ClickAsync();
        await Assertions.Expect(dialog.Locator("[aria-live=polite]")).ToHaveTextAsync("第 2 张，共 6 张");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator("dialog[open]")).ToHaveCountAsync(0);
        await Assertions.Expect(thumbnail).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 单张图片直接打开_缩放旋转还原都在浏览器里(string mode)
    {
        var page = await OpenAsync(mode, "image");
        await page.GetByRole(AriaRole.Button, new() { Name = "查看大图：显示器支架（Cover）" }).ClickAsync();
        var dialog = page.Locator("dialog[open]");
        var zoom = dialog.Locator("[data-rune-lightbox-zoom]");
        var image = dialog.Locator("img[data-rune-lightbox-image]");
        await Assertions.Expect(image).ToHaveAttributeAsync("style", new Regex("width: 1200px"));
        var fitted = await zoom.TextContentAsync();
        Assert.Empty(await dialog.Locator("[data-rune-lightbox-step]").AllAsync());

        // 键盘放大一档、按钮缩小回去
        await page.Keyboard.PressAsync("+");
        await Assertions.Expect(zoom).Not.ToHaveTextAsync(fitted!);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "缩小" }).ClickAsync();
        await Assertions.Expect(zoom).ToHaveTextAsync(fitted!);

        // 滚轮以指针为中心缩放；双击（已放大）放回视口
        var viewport = dialog.Locator("[data-rune-lightbox-viewport]");
        var box = await viewport.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.WheelAsync(0, -300);
        await Assertions.Expect(zoom).Not.ToHaveTextAsync(fitted!);
        await viewport.DblClickAsync();
        await Assertions.Expect(zoom).ToHaveTextAsync(fitted!);

        // 旋转 90°：横的图竖过来，重新放进视口；0 还原
        await page.Keyboard.PressAsync("r");
        await Assertions.Expect(image).ToHaveAttributeAsync("style", new Regex("rotate\\(90deg\\)"));
        await Assertions.Expect(dialog.Locator("[data-rune-lightbox-content]")).ToHaveAttributeAsync("style", new Regex("width: 800px; height: 1200px"));
        await page.Keyboard.PressAsync("0");
        await Assertions.Expect(image).ToHaveAttributeAsync("style", new Regex("rotate\\(0deg\\)"));

        await dialog.GetByRole(AriaRole.Button, new() { Name = "关闭" }).ClickAsync();
        await Assertions.Expect(page.Locator("dialog[open]")).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("static")]
    [InlineData("wasm")]
    public async Task 地址错了时露出占位_名称是Alt(string mode)
    {
        var page = await OpenAsync(mode, "image");
        await Assertions.Expect(page.GetByTestId("missing")).ToHaveAttributeAsync("data-state", "error");
        await Assertions.Expect(page.GetByRole(AriaRole.Img, new() { Name = "不存在的图纸" })).ToBeVisibleAsync();
    }
}
