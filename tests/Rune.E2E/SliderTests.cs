namespace Rune.E2E;

/// <summary>滑块：拖动、点轨道、键盘在 JS 里算并当场画出来，值经 rune-slide 交给 C#；静态 SSR 下隐藏 input 跟着变。</summary>
public class SliderTests(DemoServer demo)
{
    static ILocator Thumbs(IPage page, string testId) => page.GetByTestId(testId);

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 拖动范围的低端_实时写回_松开时提交_不能越过高端(string mode)
    {
        var page = await demo.OpenAsync(mode, "slider");
        var thumbs = Thumbs(page, "price");
        var summary = page.GetByTestId("price-summary");
        var low = await thumbs.Nth(0).BoundingBoxAsync() ?? throw new InvalidOperationException("看不到滑块");
        var high = await thumbs.Nth(1).BoundingBoxAsync() ?? throw new InvalidOperationException("看不到滑块");
        var y = low.Y + low.Height / 2;

        await page.Mouse.MoveAsync(low.X + low.Width / 2, y);
        await page.Mouse.DownAsync();
        // 轨道 0–5000，从 800 往右拖一段
        await page.Mouse.MoveAsync(low.X + low.Width / 2 + 80, y, new() { Steps = 8 });
        await Assertions.Expect(summary).Not.ToContainTextAsync("正在看 ¥800–");
        await Assertions.Expect(summary).ToContainTextAsync("已查询 ¥800–¥3,500");
        // 拖过高端：停在高端
        await page.Mouse.MoveAsync(high.X + high.Width + 100, y, new() { Steps = 8 });
        await page.Mouse.UpAsync();
        await Assertions.Expect(summary).ToContainTextAsync("正在看 ¥3,500–¥3,500；已查询 ¥3,500–¥3,500");
        await Assertions.Expect(thumbs.Nth(0)).ToBeFocusedAsync();
        await Assertions.Expect(thumbs.Nth(0)).ToHaveAttributeAsync("aria-valuetext", "¥3,500");

        // 键盘：Home 到能取的最小值，高端的取值范围跟着变
        await page.Keyboard.PressAsync("Home");
        await Assertions.Expect(summary).ToContainTextAsync("正在看 ¥0–¥3,500；已查询 ¥0–¥3,500");
        await Assertions.Expect(thumbs.Nth(1)).ToHaveAttributeAsync("aria-valuemin", "0");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 点轨道跳过去_方向键与PageUp_竖排上键增大_预设改值后跟着变(string mode)
    {
        var page = await demo.OpenAsync(mode, "slider");
        var position = Thumbs(page, "position");
        var elapsed = page.GetByTestId("elapsed");
        var track = position.Locator("xpath=..");
        var box = await track.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到轨道");

        // 点在轨道中间：0–245 的一半
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Assertions.Expect(elapsed).ToHaveTextAsync("2:03");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("PageUp");
        // 大步长默认是范围的十分之一（24.5）：124 + 24.5 对齐到步长 1，正好一半往大取
        await Assertions.Expect(position).ToHaveAttributeAsync("aria-valuenow", "149");
        await Assertions.Expect(elapsed).ToHaveTextAsync("2:29");

        var band = page.GetByTestId("bands").GetByRole(AriaRole.Slider).First;
        await band.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(band).ToHaveAttributeAsync("aria-valuetext", "+9 dB");
        await page.GetByRole(AriaRole.Group, new() { Name = "预设" }).GetByRole(AriaRole.Button, new() { Name = "平直" }).ClickAsync();
        await Assertions.Expect(band).ToHaveAttributeAsync("aria-valuenow", "0");
    }

    [Fact]
    public async Task 静态SSR下键盘改值_隐藏input跟着变_随表单提交()
    {
        var page = await demo.OpenAsync("static", "slider");
        var form = page.GetByTestId("slider-form");
        var thumb = form.GetByRole(AriaRole.Slider).First;

        await thumb.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("PageUp");
        await Assertions.Expect(thumb).ToHaveAttributeAsync("aria-valuenow", "71");
        Assert.Equal("71", await form.EvaluateAsync<string>("form => new FormData(form).get('volume')"));
        // 不可用的滑块不能聚焦
        await Assertions.Expect(form.GetByRole(AriaRole.Slider).Nth(1)).Not.ToHaveAttributeAsync("tabindex", "0");
    }
}
