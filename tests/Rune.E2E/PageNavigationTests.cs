namespace Rune.E2E;

/// <summary>本页目录随滚动高亮当前节、点链接跳过去且不离开本页；顶部加载条延迟出现、完成后淡出，很快完成的不出现。</summary>
public class PageNavigationTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 目录随滚动高亮当前节_点链接不离开本页(string mode)
    {
        var page = await OpenAsync(mode, "nav-list");
        var toc = page.GetByTestId("toc");
        await toc.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(toc.GetByRole(AriaRole.Link, new() { Name = "安装", Exact = true })).ToHaveAttributeAsync("aria-current", "location");

        await page.Locator("#toc-demo-article").EvaluateAsync("el => el.scrollTop = document.getElementById('toc-demo-usage').offsetTop");
        await Assertions.Expect(toc.GetByRole(AriaRole.Link, new() { Name = "使用" })).ToHaveAttributeAsync("aria-current", "location");
        await Assertions.Expect(toc.Locator("[aria-current]")).ToHaveCountAsync(1);

        await toc.GetByRole(AriaRole.Link, new() { Name = "用 NuGet 安装" }).ClickAsync();
        await Assertions.Expect(toc.GetByRole(AriaRole.Link, new() { Name = "用 NuGet 安装" })).ToHaveAttributeAsync("aria-current", "location");
        Assert.EndsWith($"/{mode}/nav-list#toc-demo-nuget", page.Url);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 加载条延迟出现_完成后淡出_出错变危险色(string mode)
    {
        var page = await OpenAsync(mode, "loading-bar");
        var bar = page.GetByTestId("bar");
        await page.GetByRole(AriaRole.Tab, new() { Name = "库存台账" }).ClickAsync();
        await Assertions.Expect(bar).ToHaveAttributeAsync("data-state", "loading");
        await Assertions.Expect(bar).Not.ToHaveAttributeAsync("aria-hidden", "true");
        await Assertions.Expect(bar).ToHaveAttributeAsync("data-state", "idle", new() { Timeout = 5000 });

        await page.GetByRole(AriaRole.Tab, new() { Name = "财务报表" }).ClickAsync();
        await Assertions.Expect(bar).ToHaveAttributeAsync("data-state", "error", new() { Timeout = 5000 });

        // 120ms 就好：一直不出现。Server 模式还要加上往返的时间，机器慢时会超过 200ms 的延迟，只在 WebAssembly 下断言
        await Assertions.Expect(bar).ToHaveAttributeAsync("data-state", "idle", new() { Timeout = 5000 });
        if (mode != "wasm") return;
        await page.GetByRole(AriaRole.Tab, new() { Name = "个人设置" }).ClickAsync();
        for (var i = 0; i < 6; i++)
        {
            Assert.Equal("idle", await bar.GetAttributeAsync("data-state"));
            await page.WaitForTimeoutAsync(50);
        }
    }
}
