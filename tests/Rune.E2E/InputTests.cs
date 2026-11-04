namespace Rune.E2E;

/// <summary>输入类组件的客户端行为：数字框的方向键与按住连续加减、文本框的显示密码与清空、复制按钮、标签移除、表单错误汇总。</summary>
public class InputTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 数字框方向键加减_Home到最小_提交时夹到范围(string mode)
    {
        var page = await demo.OpenAsync(mode, "number-field");
        var quantity = page.GetByTestId("quantity");

        await quantity.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(quantity).ToHaveValueAsync("4");

        await quantity.FocusAsync();
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(quantity).ToHaveValueAsync("1");

        await quantity.FillAsync("150");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(quantity).ToHaveValueAsync("99");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 数字框的加号按住连续加_焦点留在输入框(string mode)
    {
        var page = await demo.OpenAsync(mode, "number-field");
        var quantity = page.GetByTestId("quantity");
        await quantity.FocusAsync();

        var plus = quantity.Locator("xpath=..").GetByRole(AriaRole.Button, new() { Name = "增加" });
        var box = (await plus.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.WaitForTimeoutAsync(900);
        await page.Mouse.UpAsync();

        await Assertions.Expect(quantity).ToBeFocusedAsync();
        await Assertions.Expect(quantity).Not.ToHaveValueAsync("2");
        Assert.True(int.Parse(await quantity.InputValueAsync()) > 3);
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 显示密码切换输入框的类型(string mode)
    {
        var page = await demo.OpenAsync(mode, "text-field");
        var password = page.GetByTestId("password");
        var reveal = page.GetByRole(AriaRole.Button, new() { Name = "显示密码" });

        await reveal.ClickAsync();
        await Assertions.Expect(password).ToHaveAttributeAsync("type", "text");
        await Assertions.Expect(reveal).ToHaveAttributeAsync("aria-pressed", "true");
        await reveal.ClickAsync();
        await Assertions.Expect(password).ToHaveAttributeAsync("type", "password");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 清空后焦点回到输入框(string mode)
    {
        var page = await demo.OpenAsync(mode, "text-field");
        var search = page.GetByTestId("search");
        await search.Locator("xpath=..").GetByRole(AriaRole.Button, new() { Name = "清空" }).ClickAsync();

        await Assertions.Expect(search).ToHaveValueAsync("");
        await Assertions.Expect(search).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 复制按钮写入剪贴板并显示对勾(string mode)
    {
        var page = await demo.OpenAsync(mode, "icon-button");
        var copy = page.GetByTestId("copy-key");
        await copy.ClickAsync();

        await Assertions.Expect(copy).ToHaveAttributeAsync("data-state", "copied");
        Assert.Equal("sk-live-3f9a7c21e8b44d0a", await page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 移除标签后焦点交给下一个标签的移除按钮(string mode)
    {
        var page = await demo.OpenAsync(mode, "badge");
        var filters = page.GetByTestId("filters");
        await Assertions.Expect(filters.Locator("[data-part=root]")).ToHaveCountAsync(6);

        await page.GetByRole(AriaRole.Button, new() { Name = "移除 品牌：小米" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(filters.Locator("[data-part=root]")).ToHaveCountAsync(5);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "移除 品牌：华为" })).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 表单提交未通过时列出错误_点一项跳到控件(string mode)
    {
        var page = await demo.OpenAsync(mode, "form");
        await page.GetByTestId("submit").ClickAsync();

        var summary = page.GetByRole(AriaRole.Alert).First;
        await Assertions.Expect(summary).ToBeVisibleAsync();
        await summary.GetByRole(AriaRole.Link).First.ClickAsync();
        await Assertions.Expect(page.GetByTestId("name")).ToBeFocusedAsync();
        // 跳转留在本页：Blazor 的链接拦截没有把 #id 当成别的地址
        Assert.EndsWith($"/{mode}/form", page.Url);
    }
}
