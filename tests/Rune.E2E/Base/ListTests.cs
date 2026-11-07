namespace Rune.E2E;

/// <summary>列表的键盘：JS 初始化器移动高亮（aria-activedescendant），Enter / 空格点击高亮项，C# 处理选中。</summary>
public class ListTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 列表框获得焦点时高亮已选项_方向键跳过不可用项_Enter选中(string mode)
    {
        var page = await OpenAsync(mode, "listbox");
        var list = page.GetByTestId("plans");
        var highlighted = list.Locator("[role=option][data-highlighted]");

        await list.FocusAsync();
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "专业版");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-activedescendant", (await highlighted.GetAttributeAsync("id"))!);

        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "团队版");
        // 企业版不可用：停在团队版
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "团队版");

        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId("plan-picked")).ToContainTextAsync("免费版");
        await Assertions.Expect(list).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 多选列表_按字母跳转_空格切换勾选(string mode)
    {
        var page = await OpenAsync(mode, "listbox");
        var list = page.GetByTestId("columns");
        var highlighted = list.Locator("[role=option][data-highlighted]");

        await list.FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "自定义字段 994");
        await Assertions.Expect(highlighted).ToBeInViewportAsync();
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-activedescendant", new System.Text.RegularExpressions.Regex("-999$"));
        // 虚拟滚动：一千个选项只渲染了看得见的一批；按字母跳到的“收货地址”在最上面，此时还没渲染
        Assert.True(await list.Locator("[role=option]").CountAsync() < 100);

        // Playwright 按不出键盘布局之外的字符：直接发一个带这个字符的 keydown
        await list.DispatchEventAsync("keydown", new Dictionary<string, object> { ["key"] = "收", ["bubbles"] = true });
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "收货地址");
        await page.Keyboard.PressAsync(" ");
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(page.GetByText("显示 5 列")).ToBeVisibleAsync();
    }
}
