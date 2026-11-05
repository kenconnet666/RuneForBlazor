namespace Rune.E2E;

/// <summary>组合框与自动完成：焦点一直在输入框里，开关与高亮由 JS 初始化器处理，过滤、选中、提交由 C# 处理。</summary>
public class ComboboxTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 输入即过滤并高亮第一项_Enter选中后填入文字并关闭(string mode)
    {
        var page = await demo.OpenAsync(mode, "combobox");
        var input = page.GetByTestId("city");
        var list = page.Locator($"#{await input.GetAttributeAsync("aria-controls")}");

        await input.FillAsync("州");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(5);
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "广州");
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-expanded", "true");

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(input).ToHaveValueAsync("杭州");
        await Assertions.Expect(page.GetByTestId("basic-summary")).ToContainTextAsync("城市：杭州");
        await Assertions.Expect(input).ToBeFocusedAsync();

        // ↓ 打开时显示全部选项
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(30);
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "杭州");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 失焦时文字不是选项就恢复_Esc先关闭再清空(string mode)
    {
        var page = await demo.OpenAsync(mode, "combobox");
        var input = page.GetByTestId("city");
        var list = page.Locator($"#{await input.GetAttributeAsync("aria-controls")}");

        await input.FillAsync("西安");
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "西安");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId("basic-summary")).ToContainTextAsync("城市：西安");

        await input.FillAsync("不存在的城市");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(input).ToHaveValueAsync("西安");

        await input.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(input).ToHaveValueAsync("西安");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(input).ToHaveValueAsync("");
        await Assertions.Expect(page.GetByTestId("basic-summary")).ToContainTextAsync("城市：未选");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 远程搜索_输入停下后请求_选中结果(string mode)
    {
        var page = await demo.OpenAsync(mode, "combobox");
        var input = page.GetByTestId("member");
        var list = page.Locator($"#{await input.GetAttributeAsync("aria-controls")}");

        await input.FillAsync("chen");
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(1);
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "chenmo");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId("basic-summary")).ToContainTextAsync("成员：chenmo");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 自由输入_失焦后保留文字_值为空(string mode)
    {
        var page = await demo.OpenAsync(mode, "combobox");
        var input = page.GetByTestId("title");

        await input.FillAsync("架构师");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(input).ToHaveValueAsync("架构师");
        await Assertions.Expect(page.GetByText("自由输入：架构师，Value 为 null")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 自动完成_有建议时弹出_Enter填入_没有建议时不弹出(string mode)
    {
        var page = await demo.OpenAsync(mode, "autocomplete");
        var input = page.GetByTestId("email");
        var list = page.Locator($"#{await input.GetAttributeAsync("aria-controls")}");

        await input.FillAsync("lin@q");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(input).ToHaveValueAsync("lin@qq.com");
        await Assertions.Expect(page.GetByText("值就是文字：lin@qq.com")).ToBeVisibleAsync();

        await input.FillAsync("lin@zzz");
        await Assertions.Expect(page.GetByText("值就是文字：lin@zzz")).ToBeVisibleAsync();
        await Assertions.Expect(list).ToBeHiddenAsync();
        // 自动完成没有展开按钮
        await Assertions.Expect(input.Locator("xpath=..").GetByRole(AriaRole.Button, new() { Name = "显示选项" })).ToHaveCountAsync(0);
    }
}
