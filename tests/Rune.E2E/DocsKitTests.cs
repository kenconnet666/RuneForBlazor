namespace Rune.E2E;

/// <summary>文档站组件：示例卡片展开源码时上色；色块显示 token 在当前主题下的取值，换主题时跟着变。</summary>
public class DocsKitTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 示例卡片展开源码_上色_再收起(string mode)
    {
        var page = await demo.OpenAsync(mode, "code");
        var preview = page.GetByTestId("preview");
        var toggle = preview.GetByRole(AriaRole.Button, new() { Name = "显示代码" });

        await toggle.ClickAsync();
        var hide = preview.GetByRole(AriaRole.Button, new() { Name = "隐藏代码" });
        await Assertions.Expect(hide).ToHaveAttributeAsync("aria-expanded", "true");
        var code = preview.Locator("code[data-rune-highlight]");
        await Assertions.Expect(code).ToBeVisibleAsync();
        // Shiki 上色后文字换成带颜色的 span
        await Assertions.Expect(code.Locator("span[style]").First).ToBeAttachedAsync();
        await Assertions.Expect(page.GetByText("源码已展开")).ToBeVisibleAsync();

        await hide.ClickAsync();
        await Assertions.Expect(code).Not.ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 色块显示token的取值_换主题后跟着变(string mode)
    {
        var page = await demo.OpenAsync(mode, "color-field");
        var surface = page.GetByTestId("swatches").Locator("[data-rune-token-value='--rune-colors-surface']");
        await Assertions.Expect(surface).ToHaveTextAsync("#fcfcfd");

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Assertions.Expect(surface).ToHaveTextAsync("#18191b");
    }
}
