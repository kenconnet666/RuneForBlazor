namespace Rune.E2E;

/// <summary>输入类组件的客户端行为：数字框的方向键（输入法组合中不算）与按住连续加减、文本框的显示密码与清空、复制按钮、标签移除、表单错误汇总。</summary>
public class InputTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 数字框方向键加减_Home到最小_提交时夹到范围(string mode)
    {
        var page = await OpenAsync(mode, "number-field");
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
        // 输入框的值是 JS 当场改的；等 C# 的渲染回来（到了最小值，减少按钮不可用）再填，否则迟到的渲染会盖掉填进去的文字
        await Assertions.Expect(quantity.Locator("xpath=..").GetByRole(AriaRole.Button, new() { Name = "减少" })).ToBeDisabledAsync();

        await quantity.FillAsync("150");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(quantity).ToHaveValueAsync("99");
    }

    [Fact]
    public async Task 输入法组合中的方向键不加减()
    {
        var page = await OpenAsync("server", "number-field");
        var quantity = page.GetByTestId("quantity");
        await quantity.FocusAsync();
        var before = await quantity.InputValueAsync();

        // 组合中：isComposing（Chrome、Firefox）或 keyCode 229（Safari 组合开始的那一下）
        await quantity.EvaluateAsync("""
            input => {
              input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', isComposing: true, bubbles: true, cancelable: true }));
              input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', keyCode: 229, bubbles: true, cancelable: true }));
            }
            """);
        await Assertions.Expect(quantity).ToHaveValueAsync(before);

        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(quantity).ToHaveValueAsync((int.Parse(before) + 1).ToString());
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 数字框的加号按住连续加_焦点留在输入框(string mode)
    {
        var page = await OpenAsync(mode, "number-field");
        var quantity = page.GetByTestId("quantity");
        await quantity.FocusAsync();

        var plus = quantity.Locator("xpath=..").GetByRole(AriaRole.Button, new() { Name = "增加" });
        var box = (await plus.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.WaitForTimeoutAsync(900);
        await page.Mouse.UpAsync();

        await Assertions.Expect(quantity).ToBeFocusedAsync();
        // 按住时连续加了几次：Server 模式下每次一个往返，忙的时候松开后还在追，等它追上（至少到 4）
        await Assertions.Expect(quantity).ToHaveValueAsync(new System.Text.RegularExpressions.Regex(@"^([4-9]|\d{2,})$"));
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 显示密码切换输入框的类型(string mode)
    {
        var page = await OpenAsync(mode, "text-field");
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
        var page = await OpenAsync(mode, "text-field");
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
        var page = await OpenAsync(mode, "icon-button");
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
        var page = await OpenAsync(mode, "badge");
        var filters = page.GetByTestId("filters");
        await Assertions.Expect(filters.Locator("[data-scope=badge][data-part=root]")).ToHaveCountAsync(6);

        await page.GetByRole(AriaRole.Button, new() { Name = "移除 品牌：小米" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(filters.Locator("[data-scope=badge][data-part=root]")).ToHaveCountAsync(5);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "移除 品牌：华为" })).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 表单提交未通过时列出错误_点一项跳到控件(string mode)
    {
        var page = await OpenAsync(mode, "form");
        await page.GetByTestId("submit").ClickAsync();

        var summary = page.GetByRole(AriaRole.Alert).First;
        await Assertions.Expect(summary).ToBeVisibleAsync();
        await Assertions.Expect(summary).ToBeFocusedAsync();
        await summary.GetByRole(AriaRole.Link).First.ClickAsync();
        await Assertions.Expect(page.GetByTestId("name")).ToBeFocusedAsync();
        // 跳转留在本页：Blazor 的链接拦截没有把 #id 当成别的地址
        Assert.EndsWith($"/{mode}/form", page.Url);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 没有错误汇总的表单提交未通过时聚焦第一个出错的控件(string mode)
    {
        var page = await OpenAsync(mode, "form");
        await page.GetByRole(AriaRole.Button, new() { Name = "注册" }).ClickAsync();

        await Assertions.Expect(page.GetByLabel("用户名", new() { Exact = true })).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 有未保存的改动时站内跳转先确认(string mode)
    {
        var page = await OpenAsync(mode, "form");
        await page.GetByLabel("一句话介绍").FillAsync("做组件库");
        await page.GetByLabel("一句话介绍").BlurAsync();

        // 改动报给 C# 后才放上拦截标记：等它出现再点（否则点得太快时链接照常跳走）
        await Assertions.Expect(page.Locator("[data-rune-leave-guard]")).ToBeAttachedAsync();
        // 点侧栏的链接：交互岛的页面之间走增强导航，由 JS 拦下
        await page.Locator($"a[href$='{mode}/button']").First.ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "离开这个页面？" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.EndsWith($"/{mode}/form", page.Url);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "离开" }).ClickAsync();
        // 增强导航只换 history、不触发 load：轮询地址
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/{mode}/button$"));
    }
}
