namespace Rune.E2E;

/// <summary>代码块：三种渲染模式下都由 JS 用 Shiki 上色；关掉高亮回到纯文本、再打开重新上色；长代码展开收起。</summary>
public class CodeBlockTests(DemoServer demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 上色后文字不变_关掉高亮回到纯文本(string mode)
    {
        var page = await demo.OpenAsync(mode, "code");
        var program = page.GetByTestId("program").Locator("code");
        // 上色后每行文字在 span 里，关键字的颜色来自主题的代码颜色 token
        await Assertions.Expect(program.Locator("[data-rune-code-text] span").First).ToBeVisibleAsync();
        await Assertions.Expect(program).ToContainTextAsync("builder.Services.AddRuneUI");
        var keyword = program.Locator("[data-rune-code-text] span").Filter(new() { HasTextString = "var" }).First;
        Assert.Contains("--shiki-token-keyword", await keyword.GetAttributeAsync("style") ?? "");
        if (mode == "static") return;

        var curl = page.GetByTestId("curl").Locator("code");
        await Assertions.Expect(curl.Locator("span").First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Switch, new() { Name = "语法高亮" }).ClickAsync();
        await Assertions.Expect(curl.Locator("span")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Switch, new() { Name = "语法高亮" }).ClickAsync();
        await Assertions.Expect(curl.Locator("span").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task 长代码折叠_展开后看得到最后一行()
    {
        var page = await demo.OpenAsync("server", "code");
        var block = page.GetByTestId("long");
        var last = block.GetByText("Name = \"项目 40\"");
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "展开全部" });
        await Assertions.Expect(last).Not.ToBeInViewportAsync();
        await toggle.ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "收起" })).ToHaveAttributeAsync("aria-expanded", "true");
        await last.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(last).ToBeInViewportAsync();
    }
}
