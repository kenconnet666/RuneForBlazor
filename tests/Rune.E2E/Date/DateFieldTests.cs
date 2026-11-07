namespace Rune.E2E;

/// <summary>分段输入：逐段键入写回、方向键加减、清空按钮、各语言的排法；TimeField 的上午 / 下午（季度与周的排法见 DateSegmentsTests）。</summary>
public class DateFieldTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 逐段输入与方向键(string mode)
    {
        var page = await OpenAsync(mode, "date-field");

        // 空着的证件有效期：从年开始打，满了自动跳段，填齐才写回
        var expires = page.GetByRole(AriaRole.Group, new() { Name = "证件有效期至" });
        await expires.GetByRole(AriaRole.Spinbutton, new() { Name = "证件有效期至 年" }).ClickAsync();
        await page.Keyboard.TypeAsync("2030");
        await Assertions.Expect(expires.GetByRole(AriaRole.Spinbutton, new() { Name = "证件有效期至 月" })).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("3");
        await Assertions.Expect(expires.GetByRole(AriaRole.Spinbutton, new() { Name = "证件有效期至 日" })).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByText("expires=null")).ToBeVisibleAsync();
        await page.Keyboard.TypeAsync("15");
        await Assertions.Expect(page.GetByText("expires=2030-03-15")).ToBeVisibleAsync();

        // ↑ 加一，Backspace 删空一段写回 null，其余段还在
        var month = expires.GetByRole(AriaRole.Spinbutton, new() { Name = "证件有效期至 月" });
        await month.ClickAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(page.GetByText("expires=2030-04-15")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(page.GetByText("expires=null")).ToBeVisibleAsync();
        await Assertions.Expect(month).ToHaveTextAsync("月");
        await Assertions.Expect(expires.GetByRole(AriaRole.Spinbutton, new() { Name = "证件有效期至 年" })).ToHaveTextAsync("2030");

        // 清空按钮：所有段回到占位，焦点进第一段
        var birthday = page.GetByRole(AriaRole.Group, new() { Name = "生日" });
        await birthday.Locator("..").GetByRole(AriaRole.Button, new() { Name = "清空" }).ClickAsync();
        await Assertions.Expect(page.GetByText("birthday=null")).ToBeVisibleAsync();
        await Assertions.Expect(birthday.GetByRole(AriaRole.Spinbutton, new() { Name = "生日 年" })).ToBeFocusedAsync();
        await Assertions.Expect(birthday.GetByRole(AriaRole.Spinbutton, new() { Name = "生日 年" })).ToHaveAttributeAsync("aria-valuetext", "未填写");

        // 美国：月/日/年，时间 12 小时制
        await Assertions.Expect(page.GetByRole(AriaRole.Group, new() { Name = "English (US)" })).ToHaveTextAsync("10/20/2026, 2:30 PM");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 上午下午与不可空的值(string mode)
    {
        var page = await OpenAsync(mode, "time-field");

        var english = page.GetByRole(AriaRole.Group, new() { Name = "English (US)" });
        await Assertions.Expect(english).ToHaveTextAsync("7:45 PM");
        await english.GetByRole(AriaRole.Spinbutton, new() { Name = "English (US) AM/PM" }).ClickAsync();
        await page.Keyboard.TypeAsync("a");
        // 同一个值绑在几个框上：中文的 24 小时制跟着变
        await Assertions.Expect(page.GetByRole(AriaRole.Group, new() { Name = "简体中文（24 小时制）" })).ToHaveTextAsync("07:45");
        await Assertions.Expect(page.GetByRole(AriaRole.Group, new() { Name = "HourCycle.H12" })).ToHaveTextAsync("上午7:45");

        // 不可空的 TimeOnly：删空一段（00 删一位就空了）不写回，其余照旧
        var alarm = page.GetByRole(AriaRole.Group, new() { Name = "不可空的 TimeOnly" });
        var minute = alarm.GetByRole(AriaRole.Spinbutton, new() { Name = "不可空的 TimeOnly 分钟" });
        await minute.ClickAsync();
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(minute).ToHaveAttributeAsync("aria-valuetext", "未填写");
        await Assertions.Expect(page.GetByText("闹钟：07:00")).ToBeVisibleAsync();
        await page.Keyboard.TypeAsync("30");
        await Assertions.Expect(page.GetByText("闹钟：07:30")).ToBeVisibleAsync();
    }
}
