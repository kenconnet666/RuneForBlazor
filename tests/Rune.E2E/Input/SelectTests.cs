namespace Rune.E2E;

/// <summary>下拉选择：开关与高亮由 JS 初始化器处理（焦点留在触发按钮或搜索框上），选中、过滤、移除由 C# 处理。</summary>
public class SelectTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 方向键打开并高亮已选项_跳过不可用项_Enter选中后关闭(string mode)
    {
        var page = await OpenAsync(mode, "select");
        var trigger = page.GetByTestId("type");
        var list = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        var highlighted = list.Locator("[role=option][data-highlighted]");

        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "缺陷");

        // 咨询不可用：停在杂项
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(highlighted).ToHaveAttributeAsync("data-label", "杂项");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-activedescendant", (await highlighted.GetAttributeAsync("id"))!);

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToHaveTextAsync("杂项");
        await Assertions.Expect(page.GetByTestId("basic-summary")).ToContainTextAsync("chore");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 关闭时按字母直接选中_上箭头打开并高亮最后一项(string mode)
    {
        var page = await OpenAsync(mode, "select");
        var trigger = page.GetByTestId("type");
        var list = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");

        await trigger.FocusAsync();
        // Playwright 按不出键盘布局之外的字符：直接发一个带这个字符的 keydown
        await trigger.DispatchEventAsync("keydown", new Dictionary<string, object> { ["key"] = "需", ["bubbles"] = true });
        await Assertions.Expect(trigger).ToHaveTextAsync("需求");
        await Assertions.Expect(list).ToBeHiddenAsync();

        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "杂项");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 可搜索_焦点进搜索框_输入过滤并高亮第一项_选中后焦点回到触发按钮(string mode)
    {
        var page = await OpenAsync(mode, "select");
        var trigger = page.GetByTestId("supplier");
        var list = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        var search = page.Locator($"input[aria-controls='{await trigger.GetAttributeAsync("aria-controls")}']");

        await trigger.ClickAsync();
        await Assertions.Expect(search).ToBeFocusedAsync();
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "华东塑胶供应商甲");

        await search.FillAsync("密封");
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(6);
        await Assertions.Expect(list.Locator("[role=option][data-highlighted]")).ToHaveAttributeAsync("data-label", "华东密封件供应商戊");
        await Assertions.Expect(search).ToHaveAttributeAsync("aria-activedescendant", new System.Text.RegularExpressions.Regex("-0$"));

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("supplier-value")).ToHaveTextAsync("值：S14");
        await Assertions.Expect(trigger).ToBeFocusedAsync();

        // 再打开：搜索词已清空，全部选项都在；Esc 关闭后焦点回到触发按钮
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(search).ToBeFocusedAsync();
        await Assertions.Expect(search).ToHaveValueAsync("");
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(60);
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 多选_点选项切换且不关闭_Backspace移除最后一项(string mode)
    {
        var page = await OpenAsync(mode, "select");
        var trigger = page.GetByTestId("tags");
        var list = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        var summary = page.GetByTestId("basic-summary");

        // 有标签时触发按钮让出宽度（只剩读屏用的文字）：用户点的是外壳里标签那一行的空白处
        await ClickTagRowAsync(trigger);
        await list.Locator("[role=option][data-label='性能']").ClickAsync();
        await Assertions.Expect(summary).ToContainTextAsync("前端、无障碍、性能");
        await Assertions.Expect(list).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(summary).ToContainTextAsync("前端、无障碍");
        await Assertions.Expect(summary).Not.ToContainTextAsync("性能");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 新建_输入没有的标签后选新建_加进选项并选中(string mode)
    {
        var page = await OpenAsync(mode, "select");
        var trigger = page.GetByTestId("ticket-labels");
        var list = page.Locator($"#{await trigger.GetAttributeAsync("aria-controls")}");
        var search = page.Locator($"input[aria-controls='{await trigger.GetAttributeAsync("aria-controls")}']");

        await ClickTagRowAsync(trigger);
        await search.FillAsync("安全");
        var creation = list.Locator("[role=option]").Last;
        await Assertions.Expect(creation).ToHaveAttributeAsync("data-label", "新建“安全”");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(search).ToHaveValueAsync("");
        await Assertions.Expect(list.Locator("[role=option][data-label='安全']")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task 按宽度收起的标签_放不下的藏起来并显示加N()
    {
        var page = await OpenAsync("server", "select");
        var row = page.Locator("[data-rune-overflow]").First;
        var rest = row.Locator("[data-rune-overflow-rest]");

        await page.SetViewportSizeAsync(700, 900);
        await Assertions.Expect(rest).Not.ToHaveAttributeAsync("data-overflow-hidden", "");
        var hidden = await row.Locator("[data-rune-overflow-item][data-overflow-hidden]").CountAsync();
        Assert.InRange(hidden, 1, 7);
        await Assertions.Expect(rest).ToHaveTextAsync($"+{hidden}");

        // 放宽到放得下：全部显示，“+N”藏起来
        await page.SetViewportSizeAsync(1600, 900);
        await Assertions.Expect(rest).ToHaveAttributeAsync("data-overflow-hidden", "");
        await Assertions.Expect(row.Locator("[data-rune-overflow-item][data-overflow-hidden]")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task 静态SSR_点触发按钮打开_弹层贴着外壳同宽()
    {
        var page = await OpenAsync("static", "select");
        var trigger = page.GetByTestId("type");
        var panel = page.Locator($"#{await trigger.GetAttributeAsync("popovertarget")}");

        await trigger.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        // 同宽：JS 按外壳的宽度写 min-width（量边框盒会赶上缩放进场的中途）
        var shell = page.Locator($"#{await trigger.GetAttributeAsync("id")}-shell");
        var shellWidth = await shell.EvaluateAsync<double>("e => e.getBoundingClientRect().width");
        await Assertions.Expect(panel).ToHaveCSSAsync("min-width", new System.Text.RegularExpressions.Regex("^" + System.Text.RegularExpressions.Regex.Escape(shellWidth.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-side", "bottom");
    }

    // 点外壳里标签那一行的左上角（行的内边距，不在标签与移除按钮上）：与点触发按钮一样切换打开
    static Task ClickTagRowAsync(ILocator trigger) =>
        trigger.Locator("xpath=..").Locator("[data-scope=selected-tags]").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 远程分页_滚到末尾取下一页_换搜索词从第一页重来(string mode)
    {
        var page = await OpenAsync(mode, "select");
        var trigger = page.GetByTestId("suppliers");
        await trigger.ScrollIntoViewIfNeededAsync();
        var listId = await trigger.GetAttributeAsync("aria-controls");
        var list = page.Locator($"#{listId}");
        var search = page.Locator($"input[aria-controls='{listId}']");

        await trigger.ClickAsync();
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(20);

        // 滚到末尾：骨架接在后面，下一页接上
        await list.Locator("[role=option]").Last.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(40);
        Assert.Equal(40, await list.Locator("[role=option]").EvaluateAllAsync<int>("options => new Set(options.map(o => o.dataset.label)).size"));

        // 搜索：从第一页重来（“苏州”共 40 家，先给 20 家）
        await search.FillAsync("苏州");
        await Assertions.Expect(list.Locator("[role=option]")).ToHaveCountAsync(20);
        await Assertions.Expect(list.Locator("[role=option]").First).ToHaveAttributeAsync("data-label", "苏州恒精工有限公司");
    }
}
