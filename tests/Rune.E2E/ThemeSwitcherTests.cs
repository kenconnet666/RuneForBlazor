namespace Rune.E2E;

/// <summary>主题切换：选了一套就换整页的主题并记住（刷新、换页都沿用），选中标记与按钮图标跟着变；跟随系统时按系统明暗。</summary>
public class ThemeSwitcherTests(DemoServer demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 选一套主题_整页换掉并记住_跟随系统时按系统明暗(string mode)
    {
        var page = await demo.OpenAsync(mode, "theme-switcher");
        var root = page.Locator("html");
        var switcher = page.GetByTestId("switcher");
        var trigger = page.Locator($"[popovertarget='{await switcher.GetAttributeAsync("id")}']");
        // 示例的效果区（表面色衬底）
        var canvas = page.Locator("[data-demo]").First;

        // 没选过：跟随系统（测试浏览器是亮色）
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "light");
        await Assertions.Expect(switcher.Locator("[data-rune-theme-choice=system]")).ToHaveAttributeAsync("aria-checked", "true");

        await trigger.ClickAsync();
        await switcher.GetByRole(AriaRole.Menuitemradio, new() { Name = "纸张" }).ClickAsync();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "paper");
        await Assertions.Expect(canvas).ToHaveCSSAsync("background-color", "rgb(250, 245, 232)");
        await Assertions.Expect(switcher).Not.ToBeVisibleAsync();
        await Assertions.Expect(switcher.Locator("[data-rune-theme-choice=paper]")).ToHaveAttributeAsync("aria-checked", "true");
        Assert.Equal(
            await switcher.Locator("[data-rune-theme-choice=paper] svg[data-rune-choice-icon]").InnerHTMLAsync(),
            await trigger.Locator("svg").InnerHTMLAsync());

        // 刷新后沿用，页面一出来就是这套（开机脚本）
        await page.ReloadAsync();
        Assert.Equal("paper", await root.GetAttributeAsync("data-theme"));
        // 交互模式接管后重新渲染（弹层的 id 会变）：等它接管完再操作
        if (mode != "static")
            await page.WaitForSelectorAsync("[data-demo-interactive=true]", new() { Timeout = 60_000 });
        await Assertions.Expect(page.GetByTestId("switcher").Locator("[data-rune-theme-choice=paper]")).ToHaveAttributeAsync("aria-checked", "true");

        // 跟随系统：系统改成暗色时跟着变
        await page.Locator($"[popovertarget='{await page.GetByTestId("switcher").GetAttributeAsync("id")}']").ClickAsync();
        await page.GetByTestId("switcher").GetByRole(AriaRole.Menuitemradio, new() { Name = "跟随系统" }).ClickAsync();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "light");
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "dark");
    }
}
