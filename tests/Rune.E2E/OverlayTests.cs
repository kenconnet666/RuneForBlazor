namespace Rune.E2E;

/// <summary>浮层的键盘与焦点：菜单、提示、气泡确认、对话框。JS 初始化器做的事在三种模式下都要一样。</summary>
public class OverlayTests(DemoServer demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 菜单定位好后聚焦第一项_方向键跳过不可用项_Esc关闭并把焦点还给触发按钮(string mode)
    {
        var page = await demo.OpenAsync(mode, "menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "更多操作" });

        await trigger.ClickAsync();
        var menu = page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(menu).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "重命名" })).ToBeFocusedAsync();
        // 位置由 Floating UI 算好：在触发按钮下方，与它的结束边对齐
        await Assertions.Expect(menu).ToHaveAttributeAsync("data-side", "bottom");
        await Assertions.Expect(menu).ToHaveAttributeAsync("data-align", "end");

        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "创建副本" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        // 归档不可用：从分享直接跳到删除
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "删除" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "重命名" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menu).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 选了菜单项就关闭并执行(string mode)
    {
        var page = await demo.OpenAsync(mode, "menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "更多操作" }).ClickAsync();
        // 等焦点进了菜单再按键：否则方向键与 Enter 落在触发按钮上（Enter 把菜单关掉，什么也没选）
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "重命名" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "创建副本" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("menu-last")).ToContainTextAsync("创建副本");
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 提示悬停一会儿后显示_键盘聚焦立即显示_Esc隐藏(string mode)
    {
        var page = await demo.OpenAsync(mode, "popover");
        var bold = page.GetByRole(AriaRole.Button, new() { Name = "加粗" }).First;
        var tooltip = page.GetByRole(AriaRole.Tooltip).Filter(new() { HasText = "Ctrl" }).First;

        await bold.HoverAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "加粗 Ctrl + B" })).ToBeVisibleAsync();
        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-describedby", new System.Text.RegularExpressions.Regex("^tooltip-"));
        await page.Mouse.MoveAsync(0, 0);
        await Assertions.Expect(tooltip).ToBeHiddenAsync();

        // 键盘聚焦（:focus-visible）立即显示，Esc 隐藏
        await page.GetByRole(AriaRole.Button, new() { Name = "斜体" }).First.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        var italic = page.GetByRole(AriaRole.Tooltip, new() { Name = "斜体 Ctrl + I" });
        await Assertions.Expect(italic).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(italic).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 气泡确认打开时聚焦取消_Esc算取消_确认后执行(string mode)
    {
        var page = await demo.OpenAsync(mode, "popconfirm");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "撤回（warning）" });
        await trigger.ClickAsync();

        var popconfirm = page.GetByRole(AriaRole.Alertdialog, new() { Name = "撤回这次提交？" });
        await Assertions.Expect(popconfirm.GetByRole(AriaRole.Button, new() { Name = "取消" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByTestId("popconfirm-last")).ToContainTextAsync("取消了撤回");
        // 等离场过渡结束再点：过渡中的浮层还盖在触发按钮上，点击会落在它上面
        await Assertions.Expect(popconfirm).ToBeHiddenAsync();

        await trigger.ClickAsync();
        await Assertions.Expect(popconfirm.GetByRole(AriaRole.Button, new() { Name = "取消" })).ToBeFocusedAsync();
        await popconfirm.GetByRole(AriaRole.Button, new() { Name = "撤回" }).ClickAsync();
        await Assertions.Expect(popconfirm).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("popconfirm-last")).ToContainTextAsync("已撤回");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 气泡确认的异步确认_失败时保持打开_成功后关闭(string mode)
    {
        var page = await demo.OpenAsync(mode, "popconfirm");
        await page.GetByRole(AriaRole.Button, new() { Name = "移出 周一航" }).ClickAsync();
        var failing = page.GetByRole(AriaRole.Alertdialog, new() { Name = "把 周一航 移出项目？" });
        await Assertions.Expect(failing.GetByRole(AriaRole.Button, new() { Name = "取消" })).ToBeFocusedAsync();
        await failing.GetByRole(AriaRole.Button, new() { Name = "移出" }).ClickAsync();
        await Assertions.Expect(page.GetByText("没有权限")).ToBeVisibleAsync();
        await Assertions.Expect(failing).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(failing).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "移出 韩笑" }).ClickAsync();
        var passing = page.GetByRole(AriaRole.Alertdialog, new() { Name = "把 韩笑 移出项目？" });
        await passing.GetByRole(AriaRole.Button, new() { Name = "移出" }).ClickAsync();
        await Assertions.Expect(passing).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "移出 韩笑" })).ToHaveCountAsync(0);
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
