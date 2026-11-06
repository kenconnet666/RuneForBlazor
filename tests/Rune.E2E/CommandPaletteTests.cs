namespace Rune.E2E;

/// <summary>命令面板：快捷键打开、高亮第一项、输入过滤、方向键与回车执行、不可用项跳过；自定义过滤与远程结果。</summary>
public class CommandPaletteTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 快捷键打开_过滤后回车执行(string mode)
    {
        var page = await OpenAsync(mode, "command-palette");
        await page.Keyboard.PressAsync("Control+j");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "命令" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        var input = dialog.GetByRole(AriaRole.Combobox);
        await Assertions.Expect(input).ToBeFocusedAsync();
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-activedescendant", new System.Text.RegularExpressions.Regex("-0$"));

        // 说明也参与匹配：“库存”只在“报表”的说明里
        await input.PressSequentiallyAsync("库存");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("last")).ToContainTextAsync("执行了：报表");

        // 再打开时从空白开始；方向键移到“设置”后再往下停住（“退出登录”不可用不能执行）
        await page.GetByTestId("commands").Locator("..").GetByRole(AriaRole.Button).First.ClickAsync();
        await Assertions.Expect(input).ToHaveValueAsync("");
        await input.PressSequentiallyAsync("设置");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId("last")).ToContainTextAsync("执行了：设置");
    }

    [Fact]
    public async Task 自定义过滤按邮箱搜_自定义触发按钮()
    {
        var page = await OpenAsync("server", "command-palette");
        await page.GetByRole(AriaRole.Button, new() { Name = "提及成员" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "提及成员" });
        await dialog.GetByRole(AriaRole.Combobox).PressSequentiallyAsync("HAORAN");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Option).ClickAsync();
        await Assertions.Expect(page.GetByText("已提及 @赵浩然（前端）")).ToBeVisibleAsync();
    }
}
