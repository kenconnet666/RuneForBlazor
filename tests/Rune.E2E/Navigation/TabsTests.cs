namespace Rune.E2E;

/// <summary>标签页：方向键切换、指示条跟随；关闭后焦点交给选中的标签；新建；放不下时的滚动按钮；Alt + ←→ 排序。</summary>
public class TabsTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 方向键移动并切换_指示条跟到当前标签(string mode)
    {
        var page = await OpenAsync(mode, "tabs");
        var list = page.GetByTestId("orders");
        var all = list.GetByRole(AriaRole.Tab).First;
        await all.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        var unpaid = list.GetByRole(AriaRole.Tab).Nth(1);
        await Assertions.Expect(unpaid).ToBeFocusedAsync();
        await Assertions.Expect(unpaid).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(page.GetByRole(AriaRole.Tabpanel).Filter(new() { HasText = "#10233" })).ToBeVisibleAsync();
        // 指示条的位置与宽度对上当前标签
        await page.WaitForFunctionAsync("""
            () => {
                const list = document.querySelector('[data-testid="orders"]');
                const tab = list.querySelector('[aria-selected="true"]');
                const bar = list.querySelector('[data-rune-indicator]');
                return Math.abs(bar.getBoundingClientRect().left - tab.getBoundingClientRect().left) < 1
                    && Math.abs(bar.offsetWidth - tab.offsetWidth) < 1;
            }
            """);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task Delete关闭当前标签_焦点到下一个_新建与排序(string mode)
    {
        var page = await OpenAsync(mode, "tabs");
        var list = page.GetByTestId("pages");
        var first = list.GetByRole(AriaRole.Tab, new() { Name = "采购单 PO-2401" });
        await first.FocusAsync();
        await page.Keyboard.PressAsync("Delete");
        var next = list.GetByRole(AriaRole.Tab, new() { Name = "采购单 PO-2402" });
        await Assertions.Expect(first).ToHaveCountAsync(0);
        await Assertions.Expect(next).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(next).ToBeFocusedAsync();

        // Alt + → 往后挪一位，焦点留在它上面
        await page.Keyboard.PressAsync("Alt+ArrowRight");
        await Assertions.Expect(list.GetByRole(AriaRole.Tab).Nth(2)).ToHaveAccessibleNameAsync("采购单 PO-2402");
        await Assertions.Expect(next).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "新建标签页" }).ClickAsync();
        await Assertions.Expect(list.GetByRole(AriaRole.Tab, new() { Name = "新建草稿 1" })).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task 放不下时出现滚动按钮_滚到头的那边不可用()
    {
        var page = await OpenAsync("server", "tabs");
        await page.SetViewportSizeAsync(560, 800);
        var bar = page.Locator("[data-rune-tabs]").Filter(new() { Has = page.GetByTestId("pages") });
        var back = bar.GetByRole(AriaRole.Button, new() { Name = "向前滚动标签" }).Or(bar.Locator("[data-rune-tab-scroll='-1']")).First;
        var forward = bar.Locator("[data-rune-tab-scroll='1']");
        await Assertions.Expect(forward).ToBeVisibleAsync();
        await Assertions.Expect(back).ToBeDisabledAsync();
        await forward.ClickAsync();
        await Assertions.Expect(back).ToBeEnabledAsync();
    }
}
