namespace Rune.E2E;

/// <summary>浮层的键盘与焦点：菜单、对话框、气泡确认。JS 初始化器做的事在三种模式下都要一样。</summary>
public class OverlayTests(DemoServer demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 菜单打开时聚焦第一项_方向键跳过不可用项_Esc关闭并把焦点还给触发按钮(string mode)
    {
        var page = await demo.OpenAsync(mode, "popover");
        var trigger = page.GetByTestId("row-menu").Locator("xpath=..").GetByRole(AriaRole.Button, new() { Name = "操作" });

        await trigger.ClickAsync();
        var menu = page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(menu).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "编辑" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "复制" })).ToBeFocusedAsync();
        // 归档不可用：直接跳到删除
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "删除" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "编辑" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menu).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 选了菜单项就关闭并执行(string mode)
    {
        var page = await demo.OpenAsync(mode, "popover");
        await page.GetByRole(AriaRole.Button, new() { Name = "操作" }).ClickAsync();
        // 等焦点进了菜单再按键：否则方向键与 Enter 落在触发按钮上（Enter 把菜单关掉，什么也没选）
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "编辑" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "复制" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("menu-last")).ToContainTextAsync("复制");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 气泡确认打开时聚焦取消_确认后执行(string mode)
    {
        var page = await demo.OpenAsync(mode, "popover");
        // 带文字的“删除”按钮（提示示例里同名的是只有图标的按钮）
        var trigger = page.GetByRole(AriaRole.Button).Filter(new() { HasText = "删除", Visible = true }).First;
        await trigger.ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "取消" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToBeFocusedAsync();

        await trigger.ClickAsync();
        await page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, new() { Name = "删除" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("deleted")).ToContainTextAsync("已删除 1 次");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 对话框打开时焦点在里面_Esc关闭后回到打开它的按钮(string mode)
    {
        var page = await demo.OpenAsync(mode, "dialog");
        var opener = page.GetByTestId("open-edit");
        await opener.ClickAsync();

        var dialog = page.GetByTestId("edit-dialog");
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("d => d.contains(document.activeElement)"));
        // 模态时锁定页面滚动
        Assert.Equal("hidden", await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).overflow"));

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();
        Assert.NotEqual("hidden", await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).overflow"));
    }

    [Fact]
    public async Task 静态渲染里用Trigger打开的对话框也能开关()
    {
        var page = await demo.OpenAsync("static", "dialog");
        var opener = page.GetByRole(AriaRole.Button, new() { Name = "关于（Trigger，静态 SSR 也能开）" });
        await opener.ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "关于" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "知道了" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();
    }
}
