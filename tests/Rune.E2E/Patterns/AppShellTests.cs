namespace Rune.E2E;

/// <summary>应用外壳：窄屏时侧栏在抽屉里（点链接、变宽时关上）；全屏隐藏顶栏与侧栏，Esc 或离开浏览器全屏时退出；侧栏可以收起成图标栏。</summary>
public class AppShellTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 窄屏时菜单按钮打开抽屉_点了链接关上_窗口变宽时关上(string mode)
    {
        var page = await OpenAsync(mode, "frame/app-shell/admin");
        await page.SetViewportSizeAsync(800, 700);
        var menu = page.GetByRole(AriaRole.Button, new() { Name = "打开导航" });
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "导航" });

        await menu.ClickAsync();
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        await drawer.GetByRole(AriaRole.Link, new() { Name = "商品" }).ClickAsync();
        await Assertions.Expect(drawer).ToBeHiddenAsync();
        if (mode != "static")
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("商品");

        await menu.ClickAsync();
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        await page.SetViewportSizeAsync(1280, 700);
        await Assertions.Expect(drawer).ToBeHiddenAsync();
        await Assertions.Expect(menu).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 全屏隐藏顶栏与侧栏_退出按钮_离开浏览器全屏或按Esc时退出(string mode)
    {
        var page = await OpenAsync(mode, "frame/app-shell/fullscreen");
        var enter = page.GetByRole(AriaRole.Button, new() { Name = "全屏", Exact = true });
        var exit = page.GetByRole(AriaRole.Button, new() { Name = "退出全屏" });
        var sidebar = page.GetByRole(AriaRole.Navigation, new() { Name = "导航" }).First;

        await enter.ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("只看正文");
        await Assertions.Expect(page.Locator("header")).ToBeHiddenAsync();
        await Assertions.Expect(sidebar).ToBeHiddenAsync();
        await exit.ClickAsync();
        await Assertions.Expect(page.Locator("header")).ToBeVisibleAsync();
        await Assertions.Expect(sidebar).ToBeVisibleAsync();
        // 退出浏览器全屏是异步的
        await page.WaitForFunctionAsync("() => !document.fullscreenElement");

        // 进了浏览器全屏：用户离开浏览器全屏就退出；没进去（浏览器不让）：Esc 退出
        await enter.ClickAsync();
        await Assertions.Expect(exit).ToBeVisibleAsync();
        // 请求全屏是异步的：等一会儿看进没进去（headless 时浏览器可能不让）
        var entered = await page.EvaluateAsync<bool>("async () => { for (let i = 0; i < 20 && !document.fullscreenElement; i++) await new Promise(r => setTimeout(r, 50)); return !!document.fullscreenElement; }");
        if (entered)
            await page.EvaluateAsync("() => document.exitFullscreen()");
        else
            await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(exit).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("工作台");
    }

    [Fact]
    public async Task 收起侧栏成图标栏_悬停看名称_展开后恢复()
    {
        var page = await OpenAsync("server", "frame/app-shell/mixed");
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "采购" }).First;
        var quote = nav.GetByRole(AriaRole.Link, new() { Name = "询价比价" });

        await page.GetByRole(AriaRole.Button, new() { Name = "收起侧栏" }).ClickAsync();
        await Assertions.Expect(nav).ToHaveAttributeAsync("data-compact", "");
        Assert.Equal(48, (await quote.BoundingBoxAsync())!.Width, 1);
        await quote.HoverAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "询价比价" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "展开侧栏" }).ClickAsync();
        await Assertions.Expect(nav).Not.ToHaveAttributeAsync("data-compact", "");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "收起侧栏" })).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 桌面模式_顶栏是拖动区域_窗口按钮控制窗口_失去焦点变淡_滚动条在顶栏下面_底边留一条(string mode)
    {
        var page = await OpenAsync(mode, "frame/app-shell/desktop");
        var header = page.Locator("[data-desktop] > header");
        var state = page.GetByTestId("state");

        // 顶栏的空白处当标题栏拖动；里面能点的（全屏、窗口按钮）不可拖
        Assert.Equal("drag", await header.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('app-region')"));
        Assert.Equal("no-drag", await page.GetByRole(AriaRole.Button, new() { Name = "全屏", Exact = true }).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('app-region')"));
        // 窗口按钮贴着顶栏的结束边
        var (headerBox, closeBox) = (await header.BoundingBoxAsync(), await page.GetByRole(AriaRole.Button, new() { Name = "关闭" }).BoundingBoxAsync());
        Assert.Equal(headerBox!.X + headerBox.Width, closeBox!.X + closeBox.Width, 1);

        await page.GetByRole(AriaRole.Button, new() { Name = "最大化" }).ClickAsync();
        await Assertions.Expect(state).ToContainTextAsync("最大化了");
        await page.GetByRole(AriaRole.Button, new() { Name = "还原" }).ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("还原了");

        await page.GetByRole(AriaRole.Button, new() { Name = "模拟窗口失去焦点" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-desktop]")).ToHaveAttributeAsync("data-window-active", "false");
        await Assertions.Expect(page.GetByRole(AriaRole.Group, new() { Name = "窗口" })).ToHaveAttributeAsync("data-active", "false");

        // 整页滚动条从顶栏下面开始，到底边留出的一条为止
        var track = await page.Locator("[data-rune-page-scrollbar]").EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.top, innerHeight - r.bottom]; }");
        Assert.Equal(headerBox.Height, track[0], 1);
        // 窗口底边：一条和页面同色的边（6px），滚动的正文在它上面结束
        var edge = await page.Locator("[data-desktop]").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el, '::after').height)");
        Assert.Equal(6, edge, 1);
        Assert.Equal(edge, track[1], 1);
    }
}
