namespace Rune.E2E;

/// <summary>评分：已选的填充按勾上的 radio 用 CSS 算，悬停预览由 JS 写在星上；再点一次清空要 C#。</summary>
public class RatingTests(DemoServer demo) : E2ETest(demo)
{
    /// <summary>各颗星填色那层的宽度（0、半颗、整颗）。</summary>
    static Task<string[]> Fills(ILocator rating) => rating.EvaluateAsync<string[]>("""
        root => [...root.querySelectorAll(':scope > span')].map(star => {
            const fill = star.firstElementChild.nextElementSibling.getBoundingClientRect().width / star.getBoundingClientRect().width;
            return fill > 0.9 ? 'full' : fill > 0.4 ? 'half' : 'empty';
        })
        """);

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 悬停预览_点选_再点清空(string mode)
    {
        var page = await OpenAsync(mode, "rating");
        var service = page.GetByTestId("service");
        var fourth = service.GetByRole(AriaRole.Radio, new() { Name = "4 星", Exact = true });

        await fourth.Locator("xpath=..").HoverAsync();
        Assert.Equal(["full", "full", "full", "full", "empty"], await Fills(service));
        await fourth.Locator("xpath=..").ClickAsync();
        await Assertions.Expect(fourth).ToBeCheckedAsync();
        await Assertions.Expect(page.GetByTestId("average-text")).ToHaveTextAsync("4");

        // 移开后显示勾上的；再点一次清空
        await page.GetByTestId("average-text").HoverAsync();
        Assert.Equal(["full", "full", "full", "full", "empty"], await Fills(service));
        await fourth.Locator("xpath=..").ClickAsync();
        await Assertions.Expect(fourth).Not.ToBeCheckedAsync();
        await page.GetByTestId("average-text").HoverAsync();
        await Assertions.Expect(page.GetByTestId("average-text")).ToHaveTextAsync("4");
        Assert.Equal(["empty", "empty", "empty", "empty", "empty"], await Fills(service));
    }

    [Fact]
    public async Task 静态SSR下点选_填充按勾上的radio变化()
    {
        var page = await OpenAsync("static", "rating");
        var delivery = page.GetByTestId("delivery");

        Assert.Equal(["full", "full", "full", "empty", "empty"], await Fills(delivery));
        await delivery.GetByRole(AriaRole.Radio, new() { Name = "1.5 星" }).Locator("xpath=..").ClickAsync();
        await page.GetByTestId("average-text").HoverAsync();
        Assert.Equal(["full", "half", "empty", "empty", "empty"], await Fills(delivery));
    }
}
