namespace Rune.E2E;

/// <summary>拼图滑块验证：键盘挪到终点后松手交出结果，指针拖动后松手交出结果（位置随滑块走、轨迹有多个点）。</summary>
public class SlideCaptchaTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键盘挪动后松手_指针拖动后松手(string mode)
    {
        var page = await OpenAsync(mode, "slide-captcha");
        var slider = page.GetByRole(AriaRole.Slider);
        // 题目在交互开始后才出
        await Assertions.Expect(slider).ToHaveAttributeAsync("tabindex", "0");

        await slider.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(slider).ToHaveAttributeAsync("aria-valuenow", "4");
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(slider).ToHaveAttributeAsync("aria-valuenow", "263");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByText(new System.Text.RegularExpressions.Regex(@"^位置 263px"))).ToBeVisibleAsync();

        // 没对准：过一会儿换一张新图，滑块回到起点
        await Assertions.Expect(slider).ToHaveAttributeAsync("aria-valuenow", "0", new() { Timeout = 5000 });
        await Assertions.Expect(slider).ToHaveAttributeAsync("tabindex", "0");

        // 滑块回到起点有过渡：悬停会等它停稳
        await slider.HoverAsync();
        var box = await slider.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2 + 80, box.Y + box.Height / 2, new() { Steps = 12 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(page.GetByText(new System.Text.RegularExpressions.Regex(@"^位置 \d+px.*轨迹 \d+ 点$"))).ToBeVisibleAsync();
        var now = int.Parse(await slider.GetAttributeAsync("aria-valuenow") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(now, 60, 120);
    }
}
