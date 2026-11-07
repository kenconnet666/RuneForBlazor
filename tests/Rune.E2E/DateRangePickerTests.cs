namespace Rune.E2E;

/// <summary>区间选择器：开始那组填满自动进结束那组，两端都填好才写回；快捷区间写回、关闭；清空按钮清两组。</summary>
public class DateRangePickerTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 键入两端_快捷区间_清空(string mode)
    {
        var page = await OpenAsync(mode, "date-range-picker");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var stay = page.GetByRole(AriaRole.Group, new() { Name = "入住与离店" });

        // 键入开始：填满后焦点进结束那组的第一段（中文是“年”）
        var start = today.AddDays(1);
        var end = today.AddDays(3);
        await stay.GetByRole(AriaRole.Spinbutton, new() { Name = "入住与离店 开始日期 年" }).ClickAsync();
        await page.Keyboard.TypeAsync(start.ToString("yyyyMMdd"));
        await Assertions.Expect(stay.GetByRole(AriaRole.Spinbutton, new() { Name = "入住与离店 结束日期 年" })).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync(end.ToString("yyyyMMdd"));
        await Assertions.Expect(page.GetByText("2 晚", new() { Exact = true })).ToBeVisibleAsync();

        // 清空：两组都回到占位，值是 null
        await stay.GetByRole(AriaRole.Button, new() { Name = "清空" }).ClickAsync();
        await Assertions.Expect(page.GetByText("先选入住，再选离店", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(stay.GetByRole(AriaRole.Spinbutton, new() { Name = "入住与离店 结束日期 年" })).ToHaveAttributeAsync("aria-valuetext", "未填写");

        // 快捷区间：“最近 30 天”（“本月”会超过今天，不可用）
        var report = page.GetByRole(AriaRole.Group, new() { Name = "统计区间" });
        await report.GetByRole(AriaRole.Button, new() { Name = "选择日期" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog).Filter(new() { Visible = true });
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "本月" })).ToBeDisabledAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "最近 30 天" }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText($"{today.AddDays(-29):yyyy-MM-dd} – {today:yyyy-MM-dd}，共 30 天", new() { Exact = true })).ToBeVisibleAsync();
    }
}
