namespace Rune.E2E;

/// <summary>切换按钮组（漫游焦点、滑块）、复选框组、组合输入（相连的圆角与边框）。</summary>
public class GroupTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 切换按钮组_点选_滑块到选中项_整组一个Tab位置_方向键移动(string mode)
    {
        var page = await demo.OpenAsync(mode, "toggle-group");
        var group = page.GetByTestId("view");
        var items = group.Locator("button");
        var summary = page.GetByTestId("view-summary");

        await items.Nth(3).ClickAsync();
        await Assertions.Expect(summary).ToContainTextAsync("calendar · week");
        await Assertions.Expect(items.Nth(3)).ToHaveAttributeAsync("aria-pressed", "true");
        // 滑块滑到选中项上（有过渡，等它停下）
        await page.WaitForFunctionAsync("""
            group => {
                const indicator = group.querySelector('[data-rune-indicator]').getBoundingClientRect();
                const item = group.querySelector('[aria-pressed=true]').getBoundingClientRect();
                return Math.abs(indicator.left - item.left) < 1 && Math.abs(indicator.width - item.width) < 1;
            }
            """, await group.ElementHandleAsync());

        // Tab 进入落在选中项上；方向键只移动焦点，不改选择
        await items.Nth(3).FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(items.Nth(3)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(items.Nth(0)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(items.Nth(3)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(items.Nth(2)).ToBeFocusedAsync();
        await Assertions.Expect(group.Locator("button[tabindex='0']")).ToHaveCountAsync(1);
        await Assertions.Expect(items.Nth(2)).ToHaveAttributeAsync("tabindex", "0");
        await Assertions.Expect(summary).ToContainTextAsync("calendar");

        // 空格选中焦点所在的项
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(summary).ToContainTextAsync("grid · week");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 多选的字形各自开关_必填的单选再点不弹起(string mode)
    {
        var page = await demo.OpenAsync(mode, "toggle-group");
        var marks = page.GetByTestId("marks");

        await marks.GetByRole(AriaRole.Button, new() { Name = "斜体" }).ClickAsync();
        await marks.GetByRole(AriaRole.Button, new() { Name = "加粗" }).ClickAsync();
        await Assertions.Expect(marks.Locator("[aria-pressed=true]")).ToHaveCountAsync(1);
        await Assertions.Expect(marks.GetByRole(AriaRole.Button, new() { Name = "斜体" })).ToHaveAttributeAsync("aria-pressed", "true");

        var view = page.GetByTestId("view");
        await view.Locator("[aria-pressed=true]").ClickAsync();
        await Assertions.Expect(view.Locator("[aria-pressed=true]")).ToHaveCountAsync(1);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 复选框组_勾选进列表_全选框跟着变(string mode)
    {
        var page = await demo.OpenAsync(mode, "checkbox");
        var permissions = page.GetByTestId("permissions");
        var granted = page.GetByTestId("granted");

        await permissions.GetByRole(AriaRole.Checkbox).Nth(1).CheckAsync();
        await Assertions.Expect(granted).ToContainTextAsync("orders:write");
        await permissions.GetByRole(AriaRole.Checkbox).Nth(0).UncheckAsync();
        await Assertions.Expect(granted).Not.ToContainTextAsync("orders:read");

        var notify = page.GetByTestId("notify-by");
        await Assertions.Expect(notify.GetByRole(AriaRole.Checkbox, new() { Name = "短信" })).ToBeDisabledAsync();
    }

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 组合输入_相邻一侧没有圆角_聚焦的浮到上面(string mode)
    {
        var page = await demo.OpenAsync(mode, "control-group");
        var group = page.GetByTestId("amount-group");

        // 每个子元素的外框（选择框的按钮、数字框的外壳、按钮）：[左上, 右上] 圆角
        var radii = await group.EvaluateAsync<string[][]>("""
            group => [...group.querySelectorAll(':scope > :not([popover], [hidden], input[type=hidden], template)')].map(child => {
                const style = getComputedStyle(child);
                return [style.borderTopLeftRadius, style.borderTopRightRadius, style.marginInlineStart];
            })
            """);
        Assert.Equal(3, radii.Length);
        Assert.NotEqual("0px", radii[0][0]);
        Assert.Equal("0px", radii[0][1]);
        Assert.Equal(["0px", "0px"], radii[1][..2]);
        Assert.Equal("0px", radii[2][0]);
        Assert.NotEqual("0px", radii[2][1]);
        // 边框叠在一起：后面的往前挪一个边框宽
        Assert.StartsWith("-", radii[1][2]);

        await group.GetByRole(AriaRole.Spinbutton).FocusAsync();
        var zIndex = await group.GetByRole(AriaRole.Spinbutton).EvaluateAsync<string>("input => getComputedStyle(input.closest('[data-rune-control]')).zIndex");
        Assert.NotEqual("auto", zIndex);
    }
}
