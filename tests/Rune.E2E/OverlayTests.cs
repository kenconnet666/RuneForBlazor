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
        var menu = page.GetByTestId("row-menu");
        await Assertions.Expect(menu).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "重命名" })).ToBeFocusedAsync();
        // 位置由 Floating UI 算好：在触发按钮下方，与它的结束边对齐
        await Assertions.Expect(menu).ToHaveAttributeAsync("data-side", "bottom");
        await Assertions.Expect(menu).ToHaveAttributeAsync("data-align", "end");

        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "创建副本" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        // 子菜单项：→ 打开子菜单并聚焦第一项，← 关上回到这一项
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "移动到" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "设计稿" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "移动到" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        // 归档不可用：从分享直接跳到删除
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "删除" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "重命名" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menu).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();

        // 触发按钮上按 ↑：打开并聚焦最后一项；↓：打开并聚焦第一项
        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "删除" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "重命名" })).ToBeFocusedAsync();
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
        // 示例的源码（收起着）里也有这句：只看可见的
        await Assertions.Expect(page.GetByText("没有权限").Filter(new() { Visible = true })).ToBeVisibleAsync();
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
    public async Task 命令式确认与对话框_焦点先在取消上_关上的原因报给CSharp(string mode)
    {
        var page = await demo.OpenAsync(mode, "dialog");

        await page.GetByRole(AriaRole.Button, new() { Name = "退出登录（ConfirmAsync）" }).ClickAsync();
        var alert = page.GetByRole(AriaRole.Alertdialog);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "取消" }).Last).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(alert).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByText("取消了退出", new() { Exact = true })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "查看" }).First.ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "发票 FP-20261021-031" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByText("关上 · Esc", new() { Exact = true })).ToBeVisibleAsync();
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
        var opener = page.GetByRole(AriaRole.Button, new() { Name = "阅读用户协议" });
        await opener.ClickAsync();

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "用户协议" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "关闭" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 对话框的确认_校验没过时保持打开_保存期间加载中_完成后关闭(string mode)
    {
        var page = await demo.OpenAsync(mode, "dialog");
        await page.GetByTestId("open-edit").ClickAsync();
        var dialog = page.GetByTestId("edit-dialog");
        var name = dialog.GetByRole(AriaRole.Textbox, new() { Name = "姓名" });
        await Assertions.Expect(name).ToBeFocusedAsync();

        await name.FillAsync("");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "保存" }).ClickAsync();
        await Assertions.Expect(dialog.GetByText("请填写姓名")).ToBeVisibleAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();

        await name.FillAsync("林小满");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "保存" }).ClickAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "保存" })).ToHaveAttributeAsync("aria-busy", "true");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByText("林小满").First).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 抽屉有未保存的改动时_Esc先确认_确认放弃才关(string mode)
    {
        var page = await demo.OpenAsync(mode, "drawer");
        await page.GetByRole(AriaRole.Button, new() { Name = "编辑" }).ClickAsync();
        var drawer = page.GetByTestId("edit-drawer");
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        await drawer.GetByRole(AriaRole.Textbox, new() { Name = "项目名称" }).FillAsync("显示器支架一体成型");

        // 有改动：Esc 不直接关，先问
        await page.Keyboard.PressAsync("Escape");
        var ask = page.GetByRole(AriaRole.Dialog, new() { Name = "放弃未保存的改动？" });
        await Assertions.Expect(ask).ToBeVisibleAsync();
        await ask.GetByRole(AriaRole.Button, new() { Name = "取消" }).ClickAsync();
        await Assertions.Expect(ask).ToBeHiddenAsync();
        await Assertions.Expect(drawer).ToBeVisibleAsync();

        await drawer.GetByRole(AriaRole.Button, new() { Name = "关闭" }).ClickAsync();
        await ask.GetByRole(AriaRole.Button, new() { Name = "放弃" }).ClickAsync();
        await Assertions.Expect(drawer).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("project-name")).ToHaveTextAsync("显示器支架合并");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 悬停卡片_停留后打开_可以移到卡片上_键盘Tab进入卡片_Esc关闭(string mode)
    {
        var page = await demo.OpenAsync(mode, "popover");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "@yihang" });
        var card = page.GetByTestId("card-yihang");
        var follow = card.GetByRole(AriaRole.Button, new() { Name = "关注" });

        await link.HoverAsync();
        await Assertions.Expect(card).ToBeVisibleAsync();
        await Assertions.Expect(card).ToContainTextAsync("交互设计师");
        // 移到卡片上不关，离开后关
        await follow.HoverAsync();
        await page.WaitForTimeoutAsync(500);
        await Assertions.Expect(card).ToBeVisibleAsync();
        await page.Mouse.MoveAsync(0, 0);
        await Assertions.Expect(card).ToBeHiddenAsync();

        // 键盘聚焦立即打开；Tab 进卡片里的按钮时不关，Esc 关闭
        await link.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(card).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(follow).ToBeFocusedAsync();
        await Assertions.Expect(card).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(card).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 通知_F8进入区域_Esc关闭焦点所在的一条(string mode)
    {
        var page = await demo.OpenAsync(mode, "toast");
        await page.GetByTestId("toast-success").ClickAsync();
        await page.GetByTestId("toast-info").ClickAsync();
        var open = page.Locator("[data-rune-toast][data-state=open]");
        await Assertions.Expect(open).ToHaveCountAsync(2);

        await page.Keyboard.PressAsync("F8");
        var region = page.Locator("[data-rune-toaster]").Filter(new() { Has = page.Locator("[data-rune-toast]") });
        await Assertions.Expect(region).ToBeFocusedAsync();
        // 区域里第一个可聚焦的是第一条的关闭按钮；Esc 关掉它，焦点交给剩下的那条
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(open).ToHaveCountAsync(1);
        await Assertions.Expect(open.Locator("[data-part=close]")).ToBeFocusedAsync();
    }
}
