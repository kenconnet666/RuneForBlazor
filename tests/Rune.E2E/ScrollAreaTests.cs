namespace Rune.E2E;

/// <summary>滚动区域：原生滚动，浮层滚动条由 JS 画；拖动滑块、点轨道翻页；内容超出时视口可聚焦。列表与菜单改用它后照常滚动。</summary>
public class ScrollAreaTests(DemoServer demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 滚动时滑块跟着走_拖动滑块滚动_点轨道翻一页_超出时视口可聚焦(string mode)
    {
        var page = await demo.OpenAsync(mode, "scroll-area");
        var area = page.GetByTestId("inbox");
        var viewport = area.Locator("[data-rune-scroll-viewport]");
        var track = area.Locator("[data-rune-scrollbar]");
        var thumb = track.Locator("> div");

        // 原生滚动条藏起来了，内容超出时视口进 Tab 顺序
        Assert.Equal(0, await viewport.EvaluateAsync<int>("v => v.offsetWidth - v.clientWidth"));
        await Assertions.Expect(viewport).ToHaveAttributeAsync("tabindex", "0");

        await area.HoverAsync();
        await Assertions.Expect(track).ToHaveAttributeAsync("data-state", "visible");
        await page.Mouse.WheelAsync(0, 300);
        await page.WaitForFunctionAsync("v => v.scrollTop > 0", await viewport.ElementHandleAsync());
        var thumbTop = await thumb.EvaluateAsync<double>("t => t.getBoundingClientRect().top - t.parentElement.getBoundingClientRect().top");
        Assert.True(thumbTop > 0);

        // 拖动滑块到底
        var box = await thumb.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到滑块");
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + 1000, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await page.WaitForFunctionAsync("v => Math.abs(v.scrollTop + v.clientHeight - v.scrollHeight) < 2", await viewport.ElementHandleAsync());

        // 点轨道上方的空白：往回翻一页
        var trackBox = await track.BoundingBoxAsync() ?? throw new InvalidOperationException("看不到轨道");
        var before = await viewport.EvaluateAsync<double>("v => v.scrollTop");
        await page.Mouse.ClickAsync(trackBox.X + trackBox.Width / 2, trackBox.Y + 4);
        await page.WaitForFunctionAsync($"v => v.scrollTop < {before} - 100", await viewport.ElementHandleAsync());
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 下拉选项在滚动区域里_键盘移到底时滚进可见范围(string mode)
    {
        var page = await demo.OpenAsync(mode, "select");
        var trigger = page.GetByRole(AriaRole.Combobox).First;
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var list = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToBeInViewportAsync();
        Assert.True(await list.EvaluateAsync<bool>("l => !!l.closest('[data-rune-scroll-area]')"));
    }
}
