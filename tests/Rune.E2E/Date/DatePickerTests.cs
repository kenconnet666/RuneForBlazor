namespace Rune.E2E;

/// <summary>日期选择器：按钮打开日历、焦点在当前那一格，点选后写回、关闭；分段里 Alt + ↓ 打开、Esc 关闭；快捷选项与季度。</summary>
public class DatePickerTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 打开_点选_写回(string mode)
    {
        var page = await OpenAsync(mode, "date-picker");
        var birthday = page.GetByRole(AriaRole.Group, new() { Name = "出生日期" });
        var shell = birthday.Locator("..");

        await shell.GetByRole(AriaRole.Button, new() { Name = "选择日期" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(":focus")).ToHaveAttributeAsync("data-date", "1992-08-16");
        await dialog.Locator("button[data-date=\"1992-08-20\"]").ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByText("birthday=1992-08-20")).ToBeVisibleAsync();

        // 分段里 Alt + ↓ 打开，Esc 关闭
        await birthday.GetByRole(AriaRole.Spinbutton, new() { Name = "出生日期 年" }).ClickAsync();
        await page.Keyboard.PressAsync("Alt+ArrowDown");
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();

        // 快捷选项：“明天”写回明天
        var delivery = page.GetByRole(AriaRole.Group, new() { Name = "交货日期" });
        await delivery.Locator("..").GetByRole(AriaRole.Button, new() { Name = "选择日期" }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog).Filter(new() { Visible = true }).GetByRole(AriaRole.Button, new() { Name = "明天" }).ClickAsync();
        await Assertions.Expect(page.GetByText(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"), new() { Exact = true })).ToBeVisibleAsync();

        // 季度：季度面板翻到下一年，点第一季度
        var quarter = page.GetByRole(AriaRole.Group, new() { Name = "考核季度" });
        await quarter.Locator("..").GetByRole(AriaRole.Button, new() { Name = "选择日期" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog).Filter(new() { Visible = true });
        await Assertions.Expect(page.Locator(":focus")).ToHaveAttributeAsync("data-date", "2026-10-01");
        await panel.GetByRole(AriaRole.Button, new() { Name = "下一年" }).ClickAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "2027年第1季度" }).ClickAsync();
        await Assertions.Expect(quarter).ToHaveTextAsync("2027年第1季度");
    }
}
